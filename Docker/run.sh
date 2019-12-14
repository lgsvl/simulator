#!/bin/sh

docker run -ti \
     --runtime=nvidia \
     --net=host \
     -e DISPLAY \
     -e XAUTHORITY=/tmp/.Xauthority \
     -v ${XAUTHORITY}:/tmp/.Xauthority \
     -v /tmp/.X11-unix:/tmp/.X11-unix \
     -v lgsvlsimulator:/root/.config/unity3d \
     lgsvlsimulator