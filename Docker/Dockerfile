# install dependencies

FROM ubuntu:18.04 AS base

RUN set -ex \
  && apt-get update \
  && DEBIAN_FRONTEND=noninteractive apt-get upgrade -y \
  && DEBIAN_FRONTEND=noninteractive apt-get install --no-install-recommends -y \
    jq \
    wget \
    unzip \
    libgl1 \
    libxau6 \
    libxcb1 \
    libx11-6 \
    libxext6 \
    libxdmcp6 \
    libvulkan1 \
    libgtk2.0-0 \
    ca-certificates \
  && apt-get clean

ENV NVIDIA_VISIBLE_DEVICES all
ENV NVIDIA_DRIVER_CAPABILITIES graphics,display

ADD "https://gitlab.com/nvidia/container-images/vulkan/raw/master/nvidia_icd.json" /etc/vulkan/icd.d/nvidia_icd.json

# unzip LGSVL Simulator

FROM base AS unzipper

RUN set -ex \
  && export LGSVL_SIMULATOR_VERSION=`wget -q -O- https://api.github.com/repos/lgsvl/simulator/releases/latest | jq -r '.tag_name'` \
  && wget -q -O /tmp/lgsvlsimulator.zip "https://github.com/lgsvl/simulator/releases/download/${LGSVL_SIMULATOR_VERSION}/lgsvlsimulator-linux64-${LGSVL_SIMULATOR_VERSION}.zip" \
  && cd /tmp \
  && unzip "lgsvlsimulator.zip" \
  && mv "lgsvlsimulator-linux64-${LGSVL_SIMULATOR_VERSION}" lgsvlsimulator \
  && rm /tmp/lgsvlsimulator.zip

# final image

FROM base

COPY --from=unzipper /tmp/lgsvlsimulator /opt/lgsvlsimulator

CMD /opt/lgsvlsimulator/simulator
