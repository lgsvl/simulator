@echo off
setlocal enabledelayedexpansion

where /q cl.exe

if errorlevel 1 (
  if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" (
    for /f "usebackq tokens=*" %%i in (`"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath`) do (
      set InstallDir=%%i
    )

    if exist "!InstallDir!\Common7\Tools\VsDevCmd.bat" (
      call "!InstallDir!\Common7\Tools\VsDevCmd.bat" -arch=x64 -host_arch=x64
    )
  )
)


set CL=/O2
set LINK=/OPT:REF /OPT:ICF

rem set CL=/Zi
rem set LINK=/DEBUG

fxc.exe /nologo /T cs_5_0 /E FlipKernel /O3 /WX /Ges /Fh VideoCaptureFlip.cs.h /Qstrip_reflect /Qstrip_debug /Qstrip_priv VideoCaptureFlip.hlsl
cl.exe /nologo /W3 /fp:fast /MT /Iinclude VideoCapture.c /link /DLL /INCREMENTAL:NO /OUT:..\x64\VideoCapture.dll kernel32.lib user32.lib

del /q *.pdb* *.obj* *.cs.h* ..\x64\*.exp* ..\x64\*.lib*
