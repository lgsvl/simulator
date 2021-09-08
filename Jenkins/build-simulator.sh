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

if [ ! -v UNITY_VERSION ]; then
    echo "ERROR: UNITY_VERSION environment variable is not set"
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

PREFIX=svlsimulator
SUFFIX=
UNITY=/usr/bin/unity-editor

if [ ! -x "${UNITY}" ] ; then
    echo "ERROR: ${UNITY} doesn't exist or isn't executable"
    exit 1
fi

if [ -v GIT_TAG ]; then
    export BUILD_VERSION=${GIT_TAG}
    SUFFIX=${SUFFIX}-${GIT_TAG}
else
    export BUILD_VERSION="dev"
    if [ -v GIT_BRANCH ]; then
        GIT_BRANCH_SUFFIX=`echo ${GIT_BRANCH} | tr / -  | tr [:upper:] [:lower:]`
        SUFFIX=${SUFFIX}-${GIT_BRANCH_SUFFIX}
    fi
    if [ -v JENKINS_BUILD_ID ]; then
        SUFFIX=${SUFFIX}-${JENKINS_BUILD_ID}
    fi
fi

if $FORCE_DEV_BUILD ; then
    DEVELOPMENT_BUILD=-developmentBuild
else
    DEVELOPMENT_BUILD=
fi

# Replace any '/' with '-' using bash's Pattern Replace
# ${VAR//pattern/replacement}

