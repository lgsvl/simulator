# Running LGSVL Simulator in Docker

This folder contains Dockerfile and instructions how to run LGSVL Simulator in a Docker container. `Dockerfile` creates a minimal container image with Vulkan capabilities, and downloads and unpacks the LGSVL Simulator under `/opt/simulator`.

The container image should work on __Ubuntu 18.04__ and on __ArchLinux__.

## Build the container image
Run the following command to build the container image:

```shell
# Specify --pull and --no-cache in order to get upstream images and dependencies with the latest security fixes applied.
# Remove them when developing Dockerfile.
# Only one of the simulator_* build args should be specified.
# Default <VERSION> is "latest".
# Default <URL> is https://github.com/lgsvl/simulator/releases/download/<VERSION>/lgsvlsimulator-linux64-<VERSION>.zip .
# Default <ZIPFILE> is none, ie, fetch from URL; <ZIPFILE> must be in the same directory tree as Dockerfile.
# Default <IMAGE:TAG> is "ubuntu:18.04".
# Default <VL_VERSION> ls "sdk-1.2.131.2".
$ docker build --pull --no-cache [--build-arg simulator_version=<VERSION>|simulator_url=<URL>|simulator_zipfile=<ZIPFILE>] \
                                 [--build-arg base_image=<IMAGE:TAG>] \
                                 [--build-arg vulkan_loader_version=<VL_VERSION>] \
                                 -t lgsvlsimulator[:<VERSION>] .
```

## Launch the container image

Make sure you have installed the [**NVIDIA Container Toolkit**](https://github.com/NVIDIA/nvidia-docker#quickstart) and the
[**NVIDIA Container Runtime**](https://github.com/NVIDIA/nvidia-container-runtime#installation), and have
[registered the runtime](https://github.com/NVIDIA/nvidia-container-runtime#docker-engine-setup) with the Docker Engine.

To run the simulator using the host's X Server, use the following command:

```shell
# Replace "--gpus=all" with "--runtime=nvidia" if you are using Docker < v19.03 or have `nvidia-docker` installed instead of the NVIDIA Container Toolkit.
$ docker run -ti \
     --gpus=all \
     --net=host \
     -e DISPLAY \
     -e XAUTHORITY=/tmp/.Xauthority \
     -v ${XAUTHORITY}:/tmp/.Xauthority \
     -v /tmp/.X11-unix:/tmp/.X11-unix \
     -v lgsvlsimulator-data:/root/.config/unity3d \
     lgsvlsimulator[:<VERSION>]
```

This will store persistent data (database, downloaded maps/vehicles, logfile) in the `lgsvlsimulator-data` Docker volume.

The **Open Browser...** button shown by the simulator does not function when it is running in a container. Instead, you must
manually browse to <http://localhost:8080>.
