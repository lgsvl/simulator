#!/bin/sh

set -eu

git clone --depth=1 -b 3.4.1 https://github.com/LASzip/LASzip
cd LASzip
cmake -DCMAKE_BUILD_TYPE=Release .
cmake --build . -- -j`nproc`

strip --strip-unneeded lib/liblaszip.so
cp -L lib/liblaszip.so ../x64/

cd ..
rm -rf LASzip
