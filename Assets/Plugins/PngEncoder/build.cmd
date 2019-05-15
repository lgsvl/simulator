@echo off

setlocal

git clone --depth=1 -b v1.2.11 https://github.com/madler/zlib
git clone --depth=1 -b libpng-1.6.31-signed git://git.code.sf.net/p/libpng/code libpng

pushd zlib
git apply ..\patch\zlib.patch
cmake -G "Visual Studio 15 2017" -T host=x64 -A x64 .
cmake --build . --config Release
popd

set CFLAGS=/I%CD%\zlib
set LDFLAGS=%CD%\zlib\Release\zlibstatic.lib
pushd libpng
git apply ..\patch\libpng.patch
cmake -G "Visual Studio 15 2017" -T host=x64 -A x64 -DPNG_BUILD_ZLIB=ON -DPNG_SHARED=ON -DPNG_STATIC=OFF -DPNG_TESTS=OFF .
cmake --build . --config Release
popd

move libpng\Release\libpng.dll x64\
rd /s /q zlib libpng
