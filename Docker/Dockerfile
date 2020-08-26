ARG base_image=ubuntu:18.04
FROM ${base_image} AS base

# Install dependencies of simulator
RUN set -ex \
  && apt-get update \
  && DEBIAN_FRONTEND=noninteractive apt-get install --no-install-recommends -y \
    ca-certificates \
    libgl1 \
    libgtk2.0-0 \
    libvulkan1 \
    libx11-6 \
    libxau6 \
    libxcb1 \
    libxdmcp6 \
    libxext6 \
  && apt-get clean

# NB. This is overwritten when launched by docker with --gpus=N option
ENV NVIDIA_VISIBLE_DEVICES all
# Include "utility" to get nvidia-smi
ENV NVIDIA_DRIVER_CAPABILITIES graphics,display,utility

ADD "https://gitlab.com/nvidia/container-images/vulkan/raw/master/nvidia_icd.json" /etc/vulkan/icd.d/nvidia_icd.json

# Build patched libvulkan1
ARG base_image=ubuntu:18.04
FROM ${base_image} AS vulkan_loader

# No need cleanup as this layer will be discarded.
RUN set -ex \
  && apt-get update \
  && DEBIAN_FRONTEND=noninteractive apt-get install --no-install-recommends -y \
    build-essential \
    ca-certificates \
    cmake \
    git \
    libx11-xcb-dev \
    libxkbcommon-dev \
    libwayland-dev  \
    libxrandr-dev \
    pkg-config \
    python

ARG vulkan_loader_version=sdk-1.2.131.2
ADD https://github.com/KhronosGroup/Vulkan-Loader/archive/${vulkan_loader_version}.tar.gz /tmp

ADD 0001-loader.c-Implement-VK_LOADER_SINGLE_PHYSDEV_INDEX_LG.patch /tmp

RUN set -ex \
  && cd /tmp \
  && tar -xzf ${vulkan_loader_version}.tar.gz \
  && cd Vulkan-Loader-${vulkan_loader_version} \
  && patch -p1 < ../0001-loader.c-Implement-VK_LOADER_SINGLE_PHYSDEV_INDEX_LG.patch \
  && mkdir build \
  && cd build \
  && ../scripts/update_deps.py \
  && cmake -C helper.cmake -DCMAKE_BUILD_TYPE=Release -DCMAKE_INSTALL_PREFIX=/tmp .. \
  && cmake --build . \
  && make install \
  && cd /tmp/lib \
  && : Add a link to indicate that it is our version \
  && ln -s $(readlink libvulkan.so) libvulkan.so.${vulkan_loader_version}+lge1 \
  && : COPY does not copy symlinks, so extract from a tarball. libvulkan.so is only needed for development, so do not include it \
  && tar -cvf /tmp/libvulkan.tar libvulkan.so.*

# Unzip LGSVL Simulator
ARG base_image=ubuntu:18.04
FROM ${base_image} AS unzipper

# No need to cleanup as this build stage will be discarded
RUN set -ex \
  && apt-get update \
  && DEBIAN_FRONTEND=noninteractive apt-get install --no-install-recommends -y \
    ca-certificates \
    jq \
    unzip \
    wget

ARG simulator_zipfile
ARG simulator_version=latest
ARG simulator_url

# We have to copy something ...
COPY ${simulator_zipfile:-Dockerfile} /tmp/

RUN set -ex \
  && cd /tmp \
  && : If simulator_zipfile is set, then skip downloading from simulator_url but rename the .zip as if it had been \
  && if [ -e Dockerfile ]; then rm Dockerfile; else mv *.zip lgsvlsimulator.zip; exit 0; fi  \
  && if [ "${simulator_url}" = "" ]; then version=${simulator_version}; else version=":ignored" ; fi \
  && if [ $version = "latest" ]; then version=$(wget -q -O- https://api.github.com/repos/lgsvl/simulator/releases/latest | jq -r '.tag_name'); else true; fi \
  && url=${simulator_url} \
  && if [ "$url" = "" ]; then url="https://github.com/lgsvl/simulator/releases/download/${version}/lgsvlsimulator-linux64-${version}.zip"; else true; fi \
  && wget -q -O lgsvlsimulator.zip $url

RUN set -ex \
  && cd /tmp \
  && unzip lgsvlsimulator.zip \
  && mv lgsvlsimulator-linux64-* lgsvlsimulator

# Final image
FROM base

# Add our libvulkan, removing any that might already be there.
COPY --from=vulkan_loader /tmp/libvulkan.tar /tmp/
RUN set -ex \
  && rm -f /usr/lib/x86_64-linux-gnu/libvulkan.* \
  && tar -xvf /tmp/libvulkan.tar -C /usr/lib/x86_64-linux-gnu \
  && rm -f /tmp/libvulkan.tar

COPY --from=unzipper /tmp/lgsvlsimulator /opt/simulator

CMD /opt/simulator/simulator
