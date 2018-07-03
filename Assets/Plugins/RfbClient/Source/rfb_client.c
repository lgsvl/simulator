#include "rfb_client.h"
#include "zlib.h"

#ifdef _WIN32
#define WIN32_LEAN_AND_MEAN
#include <winsock2.h>
#include <ws2tcpip.h>
#include <windows.h>
#pragma comment (lib, "ws2_32.lib")
#else
#include <arpa/inet.h>
#include <sys/types.h>
#include <sys/socket.h>
#include <sys/select.h>
#include <pthread.h>
#include <unistd.h>
#include <netdb.h>
#include <errno.h>
#include <time.h>
#endif

#include <stdarg.h>
#include <stdlib.h>
#include <string.h>
#include <stdio.h>

// https://github.com/qemu/qemu/tree/v2.5.0/ui
// https://tools.ietf.org/html/rfc6143
// https://github.com/rfbproto/rfbproto/blob/master/rfbproto.rst

// 1. connect to server
// 2. server sends ProtocolVersion (12 bytes)
// 3. client sends ProtocolVersion (12 bytes)
// if version == 3.3
//   4. server sends selected security (4 bytes)
//   5. nop
//   6. nop
// else
//   4. server sends security types (on error - 0 & 4+N error message) (1 + N)
//   5. client sends select security type (1)
//   if version == 3.8
//     6. server sends SecurityResult (4 bytes, 0=ok, 1=error + 4+N bytes message)
//   else // version == 3.7
//     6. nop
// 7. client sends ClientInit
// 8. server sends ServerInit
// 9. done

#define RFBC_CONNECT_PAUSE_MSEC 100

#define RFBC_VERSION_SIZE 12
#define RFBC_SECURITY_SIZE 4
#define RFBC_SECURITY_RESULT_SIZE 4
#define RFBC_SERVER_INIT_PREFIX_SIZE (2+2+16+4)
#define RFBC_UPDATE_RECT_SIZE (2+2+2+2+4)

#define RFBC_SECURITY_NONE 1

#define RFBC_ENCODING_RAW  0
#define RFBC_ENCODING_COPY 1
#define RFBC_ENCODING_ZLIB 6
#define RFBC_ENCODING_ZRLE 16
#define RFBC_ENCODING_RESIZE 0xFFFFFF21
#define RFBC_ENCODING_COMPRESSION_LEVEL0 0xFFFFFF00

#define RFBC_STATE_PROTOCOL_VERSION   1
#define RFBC_STATE_SECURITY_TYPE      2
#define RFBC_STATE_SECURITY_RESULT    3
#define RFBC_STATE_SERVER_INIT        4
#define RFBC_STATE_CONNECTED          5
#define RFBC_STATE_UPDATE_RECT        6
#define RFBC_STATE_ENCODING_RAW       7
#define RFBC_STATE_ENCODING_COPY      8
#define RFBC_STATE_ENCODING_ZLIB      9
#define RFBC_STATE_ENCODING_ZRLE      10

#define RFBC_VERSION_3_3 0x0303
#define RFBC_VERSION_3_7 0x0307
#define RFBC_VERSION_3_8 0x0308

struct rfbc
{
    char* host;
    uint16_t port;
    uint32_t flags;

    int updated;
    int status;
    int state;
    uint32_t version;

    uint32_t width;
    uint32_t height;
    uint8_t* data;
    uint8_t* data_copy;

    uint8_t* buffer;
    uint32_t buffer_count;
    uint32_t buffer_need;

    uint8_t temp[8 * 1024];

    uint32_t rect_index;
    uint32_t rect_count;
    uint32_t x;
    uint32_t y;
    uint32_t w;
    uint32_t h;

    z_stream zlib;
    int zlib_initialized;

#ifdef _WIN32
    HANDLE thread;
    HANDLE stop;
    HANDLE event;
    SOCKET socket;
    CRITICAL_SECTION lock;
    LARGE_INTEGER next_connect;
#else
    int socket;
    int pipe[2];
    pthread_t thread;
    pthread_mutex_t lock;
    uint64_t next_connect;
#endif
};

static void rfbc_lock(rfbc* client)
{
#ifdef _WIN32
    EnterCriticalSection(&client->lock);
#else
    pthread_mutex_lock(&client->lock);
#endif
}

static void rfbc_unlock(rfbc* client)
{
#ifdef _WIN32
    LeaveCriticalSection(&client->lock);
#else
    pthread_mutex_unlock(&client->lock);
#endif
}

static void rfbc_log(const char* msg, ...)
{
    va_list args;
    va_start(args, msg);
    printf("RFB: ");
    vprintf(msg, args);
    printf("\n");
    fflush(stdout);
    va_end(args);
}

