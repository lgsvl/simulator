This folder contains files that are used docker based build on Jenkins CI.

If you want to use it on your own, follow these steps:

1. Build the docker image:

    ```
    docker-compose build
    ```

2. Set needed environment variables and , then run the build with following command:

    ```
    export UID
    export UNITY_USERNAME=...
    export UNITY_PASSWORD=...
    export UNITY_SERIAL=...
    docker-compose run auto-sim-build
    ```

    export UID command will make docker container to run under your user, not as root.

    The output should be two zip files - one for Windows, one for Linux build.

    Optionally, set `BUILD_NUMBER` and `GIT_COMMIT` environment variables - they will be
    used to set zip filename and version information accordingly. These variables are
    automatically set by Jenkins build.
