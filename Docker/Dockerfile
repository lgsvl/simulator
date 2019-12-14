# install dependencies

FROM ubuntu:18.04 AS base

RUN set -ex \
  && apt-get update \
  && DEBIAN_FRONTEND=noninteractive apt-get upgrade -y \
  && DEBIAN_FRONTEND=noninteractive apt-get install --no-install-recommends -y \
    unzip \
    ca-certificates \
    libx11-6 \
    libxau6 \
    libxcb1 \
    libxdmcp6 \
    libxext6 \
    libvulkan1 \
    libgl1 \
  && apt-get clean

ENV NVIDIA_VISIBLE_DEVICES all
ENV NVIDIA_DRIVER_CAPABILITIES graphics,display

ADD "https://gitlab.com/nvidia/container-images/vulkan/raw/master/nvidia_icd.json" /etc/vulkan/icd.d/nvidia_icd.json

# unzip LGSVL Simulator

FROM base AS unzipper

ARG LGSVL_SIMULATOR_VERSION=2019.11
ADD "https://github.com/lgsvl/simulator/releases/download/${LGSVL_SIMULATOR_VERSION}/lgsvlsimulator-linux64-${LGSVL_SIMULATOR_VERSION}.zip" /tmp/lgsvlsimulator.zip

RUN set -ex \
  && cd /tmp \
  && unzip "lgsvlsimulator.zip" \
  && mv "lgsvlsimulator-linux64-${LGSVL_SIMULATOR_VERSION}" lgsvlsimulator

# final image

FROM base

COPY --from=unzipper /tmp/lgsvlsimulator /opt/lgsvlsimulator

CMD /opt/lgsvlsimulator/simulator