static void rfbc_disconnect(rfbc* client)
{
    if (client->zlib_initialized)
    {
        inflateEnd(&client->zlib);
        client->zlib_initialized = 0;
    }

#ifdef _WIN32
    if (client->state == RFBC_STATE_CONNECTED) shutdown(client->socket, SD_BOTH);
    closesocket(client->socket);
    client->socket = INVALID_SOCKET;

    LARGE_INTEGER freq;
    QueryPerformanceFrequency(&freq);
    QueryPerformanceCounter(&client->next_connect);
    client->next_connect.QuadPart += freq.QuadPart * RFBC_CONNECT_PAUSE_MSEC / 1000;
#else
    if (client->state == RFBC_STATE_CONNECTED) shutdown(client->socket, SHUT_RDWR);
    close(client->socket);
    client->socket = -1;

    struct timespec ts;
    clock_gettime(CLOCK_MONOTONIC, &ts);
    client->next_connect = ts.tv_sec * 1000000 + ts.tv_nsec / 1000 + RFBC_CONNECT_PAUSE_MSEC * 1000;
#endif

    client->status = RFBC_STATUS_CONNECTING;
    client->updated = 0;

    if (client->buffer != client->temp)
    {
        free(client->buffer);
        client->buffer = NULL;
    }
    free(client->data);
    client->data = NULL;

    rfbc_lock(client);
    free(client->data_copy);
    client->data_copy = NULL;
    rfbc_unlock(client);
}

static uint16_t get16be(const uint8_t* buffer)
{
    return (buffer[0] << 8) | buffer[1];
}

static uint32_t get32be(const uint8_t* buffer)
{
    return (buffer[0] << 24) | (buffer[1] << 16) | (buffer[2] << 8) | buffer[3];
}

static void set16be(uint8_t* buffer, uint16_t value)
{
    buffer[0] = (uint8_t)(value >> 8);
    buffer[1] = (uint8_t)value;
}

static void set32be(uint8_t* buffer, uint32_t value)
{
    buffer[0] = (uint8_t)(value >> 24);
    buffer[1] = (uint8_t)(value >> 16);
    buffer[2] = (uint8_t)(value >> 8);
    buffer[3] = (uint8_t)value;
}

int rfbc_refresh(rfbc* client, int incremental)
{
    uint8_t msg[10];
    msg[0] = 3;
    msg[1] = incremental != 0;
    set16be(msg + 2, 0); // x
    set16be(msg + 4, 0); // y
    rfbc_lock(client);
    set16be(msg + 6, (uint16_t)(client->width));
    set16be(msg + 8, (uint16_t)(client->height));
    rfbc_unlock(client);
    return send(client->socket, (char*)msg, sizeof(msg), 0) == sizeof(msg);
}

int rfbc_mouse(rfbc* client, uint16_t x, uint16_t y, uint32_t buttons)
{
    uint8_t msg[6];
    msg[0] = 5;
    msg[1] = (uint8_t)buttons;
    set16be(msg + 2, x);
    set16be(msg + 4, y);
    return send(client->socket, (char*)msg, sizeof(msg), 0) == sizeof(msg);
}

static void rfbc_set_state(rfbc* client, uint32_t state, uint32_t need)
{
    client->state = state;
    client->buffer_count = 0;
    client->buffer_need = need;
}

static void rfbc_next_rect(rfbc* client, int updated)
{
    if (++client->rect_index == client->rect_count)
    {
        if (updated)
        {
            rfbc_lock(client);
            memcpy(client->data_copy, client->data, client->width * client->height * 4);
            client->updated = 1;
            rfbc_unlock(client);
        }
        rfbc_set_state(client, RFBC_STATE_CONNECTED, 1);
    }
    else
    {
        rfbc_set_state(client, RFBC_STATE_UPDATE_RECT, RFBC_UPDATE_RECT_SIZE);
    }
}

static void rfbc_copy_bitmap(
    uint8_t* dst, uint32_t dst_x, uint32_t dst_y, uint32_t dst_stride,
    const uint8_t* src, uint32_t src_x, uint32_t src_y, uint32_t src_width, uint32_t src_height, uint32_t src_stride)
{
    dst += dst_y * dst_stride + dst_x * 4;
    src += src_y * src_stride + src_x * 4;
    for (uint32_t y = 0; y < src_height; y++)
    {
        memcpy(dst, src, src_width * 4);
        dst += dst_stride;
        src += src_stride;
    }
}

