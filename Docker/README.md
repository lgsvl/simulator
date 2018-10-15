This folder contains files that are used docker based build on Jenkins CI.

If you want to use it on your own, follow these steps:

1. Build the docker image:

    ```
    docker-compose build
    ```

2. Activate Unity3D (when running on new host, do this only once):

    ```
    docker-compose run auto-sim-activate
    ```

    This will store activation information in `~/.cache/auto-sim-unity` folder.

3. Run the actual build:

    ```
    export UID
    docker-compose run auto-sim-build
    ```

    export command will make docker container to run under your user, not as root.

    The output should be two zip files - one for Windows, one for Linux build.

    Optionally, set `BUILD_NUMBER` and `GIT_COMMIT` environment variables - they will be
    used to set zip filename and version information accordingly. These variables are
    automatically set by Jenkins build.
