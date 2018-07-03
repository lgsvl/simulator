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

    GLsync sync;
    void* buffer;

    volatile int status;
    volatile int start;
    volatile int destroy;
    int used;
} AsyncTextureReader;

#define READER_MAX_COUNT 32
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
int AsyncTextureReaderCreate(void* texture, int width, int height)
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
    reader->size = width * height * 4;
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

        //Debug("ok");
        return;
    }

    //char m[1024];
    //sprintf(m, "status=%d start=%d\n", (int)reader->status, reader->start);
    //Debug(m);

    if (reader->start)
    {
        //Debug("start begin");

        reader->start = 0;
        glBindTexture(GL_TEXTURE_2D, reader->texture);
        glBindBuffer(GL_PIXEL_PACK_BUFFER, reader->pbo);
        glGetTexImage(GL_TEXTURE_2D, 0, GL_RGBA, GL_UNSIGNED_BYTE, NULL);
        reader->sync = glFenceSync(GL_SYNC_GPU_COMMANDS_COMPLETE, 0);
        glBindBuffer(GL_PIXEL_PACK_BUFFER, 0);
        glBindTexture(GL_TEXTURE_2D, 0);

        while (glGetError() != GL_NO_ERROR)
        {
        }

        //Debug("start end");

        return;
    }

    if (reader->status == STATUS_READING)
    {
        //Debug("START_READING begin");

        GLenum e = glClientWaitSync(reader->sync, 0, 0);
        if (e == GL_ALREADY_SIGNALED || e == GL_CONDITION_SATISFIED)
        {
            //Debug("REQUEST FINISHED");

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
            //Debug("REQUEST IN PROGRESS");
        }

        while (glGetError() != GL_NO_ERROR)
        {
        }

        //Debug("START_READING end");
        return;
    }
}

__attribute__((visibility("default")))
void* AsyncTextureReaderGetUpdate(void)
{
    return AsyncTextureReaderUpdate;
}
