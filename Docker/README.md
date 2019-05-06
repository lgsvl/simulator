# Files for running Docker builds and Jenkins CI

# Requirements

* [Docker](https://docs.docker.com/install/)
* [docker-compose](https://docs.docker.com/compose/install/)
* [nvidia-docker](https://github.com/NVIDIA/nvidia-docker) runtime
* Pro Unity license

# Running

To run builds locally you run them via docker-compose.

First export necessary environment variables:

```
export UNITY_USERNAME=...
export UNITY_PASSWORD=...
export UNITY_SERIAL=...
export DOCKER_IMAGE_NAME=hdrp/simulator
export PYTHONUNBUFFERED=1
export UID
```

## Building WebUI

```
docker-compose run --rm build-webui
```

This will create `WebUI/dist/*` files.

## Running file/folder check

```
docker-compose run --rm buid-simulator check
```

Output will be in `lgsvlsimulator-check.html` file.

## Running binary build

```
docker-compose run --rm buid-simulator windows
```

Output will be `lgsvlsimulator-windows.zip` file
Replace `windows` with `linux` or `macos` to build for other OS'es.

# Jenkins setup

To setup Pipeline CI job on jenkins following global environment variables are required:

* `UNITY_USERNAME` - Unity username for license
* `UNITY_PASSWORD` - Unity username for password
* `UNITY_SERIAL` - Unity username for serial
* `GITLAB_HOST` - hostname of GitLab instance, ex: `gitlab.example.com`
* `DOCKER_IMAGE_NAME` - name of Docker image, ex: `gitlab.example.com:4567/hdrp/simulator`
* `SIMULATOR_ENVIRONMENTS` - comma separated list of environment bundles to build, ex: `CubeTown,SanFrancisco`
* `SIMULATOR_VEHICLES` - comma separated list of vehicle bundles to build, ex: `Car1,Car2`

Pipeline requires following parameters available:

* `BUILD_WINDOWS` - boolean param, with value "true" if Windows binary needs to be built
* `BUILD_LINUX` - boolean param, with value "true" if Linux binary needs to be built
* `BUILD_MACOS` - boolean param, with value "true" if macOS binary needs to be built
* `GIT_BRANCH` - branch name (in `origins/master` format) for which Simulator branch to build

Job will produce following artifacts:

* `lgsvlsimulator-check-BRANCH-NUM.html`
* `lgsvlsimulator-OS-BRANCH-NUM.zip` - for each OS selected for building
