@echo off

git clone --depth=1 -b 2.0.2 https://github.com/libjpeg-turbo/libjpeg-turbo
cd libjpeg-turbo
cmake -G "Visual Studio 15 2017 Win64" -DCMAKE_BUILD_TYPE=Release -DENABLE_SHARED=ON -DENABLE_STATIC=OFF -DREQUIRE_SIMD=ON .
cmake --build . --config Release
move release\turbojpeg.dll ..\x64\
cd ..
rd /s /q libjpeg-turbo
