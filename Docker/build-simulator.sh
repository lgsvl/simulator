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

PREFIX=lgsvlsimulator
SUFFIX=

if [ "${GIT_TAG}" == "" ]; then
  export BUILD_VERSION="dev"
  DEVELOPMENT_BUILD=-developmentBuild
  if [ -v GIT_BRANCH ]; then
    SUFFIX=${SUFFIX}-${GIT_BRANCH}
  fi
  if [ -v JENKINS_BUILD_ID ]; then
    SUFFIX=${SUFFIX}-${JENKINS_BUILD_ID}
  fi
else
  export BUILD_VERSION=${GIT_TAG}
  DEVELOPMENT_BUILD=
  SUFFIX=${SUFFIX}-${GIT_TAG}
fi

function finish
{
  /opt/Unity/Editor/Unity \
    -batchmode \
    -force-glcore \
    -silent-crashes \
    -quit \
    -returnlicense
}
trap finish EXIT

if [ "$1" == "check" ]; then

  /opt/Unity/Editor/Unity \
    -serial ${UNITY_SERIAL} \
    -username ${UNITY_USERNAME} \
    -password ${UNITY_PASSWORD} \
    -batchmode \
    -force-glcore \
    -silent-crashes \
    -quit \
    -projectPath /mnt \
    -executeMethod Simulator.Editor.Check.Run \
    -saveCheck /mnt/${PREFIX}-check${SUFFIX}.html \
    -logFile /dev/stdout

  exit 0

elif [ "$1" == "test" ]; then

  # first run Unity to activate license, because -runEditorTests does not do it
  /opt/Unity/Editor/Unity \
    -serial ${UNITY_SERIAL} \
    -username ${UNITY_USERNAME} \
    -password ${UNITY_PASSWORD} \
    -batchmode \
    -force-glcore \
    -silent-crashes \
    -quit \
    -projectPath /mnt \
    -logFile /dev/stdout

  # now run unit tests without username/password/serial
  /opt/Unity/Editor/Unity \
    -batchmode \
    -force-glcore \
    -silent-crashes \
    -projectPath /mnt \
    -runEditorTests \
    -editorTestsResultFile /mnt/${PREFIX}-test${SUFFIX}.xml \
    -logFile /dev/stdout \
  || true

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

/opt/Unity/Editor/Unity ${DEVELOPMENT_BUILD} \
  -serial ${UNITY_SERIAL} \
  -username ${UNITY_USERNAME} \
  -password ${UNITY_PASSWORD} \
  -batchmode \
  -force-glcore \
  -silent-crashes \
  -quit \
  -projectPath /mnt \
  -executeMethod Simulator.Editor.Build.Run \
  -buildTarget ${BUILD_TARGET} \
  -buildOutput /tmp/${BUILD_OUTPUT} \
  -skipBundles \
  -logFile /dev/stdout


if [ ! -f /tmp/${BUILD_OUTPUT}/${BUILD_CHECK} ]; then
  echo "ERROR: *****************************************************************"
  echo "ERROR: Simulator executable was not build, scroll up to see actual error"
  echo "ERROR: *****************************************************************"
  exit 1
fi

cp /mnt/LICENSE /tmp/${BUILD_OUTPUT}/LICENSE.txt
cp /mnt/PRIVACY /tmp/${BUILD_OUTPUT}/PRIVACY.txt
cp /mnt/README.md /tmp/${BUILD_OUTPUT}/README.txt

cd /tmp
zip -r /mnt/${BUILD_OUTPUT}.zip ${BUILD_OUTPUT}
