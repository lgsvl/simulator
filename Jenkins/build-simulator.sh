#!/bin/bash

set -eu

if [[ $EUID -eq 0 ]]; then
  echo "ERROR: running as root is not supported"
  echo "Please run 'export UID' before running docker-compose!"
  exit 1
fi

if [ ! -v UNITY_USERNAME ]; then
  echo "ERROR: UNITY_USERNAME environment variable is not set"
  exit 1
fi

if [ ! -v UNITY_PASSWORD ]; then
  echo "ERROR: UNITY_PASSWORD environment variable is not set"
  exit 1
fi

if [ ! -v UNITY_SERIAL ]; then
  echo "ERROR: UNITY_SERIAL environment variable is not set"
  exit 1
fi

if [ $# -ne 1 ]; then
  echo "ERROR: please specify command!"
  echo "  check - runs file/folder structure check"
  echo "  test - runs unit tests"
  echo "  windows - runs 64-bit Windows build"
  echo "  linux - runs 64-bit Linux build"
  echo "  macos - runs macOS build"
  exit 1
fi

export HOME=/tmp

CHECK_UNITY_LOG=$(readlink -f "$(dirname $0)/check-unity-log.sh")

cd /mnt

PREFIX=lgsvlsimulator
SUFFIX=

if [ -v GIT_TAG ]; then
  export BUILD_VERSION=${GIT_TAG}
  DEVELOPMENT_BUILD=
  SUFFIX=${SUFFIX}-${GIT_TAG}
else
  export BUILD_VERSION="dev"
  DEVELOPMENT_BUILD=-developmentBuild
  if [ -v GIT_BRANCH ]; then
    SUFFIX=${SUFFIX}-${GIT_BRANCH}
  fi
  if [ -v JENKINS_BUILD_ID ]; then
    SUFFIX=${SUFFIX}-${JENKINS_BUILD_ID}
  fi
fi

# Replace any '/' with '-' using bash's Pattern Replace
# ${VAR//pattern/replacement}

SUFFIX=${SUFFIX//\//-}

if [ ! -z ${SIMULATOR_CONTROLLABLES+x} ]; then
  CONTROLLABLES="-buildBundles -buildControllables ${SIMULATOR_CONTROLLABLES}"
else
  CONTROLLABLES=
fi

if [ ! -z ${SIM_SENSORS+x} ]; then
  SENSORS="-buildBundles -buildSensors ${SIM_SENSORS}"
else
  SENSORS=
fi

if [ ! -z ${SIMULATOR_NPCS+x} ]; then
  NPCS="-buildBundles -buildNPCs ${SIMULATOR_NPCS}"
else
  NPCS=
fi

function check_unity_log {
    ${CHECK_UNITY_LOG} $@
}

function get_unity_license {
    echo "Fetching unity license"

    mkdir -p dummy-unity-project
    pushd dummy-unity-project

    /opt/Unity/Editor/Unity \
        -logFile /dev/stdout \
        -batchmode \
        -serial ${UNITY_SERIAL} \
        -username ${UNITY_USERNAME} \
        -password ${UNITY_PASSWORD} \
        -projectPath /mnt/dummy-unity-project \
        -quit

    popd
}

function finish
{
  /opt/Unity/Editor/Unity \
    -batchmode \
    -force-vulkan \
    -silent-crashes \
    -quit \
    -returnlicense
}
trap finish EXIT

get_unity_license

if [ "$1" == "check" ]; then

  /opt/Unity/Editor/Unity \
    -batchmode \
    -force-vulkan \
    -silent-crashes \
    -quit \
    -projectPath /mnt \
    -executeMethod Simulator.Editor.Check.Run \
    -saveCheck /mnt/${PREFIX}-check${SUFFIX}.html \
    -logFile /dev/stdout | tee unity-check.log

  check_unity_log unity-check.log

  exit 0

elif [ "$1" == "test" ]; then

  /opt/Unity/Editor/Unity \
    -batchmode \
    -force-vulkan \
    -silent-crashes \
    -projectPath /mnt \
    -runEditorTests \
    -editorTestsResultFile /mnt/${PREFIX}-test${SUFFIX}.xml \
    -logFile /dev/stdout | tee unity-test.log \
  || true

  check_unity_log unity-test.log

  exit 0

fi

if [ "$1" == "windows" ]; then

  BUILD_TARGET=Win64
  BUILD_OUTPUT=${PREFIX}-windows64${SUFFIX}
  BUILD_CHECK=simulator.exe

elif [ "$1" == "linux" ]; then

  BUILD_TARGET=Linux64
  BUILD_OUTPUT=${PREFIX}-linux64${SUFFIX}
  BUILD_CHECK=simulator

elif [ "$1" == "macos" ]; then

  BUILD_TARGET=OSXUniversal
  BUILD_OUTPUT=${PREFIX}-macOS${SUFFIX}
  BUILD_CHECK=simulator.app/Contents/MacOS/simulator

else

  echo "Unknown command $1"
  exit 1

fi

echo "I: Cleanup AssetBundles before build"

rm -Rf /mnt/AssetBundles || true
mkdir -p /mnt/AssetBundles/{Controllables,NPCs,Sensors} || true

/opt/Unity/Editor/Unity ${DEVELOPMENT_BUILD} \
  -batchmode \
  -force-vulkan \
  -silent-crashes \
  -projectPath /mnt \
  -executeMethod Simulator.Editor.Build.Run \
  -buildTarget ${BUILD_TARGET} \
  -buildPlayer /tmp/${BUILD_OUTPUT} \
  ${CONTROLLABLES} \
  ${SENSORS} \
  ${NPCS} \
  -logFile /dev/stdout | tee unity-build-player-${BUILD_TARGET}.log

check_unity_log unity-build-player-${BUILD_TARGET}.log

if [ ! -f /tmp/${BUILD_OUTPUT}/${BUILD_CHECK} ]; then
  echo "ERROR: *****************************************************************"
  echo "ERROR: Simulator executable was not build, scroll up to see actual error"
  echo "ERROR: *****************************************************************"
  exit 1
fi

if [ "$1" == "windows" ] && [ -v CODE_SIGNING_PASSWORD ]; then
  EXE="/tmp/${BUILD_OUTPUT}/${BUILD_CHECK}"
  SIGNED="/tmp/${BUILD_OUTPUT}/signed.exe"

  osslsigncode sign                        \
    -pkcs12 /tmp/signing.p12               \
    -pass "${CODE_SIGNING_PASSWORD}" \
    -n "LGSVL Simulator"                   \
    -i https://www.lgsvlsimulator.com      \
    -t http://timestamp.digicert.com       \
    -in "${EXE}"                           \
    -out "${SIGNED}"

  mv "${SIGNED}" "${EXE}"
fi

cp /mnt/config.yml.template /tmp/${BUILD_OUTPUT}/config.yml

if [ -v CLOUD_URL ]; then
  echo "cloud_url: \"${CLOUD_URL}\"" >> /tmp/${BUILD_OUTPUT}/config.yml
fi

cp /mnt/LICENSE /tmp/${BUILD_OUTPUT}/LICENSE.txt
cp /mnt/LICENSE-3RD-PARTY /tmp/${BUILD_OUTPUT}/LICENSE-3RD-PARTY.txt
cp /mnt/PRIVACY /tmp/${BUILD_OUTPUT}/PRIVACY.txt
cp /mnt/README.md /tmp/${BUILD_OUTPUT}/README.txt

mkdir -p /tmp/${BUILD_OUTPUT}/AssetBundles/Controllables
cp /mnt/AssetBundles/Controllables/controllable_* /tmp/${BUILD_OUTPUT}/AssetBundles/Controllables

mkdir -p /tmp/${BUILD_OUTPUT}/AssetBundles/Sensors
cp /mnt/AssetBundles/Sensors/sensor_* /tmp/${BUILD_OUTPUT}/AssetBundles/Sensors

mkdir -p /tmp/${BUILD_OUTPUT}/AssetBundles/NPCs
cp -R /mnt/AssetBundles/NPCs/* /tmp/${BUILD_OUTPUT}/AssetBundles/NPCs

# TODO: This supports Jenkins only. For local build, need to package FFmpeg in Build.cs
mkdir -p /tmp/${BUILD_OUTPUT}/simulator_Data/Plugins
if [ "$1" == "windows" ]; then
  cp /mnt/Assets/Plugins/VideoCapture/ffmpeg/windows/ffmpeg.exe /tmp/${BUILD_OUTPUT}/simulator_Data/Plugins/ffmpeg.exe
elif [ "$1" == "linux" ]; then
  cp /mnt/Assets/Plugins/VideoCapture/ffmpeg/linux/ffmpeg /tmp/${BUILD_OUTPUT}/simulator_Data/Plugins/ffmpeg
fi

cd /tmp
zip -r /mnt/${BUILD_OUTPUT}.zip ${BUILD_OUTPUT}