SUFFIX=${SUFFIX//\//-}

if [ ! -z ${SIM_CONTROLLABLES+x} ]; then
    CONTROLLABLES="-buildBundles -buildControllables ${SIM_CONTROLLABLES}"
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

if [ ! -z ${SIMULATOR_PEDESTRIANS+x} ]; then
    PEDESTRIANS="-buildBundles -buildPedestrians ${SIMULATOR_PEDESTRIANS}"
else
    PEDESTRIANS=
fi

if [ -n ${SENTRY_DSN} ]; then
    echo "SENTRY_DSN=${SENTRY_DSN}"
else
    echo "Warning: SENTRY_DSN is not set"
fi

function check_unity_log {
      ${CHECK_UNITY_LOG} $@
}

function get_unity_license {
    echo "Fetching unity license"

    mkdir -p dummy-unity-project
    pushd dummy-unity-project

    ${UNITY} \
        -logFile /dev/stdout \
        -batchmode \
        -serial ${UNITY_SERIAL} \
        -username ${UNITY_USERNAME} \
        -password ${UNITY_PASSWORD} \
        -quit

    popd
}

function finish
{
    ${UNITY} \
        -batchmode \
        -force-vulkan \
        -silent-crashes \
        -quit \
        -returnlicense
}
trap finish EXIT

function unity_check
{
  ${UNITY} \
        -batchmode \
        -force-vulkan \
        -silent-crashes \
        -quit \
        -projectPath /mnt \
        -executeMethod Simulator.Editor.Check.Run \
        -saveCheck /mnt/${PREFIX}-check${SUFFIX}.html \
        -logFile /dev/stdout | tee unity-check.log

    check_unity_log unity-check.log
}

function unity_test
{
    ${UNITY} \
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
}

get_unity_license

if [ "$1" = "check" ]; then
    for i in `seq 1 5`; do
        if ! unity_check; then
            echo "WARN: unity_check failed, attempt $i in 5, trying again"
        else
            echo "INFO: attempt $i seems to be successful"
            exit 0
        fi
    done
    echo "ERROR: all 5 unity_check attempts failed, giving up"
    exit 1
elif [ "$1" = "test" ]; then
    unity_test
fi

if [ "$1" = "windows" ]; then

    BUILD_TARGET=Win64
    BUILD_OUTPUT=${PREFIX}-windows64${SUFFIX}
    BUILD_CHECK=simulator.exe

elif [ "$1" = "linux" ]; then

    BUILD_TARGET=Linux64
    BUILD_OUTPUT=${PREFIX}-linux64${SUFFIX}
    BUILD_CHECK=simulator

elif [ "$1" = "macos" ]; then

    BUILD_TARGET=OSXUniversal
    BUILD_OUTPUT=${PREFIX}-macOS${SUFFIX}
    BUILD_CHECK=simulator.app/Contents/MacOS/simulator

else

    echo "Unknown command $1"
    exit 1

fi

echo "I: Cleanup AssetBundles before build"

rm -Rf /mnt/AssetBundles || true
mkdir -p /mnt/AssetBundles/{Controllables,NPCs,Sensors,Pedestrians} || true

${UNITY} ${DEVELOPMENT_BUILD} \
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
    ${PEDESTRIANS} \
    -logFile /dev/stdout | tee unity-build-player-${BUILD_TARGET}.log

check_unity_log unity-build-player-${BUILD_TARGET}.log

if [ ! -f /tmp/${BUILD_OUTPUT}/${BUILD_CHECK} ]; then
    echo "ERROR: *****************************************************************"
    echo "ERROR: Simulator executable '/tmp/${BUILD_OUTPUT}/${BUILD_CHECK}' was not build, scroll up to see actual error"
    echo "ERROR: *****************************************************************"
    exit 1
fi

if [ "$1" = "windows" ] && [ -v CODE_SIGNING_PASSWORD ]; then
    EXE="/tmp/${BUILD_OUTPUT}/${BUILD_CHECK}"
    SIGNED="/tmp/${BUILD_OUTPUT}/signed.exe"

    echo "I: Signing ${EXE} with pkcs12 from CODE_SIGNING_FILE variable mounted in /tmp/signing.p12"
    osslsigncode sign                          \
        -pkcs12 /tmp/signing.p12               \
        -pass "${CODE_SIGNING_PASSWORD}"       \
        -n "LGSVL Simulator"                   \
        -i https://www.svlsimulator.com        \
        -t http://timestamp.digicert.com       \
        -in "${EXE}"                           \
        -out "${SIGNED}"

    echo "I: Replacing original ${EXE} with ${SIGNED}"
    mv "${SIGNED}" "${EXE}"
else
    echo "I: Not signing /tmp/${BUILD_OUTPUT}/${BUILD_CHECK}, because CODE_SIGNING_PASSWORD isn't set or not building for Windows"
fi

if [ -v CLOUD_URL ]; then
    # Override cloud_url
    (
        # drop cloud_url from the template if exist
        grep -v 'cloud_url:' /mnt/config.yml.template;
        echo "cloud_url: \"${CLOUD_URL}\""
    ) > /tmp/${BUILD_OUTPUT}/config.yml
else
    # Copy template as is
    cp /mnt/config.yml.template /tmp/${BUILD_OUTPUT}/config.yml
fi

cp /mnt/LICENSE /tmp/${BUILD_OUTPUT}/LICENSE.txt
cp /mnt/LICENSE-3RD-PARTY /tmp/${BUILD_OUTPUT}/LICENSE-3RD-PARTY.txt
cp /mnt/PRIVACY /tmp/${BUILD_OUTPUT}/PRIVACY.txt
cp /mnt/README.md /tmp/${BUILD_OUTPUT}/README.txt

# Copy all asset bundles
cp -R /mnt/AssetBundles /tmp/${BUILD_OUTPUT}

# Dump asset bundle list for inspection
find /tmp/${BUILD_OUTPUT}/AssetBundles -type f

# TODO: This supports Jenkins only. For local build, need to package FFmpeg in Build.cs
mkdir -p /tmp/${BUILD_OUTPUT}/simulator_Data/Plugins
if [ "$1" = "windows" ]; then
    cp /mnt/Assets/Plugins/VideoCapture/ffmpeg/windows/ffmpeg.exe /tmp/${BUILD_OUTPUT}/simulator_Data/Plugins/ffmpeg.exe
elif [ "$1" = "linux" ]; then
    cp /mnt/Assets/Plugins/VideoCapture/ffmpeg/linux/ffmpeg /tmp/${BUILD_OUTPUT}/simulator_Data/Plugins/ffmpeg
fi

cd /tmp
zip -r /mnt/${BUILD_OUTPUT}.zip ${BUILD_OUTPUT}
