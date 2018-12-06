#define GL_GLEXT_PROTOTYPES
#include <GL/glcorearb.h>
#include <stdint.h>
#include <stdlib.h>
#include <string.h>
#include <stdio.h>

#define STATUS_IDLE 0
#define STATUS_READING 1
#define STATUS_FINISHED 2

typedef struct
{
    GLuint texture;
    GLuint size;

    GLuint pbo;
    GLenum format;
    GLenum type;

    GLsync sync;
    void* buffer;

    volatile int status;
    volatile int start;
    volatile int destroy;
    int used;
} AsyncTextureReader;

#define READER_MAX_COUNT 1024
static AsyncTextureReader readers[READER_MAX_COUNT];

typedef void OnDebugOutput(const char* msg);

static void DebugOutput(const char* msg)
{
    (void)msg;
}

static OnDebugOutput* Debug = DebugOutput;

__attribute__((visibility("default")))
void AsyncTextureReaderSetDebug(OnDebugOutput* output)
{
    Debug = output;
}

__attribute__((visibility("default")))
int AsyncTextureReaderCreate(void* texture, int size)
{
    int id = -1;
    for (int i=0; i<READER_MAX_COUNT; i++)
    {
        if (!readers[i].used)
        {
            id = i;
            break;
        }
    }
    if (id < 0)
    {
        return -1;
    }

    AsyncTextureReader* reader = readers + id;
    memset(reader, 0, sizeof(*reader));

    reader->texture = (GLuint)(uintptr_t)texture;
    reader->size = size;
    reader->used = 1;

    return id;
}

__attribute__((visibility("default")))
void AsyncTextureReaderDestroy(int id)
{
    AsyncTextureReader* reader = readers + id;
    reader->destroy = 1;
}

__attribute__((visibility("default")))
void AsyncTextureReaderStart(int id, void* buffer)
{
    AsyncTextureReader* reader = readers + id;
    reader->start = 1;
    reader->buffer = buffer;
    reader->status = STATUS_READING;
}

__attribute__((visibility("default")))
int AsyncTextureReaderGetStatus(int id)
{
    AsyncTextureReader* reader = readers + id;
    //char m[1024];
    //sprintf(m, "id=%d status=%d\n", id, (int)reader->status);
    //Debug(m);
    return reader->status;
}

