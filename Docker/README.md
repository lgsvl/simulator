# Running SVL Simulator in Docker

This folder contains a Dockerfile and instructions how to run SVL Simulator in a Docker container. `Dockerfile` creates a minimal container image with Vulkan capabilities, and downloads and unpacks the SVL Simulator under `/opt/simulator`.

The container image should work on __Ubuntu 18.04__ and on __ArchLinux__.

## Build the container image
Run the following command to build the container image:

```shell
# Specify --pull and --no-cache in order to get upstream images and dependencies with the latest security fixes applied.
# Remove them when developing Dockerfile.
# Only one of the simulator_* build args should be specified.
# Default <VERSION> is "latest".
# Default <URL> is https://github.com/lgsvl/simulator/releases/download/<VERSION>/svlsimulator-linux64-<VERSION>.zip .
# Default <ZIPFILE> is none, ie, fetch from URL; <ZIPFILE> must be in the same directory tree as Dockerfile.
# Default <IMAGE:TAG> is "ubuntu:18.04".
# Default <VL_VERSION> ls "sdk-1.2.131.2".
$ docker build --pull --no-cache [--build-arg simulator_version=<VERSION>|simulator_url=<URL>|simulator_zipfile=<ZIPFILE>] \
                                 [--build-arg base_image=<IMAGE:TAG>] \
                                 [--build-arg vulkan_loader_version=<VL_VERSION>] \
                                 [--build-arg image_git_describe=$(git describe --always --tags>) \
                                 [--build-arg image_uuidgen=$(uuidgen)] \
                                 -t svlsimulator[:<VERSION>] .
```

## Launch the container image

Docker v20.10 or later must be installed on Linux and the [**NVIDIA Container Toolkit**](https://docs.nvidia.com/datacenter/cloud-native/container-toolkit/install-guide.html#setting-up-nvidia-container-toolkit).

To run the simulator using the host's X Server, first run the simulator outside of the container, link it to the cloud, and exit. Then use the following command:

```shell
$ docker run -ti \
     --gpus=all \
     --net=host \
     -e DISPLAY \
     -e XAUTHORITY=/tmp/.Xauthority \
     -v ${XAUTHORITY}:/tmp/.Xauthority \
     -v /tmp/.X11-unix:/tmp/.X11-unix \
     -v ~/.config/unity3d:/root/.config/unity3d \
     svlsimulator[:<VERSION>]
```

Note that only simulations which use the __API Only__ and __Random Traffic__ runtime templates can be run. Also, the **OPEN BROWSER** and **Visual Editor** buttons do not function.
