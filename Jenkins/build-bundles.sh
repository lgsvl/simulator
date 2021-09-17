#!/bin/bash

set -euo pipefail

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

if [ -z ${SIM_ENVIRONMENTS+x} ] && [ -z ${SIM_VEHICLES+x} ] && [ -z ${SIM_SENSORS+x} ] && [ -z ${SIM_BRIDGES+x} ]; then
    echo All environments, vehicles, sensors and bridges are up to date!
    exit 0
fi

function getAssets()
{
    ASSETS=
    local DELIM=
    while read -r LINE; do
        local ITEMS=( ${LINE} )
        local NAME="${ITEMS[1]}"
        ASSETS="${ASSETS}${DELIM}${NAME}"
        DELIM=" "
    done <<< "$1"
}

CHECK_UNITY_LOG=$(readlink -f "$(dirname $0)/check-unity-log.sh")

function check_unity_log {
    ${CHECK_UNITY_LOG} $@
}

export HOME=/tmp

cd /mnt

UNITY=/usr/bin/unity-editor

if [ ! -x "${UNITY}" ] ; then
    echo "ERROR: ${UNITY} doesn't exist or isn't executable"
    exit 1
fi

###

function get_unity_license {
    echo "Fetching unity license"

    for i in `seq 1 3`; do
        if ! ${UNITY} \
            -batchmode \
            -serial ${UNITY_SERIAL} \
            -username ${UNITY_USERNAME} \
            -password ${UNITY_PASSWORD} \
            -silent-crashes \
            -projectPath /mnt \
            -logFile /dev/stdout \
            -quit; then
            echo "WARN: Fetching unity license failed, attempt ${i} in 3, trying again"
        else
            echo "INFO: Fetching unity license attempt ${i} seems to be successful"
            break
        fi
    done
}

