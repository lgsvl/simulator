@echo off

setlocal

git clone --depth=1 -b 3.4.1 https://github.com/LASzip/LASzip
cd LASzip
git apply ..\patch\laszip.patch
cmake -G "Visual Studio 15 2017 Win64" .
cmake --build . --config Release
move bin\laszip3.dll ..\x64\laszip.dll
cd ..
rd /s /q LASzip
