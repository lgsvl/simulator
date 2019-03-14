#!/bin/sh

set -eu

git clone --depth=1 -b v1.2.11 https://github.com/madler/zlib
git clone --depth=1 -b libpng-1.6.31-signed git://git.code.sf.net/p/libpng/code libpng

export CFLAGS="-fPIC"

cd zlib
git apply ../zlib.patch
cmake -DCMAKE_BUILD_TYPE=Release .
cmake --build . -- -j`nproc`
cd ..

export CFLAGS="-I`pwd`/zlib -Wl,--start-group `pwd`/zlib/libz.a"

cd libpng
git apply ../libpng.patch
cmake -DCMAKE_BUILD_TYPE=Release -DPNG_BUILD_ZLIB=ON -DPNG_SHARED=ON -DPNG_STATIC=OFF -DPNG_TESTS=OFF .
cmake --build . -- -j`nproc`
strip --strip-unneeded libpng.so
cd ..

cp -L libpng/libpng.so x64/
rm -rf zlib libpng