static void rfbc_update(rfbc* client)
{
    if (client->state == RFBC_STATE_PROTOCOL_VERSION)
    {
        int major, minor;
        if (sscanf((char*)client->buffer, "RFB %03d.%03d\n", &major, &minor) != 2)
        {
            rfbc_log("unsupported protocol version message: '%.*s'", RFBC_VERSION_SIZE, client->buffer);
            rfbc_disconnect(client);
            return;
        }
        if (major > 3 || (major == 3 && minor == 8))
        {
            client->version = RFBC_VERSION_3_8;
        }
        else if (major == 3 && minor == 7)
        {
            client->version = RFBC_VERSION_3_7;
        }
        else if (major == 3 && minor >= 3)
        {
            client->version = RFBC_VERSION_3_3;
        }
        else
        {
            rfbc_log("unsupported protocol version: '%.*s'", RFBC_VERSION_SIZE, client->buffer);
            rfbc_disconnect(client);
            return;
        }

        char version[RFBC_VERSION_SIZE + 1];
        snprintf(version, sizeof(version), "RFB %03d.%03d\n", client->version >> 8, client->version & 0xff);

        if (send(client->socket, version, RFBC_VERSION_SIZE, 0) != RFBC_VERSION_SIZE)
        {
            rfbc_log("error sending chosen protocol version");
            rfbc_disconnect(client);
            return;
        }

        uint32_t need = client->version == RFBC_VERSION_3_3 ? sizeof(uint32_t) : sizeof(uint8_t);
        rfbc_set_state(client, RFBC_STATE_SECURITY_TYPE, need);
    }
    else if (client->state == RFBC_STATE_SECURITY_TYPE)
    {
        if (client->version == RFBC_VERSION_3_3)
        {
            uint8_t type = client->buffer[0];
            if (type != RFBC_SECURITY_NONE)
            {
                rfbc_log("server does not support none security type 'NONE'");
                rfbc_disconnect(client);
                return;
            }
        }
        else
        {
            uint8_t count = client->buffer[0];
            if (count == 0)
            {
                // TODO: error message follows (4 + N bytes)
                rfbc_log("server does not support the desired protocol version");
                rfbc_disconnect(client);
                return;
            }
            if (client->buffer_count == 1U + count)
            {
                int found = 0;
                for (size_t i = 0; i < count; i++)
                {
                    if (client->buffer[1 + i] == RFBC_SECURITY_NONE)
                    {
                        found = 1;
                        break;
                    }
                }
                if (!found)
                {
                    rfbc_log("server does not support none security type 'NONE'");
                    rfbc_disconnect(client);
                    return;
                }

                uint8_t type = RFBC_SECURITY_NONE;
                if (send(client->socket, (char*)&type, sizeof(type), 0) != sizeof(type))
                {
                    rfbc_log("error sending selected security type");
                    rfbc_disconnect(client);
                    return;
                }

                rfbc_set_state(client, RFBC_STATE_SECURITY_RESULT, RFBC_SECURITY_RESULT_SIZE);

                if (client->version == RFBC_VERSION_3_7)
                {
                    uint32_t result = 0;
                    set32be(client->buffer, result);
                    client->buffer_count = RFBC_SECURITY_RESULT_SIZE;
                    rfbc_update(client);
                }
            }
            else
            {
                client->buffer_need += count;
            }
        }
    }
    else if (client->state == RFBC_STATE_SECURITY_RESULT)
    {
        uint32_t result = get32be(client->buffer);
        if (result != 0)
        {
            // TODO: error message follows (4 + N bytes)
            rfbc_log("server does not support none security type 'NONE'");
            rfbc_disconnect(client);
            return;
        }

        uint8_t shared = (client->flags & RFBC_FLAG_EXCLUSIVE) ? 0 : 1;
        if (send(client->socket, (char*)&shared, sizeof(shared), 0) != sizeof(shared))
        {
            rfbc_log("error sending shared flag");
            rfbc_disconnect(client);
            return;
        }

        rfbc_set_state(client, RFBC_STATE_SERVER_INIT, RFBC_SERVER_INIT_PREFIX_SIZE);
    }
    else if (client->state == RFBC_STATE_SERVER_INIT)
    {
        uint32_t name_length = get32be(client->buffer + 20);
        if (name_length > sizeof(client->temp) - RFBC_SERVER_INIT_PREFIX_SIZE)
        {
            rfbc_log("server name too long");
            rfbc_disconnect(client);
            return;
        }
        else if (client->buffer_count != RFBC_SERVER_INIT_PREFIX_SIZE + name_length)
        {
            client->buffer_need += name_length;
            return;
        }

        uint16_t width = get16be(client->buffer + 0);
        uint16_t height = get16be(client->buffer + 2);
        uint8_t bits = client->buffer[4];
        uint8_t depth = client->buffer[5];
        uint8_t big_endian = client->buffer[6];
        uint8_t true_color = client->buffer[7];
        uint16_t red_max = get16be(client->buffer + 8);
        uint16_t green_max = get16be(client->buffer + 10);
        uint16_t blue_max = get16be(client->buffer + 12);
        uint8_t red_shift = client->buffer[14];
        uint8_t green_shift = client->buffer[15];
        uint8_t blue_shift = client->buffer[16];

        // GL_BGRA
        // redshift = 16
        // greenshfit = 8
        // blueshift = 0

        // GL_RGBA
        // redshift = 0
        // greenshfit = 8
        // blueshift = 16

        if (bits != 32 || depth != 24 || big_endian != 0 || true_color != 1 ||
            red_max != 255 || green_max != 255 || blue_max != 255 ||
            red_shift != 16 || green_shift != 8 || blue_shift != 0)
        {
            rfbc_log("usnupported pixel format from server");
            rfbc_disconnect(client);
            return;
        }

        // SetEncodings
        {
            uint32_t encodings[] =
            {
                RFBC_ENCODING_COPY,
                RFBC_ENCODING_ZRLE,
                RFBC_ENCODING_ZLIB, // ZLIB encoding is broken in QEMU vnc server
                RFBC_ENCODING_RAW,
                RFBC_ENCODING_COMPRESSION_LEVEL0 + 1,
                RFBC_ENCODING_RESIZE,
            };
            int count = sizeof(encodings) / sizeof(*encodings);
            uint8_t temp[64];
            temp[0] = 2;
            temp[1] = 0;
            set16be(temp + 2, (uint16_t)count);
            for (int i = 0; i < count; i++)
            {
                set32be(temp + 2 + 2 + sizeof(uint32_t) * i, encodings[i]);
            }

            int size = 2 + 2 + sizeof(uint32_t) * count;
            if (send(client->socket, (char*)temp, size, 0) != size)
            {
                rfbc_log("error sending requested encodings");
                rfbc_disconnect(client);
                return;
            }
        }

        client->width = width;
        client->height = height;

        if (!rfbc_refresh(client, 0))
        {
            rfbc_log("error sending update request");
            rfbc_disconnect(client);
            return;
        }

        client->data = calloc(width * height, 4);
        if (!client->data)
        {
            rfbc_log("out of memory");
            rfbc_disconnect(client);
            return;
        }

        client->data_copy = calloc(width * height, 4);
        if (!client->data_copy)
        {
            rfbc_log("out of memory");
            rfbc_disconnect(client);
            return;
        }

        rfbc_set_state(client, RFBC_STATE_CONNECTED, 1);
        client->status = RFBC_STATUS_CONNECTED;

        inflateInit(&client->zlib);
        client->zlib_initialized = 1;
    }
    else if (client->state == RFBC_STATE_CONNECTED)
    {
        if (client->buffer[0] == 0)
        {
            if (client->buffer_count != sizeof(uint32_t))
            {
                client->buffer_need = sizeof(uint32_t);
                return;
            }

            client->rect_index = 0;
            client->rect_count = get16be(client->buffer + 2);
            if (client->rect_count == 0)
            {
                client->buffer_count = 0;
                client->buffer_need = 1;
                return;
            }

            //rfbc_log("rectangles received %d", client->rect_count);
            rfbc_set_state(client, RFBC_STATE_UPDATE_RECT, RFBC_UPDATE_RECT_SIZE);
        }
        else
        {
            rfbc_log("unknown message received from server");
            rfbc_disconnect(client);
            return;
        }
    }
    else if (client->state == RFBC_STATE_UPDATE_RECT)
    {
        client->x = get16be(client->buffer + 0);
        client->y = get16be(client->buffer + 2);
        client->w = get16be(client->buffer + 4);
        client->h = get16be(client->buffer + 6);
        uint32_t encoding = get32be(client->buffer + 8);

        //if (client->buffer_count == RFBC_UPDATE_RECT_SIZE && encoding != RFBC_ENCODING_ZLIB)
        //{
        //    rfbc_log("(%d,%d) %dx%d, enc=%u", client->x, client->y, client->w, client->h, encoding);
        //}

        if (encoding == RFBC_ENCODING_RAW)
        {
            uint32_t size = client->w * client->h * 4;
            if (size > sizeof(client->temp))
            {
                client->buffer = malloc(size);
            }
            rfbc_set_state(client, RFBC_STATE_ENCODING_RAW, size);
        }
        else if (encoding == RFBC_ENCODING_COPY)
        {
            rfbc_set_state(client, RFBC_STATE_ENCODING_COPY, 2 * sizeof(uint16_t));
        }
        else if (encoding == RFBC_ENCODING_ZLIB)
        {
            if (client->buffer_count != RFBC_UPDATE_RECT_SIZE + sizeof(uint32_t))
            {
                client->buffer_need = RFBC_UPDATE_RECT_SIZE + sizeof(uint32_t);
                return;
            }

            uint32_t size = get32be(client->buffer + RFBC_UPDATE_RECT_SIZE);
            if (size > sizeof(client->temp))
            {
                client->buffer = malloc(size);
            }
            rfbc_set_state(client, RFBC_STATE_ENCODING_ZLIB, size);
        }
        else if (encoding == RFBC_ENCODING_ZRLE)
        {
            if (client->buffer_count != RFBC_UPDATE_RECT_SIZE + sizeof(uint32_t))
            {
                client->buffer_need = RFBC_UPDATE_RECT_SIZE + sizeof(uint32_t);
                return;
            }

            uint32_t size = get32be(client->buffer + RFBC_UPDATE_RECT_SIZE);
            if (size > sizeof(client->temp))
            {
                client->buffer = malloc(size);
            }
            rfbc_set_state(client, RFBC_STATE_ENCODING_ZRLE, size);
        }
        else if (encoding == RFBC_ENCODING_RESIZE)
        {
            free(client->data);
            client->data = malloc(client->w * client->h * 4);

            rfbc_lock(client);
            free(client->data_copy);
            client->data_copy = calloc(client->w * client->h, 4);
            client->width = client->w;
            client->height = client->h;
            rfbc_unlock(client);

            rfbc_refresh(client, 0);

            rfbc_next_rect(client, 0);
        }
        else
        {
            rfbc_log("unsupported encoding");
            rfbc_disconnect(client);
            return;
        }
    }
    else if (client->state == RFBC_STATE_ENCODING_RAW)
    {
        rfbc_copy_bitmap(
            client->data, client->x, client->y, client->width * 4,
            client->buffer, 0, 0, client->w, client->h, client->w * 4);

        if (client->buffer != client->temp)
        {
            free(client->buffer);
            client->buffer = client->temp;
        }

        rfbc_next_rect(client, 1);
    }
    else if (client->state == RFBC_STATE_ENCODING_COPY)
    {
        uint32_t x = get16be(client->buffer + 0);
        uint32_t y = get16be(client->buffer + 2);

        rfbc_copy_bitmap(
            client->data, client->x, client->y, client->width * 4,
            client->data, x, y, client->w, client->h, client->width * 4);

        rfbc_next_rect(client, 1);
    }
    else if (client->state == RFBC_STATE_ENCODING_ZLIB)
    {
        uint32_t raw_size = client->w * client->h * 4;
        uint8_t* raw = malloc(raw_size);
        if (raw)
        {
            z_stream* z = &client->zlib;

            z->next_in = client->buffer;
            z->avail_in = client->buffer_count;
            z->next_out = raw;
            z->avail_out = raw_size;

            int r = inflate(z, Z_SYNC_FLUSH);
            if (r != Z_OK || z->avail_in != 0 || z->avail_out != 0)
            {
                rfbc_log("zlib decompression error");
                rfbc_disconnect(client);
                return;
            }

            rfbc_copy_bitmap(
                client->data, client->x, client->y, client->width * 4,
                raw, 0, 0, client->w, client->h, client->w * 4);

            free(raw);
        }
        if (client->buffer != client->temp)
        {
            free(client->buffer);
            client->buffer = client->temp;
        }

        rfbc_next_rect(client, 1);
    }
    else if (client->state == RFBC_STATE_ENCODING_ZRLE)
    {
        uint32_t raw_size = client->width * client->height * 4;
        uint8_t* raw = malloc(raw_size);
        if (raw)
        {
            z_stream* z = &client->zlib;

            z->next_in = client->buffer;
            z->avail_in = client->buffer_count;
            z->next_out = raw;
            z->avail_out = raw_size;

            int r = inflate(z, Z_SYNC_FLUSH);
            if (r != Z_OK || z->avail_in != 0)
            {
                rfbc_log("zlib decompression error");
                rfbc_disconnect(client);
                return;
            }

            const uint8_t* ptr = raw;

            const uint32_t tile = 64;
            uint8_t temp[64 * 64 * 4];

            uint32_t w = client->w;
            uint32_t h = client->h;
            for (uint32_t y = 0; y < h; y += tile)
            {
                uint32_t th = y + tile > h ? h - y : tile;
                for (uint32_t x = 0; x < w; x += tile)
                {
                    uint32_t tw = x + tile > w ? w - x : tile;

                    uint8_t type = *ptr++;
                    if (type == 0)
                    {
                        uint8_t* dst = temp;
                        for (uint32_t i = 0; i < tw * th; i++)
                        {
                            *dst++ = *ptr++;
                            *dst++ = *ptr++;
                            *dst++ = *ptr++;
                            *dst++ = 0;
                        }
                    }
                    else if (type == 1)
                    {
                        uint8_t* dst = temp;
                        for (uint32_t i = 0; i < tw * th; i++)
                        {
                            dst[0] = ptr[0];
                            dst[1] = ptr[1];
                            dst[2] = ptr[2];
                            dst[3] = 0;
                            dst += 4;
                        }
                        ptr += 3;
                    }
                    else if (type >= 2 && type <= 16)
                    {
                        uint32_t palcount = type;
                        const uint8_t* palette = ptr;
                        ptr += palcount * 3;

                        uint32_t bits = palcount > 4 ? 4 : palcount > 2 ? 2 : 1;
                        uint32_t mask = (1 << bits) - 1;

                        uint8_t* dst = temp;
                        for (uint32_t py = 0; py < th; py++)
                        {
                            uint8_t byte = 0;
                            uint32_t n = 0;

                            for (uint32_t px = 0; px < tw; px++)
                            {
                                if (n == 0)
                                {
                                    byte = *ptr++;
                                    n = 8;
                                }
                                n -= bits;
                                uint32_t idx = (byte >> n) & mask;
                                *dst++ = palette[3 * idx + 0];
                                *dst++ = palette[3 * idx + 1];
                                *dst++ = palette[3 * idx + 2];
                                *dst++ = 0;
                            }
                        }
                    }
                    else if (type == 128)
                    {
                        const uint8_t* color = NULL;
                        uint32_t count = 0;

                        uint8_t* dst = temp;
                        for (uint32_t i = 0; i < tw * th; i++)
                        {
                            if (count == 0)
                            {
                                color = ptr;
                                ptr += 3;
                                uint8_t value;
                                while ((value = *ptr++) == 255)
                                {
                                    count += value;
                                }
                                count += value + 1;
                            }
                            *dst++ = color[0];
                            *dst++ = color[1];
                            *dst++ = color[2];
                            *dst++ = 0;
                            count--;
                        }
                    }
                    else if (type >= 130) // && type <= 255
                    {
                        uint32_t palcount = type - 128;
                        const uint8_t* palette = ptr;
                        ptr += 3 * palcount;

                        const uint8_t* color = NULL;
                        uint32_t count = 0;

                        uint8_t* dst = temp;
                        for (uint32_t i = 0; i < tw * th; i++)
                        {
                            if (count == 0)
                            {
                                uint8_t index = *ptr++;
                                color = palette + 3 * (index & 0x7f);
                                count = 1;
                                if (index & 0x80)
                                {
                                    uint8_t value;
                                    while ((value = *ptr++) == 255)
                                    {
                                        count += value;
                                    }
                                    count += value;
                                }
                            }
                            *dst++ = color[0];
                            *dst++ = color[1];
                            *dst++ = color[2];
                            *dst++ = 0;
                            count--;
                        }
                    }

                    rfbc_copy_bitmap(
                        client->data, client->x + x, client->y + y, client->width * 4,
                        temp, 0, 0, tw, th, tw * 4);
                }
            }
            free(raw);
        }
        if (client->buffer != client->temp)
        {
            free(client->buffer);
            client->buffer = client->temp;
        }

        rfbc_next_rect(client, 1);
    }
    else
    {
        rfbc_log("unknown state");
    }
}

