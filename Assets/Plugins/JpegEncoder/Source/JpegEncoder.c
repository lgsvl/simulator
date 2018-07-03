#define STB_IMAGE_WRITE_IMPLEMENTATION
#define STB_IMAGE_WRITE_STATIC
#define STBI_WRITE_NO_STDIO
#include "stb_image_write.h"

#ifdef _MSC_VER
#define DLL_EXPORT __declspec(dllexport)
#else
#define DLL_EXPORT __attribute__((visibility("default")))
#endif

struct out
{
    unsigned char* data;
    int remaining;
};

static void out_writer(void* context, void* data, int size)
{
    struct out* out = context;
    if (size > out->remaining)
    {
        size = out->remaining;
    }

    memcpy(out->data, data, size);
    out->data += size;
    out->remaining -= size;
}

DLL_EXPORT int JpegEncoder_Encode(void* source, int width, int height, int comp, int quality, void* output, int maxout)
{
    struct out out = { output, maxout };

    stbi_flip_vertically_on_write(1);
    if (stbi_write_jpg_to_func(&out_writer, &out, width, height, comp, source, quality) == 0)
    {
        return -1;
    }

    return maxout - out.remaining;
}
