<h1 align="center">SVL Simulator:  An Autonomous Vehicle Simulator</h1>

<div align="center">
<a href="https://github.com/lgsvl/simulator/releases/latest">
<img src="https://img.shields.io/github/release-pre/lgsvl/simulator.svg" alt="Github release" /></a>
<a href="">
<img src="https://img.shields.io/github/downloads/lgsvl/simulator/total.svg" alt="Release downloads" /></a>
</div>
<div align="center">
  <h4>
    <a href="https://svlsimulator.com" style="text-decoration: none">
    Website</a>
    <span> | </span>
    <a href="https://svlsimulator.com/docs" style="text-decoration: none">
    Documentation</a>
    <span> | </span>
    <a href="https://github.com/lgsvl/simulator/releases/latest" style="text-decoration: none">
    Download</a>
  </h4>
</div>

## Stay Informed

Check out our latest [news](https://www.svlsimulator.com/news/) and subscribe to our [mailing list](http://eepurl.com/htlRjH) to get the latest updates.


## Introduction

LG Electronics America R&D Lab has developed an HDRP Unity-based multi-robot simulator for autonomous vehicle developers. 
We provide an out-of-the-box solution which can meet the needs of developers wishing to focus on testing their autonomous vehicle algorithms. 
It currently has integration with The Autoware Foundation's [Autoware.auto](https://gitlab.com/autowarefoundation/autoware.auto/AutowareAuto) and Baidu's [Apollo](https://github.com/ApolloAuto/apollo) platforms, can generate HD maps, and can be immediately used for testing and validation of a whole system with little need for custom integrations. 
We hope to build a collaborative community among robotics and autonomous vehicle developers by open sourcing our efforts. 

*To use the simulator with Apollo 6.0 or master, first download the simulator binary, then follow our [Running with latest Apollo](https://www.svlsimulator.com/docs/system-under-test/apollo-master-instructions/) docs.*

*To use the simulator with Autoware.auto, first download the simulator binary, then follow the guide on our [Autoware.auto](https://autowarefoundation.gitlab.io/autoware.auto/AutowareAuto/lgsvl.html).*

For users in China, you can view our latest videos [here](https://space.bilibili.com/412295691) and download our simulator releases [here](https://pan.baidu.com/s/1M33ysJYZfi4vya41gmB0rw) (code: 6k91).
对于中国的用户，您也可在[哔哩哔哩](https://space.bilibili.com/412295691)上观看我们最新发布的视频，从[百度网盘](https://pan.baidu.com/s/1M33ysJYZfi4vya41gmB0rw)(提取码: 6k91)上下载使用我们的仿真器。


## Paper
If you are using SVL Simulator for your research paper, please cite our ITSC 2020 paper:
[LGSVL Simulator: A High Fidelity Simulator for Autonomous Driving](https://arxiv.org/pdf/2005.03778)

```
@article{rong2020lgsvl,
  title={LGSVL Simulator: A High Fidelity Simulator for Autonomous Driving},
  author={Rong, Guodong and Shin, Byung Hyun and Tabatabaee, Hadi and Lu, Qiang and Lemke, Steve and Mo{\v{z}}eiko, M{\=a}rti{\c{n}}{\v{s}} and Boise, Eric and Uhm, Geehoon and Gerow, Mark and Mehta, Shalin and others},
  journal={arXiv preprint arXiv:2005.03778},
  year={2020}
}
```



## Getting Started

You can find complete and the most up-to-date guides on our [documentation website](https://www.svlsimulator.com/docs).

Running the simulator with reasonable performance and frame rate (for perception related tasks) requires a high performance desktop. Below is the recommended system for running the simulator at high quality. We are currently working on performance improvements for a better experience. 

**Recommended system:**

- 4 GHz Quad Core CPU
- Nvidia GTX 1080, 8GB GPU memory
- Windows 10 64 Bit

The easiest way to get started with running the simulator is to download our [latest release](https://github.com/lgsvl/simulator/releases/latest) and run as a standalone executable.

Currently, running the simulator in Windows yields better performance than running on Linux. 

### Downloading and starting simulator

1. Download the latest release of the SVL Simulator for your supported operating system (Windows or Linux) here: [https://github.com/lgsvl/simulator/releases/latest](https://github.com/lgsvl/simulator/releases/latest)
2. Unzip the downloaded folder and run the executable.

See the full installation guide [here](https://svlsimulator.com/docs/installation-guide/installing-simulator).

### Building and running from source

If you would like to customize the simulator, build simulation content, or access specific features available in [Developer Mode](https://www.svlsimulator.com/docs/running-simulations/developer-mode), you can clone the project with Unity Editor, and build the project from source.

Check out our instructions for getting started with building from source [here](https://www.svlsimulator.com/docs/installation-guide/build-instructions).
**Note:** Please checkout the "release-*" branches or release tags for stable (ready features) and "master" branch for unstable (preview of work in progress).


## Simulator Instructions (from 2021.1 release onwards)

1. After starting the simulator, you should see a button to "Link to Cloud".
2. Use this button to link your local simulator instance to a [cluster](https://www.svlsimulator.com/docs/user-interface/web/clusters-tab) on our [web user interface](https://wise.svlsimulator.com).
3. Now create a [random traffic](https://www.svlsimulator.com/docs/creating-scenarios/random-traffic-scenarios/) simulation. For a standard setup, select "BorregasAve" for map and "Jaguar2015XE with Apollo 5.0 sensor configuration" for vehicle. Click "Run" to begin.
4. The vehicle should spawn inside the map environment that was selected.
5. Read [here](https://www.svlsimulator.com/docs/user-interface/keyboard-shortcuts/) for an explanation of all current keyboard shortcuts and controls.
6. Follow the guides on our respective [Autoware](https://github.com/lgsvl/Autoware) and [Apollo 5.0](https://github.com/lgsvl/apollo-5.0) repositories for instructions on running the platforms with the simulator.

**NOTE**: If using a release older than "2021.1", please follow the instructions on our documentation [archives](https://www.svlsimulator.com/docs/archive/).

### Guide to simulator functionality

Look [here](https://www.svlsimulator.com/docs) for a guide to currently available functionality and features.



## Contact

Please feel free to provide feedback or ask questions by creating a Github issue. For inquiries about collaboration, please email us at [contact@svlsimulator.com](mailto:contact@svlsimulator.com).



## Copyright and License

Copyright (c) 2019-2021 LG Electronics, Inc.

This software contains code licensed as described in LICENSE.
