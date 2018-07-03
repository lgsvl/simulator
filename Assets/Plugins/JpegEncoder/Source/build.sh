#!/bin/sh

gcc -s -fPIC -shared -fvisibility=hidden -O3 -ffast-math -mtune=corei7 \
    JpegEncoder.c -o ../x64/libJpegEncoder.so
