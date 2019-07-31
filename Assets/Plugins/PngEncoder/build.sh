#!/bin/sh

set -eu

git clone --depth=1 -b v1.2.11 https://github.com/madler/zlib
git clone --depth=1 -b v1.6.37 git://git.code.sf.net/p/libpng/code libpng

export CFLAGS="-fPIC"

cd zlib
git apply ../patch/zlib.patch
cmake -DCMAKE_BUILD_TYPE=Release .
cmake --build . -- -j`nproc`
cd ..

export CFLAGS="-I`pwd`/zlib -Wl,--start-group `pwd`/zlib/libz.a"

cd libpng
git apply ../patch/libpng.patch
cmake -DCMAKE_BUILD_TYPE=Release -DPNG_BUILD_ZLIB=ON -DPNG_SHARED=ON -DPNG_STATIC=OFF -DPNG_TESTS=OFF .
cmake --build . -- -j`nproc`
strip --strip-unneeded libpng.so.16
cd ..

cp -L libpng/libpng.so.16 x64/libpng.so
rm -rf zlib libpng
