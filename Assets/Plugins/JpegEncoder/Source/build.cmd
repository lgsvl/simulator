@echo off

rem VS2017
call "C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\Common7\Tools\VsDevCmd.bat" -arch=amd64 -host_arch=amd64

set CL=/nologo /errorReport:none /Wall /wd4204 /wd4013 /wd4820 /wd4711 /WX /Gm- /GF /fp:fast /GS- /LD
set LINK=/errorReport:none /DLL /INCREMENTAL:NO

set CL=%CL% /Ox /Ot
rem set CL=%CL% /Od /Zi /D_DEBUG
rem set LINK=%LINK% /DEBUG

cl.exe JpegEncoder.c /Fe..\x64\JpegEncoder.dll
del *.obj ..\x64\*.exp ..\x64\*.lib
