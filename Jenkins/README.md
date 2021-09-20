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
export UNITY_VERSION="2020.3.3f1"
export UNITY_DOCKER_IMAGE="hdrp/unityci/editor-lgsvl"
export UNITY_DOCKER_IMAGE_TAG="2020.3.3f1-v0.15-4-g07a8336__build__30"
export PYTHONUNBUFFERED=1
export UID
export CODE_SIGNING_FILE=/dev/urandom
export FORCE_DEV_BUILD=true
```

## Running file/folder check

```
docker-compose run --rm build-simulator check
```

Output will be in `svlsimulator-check.html` file.

## Running unit tests

```
docker-compose run --rm build-simulator test
```

Output will be in `svlsimulator-test.xml` file in NUnit v3 format

## Running binary build

```
docker-compose run --rm build-simulator windows
```

Output will be `svlsimulator-windows.zip` file.
Replace `windows` with `linux` or `macos` to build for other OS'es.

## Running build for Asset Bundles

```
docker-compose run --rm build-bundles
```

Output will be in AssetBundles folder.

# Jenkins setup

To setup Pipeline CI job on jenkins following global environment variables are required:

* `UNITY_USERNAME` - Unity username for license
* `UNITY_PASSWORD` - Unity username for password
* `UNITY_SERIAL` - Unity username for serial
* `UNITY_VERSION` - Version of Unity, ex: `2020.3.3f1`
* `GITLAB_HOST` - hostname of GitLab instance, ex: `gitlab.example.com`
* `UNITY_DOCKER_IMAGE` - name of Docker image, ex: `gitlab.example.com:4567/hdrp/simulator`
* `UNITY_DOCKER_IMAGE_TAG` - tag of Docker image with Unity, ex: `2020.3.3f1-v0.15-4-g07a8336__build__30`
* `SIMULATOR_ENVIRONMENTS` - comma separated list of environment bundles to build, ex: `CubeTown,SanFrancisco`
* `SIMULATOR_VEHICLES` - comma separated list of vehicle bundles to build, ex: `Car1,Car2`
* `S3_BUCKET_NAME` - AWS S3 bucket name to where upload bundles
* `SIMULATOR_STAGING_CLOUD_URL` - staging URL to use for cloud access, used only for non-release job
* `SIMULATOR_RELEASE_EMAILS` - comma separated e-mails where to send start/finish e-mails abour release job

Following credentials must be set up in Jenkins:

* `auto-gitlab` - ssh key for cloning git repositories, this key must have access to HDRP repositories
* `auto-gitlab-docker-registry` - username/password combo for Docker registry on GitLab where to push Simulator docker image
* `dockerhub-docker-registry` - username/password combo for default Docker registry from docker deamon (usually dockerhub https://hub.docker.com/)
* `s3--aws-credentials` - credentials to upload assets to s3
* `s3-release--aws-credentials` - credentials to upload assets to release s3

Pipeline requires following parameters available:

* `BUILD_WINDOWS` - boolean param, with value "true" if Windows binary needs to be built
* `BUILD_LINUX` - boolean param, with value "true" if Linux binary needs to be built
* `BUILD_MACOS` - boolean param, with value "true" if macOS binary needs to be built
* `CLOUD_URL` - string param, if non-empty then the value will be used for cloud access
* `LGSVL_CODE_SIGNING_FILE` - secret file containing code signing key/certificate for Authenticode
* `LGSVL_CODE_SIGNING_PASSWORD` - if nonempty then value of this variable will be used as password to
    code signing key from `LGSVL_CODE_SIGNING_FILE` secret file credential

It will automatically upload asset bundles to AWS S3 bucket when `master` branch is built.

Job will produce following artifacts:

* `svlsimulator-check-BRANCH-NUM.html`
* `svlsimulator-OS-BRANCH-NUM.zip` - for each OS selected for building