#ifdef _WIN32

static INIT_ONCE rfbc_init_once_win32 = INIT_ONCE_STATIC_INIT;

static BOOL rfbc_init_win32(PINIT_ONCE init, PVOID arg, PVOID* ctx)
{
    (void)init;
    (void)arg;
    (void)ctx;
    WSADATA wsa;
    if (WSAStartup(MAKEWORD(2, 2), &wsa) != 0)
    {
        rfbc_log("failed to initialized Windows Socket library");
    }
    return TRUE;
}

static DWORD CALLBACK rfbc_thread_win32(LPVOID arg)
{
    rfbc* client = arg;

    struct addrinfo hints =
    {
        .ai_family = AF_UNSPEC,
        .ai_socktype = SOCK_STREAM,
        .ai_protocol = IPPROTO_TCP,
    };

    // TODO: make this better, so main thread can cancel getaddrinfo
    struct addrinfo* info;
    if (getaddrinfo(client->host, NULL, &hints, &info) != 0)
    {
        rfbc_log("error resolving hostname '%s'", client->host);
        client->status = RFBC_STATUS_ERROR;
        return 0;
    }
    if (info->ai_addr->sa_family == AF_INET)
    {
        ((struct sockaddr_in*)info->ai_addr)->sin_port = ntohs(client->port);
    }
    else if (info->ai_addr->sa_family == AF_INET6)
    {
        ((struct sockaddr_in6*)info->ai_addr)->sin6_port = ntohs(client->port);
    }

    int addrlen = (int)info->ai_addrlen;
    struct sockaddr_storage addr;
    memcpy(&addr, info->ai_addr, info->ai_addrlen);
    freeaddrinfo(info);

    for (;;)
    {
        if (client->socket == INVALID_SOCKET)
        {
            LARGE_INTEGER time_now;
            QueryPerformanceCounter(&time_now);
            if (time_now.QuadPart > client->next_connect.QuadPart)
            {
                client->socket = socket(addr.ss_family, hints.ai_socktype, hints.ai_protocol);
                if (client->socket == INVALID_SOCKET)
                {
                    rfbc_log("error creating socket");
                    client->status = RFBC_STATUS_ERROR;
                    break;
                }
                WSAEventSelect(client->socket, client->event, FD_CONNECT);

                if (connect(client->socket, (struct sockaddr*)&addr, addrlen) == 0)
                {
                    client->buffer = client->temp;
                    rfbc_set_state(client, RFBC_STATE_PROTOCOL_VERSION, RFBC_VERSION_SIZE);

                    WSAEventSelect(client->socket, client->event, FD_READ | FD_CLOSE);
                }
                else
                {
                    if (WSAGetLastError() != WSAEWOULDBLOCK)
                    {
                        rfbc_log("error conecting to '%s'", client->host);
                        rfbc_disconnect(client);
                        continue;
                    }
                }
            }
            else
            {
                LARGE_INTEGER timer_freq;
                QueryPerformanceFrequency(&timer_freq);

                uint64_t msec = (client->next_connect.QuadPart - time_now.QuadPart) * 1000 / timer_freq.QuadPart;
                DWORD wait = WaitForSingleObject(client->stop, (DWORD)msec);
                if (wait == WAIT_OBJECT_0)
                {
                    break;
                }
                else
                {
                    continue;
                }
            }
        }

        HANDLE ev[] = { client->stop, client->event };
        DWORD wait = WaitForMultipleObjects(sizeof(ev) / sizeof(*ev), ev, FALSE, INFINITE);
        if (wait == WAIT_OBJECT_0)
        {
            break;
        }
        else if (wait != WAIT_OBJECT_0 + 1)
        {
            rfbc_log("wait error");
            continue;
        }

        WSANETWORKEVENTS e;
        WSAEnumNetworkEvents(client->socket, client->event, &e);
        if (e.lNetworkEvents & FD_CONNECT)
        {
            if (e.iErrorCode[FD_CONNECT_BIT] != 0)
            {
                rfbc_log("error conecting to '%s'", client->host);
                rfbc_disconnect(client);
                continue;
            }

            client->buffer = client->temp;
            rfbc_set_state(client, RFBC_STATE_PROTOCOL_VERSION, RFBC_VERSION_SIZE);

            WSAEventSelect(client->socket, client->event, FD_READ | FD_CLOSE);
            continue;
        }
        else if (e.lNetworkEvents & FD_CLOSE)
        {
            rfbc_log("server disconnected");
            rfbc_disconnect(client);
            continue;
        }
        else if (e.lNetworkEvents & FD_READ)
        {
            int read = recv(client->socket, (char*)client->buffer + client->buffer_count, client->buffer_need - client->buffer_count, 0);
            if (read <= 0)
            {
                rfbc_log("error reading data from socket");
                rfbc_disconnect(client);
                continue;
            }

            client->buffer_count += read;
            if (client->buffer_count == client->buffer_need)
            {
                rfbc_update(client);
            }
        }
    }

    return 0;
}

