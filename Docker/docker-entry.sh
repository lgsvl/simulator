#!/bin/bash

set -eu

# ENV variables used:

# BUILD_WINDOWS - "true" if Windows build required
# BUILD_LINUX - "true" if Linux build required
# BUILD_MACOS - "true" if macOS build required
# BUILD_NUMBER - optional build number, added as suffix to zip file
# GIT_COMMIT - git commit, will be embedded into build
# UNITY_USERNAME - username for Unity license
# UNITY_PASSWORD - password for Unity license
# UNITY_SERIAL - serial for Unity license
# NAME - optional prefix of zip filename, "simulator" by default

# Project is expected in /mnt

# Writeable folder where npm & Unity can write
export HOME=/tmp

if [ -z ${BUILD_NUMBER+x} ]; then
  SUFFIX=
else
  SUFFIX=-${BUILD_NUMBER}
fi

if [ -z ${NAME+x} ]; then
  NAME=simulator
fi

function finish
{
  /usr/bin/xvfb-run /opt/Unity/Editor/Unity \
    -batchmode \
    -nographics \
    -silent-crashes \
    -quit \
    -returnlicense
}
trap finish EXIT

# remove old build artifacts
rm -f /mnt/*.zip

### WebUI

cd /mnt/WebUI

npm install
npm run pack-p

### Check

/usr/bin/xvfb-run /opt/Unity/Editor/Unity \
  -serial ${UNITY_SERIAL} \
  -username ${UNITY_USERNAME} \
  -password ${UNITY_PASSWORD} \
  -batchmode \
  -nographics \
  -silent-crashes \
  -quit \
  -projectPath /mnt \
  -executeMethod Simulator.Editor.Check.Run \
  -saveCheck /mnt/${NAME}-check${SUFFIX}.html \
  -logFile /dev/stdout

sleep 5

### Windows

if [ -v BUILD_WINDOWS ] && [ "${BUILD_WINDOWS}" == "true" ]; then

  echo "building windows"

  mkdir -p /tmp/${NAME}-windows64${SUFFIX}

  /usr/bin/xvfb-run /opt/Unity/Editor/Unity \
    -serial ${UNITY_SERIAL} \
    -username ${UNITY_USERNAME} \
    -password ${UNITY_PASSWORD} \
    -batchmode \
    -nographics \
    -silent-crashes \
    -quit \
    -projectPath /mnt \
    -executeMethod Simulator.Editor.Build.Run \
    -buildTarget Win64 \
    -buildOutput /tmp/${NAME}-windows64${SUFFIX} \
    -logFile /dev/stdout

  if [ ! -f /tmp/${NAME}-windows64${SUFFIX}/simulator.exe ]; then
    echo "ERROR: **********************************************************"
    echo "ERROR: simulator.exe was not build, scroll up to see actual error"
    echo "ERROR: **********************************************************"
    exit 1
  fi

  sleep 5

  cp /mnt/LICENSE /tmp/${NAME}-windows64${SUFFIX}/LICENSE.txt
  cp /mnt/README.md /tmp/${NAME}-windows64${SUFFIX}/README.txt

  cd /tmp
  zip -r /mnt/${NAME}-windows64${SUFFIX}.zip ${NAME}-windows64${SUFFIX}

fi

### Linux

if [ -v BUILD_LINUX ] && [ "${BUILD_LINUX}" == "true" ]; then

  mkdir -p /tmp/${NAME}-linux64${SUFFIX}

  /usr/bin/xvfb-run /opt/Unity/Editor/Unity \
    -serial ${UNITY_SERIAL} \
    -username ${UNITY_USERNAME} \
    -password ${UNITY_PASSWORD} \
    -batchmode \
    -nographics \
    -silent-crashes \
    -quit \
    -executeMethod Simulator.Editor.Build.Run \
    -buildTarget Linux64 \
    -buildOutput /tmp/${NAME}-linux64${SUFFIX} \
    -projectPath /mnt \
    -logFile /dev/stdout

  if [ ! -x /tmp/${NAME}-linux64${SUFFIX}/simulator ]; then
    echo "ERROR: *************************************************************"
    echo "ERROR: simulator binary was not build, scroll up to see actual error"
    echo "ERROR: *************************************************************"
    exit 1
  fi

  sleep 5

  cp /mnt/LICENSE /tmp/${NAME}-linux64${SUFFIX}/LICENSE.txt
  cp /mnt/README.md /tmp/${NAME}-linux64${SUFFIX}/README.txt

  cd /tmp
  zip -r /mnt/${NAME}-linux64${SUFFIX}.zip ${NAME}-linux64${SUFFIX}

fi

### macOS

if [ -v BUILD_MACOS ] && [ "${BUILD_MACOS}" == "true" ]; then

  mkdir -p /tmp/${NAME}-macOS${SUFFIX}

  /usr/bin/xvfb-run /opt/Unity/Editor/Unity \
    -serial ${UNITY_SERIAL} \
    -username ${UNITY_USERNAME} \
    -password ${UNITY_PASSWORD} \
    -batchmode \
    -nographics \
    -silent-crashes \
    -quit \
    -buildTarget OSXUniversal \
    -executeMethod Simulator.Editor.Build.Run \
    -buildOutput /tmp/${NAME}-macOS${SUFFIX} \
    -projectPath /mnt \
    -logFile /dev/stdout

  if [ ! -x /tmp/${NAME}-macOS${SUFFIX}/simulator.app/Contents/MacOS/simulator  ]; then
    echo "ERROR: *************************************************************"
    echo "ERROR: simulator binary was not build, scroll up to see actual error"
    echo "ERROR: *************************************************************"
    exit 1
  fi

  cp /mnt/LICENSE /tmp/${NAME}-macOS${SUFFIX}/LICENSE.txt
  cp /mnt/README.md /tmp/${NAME}-macOS${SUFFIX}/README.txt

  cd /tmp
  zip -r /mnt/${NAME}-macOS${SUFFIX}.zip ${NAME}-macOS${SUFFIX}

fi
