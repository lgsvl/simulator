@echo off

rem VS2017
call "C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\Common7\Tools\VsDevCmd.bat" -arch=amd64 -host_arch=amd64

set CL=/nologo /errorReport:none /Wall /WX /Gm- /GF /fp:fast /GS- /LD /Izlib ^
       /wd4204 /wd4013 /wd4820 /wd4711 /wd4255 /wd4201 /wd4710 /wd5045 ^
       /DNO_GZIP /D_CRT_NONSTDC_NO_DEPRECATE /D_CRT_SECURE_NO_DEPRECATE

set LINK=/errorReport:none /DLL /INCREMENTAL:NO /SUBSYSTEM:WINDOWS

set CL=%CL% /GL /Ox /Ot /MT
rem set CL=%CL% /Od /Zi /D_DEBUG /MTd
rem set LINK=%LINK% /DEBUG

cl /c /W0 zlib\adler32.c zlib\inffast.c zlib\inflate.c zlib\inftrees.c zlib\zutil.c
cl.exe plugin.c rfb_client.c adler32.obj inffast.obj inflate.obj inftrees.obj zutil.obj /Fe..\x64\RfbClient.dll
del /Q *.obj ..\x64\*.exp ..\x64\*.lib