static void rfbc_cleanup(rfbc* client)
{
    if (client->thread != NULL) CloseHandle(client->thread);
    if (client->event != NULL) WSACloseEvent(client->event);
    if (client->stop != NULL) CloseHandle(client->stop);
    rfbc_disconnect(client);
    DeleteCriticalSection(&client->lock);
    free(client->host);
}

#else

static void* rfbc_thread_posix(void* arg)
{
    rfbc* client = arg;

    struct addrinfo hints =
    {
        .ai_family = AF_UNSPEC,
        .ai_socktype = SOCK_STREAM,
        .ai_protocol = IPPROTO_TCP,
    };

    // TODO: make this better, so main thread can cancel getaddrinfo
    struct addrinfo* info;
    if (getaddrinfo(client->host, NULL, &hints, &info) != 0)
    {
        rfbc_log("error resolving hostname '%s'", client->host);
        client->status = RFBC_STATUS_ERROR;
        return 0;
    }
    if (info->ai_addr->sa_family == AF_INET)
    {
        ((struct sockaddr_in*)info->ai_addr)->sin_port = ntohs(client->port);
    }
    else if (info->ai_addr->sa_family == AF_INET6)
    {
        ((struct sockaddr_in6*)info->ai_addr)->sin6_port = ntohs(client->port);
    }

    int addrlen = (int)info->ai_addrlen;
    struct sockaddr_storage addr;
    memcpy(&addr, info->ai_addr, info->ai_addrlen);
    freeaddrinfo(info);

    for (;;)
    {
        if (client->socket <= 0)
        {
            struct timespec ts;
            clock_gettime(CLOCK_MONOTONIC, &ts);
            uint64_t time_now = ts.tv_sec * 1000000 + ts.tv_nsec / 1000;
            if (time_now > client->next_connect)
            {
                client->socket = socket(addr.ss_family, hints.ai_socktype, hints.ai_protocol);
                if (client->socket < 0)
                {
                    rfbc_log("error creating socket");
                    client->status = RFBC_STATUS_ERROR;
                    break;
                }

                // TODO: nonblocking connect

                if (connect(client->socket, (struct sockaddr*)&addr, addrlen) == 0)
                {
                    client->buffer = client->temp;
                    rfbc_set_state(client, RFBC_STATE_PROTOCOL_VERSION, RFBC_VERSION_SIZE);
                }
                else
                {
                    rfbc_log("error conecting to '%s'", client->host);
                    rfbc_disconnect(client);
                    continue;
                }
            }
            else
            {
                uint64_t usec = client->next_connect - time_now;

                fd_set set;
                FD_ZERO(&set);
                FD_SET(client->pipe[0], &set);

                struct timeval timeout = { usec / 1000000, usec % 1000000 };
                int ret = select(client->pipe[0] + 1, &set, NULL, NULL, &timeout);
                if (ret > 0)
                {
                    break;
                }
                else
                {
                    continue;
                }
            }
        }

        fd_set set;
        FD_ZERO(&set);
        FD_SET(client->pipe[0], &set);
        FD_SET(client->socket, &set);
        int max = client->pipe[0] > client->socket ? client->pipe[0] : client->socket;

        int ret = select(max + 1, &set, NULL, NULL, NULL);
        if (ret <= 0)
        {
            rfbc_log("wait error");
            continue;
        }

        if (FD_ISSET(client->pipe[0], &set))
        {
            break;
        }

        int read = recv(client->socket, client->buffer + client->buffer_count, client->buffer_need - client->buffer_count, 0);
        if (read == 0)
        {
            rfbc_log("server disconnected");
            rfbc_disconnect(client);
            continue;
        }
        else if (read < 0)
        {
            rfbc_log("error reading data from socket");
            rfbc_disconnect(client);
            continue;
        }

        client->buffer_count += read;
        if (client->buffer_count == client->buffer_need)
        {
            rfbc_update(client);
        }
    }

    return NULL;
}

