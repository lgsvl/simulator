FROM ubuntu:18.04

# install dependencies

RUN set -ex \
  && apt-get update \
  && DEBIAN_FRONTEND=noninteractive apt-get install --no-install-recommends -y \
  curl zip ca-certificates osslsigncode \
        gconf-service lib32gcc1 lib32stdc++6 libasound2 libc6 libc6-i386 libcairo2 libcap2 libcups2 \
        libdbus-1-3 libexpat1 libfontconfig1 libfreetype6 libgcc1 libgconf-2-4 libgdk-pixbuf2.0-0 \
        libgl1 libglib2.0-0 libglu1 libgtk2.0-0 libgtk-3-0 libnspr4 libnss3 libpango1.0-0 libstdc++6 \
        libx11-6 libxcomposite1 libxcursor1 libxdamage1 libxext6 libxfixes3 libxi6 libxrandr2 \
        libxrender1 libxtst6 zlib1g debconf npm libsoup2.4-1 libarchive13 libpng16-16 \
        libvulkan1 libxau6 libxcb1 libxdmcp6 systemd lsb-release \
  && apt-get clean

# NB. This is overwritten when launched by docker with --gpus=N option
# or Kubernetes with resources.limits.nvidia.com/gpu=N XXX <= confirm not overridden by K8s if not specified.
ENV NVIDIA_VISIBLE_DEVICES all
# Including "utility" to get nvidia-smi
ENV NVIDIA_DRIVER_CAPABILITIES graphics,display,utility

ADD "https://gitlab.com/nvidia/container-images/vulkan/raw/master/nvidia_icd.json" /etc/vulkan/icd.d/nvidia_icd.json
RUN chmod 644 /etc/vulkan/icd.d/nvidia_icd.json

RUN curl -fLo UnitySetup https://beta.unity3d.com/download/59ff3e03856d/UnitySetup-2019.3.15f1 \
    && chmod +x UnitySetup \
    && yes | ./UnitySetup --unattended --install-location=/opt/Unity --components=Unity,Windows-Mono,Mac-Mono \
    && rm UnitySetup