static void AsyncTextureReaderUpdate(int id)
{
    AsyncTextureReader* reader = readers + id;
    if (reader->used == 0)
    {
        return;
    }

    if (reader->destroy)
    {
        if (reader->sync)
        {
            glDeleteSync(reader->sync);
        }
        if (reader->pbo != 0)
        {
            glDeleteBuffers(1, &reader->pbo);
        }
        reader->used = 0;
        return;
    }

    if (reader->pbo == 0)
    {
        //Debug("creating");
        GLint minor, major;
        glGetIntegerv(GL_MAJOR_VERSION, &major);
        glGetIntegerv(GL_MINOR_VERSION, &minor);

        glGenBuffers(1, &reader->pbo);
        glBindBuffer(GL_PIXEL_PACK_BUFFER, reader->pbo);
        if (major == 4 && minor >= 4 || major > 4)
        {
            // GL_ARB_buffer_storage
            glBufferStorage(GL_PIXEL_PACK_BUFFER, reader->size, NULL, GL_CLIENT_STORAGE_BIT);
        }
        else
        {
            glBufferData(GL_PIXEL_PACK_BUFFER, reader->size, NULL, GL_STREAM_READ);
        }
        glBindBuffer(GL_PIXEL_PACK_BUFFER, 0);

        GLint format;
        glBindTexture(GL_TEXTURE_2D, reader->texture);
        glGetTexLevelParameteriv(GL_TEXTURE_2D, 0, GL_TEXTURE_INTERNAL_FORMAT, &format);
        glBindTexture(GL_TEXTURE_2D, 0);

        switch (format)
        {
        case GL_SRGB8_ALPHA8:
            // Debug("GL_SRGB8_ALPHA8");
            reader->format = GL_SRGB_ALPHA;
            reader->type = GL_UNSIGNED_BYTE;
            break;

        case GL_RGB:
        case GL_RGB8:
            // Debug("GL_RGB8");
            reader->format = GL_RGB;
            reader->type = GL_UNSIGNED_BYTE;
            break;

        case GL_RGBA:
        case GL_RGBA8:
            // Debug("GL_RGBA8");
            reader->format = GL_RGBA;
            reader->type = GL_UNSIGNED_BYTE;
            break;

        case GL_RGBA32F:
            // Debug("GL_RGBA32F");
            reader->format = GL_RGBA;
            reader->type = GL_FLOAT;
            break;

        case GL_R32F:
            // Debug("GL_R32F");
            reader->format = GL_RED;
            reader->type = GL_FLOAT;
            break;

        case GL_RG32F:
            // Debug("GL_RG32F");
            reader->format = GL_RG;
            reader->type = GL_FLOAT;
            break;

        default:
        {
            char buf[256];
            sprintf(buf, "UNKNOWN TEXTURE FORMAT: %08x", format);
            Debug(buf);
            reader->format = GL_RGBA;
            reader->type = GL_UNSIGNED_BYTE;
            break;
        }

        }

        // Debug("ok");
        return;
    }

    // char m[1024];
    // sprintf(m, "status=%d start=%d\n", (int)reader->status, reader->start);
    // Debug(m);

    if (reader->start)
    {
        // Debug("start begin");

        reader->start = 0;
        glBindTexture(GL_TEXTURE_2D, reader->texture);
        glBindBuffer(GL_PIXEL_PACK_BUFFER, reader->pbo);
        glGetTexImage(GL_TEXTURE_2D, 0, reader->format, reader->type, NULL);
        reader->sync = glFenceSync(GL_SYNC_GPU_COMMANDS_COMPLETE, 0);
        glBindBuffer(GL_PIXEL_PACK_BUFFER, 0);
        glBindTexture(GL_TEXTURE_2D, 0);

        GLenum err;
        while ((err = glGetError()) != GL_NO_ERROR)
        {
            // char buf[256];
            // sprintf(buf, "OpenGL error when starting: %08x", err);
            // Debug(buf);
        }

        // Debug("start end");

        return;
    }

    if (reader->status == STATUS_READING)
    {
        // Debug("START_READING begin");

        GLenum e = glClientWaitSync(reader->sync, 0, 0);
        if (e == GL_ALREADY_SIGNALED || e == GL_CONDITION_SATISFIED)
        {
            // Debug("REQUEST FINISHED");

            reader->status = STATUS_FINISHED;
            glDeleteSync(reader->sync);
            reader->sync = NULL;

            glBindBuffer(GL_PIXEL_PACK_BUFFER, reader->pbo);
            glGetBufferSubData(GL_PIXEL_PACK_BUFFER, 0, reader->size, reader->buffer);
            glBindBuffer(GL_PIXEL_PACK_BUFFER, 0);
        }
        else if (e == GL_WAIT_FAILED)
        {
            reader->status = STATUS_FINISHED; // TODO: STATUS_ERROR ?
            glDeleteSync(reader->sync);
            reader->sync = NULL;
            memset(reader->buffer, 0, reader->size);
        }
        else
        {
            // Debug("REQUEST IN PROGRESS");
        }

        GLenum err;
        while ((err = glGetError()) != GL_NO_ERROR)
        {
            // char buf[256];
            // sprintf(buf, "OpenGL error when reading: %08x", err);
            // Debug(buf);
        }

        // Debug("START_READING end");
        return;
    }
}

__attribute__((visibility("default")))
void* AsyncTextureReaderGetUpdate(void)
{
    return AsyncTextureReaderUpdate;
}
