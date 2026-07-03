@echo off
setlocal

set PROJECT_PATH=%~dp0
set BUILD_PATH=%PROJECT_PATH%Build
set UNITY_PATH=C:\Program Files\Unity\Hub\Editor\6000.5.2f1\Editor\Unity.exe

if not exist "%UNITY_PATH%" (
    echo Unity editor not found at: %UNITY_PATH%
    echo Please build manually from Unity Editor: File - Build Profiles
    exit /1
)

if not exist "%BUILD_PATH%" mkdir "%BUILD_PATH%"

echo Close Unity Editor before building...
"%UNITY_PATH%" ^
  -batchmode -nographics -quit ^
  -projectPath "%PROJECT_PATH%" ^
  -logFile "%BUILD_PATH%\unity_build.log" ^
  -buildWindows64Player "%BUILD_PATH%\CorePush.exe"

if %ERRORLEVEL% NEQ 0 (
    echo Build failed. See %BUILD_PATH%\unity_build.log
    exit /1
)

echo Build succeeded: %BUILD_PATH%\CorePush.exe
