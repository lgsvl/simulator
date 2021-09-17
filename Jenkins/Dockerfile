# To build for multiple different platforms with the same docker image
# you need to rebuild this image with https://github.com/game-ci/docker/releases/tag/v0.15 or newer (with https://github.com/game-ci/docker/pull/116)
# e.g. for 2020.3.3f1 version with linux, windows, android and mac support:
# --build-arg version="2020.3.3f1"
# --build-arg changeSet="76626098c1c4"                # you can find changeSet value in release announcement, e.g. https://unity3d.com/unity/whats-new/2020.3.3 has "Unity Hub" link unityhub://2020.3.3f1/76626098c1c4
# --build-arg module="windows-mono android mac-mono"  # linux support is already included by default
# use an image with only windows-mono support as an example until the images with multiple modules are available on dockerhub
ARG EDITOR_DOCKER_IMAGE=unityci/editor:ubuntu-2020.3.3f1-windows-mono-0.15.0
FROM ${EDITOR_DOCKER_IMAGE}

# install dependencies:
# libvulkan1:
#     - is loaded by Unity during the build
#     - Unity would use llvmpipe renderer when missing as in:
#       [vulkan] LoadVulkanLibrary libvulkan.so.1
#       [vulkan] LoadVulkanLibrary failed to load libvulkan.so.1Vulkan detection: 0)
# mesa-vulkan-drivers:
#     - is needed for libvulkan1 to work
#     - otherwise vulkaninfo fails with:
#       vulkan-tools-1.2.131.1+dfsg1/vulkaninfo/vulkaninfo.h:371: failed with ERROR_INCOMPATIBLE_DRIVER)
# osslsigncode:
#     - is needed by build-simulator.sh script when building for windows:
#       Jenkins/build-simulator.sh:    osslsigncode sign
# uuid-runtime:
#     - needed to generate IMAGE_UUID:
#       Jenkins/Dockerfile:IMAGE_UUID=\"$(uuidgen)\""
# vulkan-tools:
#     - used to be vulkan-utils in 18.04 ubuntu
#     - usefull to test that docker image can initialize vulkan with `vulkaninfo`
#     - but it doesn't work without mesa-vulkan-drivers, so install it only to verify Vulkan
# zip:
#     - is needed by build-simulator.sh script:
#       Jenkins/build-simulator.sh:zip -r /mnt/${BUILD_OUTPUT}.zip ${BUILD_OUTPUT}

RUN set -ex \
  && apt-get update \
  && DEBIAN_FRONTEND=noninteractive apt-get install --no-install-recommends -y \
    libvulkan1 \
    mesa-vulkan-drivers \
    osslsigncode \
    uuid-runtime \
    vulkan-tools \
    zip \
  && apt-get clean

# Don't use Xvfb and call Unity directly (we run it on servers with Xorg running and DISPLAY set), show /opt/unity/image-info-lgsvl.source content before calling Unity
RUN sed -i 's#xvfb-run -ae /dev/stdout "$UNITY_PATH/Editor/Unity" -batchmode "$@"#echo "Running Unity Editor from docker image:" \&\& cat /opt/unity/image-info-lgsvl.source \&\& echo "/opt/unity/Editor/Unity \"$@\"" \&\& /opt/unity/Editor/Unity "$@"#g' /usr/bin/unity-editor

# NB. This is overwritten when launched by docker with --gpus=N option
# or Kubernetes with resources.limits.nvidia.com/gpu=N XXX <= confirm not overridden by K8s if not specified.
ENV NVIDIA_VISIBLE_DEVICES all
# Including "utility" to get nvidia-smi
ENV NVIDIA_DRIVER_CAPABILITIES graphics,display,utility

ADD "https://gitlab.com/nvidia/container-images/vulkan/raw/master/nvidia_icd.json" /etc/vulkan/icd.d/nvidia_icd.json
RUN chmod 644 /etc/vulkan/icd.d/nvidia_icd.json

ARG UNITY_VERSION=(unknown)
ARG EDITOR_DOCKER_IMAGE=(unknown)
ARG IMAGE_TAG=(unknown)
ARG image_git_describe=(unknown)

RUN /bin/echo -e "IMAGE_APP=\"unity-editor-simulator\"\n\
IMAGE_CREATED_BY=\"Jenkinsfile\"\n\
IMAGE_CREATED_FROM=\"${image_git_describe}\"\n\
IMAGE_CREATED_ON=\"$(date --iso-8601=seconds --utc)\"\n\
IMAGE_UNITY_VERSION=\"${UNITY_VERSION}\"\n\
IMAGE_EDITOR_DOCKER_IMAGE=\"${EDITOR_DOCKER_IMAGE}\"\n\
IMAGE_TAG=\"${IMAGE_TAG}\"\n\
# Increment IMAGE_INTERFACE_VERSION whenever changes to the image require that the launcher be updated.\n\
IMAGE_INTERFACE_VERSION=\"1\"\n\
IMAGE_UUID=\"$(uuidgen)\"" \
  >> /opt/unity/image-info-lgsvl.source \
  && echo "unity image-info-lgsvl.source:" \
  && cat /opt/unity/image-info-lgsvl.source
