#!/bin/sh

gcc -s -fPIC -shared -fvisibility=hidden -O3 -ffast-math -mtune=corei7 -lGL \
    AsyncTextureReader.c -o ../x64/libAsyncTextureReader.so