static void rfbc_cleanup(rfbc* client)
{
    if (client->pipe[0] != 0) close(client->pipe[0]);
    if (client->pipe[1] != 0) close(client->pipe[1]);
    rfbc_disconnect(client);
    pthread_mutex_destroy(&client->lock);
    free(client->host);
}

#endif

rfbc* rfbc_connect(const char* host, uint16_t port, uint32_t flags)
{
    rfbc* client = calloc(1, sizeof(*client));
    if (!client)
    {
        return NULL;
    }

    client->host = strdup(host);
    client->port = port;
    client->flags = flags;
    client->status = RFBC_STATUS_CONNECTING;

    rfbc* result = NULL;

#ifdef _WIN32
    client->socket = INVALID_SOCKET;
    InitOnceExecuteOnce(&rfbc_init_once_win32, &rfbc_init_win32, NULL, NULL);

    InitializeCriticalSection(&client->lock);

    client->stop = CreateEventW(NULL, TRUE, FALSE, NULL);
    if (client->stop == NULL) goto bail;

    client->event = WSACreateEvent();
    if (client->event == NULL) goto bail;

    client->thread = CreateThread(NULL, 0, &rfbc_thread_win32, client, 0, NULL);
    if (client->thread == NULL) goto bail;
#else
    if (pthread_mutex_init(&client->lock, NULL) != 0) goto bail;
    if (pipe(client->pipe) != 0) goto bail;
    if (pthread_create(&client->thread, NULL, rfbc_thread_posix, client) != 0) goto bail;
#endif

    result = client;
    client = NULL;

bail:
    if (client != NULL)
    {
        rfbc_cleanup(client);
        free(client);
    }

    return result;
}

