#!/bin/bash

set -eu

if [ -z ${BUILD_NUMBER+x} ];
then
    SUFFIX=
else
    SUFFIX=-${BUILD_NUMBER}
fi

if [ -z ${NAME+x} ];
then
    NAME=auto-simulator
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

mkdir -p /tmp/${NAME}-{win64,linux64}${SUFFIX}

export HOME=/tmp

/usr/bin/xvfb-run /opt/Unity/Editor/Unity \
    -serial ${UNITY_SERIAL} \
    -username ${UNITY_USERNAME} \
    -password ${UNITY_PASSWORD} \
    -batchmode \
    -nographics \
    -silent-crashes \
    -quit \
    -buildDestination /tmp/${NAME}-win64${SUFFIX}/simulator.exe \
    -buildTarget Win64 \
    -executeMethod BuildScript.Build \
    -projectPath /mnt \
    -logFile /dev/stdout

if [ ! -f /tmp/${NAME}-win64${SUFFIX}/simulator.exe ]; then
  echo "ERROR: simulator.exe was not build, scroll up to see actual error"
  exit 1
fi

sleep 5

/usr/bin/xvfb-run /opt/Unity/Editor/Unity \
    -serial ${UNITY_SERIAL} \
    -username ${UNITY_USERNAME} \
    -password ${UNITY_PASSWORD} \
    -batchmode \
    -nographics \
    -silent-crashes \
    -quit \
    -buildDestination /tmp/${NAME}-linux64${SUFFIX}/simulator \
    -buildTarget Linux64 \
    -executeMethod BuildScript.Build \
    -projectPath /mnt \
    -logFile /dev/stdout

if [ ! -x /tmp/${NAME}-linux64${SUFFIX}/simulator ]; then
  echo "ERROR: simulator binary was not build, scroll up to see actual error"
  exit 1
fi

cp /mnt/{LICENSE,PRIVACY.txt} /tmp/${NAME}-win64${SUFFIX}/
cp /mnt/{LICENSE,PRIVACY.txt} /tmp/${NAME}-linux64${SUFFIX}/
cp /mnt/README.md /tmp/${NAME}-win64${SUFFIX}/README.txt
cp /mnt/README.md /tmp/${NAME}-linux64${SUFFIX}/README.txt

cd /tmp
zip -r /mnt/${NAME}-win64${SUFFIX}.zip ${NAME}-win64${SUFFIX}
zip -r /mnt/${NAME}-linux64${SUFFIX}.zip ${NAME}-linux64${SUFFIX}
