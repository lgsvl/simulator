# Running LGSVL Simulator in Docker

This folder contains Dockerfile and instructions how to run LGSVL Simulator in container. Dockerfile creates minimal image with Vulkan capabilities, and downloads & unpacks our simulator.

Run following command to build a container:

```shell
$ docker build -t lgsvlsimulator .
```

To run it use following command:

```shell
$ docker run -ti \
     --runtime=nvidia \
     --net=host \
     -e DISPLAY \
     -e XAUTHORITY=/tmp/.Xauthority \
     -v ${XAUTHORITY}:/tmp/.Xauthority \
     -v /tmp/.X11-unix:/tmp/.X11-unix \
     -v lgsvlsimulator:/root/.config/unity3d \
     lgsvlsimulator
```

Replace ``--runtime=nvidia`` with ``--gpus all`` if you are on Docker >=19.03.
Make sure you have NVIDIA Container Toolkit installed from [nvidia-docker](https://github.com/NVIDIA/nvidia-docker).

This will run simulator and will store persistent data in lgsvlsimulator volume (database, downloaded maps/vehicles, logfile). WebUI can be accessed on http://localhost:8080

This docker image should work on __Ubuntu 18.04__ and on __ArchLinux__.