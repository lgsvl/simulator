#pragma once

#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

typedef struct rfbc rfbc;

#define RFBC_FLAG_EXCLUSIVE (1<<0)

#define RFBC_STATUS_ERROR      0
#define RFBC_STATUS_CONNECTING 1
#define RFBC_STATUS_CONNECTED  2

rfbc* rfbc_connect(const char* host, uint16_t port, uint32_t flags);
void rfbc_close(rfbc* client);

int rfbc_get_status(rfbc* client);

void rfbc_get_size(rfbc* client, uint32_t* width, uint32_t* height);
int rfbc_get_data(rfbc* client, void* data, uint32_t stride, uint32_t width, uint32_t height);

int rfbc_refresh(rfbc* client, int incremental);
int rfbc_mouse(rfbc* client, uint16_t x, uint16_t y, uint32_t buttons);

#ifdef __cplusplus
}
#endif
