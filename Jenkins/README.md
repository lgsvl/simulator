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

## Running file/folder check

```
docker-compose run --rm buid-simulator check
```

Output will be in `lgsvlsimulator-check.html` file.

## Running unit tests

```
docker-compose run --rm buid-simulator test
```

Output will be in `lgsvlsimulator-test.xml` file in NUnit v3 format

## Running binary build

```
docker-compose run --rm buid-simulator windows
```

Output will be `lgsvlsimulator-windows.zip` file.
Replace `windows` with `linux` or `macos` to build for other OS'es.

## Running build for Asset Bundles

```
docker-compose run --rm buid-bundles
```

Output will be in AssetBundles folder.

# Jenkins setup

To setup Pipeline CI job on jenkins following global environment variables are required:

* `UNITY_USERNAME` - Unity username for license
* `UNITY_PASSWORD` - Unity username for password
* `UNITY_SERIAL` - Unity username for serial
* `GITLAB_HOST` - hostname of GitLab instance, ex: `gitlab.example.com`
* `DOCKER_IMAGE_NAME` - name of Docker image, ex: `gitlab.example.com:4567/hdrp/simulator`
* `SIMULATOR_ENVIRONMENTS` - comma separated list of environment bundles to build, ex: `CubeTown,SanFrancisco`
* `SIMULATOR_VEHICLES` - comma separated list of vehicle bundles to build, ex: `Car1,Car2`
* `AWS_ACCESS_KEY_ID` - AWS access key
* `AWS_SECRET_ACCESS_KEY` - AWS secret key
* `S3_BUCKET_NAME` - AWS S3 bucket name to where upload bundles
* `SIMULATOR_STAGING_CLOUD_URL` - staging URL to use for cloud access, used only for non-release job
* `SIMULATOR_RELEASE_EMAILS` - comma separated e-mails where to send start/finish e-mails abour release job

Following credentials must be set up in Jenkins:

* `auto-gitlab` - ssh key for cloning git repositories, this key must have access to HDRP repositories
* `Jenkins-Gitlab` - username/password combo for Docker registry on GitLab where to push Simulator docker image

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

* `lgsvlsimulator-check-BRANCH-NUM.html`
* `lgsvlsimulator-OS-BRANCH-NUM.zip` - for each OS selected for building
