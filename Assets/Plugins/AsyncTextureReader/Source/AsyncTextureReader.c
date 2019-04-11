#define GL_GLEXT_PROTOTYPES
#include <GL/glcorearb.h>
#include <stdint.h>
#include <stdlib.h>
#include <string.h>
#include <stdio.h>
#include <errno.h>
#include <semaphore.h>

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
    volatile int wait;
    sem_t sem;
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
    reader->status = STATUS_IDLE;
    sem_init(&reader->sem, 0, 0);

    return id;
}

__attribute__((visibility("default")))
void AsyncTextureReaderDestroy(int id)
{
    AsyncTextureReader* reader = readers + id;
    reader->destroy = 1;
}

__attribute__((visibility("default")))
void AsyncTextureReaderStart(int id)
{
    AsyncTextureReader* reader = readers + id;
    reader->start = 1;
    reader->status = STATUS_READING;
}

__attribute__((visibility("default")))
int AsyncTextureReaderGetStatus(int id)
{
    AsyncTextureReader* reader = readers + id;
    return reader->status;
}

__attribute__((visibility("default")))
void* AsyncTextureReaderGetBuffer(int id)
{
    AsyncTextureReader* reader = readers + id;
    return reader->buffer;
}

__attribute__((visibility("default")))
void AsyncTextureReaderWaitStart(int id)
{
    AsyncTextureReader* reader = readers + id;
    reader->wait = 1;
}

__attribute__((visibility("default")))
void AsyncTextureReaderWaitEnd(int id)
{
    AsyncTextureReader* reader = readers + id;

    while (sem_wait(&reader->sem) < 0)
    {
        if (errno != EINTR)
        {
            // TODO: failed to wait on semaphore
            break;
        }
    }
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
            glBindBuffer(GL_PIXEL_PACK_BUFFER, reader->pbo);
            glUnmapBuffer(GL_PIXEL_PACK_BUFFER);
            glDeleteBuffers(1, &reader->pbo);
            glBindBuffer(GL_PIXEL_PACK_BUFFER, 0);
        }
        reader->used = 0;
        return;
    }

    if (reader->pbo == 0)
    {
        glGenBuffers(1, &reader->pbo);
        glBindBuffer(GL_PIXEL_PACK_BUFFER, reader->pbo);
        glBufferStorage(GL_PIXEL_PACK_BUFFER, reader->size, NULL, GL_CLIENT_STORAGE_BIT | GL_MAP_READ_BIT | GL_MAP_PERSISTENT_BIT);
        reader->buffer = glMapBufferRange(GL_PIXEL_PACK_BUFFER, 0, reader->size, GL_MAP_READ_BIT | GL_MAP_PERSISTENT_BIT);
        glBindBuffer(GL_PIXEL_PACK_BUFFER, 0);

        GLint format;
        glBindTexture(GL_TEXTURE_2D, reader->texture);
        glGetTexLevelParameteriv(GL_TEXTURE_2D, 0, GL_TEXTURE_INTERNAL_FORMAT, &format);
        glBindTexture(GL_TEXTURE_2D, 0);

        switch (format)
        {
        case GL_RGB:
        case GL_RGB8:
            reader->format = GL_RGB;
            reader->type = GL_UNSIGNED_BYTE;
            break;

        case GL_RGBA:
        case GL_RGBA8:
        case GL_SRGB8_ALPHA8:
            reader->format = GL_RGBA;
            reader->type = GL_UNSIGNED_BYTE;
            break;

        case GL_RGBA32F:
            reader->format = GL_RGBA;
            reader->type = GL_FLOAT;
            break;

        case GL_R32F:
            reader->format = GL_RED;
            reader->type = GL_FLOAT;
            break;

        case GL_RG32F:
            reader->format = GL_RG;
            reader->type = GL_FLOAT;
            break;

        default:
        {
            char buf[256];
            sprintf(buf, "UNSUPPORTED TEXTURE FORMAT: %08x", format);
            Debug(buf);
            reader->format = GL_RGBA;
            reader->type = GL_UNSIGNED_BYTE;
            break;
        }

        }

        return;
    }

    if (reader->status == STATUS_READING)
    {
        if (reader->start)
        {
            // Debug("start begin");

            reader->start = 0;
            glBindTexture(GL_TEXTURE_2D, reader->texture);
            glBindBuffer(GL_PIXEL_PACK_BUFFER, reader->pbo);
            glGetTexImage(GL_TEXTURE_2D, 0, reader->format, reader->type, NULL);
            glMemoryBarrier(GL_CLIENT_MAPPED_BUFFER_BARRIER_BIT);
            glBindBuffer(GL_PIXEL_PACK_BUFFER, 0);
            glBindTexture(GL_TEXTURE_2D, 0);
            reader->sync = glFenceSync(GL_SYNC_GPU_COMMANDS_COMPLETE, 0);

            // GLenum err;
            // while ((err = glGetError()) != GL_NO_ERROR)
            // {
            //     char buf[256];
            //     sprintf(buf, "OpenGL error when starting: %08x", err);
            //     Debug(buf);
            // }

            // Debug("start end");
        }

        if (reader->sync)
        {
            // Debug("START_READING begin");

            // wait max 1 minute
            // Debug("AsyncTexture Update, before glClientWaitSync");
            GLenum e = glClientWaitSync(reader->sync, 0, reader->wait ? 60ULL * 1000 * 1000 * 1000 : 0);
            if (e == GL_ALREADY_SIGNALED || e == GL_CONDITION_SATISFIED)
            {
                // Debug("REQUEST FINISHED");

                reader->status = STATUS_FINISHED;
                glDeleteSync(reader->sync);
                reader->sync = NULL;
            }
            else if (e == GL_WAIT_FAILED)
            {
                // Debug("REQUEST WAIT FAILED");

                reader->status = STATUS_FINISHED; // TODO: STATUS_ERROR ?
                glDeleteSync(reader->sync);
                reader->sync = NULL;
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

            // Debug("AsyncTexture Update, WAIT DONE");
            if (reader->wait)
            {
                reader->wait = 0;
                sem_post(&reader->sem);
            }

            // Debug("START_READING end");
        }

        return;
    }
}

__attribute__((visibility("default")))
void* AsyncTextureReaderGetUpdate(void)
{
    return AsyncTextureReaderUpdate;
}
