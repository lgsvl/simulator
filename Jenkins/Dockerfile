# To build for multiple different platforms with the same docker image
# you need to rebuild this image wich https://github.com/game-ci/docker/pull/116
# changes and e.g. for 2020.3.3f1 version with linux, windows, android and mac support:
# --build-arg version="2020.3.3f1"
# --build-arg changeSet="76626098c1c4"                # you can find changeSet value in release announcement, e.g. https://unity3d.com/unity/whats-new/2020.3.3 has "Unity Hub" link unityhub://2020.3.3f1/76626098c1c4
# --build-arg module="windows-mono android mac-mono"  # linux support is already included by default
# use an image with only windows-mono support as an example until the images with multiple modules are available on dockerhub
ARG EDITOR_DOCKER_IMAGE=unityci/editor:ubuntu-2020.3.3f1-windows-mono-0.13.0
FROM ${EDITOR_DOCKER_IMAGE}

# install dependencies

RUN set -ex \
  && apt-get update \
  && DEBIAN_FRONTEND=noninteractive apt-get install --no-install-recommends -y \
      zip ca-certificates osslsigncode \
      uuid-runtime \
  && apt-get clean

# NB. This is overwritten when launched by docker with --gpus=N option
# or Kubernetes with resources.limits.nvidia.com/gpu=N XXX <= confirm not overridden by K8s if not specified.
ENV NVIDIA_VISIBLE_DEVICES all
# Including "utility" to get nvidia-smi
ENV NVIDIA_DRIVER_CAPABILITIES graphics,display,utility

ADD "https://gitlab.com/nvidia/container-images/vulkan/raw/master/nvidia_icd.json" /etc/vulkan/icd.d/nvidia_icd.json
RUN chmod 644 /etc/vulkan/icd.d/nvidia_icd.json

ARG UNITY_VERSION=(unknown)
ARG UNITY_CHANGESET=(unknown)
ARG UNITY_MODULES=(unknown)
ARG EDITOR_DOCKER_IMAGE=(unknown)
ARG IMAGE_TAG=(unknown)
ARG image_git_describe=(unknown)

RUN /bin/echo -e "IMAGE_APP=\"unity-editor\"\n\
IMAGE_CREATED_BY=\"Jenkinsfile\"\n\
IMAGE_CREATED_FROM=\"${image_git_describe}\"\n\
IMAGE_CREATED_ON=\"$(date --iso-8601=seconds --utc)\"\n\
IMAGE_UNITY_VERSION=\"${UNITY_VERSION}\"\n\
IMAGE_UNITY_CHANGESET=\"${UNITY_CHANGESET}\"\n\
IMAGE_UNITY_MODULES=\"${UNITY_MODULES}\"\n\
IMAGE_EDITOR_DOCKER_IMAGE=\"${EDITOR_DOCKER_IMAGE}\"\n\
IMAGE_TAG=\"${IMAGE_TAG}\"\n\
# Increment IMAGE_INTERFACE_VERSION whenever changes to the image require that the launcher be updated.\n\
IMAGE_INTERFACE_VERSION=\"1\"\n\
IMAGE_UUID=\"$(uuidgen)\"" \
  >> /opt/unity/image-info-lgsvl.source \
  && echo "unity image-info-lgsvl.source:" \
  && cat /opt/unity/image-info-lgsvl.source