void rfbc_close(rfbc* client)
{
#ifdef _WIN32
    SetEvent(client->stop);
    WaitForSingleObject(client->thread, INFINITE);
#else
    uint8_t stop = 1;
    if (write(client->pipe[1], &stop, sizeof(stop)) == sizeof(stop))
    {
        pthread_join(client->thread, NULL);
    }
#endif
    rfbc_cleanup(client);
    free(client);
}

int rfbc_get_status(rfbc* client)
{
    return client->status;
}

void rfbc_get_size(rfbc* client, uint32_t* width, uint32_t* height)
{
    rfbc_lock(client);
    *width = client->width;
    *height = client->height;
    rfbc_unlock(client);
}

int rfbc_get_data(rfbc* client, void* data, uint32_t stride, uint32_t width, uint32_t height)
{
    if (!client->updated || data == NULL)
    {
        return 0;
    }

    rfbc_lock(client);
    if (client->data_copy && width == client->width && height == client->height)
    {
        uint32_t src_stride = width * 4;
        const uint8_t* src = client->data_copy + height * src_stride;
        uint8_t* dst = data;
        for (uint32_t y = 0; y < height; y++)
        {
            src -= src_stride;
            memcpy(dst, src, src_stride);
            dst += stride;
        }
    }
    rfbc_unlock(client);

    return 1;
}