function build_bundle {
    # Move the source for individual bundles from Assets/External-All to Assets/External
    # to prevent Unity importing all the available assets when we went to build just one
    # of them
    echo "Building bundle (${BUNDLE}/${BUNDLES}) $*"
    if [ ! -d .external-assets/$1/$2 ] ; then
        echo "ERROR: Bundle source doesn't exist in .external-assets/$1/$2"
    else
        mkdir -p Assets/External/$1
        mv .external-assets/$1/$2 Assets/External/$1
    fi

    for i in `seq 1 3`; do
        if ! ${UNITY} \
            -force-vulkan \
            -silent-crashes \
            -projectPath /mnt \
            -executeMethod Simulator.Editor.Build.Run \
            -buildBundles \
            -build$1 $2 \
            -logFile /dev/stdout; then
            echo "WARN: Bundle $1 $2 failed to build, attempt ${i} in 3, trying again"
        else
            echo "INFO: Bundle $1 $2 bundle attempt ${i} seems to be successful"
            break
        fi
    done


    mv Assets/External/$1/$2 .external-assets/$1/$2
    if [ ! -f AssetBundles/$1/*_$2 ] ; then
        echo "ERROR: Bundle $1 $2 wasn't created in AssetBundles/$1/*_$2"
    else
        echo "INFO: Bundle $1 $2 succeeded to build"
    fi
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

get_unity_license

PREFIX=svlsimulator

if [ -v GIT_TAG ]; then
    SUFFIX=${GIT_TAG}
elif [ -v JENKINS_BUILD_ID ]; then
    SUFFIX=${JENKINS_BUILD_ID}
else
    SUFFIX=
fi

echo "I: Cleanup AssetBundles before build"

rm -Rf /mnt/AssetBundles unity-build-bundles*log || true

BUNDLES=0
if [ ! -z ${SIM_ENVIRONMENTS+x} ]; then
    echo "INFO: going to build $(echo "${SIM_ENVIRONMENTS=}" | wc -l) Environments"
    BUNDLES=$(expr ${BUNDLES} + $(echo "${SIM_ENVIRONMENTS=}" | wc -l))
fi
if [ ! -z ${SIM_VEHICLES+x} ]; then
    echo "INFO: going to build $(echo "${SIM_VEHICLES=}" | wc -l) Vehicles"
    BUNDLES=$(expr ${BUNDLES} + $(echo "${SIM_VEHICLES=}" | wc -l))
fi
if [ ! -z ${SIM_SENSORS+x} ]; then
    echo "INFO: going to build $(echo "${SIM_SENSORS=}" | wc -l) Sensors"
    BUNDLES=$(expr ${BUNDLES} + $(echo "${SIM_SENSORS=}" | wc -l))
fi
if [ ! -z ${SIM_BRIDGES+x} ]; then
    echo "INFO: going to build $(echo "${SIM_BRIDGES=}" | wc -l) Bridges"
    BUNDLES=$(expr ${BUNDLES} + $(echo "${SIM_BRIDGES=}" | wc -l))
fi
BUNDLE=0

if [ ! -z ${SIM_ENVIRONMENTS+x} ]; then
    getAssets "${SIM_ENVIRONMENTS}"
    for A in ${ASSETS}; do
        BUNDLE=$(expr ${BUNDLE} + 1)
        build_bundle Environments ${A} 2>&1 | tee -a unity-build-bundles-Environments.log
    done
fi
if [ ! -z ${SIM_VEHICLES+x} ]; then
    getAssets "${SIM_VEHICLES}"
    for A in ${ASSETS}; do
        BUNDLE=$(expr ${BUNDLE} + 1)
        build_bundle Vehicles ${A} 2>&1 | tee -a unity-build-bundles-Vehicles.log
    done
fi

if [ ! -z ${SIM_SENSORS+x} ]; then
    getAssets "${SIM_SENSORS}"
    for A in ${ASSETS}; do
        BUNDLE=$(expr ${BUNDLE} + 1)
        build_bundle Sensors ${A} 2>&1 | tee -a unity-build-bundles-Sensors.log
    done
fi

if [ ! -z ${SIM_BRIDGES+x} ]; then
    getAssets "${SIM_BRIDGES}"
    for A in ${ASSETS}; do
        BUNDLE=$(expr ${BUNDLE} + 1)
        build_bundle Bridges ${A} 2>&1 | tee -a unity-build-bundles-Bridges.log
    done
fi

RESULT=0
OK=0

# maybe drop -e before the next section, because "|| true" is needed after
# each grep which doesn't match anything (including grep -c), and also after
# expr 0 + 0, which prints 0, but returns 1 as return status
echo
echo
echo "Build bundles summary:"
for assetType in Environments Vehicles Sensors Bridges; do
    if [ -f unity-build-bundles-${assetType}.log ] ; then
        echo "== ${assetType} built successfully =="
        grep "^INFO: .* succeeded to build" unity-build-bundles-${assetType}.log || true
        OK=$(expr ${OK} + $(grep -c "^INFO: .* succeeded to build" unity-build-bundles-${assetType}.log || true) || true)
        echo "== ${assetType} built with errors =="
        grep "^ERROR:" unity-build-bundles-${assetType}.log || true
        RESULT=$(expr ${RESULT} + $(grep -c "^ERROR:" unity-build-bundles-${assetType}.log || true) || true)
        echo "== ${assetType} built with warnings =="
        grep "^WARN:" unity-build-bundles-${assetType}.log || true
        echo "== ${assetType} other possible issues detected by check_unity_log =="
        Jenkins/check-unity-log.sh unity-build-bundles-${assetType}.log || RESULT=$(expr ${RESULT} + 1)
    else
        echo "No ${assetType} were built (unity-build-bundles-${assetType}.log doesn't exist)"
    fi
done
echo "${OK} from ${BUNDLES} bundles were built successfully, there were ${RESULT} issues"
exit ${RESULT}
