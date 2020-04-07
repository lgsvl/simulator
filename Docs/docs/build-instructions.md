# Instructions to build standalone executable

When a terminal is mentioned, it refers to:

`cmd.exe` on Windows

`Terminal` on Ubuntu

1. Download and Install [Unity Hub](https://unity3d.com/get-unity/download)
    - Ubuntu: You may need to allow the downloaded AppImage to be run as an executable
        - Right-click the AppImage
        - Select `Properties`
        - Go to the `Permissions` tab
        - Check `Allow executing file as program`
        - Alternatively, in the terminal run `sudo chmod +x UnityHub.AppImage`

2. Download and Install Unity 2019.3.3f1:
    - **IMPORTANT** include Mono support for both Windows and Linux when installing Unity
    - (Optional) include support for Visual Studio for easier debugging
    - [Unity Download Archive](https://unity3d.com/get-unity/download/archive)
    - Windows
        - Click the `Unity Hub` button to have Unity Hub handle the installation process
    - Ubuntu
        - Right click the `Unity Hub` button and `Copy Link Address`
        - In a terminal `PATH_TO_UNITY_HUB COPIED_LINK`
            - The copied link will be in the form `unityhub://Unity-VERSION/XXXXXX` (e.g. `unityhub://2019.3.3f1/7ceaae5f7503`)
        - Unity Hub will open and guide you through the installation of Unity Editor
    - Verify installation
        - Under the `Installs` tab of `Unity Hub` there should be the expected version shown. In the bottom-left corner of the version, there should be an icon of the other OS (e.g. on a Linux computer, the Windows logo will be shown)

3. Download and Install [Node.js](https://nodejs.org/en/)
    - Version 12.16.1 LTS is fine
    - Windows
        - Download and run the `.msi`
    - Ubuntu
        - The instructions are from the [NodeJS Github](https://github.com/nodesource/distributions/blob/master/README.md)
            - `curl -sL https://deb.nodesource.com/setup_12.x | sudo -E bash -`
            - `sudo apt-get install -y nodejs`
    - Verify installation
        - Open a terminal and type `node --version`
        - `v12.16.1` should print out

4. Make sure you have [git-lfs](https://git-lfs.github.com/) installed **before cloning the Simulator repository**. 
    - Instructions for installation are [here](https://help.github.com/en/articles/installing-git-large-file-storage)
    - Verify installation
        - In a terminal enter `git lfs install`
        - `> Git LFS initialized.` should print out

5. Clone simulator from GitHub
    - Open a terminal and navigate to where you want the Simulator to be downloaded to
        - e.g. If you want the Simulator in your `Documents` folder, use `cd` in the terminal so that the input for the terminal is similar to `/Documents$ `
    - `git clone --single-branch https://github.com/lgsvl/simulator.git`
    - Verify download
        - The `git clone` will create a `Simulator` folder
        - Open a File Explorer and navigate to where the `Simulator` folder is
        - Navigate to `Simulator/Assets/Materials/EnvironmentMaterials/`
        - There should be a `EnvironmentDamageAlbedo.png` in this folder
        - Open the image, it should be a mostly grey square that looks like concrete
        - If the image cannot be opend, `Git LFS` was not installed before cloning the repository
            - Install `Git LFS` following step 4
            - In a terminal, navigate to the `Simulator` folder so that the terminal is similar to `/Simulator$ `
            - `git lfs pull`
            - Check the image again


6. Run Unity Hub

7. In the `Projects` tab, click `Add` and select the `Simulator` folder that was created by `git clone` in Step 5

8. In the `Projects` tab, verify that the Simulator is using Unity Version 2019.3.3f1 from the dropdown

9. Double-click the name of the project to launch Unity Editor

10. Open a terminal and navigate to the `WebUI` folder of the Simulator project
    - Window ex. `C:\Users\XXX\Documents\Simulator\WebUI`
    - Linux ex. `/home/XXX/Projects/Simulator/WebUI`

11. In the terminal run `npm install` to install dependencies, do this only once or if dependencies change inside packages.json file
    - The output will be similar to below

    [![](images/npm-install.png)](images/npm-install.png)

12. In Unity Editor run `Build WebUi...` in `Unity`: `Simulator` -> `Build WebUI...`

    [![](images/build-webui.png)](images/full_size_images/build-webui.png)

13. Open `Build...` in `Unity`: `Simulator` -> `Build...`

    [![](images/build-window.png)](images/full_size_images/build-window.png)

14. Check the Environments, Vehicles, and Sensors that should be generated as AssetBundles
    - The `Simulator` repository does not contain any Environments, Vehicles, or Sensors. They are separate repositories on GitHub
    - See [assets documentation](assets.md) for information on how to add Environments and Vehicles
    - They will be located in a folder called `AssetBundles` in the folder selected as the build location 
    - These may also be built separately from the Simulator. In this case they will be put into `Simulator/AssetBundles` folder of the project

15. (Optional) Click `Build` to only build the AssetBundles. Load the `LoaderScene.unity` and click the Play button at the top of the editor to start the simulator.

16. Select the `Target OS` for the build
    - This is only used when building the Simulator. AssetBundles are built for Linux and Windows automatically

17. Verify `Build Simulator` is checked for the Simulator to be built

18. Select a folder that the simulator will be built in

19. (Optional) Check `Development Build` to create a [Development Build](https://docs.unity3d.com/ScriptReference/BuildOptions.Development.html)

20. Click `Build`
    - **NOTE** You will get an error when building AssetBundles if either Windows or Linux support is not installed in Unity
        - Open `Unity Hub` and go to the `Installs` tab
        - Click the vertical 3-dots next to the Unity version of the Simulator
        - Click `Add Modules`
        - Check `Windows Build Support (Mono)` or `Linux Build Support (Mono)`
        - Click `Done`


### Test Simulator <sup><sub>[top](#instructions-to-build-standalone-executable)</sub></sup> {: #test-simulator data-toc-label='Test Simulator'}

1. Ubuntu - Install Vulkan userspace library

    `sudo apt-get install libvulkan1`

2. Double-click the `Simulator.exe` that was built

3. Select graphics options then press `Ok`

4. Click `Open Browser`

5. In the Maps tab, `Add new` map with the URL to an environment assetbundle
- ex. `C:\Users\XXX\Desktop\Simulator\AssetBundles\environment_borregasave`

6. In the Vehicles tab, `Add new` vehicle with the URL to a vehicle assetbundle
- ex. `C:\Users\XXX\Desktop\Simulator\AssetBundles\vehicle_jaguar2015xe`

7. (Optional) Add a manual control "sensor" to the vehicle to enable driving
   1. Click the wrench icon next to the vehicle name
   2. In the text box insert `[{"type": "Manual Control", "name": "Manual Car Control"}]`

7. In the Simulations tab, `Add new` simulation with the added map and vehicle

8. Press the Play button

9. The Unity window should now show a vehicle in the built environment
