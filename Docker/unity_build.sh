#!/bin/sh

set -eu

if [ -z ${BUILD_NUMBER+x} ];
then
  SUFFIX=
else
  SUFFIX=-${BUILD_NUMBER}
fi

NAME=auto-simulator

# remove old build artifacts
rm -f /mnt/*.zip

mkdir -p /tmp/${NAME}-{win64,linux64}${SUFFIX}

export HOME=/tmp

/usr/bin/xvfb-run /opt/Unity/Editor/Unity \
    -batchmode \
    -nographics \
    -silent-crashes \
    -quit \
    -buildDestination /tmp/${NAME}-win64${SUFFIX}/simulator.exe \
    -buildTarget Win64 \
    -executeMethod BuildScript.Build \
    -projectPath /mnt \
    -logFile /dev/stdout

/usr/bin/xvfb-run /opt/Unity/Editor/Unity \
    -batchmode \
    -nographics \
    -silent-crashes \
    -quit \
    -buildDestination /tmp/${NAME}-linux64${SUFFIX}/simulator \
    -buildTarget Linux64 \
    -executeMethod BuildScript.Build \
    -projectPath /mnt \
    -logFile /dev/stdout

cd /tmp
zip -r /mnt/${NAME}-win64${SUFFIX}.zip ${NAME}-win64${SUFFIX}
zip -r /mnt/${NAME}-linux64${SUFFIX}.zip ${NAME}-linux64${SUFFIX}
