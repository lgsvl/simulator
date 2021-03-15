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

Check out our latest [news](https://www.svlsimulator.com/news/) and subscribe to our [mailing list](http://eepurl.com/gpuhkb) to get the latest updates.


## Introduction

LG Electronics America R&D Lab has developed an HDRP Unity-based multi-robot simulator for autonomous vehicle developers. 
We provide an out-of-the-box solution which can meet the needs of developers wishing to focus on testing their autonomous vehicle algorithms. 
It currently has integration with The Autoware Foundation's [Autoware](https://github.com/lgsvl/Autoware), [Autoware.auto](https://gitlab.com/autowarefoundation/autoware.auto/AutowareAuto) and Baidu's [Apollo Master](https://github.com/ApolloAuto/apollo), [Apollo 6.0](https://github.com/ApolloAuto/apollo/releases/tag/v6.0.0), [Apollo 5.0](https://github.com/lgsvl/apollo-5.0) and [Apollo 3.0](https://github.com/lgsvl/apollo) platforms, can generate HD maps, and can be immediately used for testing and validation of a whole system with little need for custom integrations. 
We hope to build a collaborative community among robotics and autonomous vehicle developers by open sourcing our efforts. 

*To use the simulator with Apollo 5.0, first download the simulator binary, then follow the guide on our [Apollo 5.0 fork](https://github.com/lgsvl/apollo-5.0).*

*To use the simulator with Apollo 6.0 or master, first download the simulator binary, then follow our [Running with latest Apollo](https://www.svlsimulator.com/docs/system-under-test/apollo-master-instructions/) docs.*

*To use the simulator with Autoware, first download the simulator binary, then follow the guide on our [Autoware fork](https://github.com/lgsvl/Autoware).*

*To use the simulator with Autoware.auto, first download the simulator binary, then follow the guide on our [Autoware.auto](https://autowarefoundation.gitlab.io/autoware.auto/AutowareAuto/lgsvl.html).*

For Chinese-speaking users, you can also view our latest videos [here](https://space.bilibili.com/412295691) and download our simulator releases [here](https://pan.baidu.com/s/1M33ysJYZfi4vya41gmB0rw) (code: 6k91).
对于中国的用户，您也可在[哔哩哔哩](https://space.bilibili.com/412295691)上观看我们最新发布的视频，从[百度网盘](https://pan.baidu.com/s/1M33ysJYZfi4vya41gmB0rw)(提取码: 6k91)上下载使用我们的仿真器。

[![](Docs/docs/images/readme-frontal.png)](Docs/docs/images/full_size_images/readme-frontal.png)


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

For the latest functionality or if you want to modify the simulator for your own needs, you can checkout our source, open it as a project in Unity, and run inside the Unity Editor. Otherwise, you can build the Unity project into a standalone executable.

Currently, running the simulator in Windows yields better performance than running on Linux. 

### Downloading and starting simulator

1. Download the latest release of the SVL Simulator for your supported operating system (Windows or Linux) here: [https://github.com/lgsvl/simulator/releases/latest](https://github.com/lgsvl/simulator/releases/latest)
2. Unzip the downloaded folder and run the executable.

### Building and running from source

**NOTE**: to clone repository faster, clone only single branch.

To get latest code from master branch:

    git clone --single-branch https://github.com/lgsvl/simulator.git

Alternatively, you can get source code of specific release. Here is an example how to checkout by release tag `2020.03`

    git clone https://github.com/lgsvl/simulator.git
    cd simulator
    git checkout 2020.03

Check out our instructions for getting started with building from source [here](https://www.svlsimulator.com/docs/installation-guide/build-instructions).


## Simulator Instructions

1. After starting the simulator, you should see a button to "Link to Cloud".
2. Use this button to link your local simulator instance to a [cluster](https://www.svlsimulator.com/docs/user-interface/web/clusters-tab) on our [web user interface](https://wise.svlsimulator.com)
3. Now create a [random traffic](https://www.svlsimulator.com/docs/creating-scenarios/random-traffic-scenarios/) simulation. For a standard setup, select "BorregasAve" for map and "Jaguar2015XE with Apollo 5.0 sensor configuration" for vehicle. Click "Run" to begin.
4. The vehicle should spawn inside the map environment that was selected.
5. Read [here](https://www.svlsimulator.com/docs/user-interface/keyboard-shortcuts/) for an explanation of all current keyboard shortcuts and controls.
6. Follow the guides on our respective [Autoware](https://github.com/lgsvl/Autoware) and [Apollo 5.0](https://github.com/lgsvl/apollo-5.0) repositories for instructions on running the platforms with the simulator.

### Guide to simulator functionality

Look [here](https://www.svlsimulator.com/docs) for a guide to currently available functionality and features.



## Contact

Please feel free to provide feedback or ask questions by creating a Github issue. For inquiries about collaboration, please email us at [contact@svlsimulator.com](mailto:contact@svlsimulator.com).



## Copyright and License

Copyright (c) 2019-2021 LG Electronics, Inc.

This software contains code licensed as described in LICENSE.
