# Instructions to build standalone executable

1. Download and Install Unity Hub:
    - Ubuntu: <https://forum.unity.com/threads/unity-hub-v2-0-0-release.677485/>
    - Windows: <https://unity3d.com/get-unity/download>

2. Download and Install Unity 2019.1.10f1:
    - **IMPORTANT** include support for both Windows and Linux when installing Unity
    - (Optional) include support for Visual Studio for easier debugging
    - Ubuntu: <https://beta.unity3d.com/download/f007ed779b7a/UnitySetup-2019.1.10f1>
    - Windows: <https://unity3d.com/get-unity/download/archive>

3. Download and Install [Node.js](https://nodejs.org/en/)
    - Version 12.13.0 LTS is fine

4. Make sure you have [git-lfs](https://git-lfs.github.com/) installed **before cloning this repository**. 
    - Instructions for installation are [here](https://help.github.com/en/articles/installing-git-large-file-storage)
    - Verify installation with:

            $ git lfs install
            > Git LFS initialized.

5. Clone simulator from GitHub:

    ```
    git clone --single-branch https://github.com/lgsvl/simulator.git
    ```

6. Run Unity Hub

7. In the `Projects` tab, click `Add` and select the folder that the Simulator was cloned to

8. In the `Installs` tab, click `Locate` and choose the `Unity` launcher in the `Unity2019.1.10f1` folder

9. In the `Projects` tab, verify that the Simulator is using Unity Version 2019.1.10f1 from the dropdown

10. Double-click the name of the project to launch Unity Editor

11. Open a terminal window
    - `cmd.exe` on Windows
    - `Terminal` on Linux

12. Navigate to the `WebUI` folder of the Simulator project
    - Window ex. `C:\Users\XXX\Documents\Simulator\WebUI`
    - Linux ex. `/home/XXX/Projects/Simulator/WebUI`
    - Where `XXX` is the user profile

13. Run `npm install` to install dependencies, do this only once or if dependencies change inside packages.json file

14. Run `Build WebUi...` in `Unity`: `Simulator` -> `Build WebUI...`

    [![](images/build-webui.png)](images/full_size_images/build-webui.png)

15. Open `Build...` in `Unity`: `Simulator` -> `Build...`

    [![](images/build-window.png)](images/full_size_images/build-window.png)

16. Check the Environments and Vehicles that should be generated as AssetBundles
    - See [assets documentation](assets.md) for information on how to add Environments and Vehicles
    - They will be located in a folder called `AssetBundles` in the folder selected as the build location 
    - These may also be built separately from the Simulator. In this case they will be put into the `AssetBundles` folder of the project

17. (Optional) Click `Build` to only build the assetbundles. Load the `LoaderScene.unity` and click the Play button at the top of the editor to start the simulator.

18. Select the `Target OS` for the build

19. Verify `Build Simulator` is checked for the Simulator to be built

20. Select a folder that the simulator will be built in

21. (Optional) Check `Development Build` to create a [Development Build](https://docs.unity3d.com/ScriptReference/BuildOptions.Development.html)

22. Click `Build`


### Test Simulator <sup><sub>[top](#instructions-to-build-standalone-executable)</sub></sup> {: #test-simulator data-toc-label='Test Simulator'}

1. Ubuntu - Install Vulkan userspace library

        sudo apt-get install libvulkan1

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
