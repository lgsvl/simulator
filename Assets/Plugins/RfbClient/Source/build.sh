#!/bin/sh

gcc -s -fPIC -shared -fvisibility=hidden -O3 -ffast-math -mtune=corei7 -Wall -Werror \
    plugin.c rfb_client.c -IUnity -lpthread -lz -lGL -o ../x64/libRfbClient.so
