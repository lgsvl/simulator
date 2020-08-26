#!/bin/sh

usage()
{
     echo "run.sh [-h|--help] [:VERSION] [COMMAND] [ARG...]"
     exit 1
}

case "$1" in
     :*)  tag=$1
          shift
          ;;

     -h|--help)
          usage
          ;;
esac

if [ -z "$DISPLAY" ]; then
    echo "ABORT: DISPLAY not set"
    exit 1
fi

DOCKER_MAJOR_VERSION=$(docker version --format '{{.Client.Version}}' | cut -d. -f1)
if [ $DOCKER_MAJOR_VERSION -ge 19 ] && ! which nvidia-docker > /dev/null; then
     readonly RUNTIME="--gpus=all"
else
     readonly RUNTIME="--runtime=nvidia"
fi

docker run -ti \
     $RUNTIME \
     --net=host \
     -e DISPLAY \
     -e XAUTHORITY=/tmp/.Xauthority \
     -v ${XAUTHORITY}:/tmp/.Xauthority \
     -v /tmp/.X11-unix:/tmp/.X11-unix \
     -v lgsvlsimulator-data:/root/.config/unity3d \
     lgsvlsimulator$tag "$@"
