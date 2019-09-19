# How to Collect Data with Control Calibration Sensor

Control Calibration Sensor is for collecting control data to generate control calibration table which can be referred by control module to decide throttle, brake and steering command.

## Setup
1. Check if there is Wide Flat Map

```
ls /apollo/modules/map/data/wide_flat_map
```

## Instructions

1. Add `WideFlatMap` into WebUI. The assetbundle is available on the [content website](https://content.lgsvlsimulator.com/maps/)

2. Create a vehicle with a `CyberRT` bridge type and add [total control calibration criteria](total-control-calibration-criteria.md) into ego vehicle's [sensor parameters](sensor-json-options.md#control-calibration).

3. Start Apollo docker container

    ```
    /apollo/docker/scripts/dev_start.sh
    ```

4. Go into Apollo docker container with
    
    ```
    /apollo/docker/scripts/dev_into.sh
    ```

5. Run Cyber bridge
    
    ```
    /apollo/scripts/bridge.sh
    ```

6. Run localization module
    
    ```
    mainboard -d /apollo/modules/localization/dag/dag_streaming_rtk_localization.dag
    ```

7. Run dreamview
    
    ```
    bootstrap.sh
    ```

8. In a browser, navigate to Dreamview at

    `http://localhost:8888/`

9. Choose vehicle model.

10. Choose Map as Wide Flat Map.

11. Choose setup mode to Vehicle Calibration.

    [![](images/control-calibration-dreamview.png)](images/full_size_images/control-calibration-dreamview.png)


12. In Others tab, choose Data Collection Monitor

13. In Modules tab, turn Recorder on.

14. In the Simulator's WebUI, create a simulation with `WideFlatMap` and the vehicle created in Step 2 (with the bridge connection string to the computer running Apollo).

15. In Simulator's WebUI, start simulation.

16. You can see progress bar filled as Apollo collects data. Once all progress bars are filled, vehicle control calibration is done.
    [![](images/control-calibration-dreamview-progress-bar.png)](images/full_size_images/control-calibration-dreamview-progress-bar.png)
