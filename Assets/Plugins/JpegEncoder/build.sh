#!/bin/sh

set -eu

git clone --depth=1 -b 2.0.2 https://github.com/libjpeg-turbo/libjpeg-turbo
cd libjpeg-turbo
cmake -DCMAKE_BUILD_TYPE=Release -DENABLE_SHARED=ON -DENABLE_STATIC=OFF -DREQUIRE_SIMD=ON .
cmake --build . --config Release -- -j`nproc`
strip --strip-unneeded libturbojpeg.so
cp libturbojpeg.so ../x64/
cd ..
rm -rf libjpeg-turbo
