# <a name="top"></a> Map Annotation

The LGSVL Simulator supports creating, editing, and exporting of HD maps of existing 3D environments (Unity scenes). The maps can be saved in the currently supported Apollo, Autoware or Lanelet2 formats.

Currently, annotating map is only recommended while running the simulator as a Unity project in a Windows environment.

<h2> Table of Contents</h2>
[TOC]

## Creating a New Map <sub><sup>[top](#top)</sup></sub> {: #creating-a-new-map data-toc-label='Creating a New Map'}

- Make sure your roads belong to the layer of `Default` since waypoints will be only created on this layer.
- Make sure your roads has added `Mesh Collider`.
- Open `HD Map Annotation` in `Unity`: `Simulator` -> `Annotate HD Map`

[![](images/annotation-window-menu.png)](images/annotation-window-menu.png)

- By default, map annotation is not shown. Click `View All`  under `View Modes` to show existing map annotation.
- Before annotating, drag in the correct `Parent Object`, for example `TrafficLanes`. Then every new object you create will be under `TrafficLanes` object.	
- The expected object hierarchy is as follows:
    - `Map` - This prefab which will be the object containing all the HD Annotations. The `MapHolder` script should be added to this prefab
      - `TrafficLanes` - This object will hold all of the `MapLaneSection` and `MapLane` which cannot be grouped under `MapLaneSection`
        - `MapLaneSection` - This will hold all of the lanes in a section of road
          - `MapLane` - A single annotated lane
      - `Intersections` - This object will hold all of the `MapIntersection`
        - `MapIntersection` - This object will hold all of the annotations for an intersection
          - `MapLane` - A single annotated lane
          - `MapLine` - A single annotated line (e.g. a stop line)
          - `MapSignal` - A single annotated traffic signal
          - `MapSign` - A single annotated traffic sign (e.g. a stop sign)
      - `BoundaryLines` - This object will hold all of boundary lines
        

[![](images/annotation-mapToolPanel.png)](images/annotation-mapToolPanel.png)

- To make `Map` a prefab, drag it from the scene `Hierarchy` into the Project folder

[![](images/annotation-createPrefabArrow.png)](images/full_size_images/annotation-createPrefabArrow.png)

- After annotation is done, remember to save: select the `Map` prefab, in the Inspector click `Overrides` -> `Apply All` 

[![](images/annotation-apply-changes.png)](images/annotation-apply-changes.png)


## Annotate Lanes <sub><sup>[top](#top)</sup></sub> {: #annotate-lanes data-toc-label='Annotate Lanes'}

#### Create Parent Object <sub><sup>[top](#top)</sup></sub> {: #create-parent-object-lanes data-toc-label='Create Parent Object'}
[![](images/annotation-mapLaneSection.png)](images/full_size_images/annotation-mapLaneSection.png)

- In the `Map` prefab (if you don't have one, you can create an empty GameObject and make it a prefab), create a new object and name it "TrafficLanes"
- In the Inspector of the `Map`, drag the new `TrafficLanes` object into the `Traffic Lanes holder`
- In `TrafficLanes`, create a new object
- Add the `MapLaneSection` script to the object and position it close the section of road that will be annotated
    - `MapLaneSection` script is needed for sections that contain more than 1 lane. For sections with only 1 lane, they can be left as a child object of the TrafficLanes object or grouped with the boundary lines under a different parent object without the `MapLaneSection` script.
- Each `MapLaneSection` will contain parallel lanes section 
    - 1 `MapLane` per lane of road as well as 1 `MapLine` per boundary line (optional)
    - If the annotations will be broken up into multiple `MapLane` (e.g. a straight section and a curved section), multiple `MapLaneSection` are required
    - A `MapLane` cannot begin or end in the middle of another `MapLane` (e.g. a lane splits or merges). This situation would require a 2nd `MapLaneSection` to be created where the split/merge begins. 
    - The other lanes on the road will also need to be broken up into multiple `MapLane` to be included in the multiple `MapLaneSection`
- `MapLaneSection` is used to automate the computation of relation of neighboring lanes. Please make sure every lane under `MapLaneSection` has at least 3 waypoints.


[![](images/annotation-laneSplit.png)](images/full_size_images/annotation-laneSplit.png)

Example of single lane splitting into a right-turn only lane and a straight lane

#### Make Lanes <sub><sup>[top](#top)</sup></sub> {: #make-lanes data-toc-label='Make Lanes'}
- Select the `Lane/Line` option under `Create Mode`
  - A large yellow `TARGET_WAYPOINT`  will appear in the center of the scene. This is where the `TEMP_WAYPOINT` objects will be placed
- Drag in the appropriate `MapLaneSection` to be the `Parent Object`
- Click `Waypoint` button to create a new `TEMP_WAYPOINT`. This is where the lane will begin

[![](images/annotation-createTempWaypoint.png)](images/full_size_images/annotation-createTempWaypoint.png)

- Move the scene so that the `TARGET_WAYPOINT`  is in the desired location of the next `TEMP_WAYPOINT`
    - With 2 waypoints, the `Create Straight` function will connect the waypoints and use the number in `Waypoint Count` to determine how many waypoints to add in between
    - More than 2 waypoints can be used with the `Create Straight` function and they will be connected in the order they were created, and the `Waypoint Count` value will not be used
    - With 3 waypoints, the `Create Curve` function can be used to create a Bezier curve between the 1st and 3rd waypoints and the `Waypoint Count` value will be used to deterin how many waypoints to add in between 
 
[![](images/annotation-createEndWaypoint.png)](images/full_size_images/annotation-createEndWaypoint.png)

- Verify `Lane` is the selected `Map Object Type`
- Verify `NO_TURN` is the selected `Lane Turn Type`
- Select the appropriate lane boundry types
- Enter the speed limit of the lane (in m/s)
- Enter the desired number of waypoints in the lane (minimum 2 for a straight lane and 3 for a curved lane) 
- Click the appropriate `Connect` button to create the lane

[![](images/annotation-createLane.png)](images/full_size_images/annotation-createLane.png)

- To adjust the positions of the waypoints, with the `MapLane` selected, in the Inspector check `Display Handles`. Individual waypoints can now have their position adjusted

#### Make Boundary Lines <sub><sup>[top](#top)</sup></sub> {: #make-boundary-lines data-toc-label='Make Boundary Lines'}
- Drag in the appropriate `MapLaneSection` to be the `Parent Object`
- The same process as for lanes can be used to create boundary lines, but the `Map Object Type` will be `BoundaryLine`
- It is better to have the direction of a boundary line match the direction of the lane if possible
- If you are annotating map for Lanelet2 format, you also need to annotate boundary lines for every lane and drag them into the corresponding field in the lane object.


## Annotate Intersections <sub><sup>[top](#top)</sup></sub> {: #annotate-intersections data-toc-label='Annotate Intersections'}
#### Create Parent Object <sub><sup>[top](#top)</sup></sub> {: #create-parent-object-intersection data-toc-label='Create Parent Object'}
- In the `Map` prefab, create a new object and name it "Intersections"
- In the Inspector of the `Map`, drag the new `Intersections` object into the `Intersections holder`
- In `Intersections`, create a new object
- Add the `MapIntersection` script to the new object and position it in the center of the intersection that will be annotated.
    - Adjust the `Trigger Bounds` of the `MapIntersection` so that the box covers the center of the intersection
- The `MapIntersection` will contain all lanes, traffic signals, stop lines, and traffic signs in the intersection

[![](images/annotation-intersectionBounds.png)](images/full_size_images/annotation-intersectionBounds.png)

#### Create Intersection Lanes <sub><sup>[top](#top)</sup></sub> {: #create-intersection-lanes data-toc-label='Create Intersection Lanes'}
- Select the `Lane/Line` option under `Create Mode`
- Drag in the `MapIntersection` as the `Parent Object`
- The same process for creating normal `MapLane` is used here
- If the Lane involves changing directions, verify the correct `Lane Turn Type` is selected
- If you are annotating for Lanelet2, you also need to annotate boundary lines for each lane, remember to set `VIRTUAL` as the `Line Type` for the line objects
- If there is a stop line in the intersection, the start of the intersection lanes should be after the stop line

#### Create Traffic Signals <sub><sup>[top](#top)</sup></sub> {: #create-traffic-signals data-toc-label='Create Traffic Signals'}
- Select the `Signal` option under `Create Mode`
- Drag in the `MapIntersection` as the `Parent Object`
- Select the correct `Signal Type`
- Traffic Signals are created on top of existing meshes. The annotation directions must match the directions of the mesh. 

[![](images/annotation-signalMesh.png)](images/full_size_images/annotation-signalMesh.png)

- Select the mesh that is going to be annotated
- Select the `Forward Vector` and `Up Vector` so that it matches the selected mesh

[![](images/annotation-signal.png)](images/full_size_images/annotation-signal.png)

- Click `Create Signal` to annotate the traffic signal


#### Create Traffic Signs <sub><sup>[top](#top)</sup></sub> {: #create-traffic-signs data-toc-label='Create Traffic Signs'}
- Select the `Sign` option under `Create Mode`
- Drag in the `MapIntersection` as the `Parent Object`
- Select the Sign mesh that is going to be annotated
- Select the `Forward Vector` and `Up Vector` so that it matches the selected mesh
- Change the `Sign Type` to the desired type
- Click `Create Sign` to create the annotation
- Find the stop line `MapLine` that is associated with the created `MapSign`
- Select the `MapSign` and drag the `MapLine` into the `Stop Line` box
- Verify the annotation is created in the correct orientation. The Z-axis (blue) should be facing the same way the sign faces.
- Move the Bounding Box so that is matches the sign
    - `Bound Offsets` adjusts the location of the box
    - `Bound Scale` adjusts the size of the box

[![](images/annotation-sign.png)](images/full_size_images/annotation-sign.png)

#### Create Stop Lines <sub><sup>[top](#top)</sup></sub> {: #create-stop-lines data-toc-label='Create Stop Lines'}
- Select the `Lane/Line` option under `Create Mode`
- Drag in the `MapIntersection` as the `Parent Object`
- A similar process to boundary lines and traffic lanes is used for stop lines
- Change the `Map Object Type` to `StopLine`
- `Forward Right` vs `Forward Left` depend on the direction of the lane related to this stopline and the order that the waypoints were created in. The "Forward" direction should match the direction of the lane
    - Example: In a right-hand drive map (cars are on the right side of the road), if the waypoints are created from the outside of the road inwards, then `Forward Right` should be selected. To verify if the correct direction was selected, `Toggle Tool Handle Rotation` so that the tool handles are in the active object's rotation. The Z-axis of the selected Stop Line should be in the same direction as the lanes. 
- A `StopLine` needs to with the lanes that approch it. The last waypoint of approaching lanes should be past the line.

[![](images/annotation-stopLine.png)](images/full_size_images/annotation-stopLine.png)

#### Create Pole <sub><sup>[top](#top)</sup></sub> {: #create-pole data-toc-label='Create Pole'}
Poles are required for Autoware Vector maps. In an intersection with traffic lights, there is 1 `MapPole` on each corner, next to a stop line. The `MapPole` holds references to all traffic lights that signal the closest stop line.

[![](images/annotation-mapPole.png)](images/full_size_images/annotation-mapPole.png)

- Select the `Pole` option under `Create Mode`
- Drag in the `MapIntersection` as the `Parent Object`
- A `TARGET_WAYPOINT` will appear in the center of the scene. This is where the pole annotation will be created.
- Position the `TARGET_WAYPOINT` on the corner of the intersection near the stop line
- Click `Create Pole` to create the annotation
- Find the traffic light `MapSignal` that are associated with the closest stop line
- Select the `MapPole`
- In the Inspector, change the `Signal Lights` size to be the number of `MapSignal` that signal the stop line
- Drag 1 `MapSignal` into each element of `Signal Lights`

[![](images/annotation-mapPoleSelection.png)](images/full_size_images/annotation-mapPoleSelection.png)

## Annotate Self-Reversing Lanes <sub><sup>[top](#top)</sup></sub> {: #annotate-self-reversing-lanes data-toc-label='Annotate Self-Reversing Lanes'}
These types of lanes are only supported on Apollo 5.0

#### Annotation Information <sub><sup>[top](#top)</sup></sub> {: #annotation-information data-toc-label='Annotation Information'}
There are TrafficLanes and Intersections objects. TrafficLanes object is for straight lane without any branch. Intersections object is for branching lanes, which has same predecessor lane.

#### Annotate Self-Reversing Lanes under Traffic Lanes <sub><sup>[top](#top)</sup></sub> {: #annotate-self-reversing-lanes-under-traffic-lanes data-toc-label='Annotate Self-Reversing Lanes under Traffic Lanes'}
[![](images/annotation-self_reverse_lane-mapLaneSection.png)](images/full_size_images/annotation-self_reverse_lane-mapLaneSection.png)

- Add MapLaneSection game object under TrafficLanes. MapLaneSection has one pair of lanes, which are forward and reverse lanes.
- Annotate lane given name to MapLane\_forward and move it under MapLaneSection.
    - Use straight line connect menu with way point count and move each point to shape lane.
    - Use Display Lane option to show width of lane. In order for parking to work, there should have some space between parking space and lane.
- Duplicate the MapLane\_forward and rename it to MapLane\_reverse.
- Choose MapLane\_reverse and click Reverse Lane.
    - Reverse Lane will have way points in reverse order and make tweak to each way point.
    - Routing module doesn't work right when forward and reverse lane have same way point coordinates.
- Choose both lanes (MapLane\_forward, MapLane\_reverse) and change properties like the following:
    - Is Self Reverse Lane: True
    - Lane Turn Type: NO\_TURN
    - Left Bound Type: SOLID\_WHITE
    - Right Bound Type: SOLID\_WHITE
    - Speed Limit: 4.444444 (This value is indicated in sunnyvale\_with\_two\_offices map)
- Set Self Reverse Lane for each lane
    - As for forward lane, you can drag reverse lane game object to Self Reverse Lane of forward lane.
    - As for reverse lane, you can do the same way.

#### Annotate Self-Reversing Lanes under Intersections <sub><sup>[top](#top)</sup></sub> {: #annotate-self-reversing-lanes-under-intersections data-toc-label='Annotate Self-Reversing Lanes under Intersections'}
[![](images/annotation-self_reverse_lane-mapIntersection.png)](images/full_size_images/annotation-self_reverse_lane-mapIntersection.png)

- Add MapIntersection game object under Intersections. MapIntersection has several pair of lanes.
- Annotate lane given name to MapLane1\_forward move it under MapIntersection.
    - Use curve connect menu with way point count, 5.
- Duplicate the MapLane1\_forward and rename it to MapLane1_reverse.
- Choose MapLane\_reverse and click Reverse Lane.
    - Reverse Lane will have way points in reverse order and make tweak to each way point.
    - Routing module doesn't work right when forward and reverse lane have same way point coordinates.
- Choose both lanes (MapLane1\_forward, MapLane1\_reverse) and change properties like the following:
    - Is Self Reverse Lane: True
    - Left Bound Type: DOTTED\_WHITE
    - Right Bound Type: DOTTED\_WHITE
    - Speed Limit: 4.444444 (This value is indicated in sunnyvale\_with\_two\_offices map).
- Choose each lane and decide its turn type considering its direction.
    - Lane Turn Type: NO\_TURN or LEFT\_TURN or RIGHT\_TURN
- Set Self Reverse Lane for each lane
    - As for forward lane, you can drag reverse lane game object to Self Reverse Lane of forward lane.
    - As for reverse lane, you can do the same way.

## Annotate Other Features <sub><sup>[top](#top)</sup></sub> {: #annotate-other-features data-toc-label='Annotate Other Features'}
Other annotation features may be included in the `TrafficLane` or `Intersection` objects or they may be sorted into other parent objects.

#### Create Pedestrian Path <sub><sup>[top](#top)</sup></sub> {: #create-pedestrian-path data-toc-label='Create Pedestrian Path'}
This annotation controls where pedestrians will pass with the highest priority. Pedestrians can walk anywhere but will stay on annotated areas if possible.

- Select the `Pedestrian` option under `Create Mode`
- Drag in the desired `Parent Object`
- A `TARGET_WAYPOINT` will appear in the center fo the scene. This is where the `TEMP_WAYPOINT` will be created.
- Position the `TARGET_WAYPOINT` on the desired path
- Click `Waypoint` to create a `TEMP_WAYPOINT`
- Repeat to create a trail of `TEMP_WAYPOINT` along the desired path
- Click `Connect` to create the `MapPedestrian` object

[![](images/annotation-pedestrian.png)](images/full_size_images/annotation-pedestrian.png)

#### Create Junction <sub><sup>[top](#top)</sup></sub> {: #create-junction data-toc-label='Create Junction'}
Junction annotations can be used by the AD stack if needed.

- Select the `Junction` option under `Create Mode`
- Drag in the desired `Parent Object`
- A `TARGET_WAYPOINT` will appear in the center fo the scene. This is where the `TEMP_WAYPOINT` will be created.
- Position the `TARGET_WAYPOINT` to one vertex of the junction
- Click `Waypoint` to create a `TEMP_WAYPOINT`
- Create the desired number of `TEMP_WAYPOINTS`
- Click `Connect` to create the `MapJunction`

#### Create Crosswalk <sub><sup>[top](#top)</sup></sub> {: #create-crosswalk data-toc-label='Create Crosswalk'}
- Select the `CrossWalk` option under `Create Mode`
- Drag in the desired `Parent Object`
- A `TARGET_WAYPOINT` will appear in the center fo the scene. This is where the `TEMP_WAYPOINT` will be created.
- Position the `TARGET_WAYPOINT` to one corner of the crosswalk
- Click `Waypoint` to create a `TEMP_WAYPOINT`
- Create 4 `TEMP_WAYPOINT` in the order shown below
- Click `Connect` to create the `MapCrossWalk`

[![](images/annotation-crosswalk.png)](images/full_size_images/annotation-crosswalk.png)

#### Create Clear Area <sub><sup>[top](#top)</sup></sub> {: #create-clear-area data-toc-label='Create Clear Area'}
- Select the `ClearArea` option under `Create Mode`
- Drag in the desired `Parent Object`
- A `TARGET_WAYPOINT` will appear in the center fo the scene. This is where the `TEMP_WAYPOINT` will be created.
- Position the `TARGET_WAYPOINT` to one corner of the clear area
- Click `Waypoint` to create a `TEMP_WAYPOINT`
- Create 4 `TEMP_WAYPOINT` in the order shown below
- Click `Connect` to create the `MapClearArea`

[![](images/annotation-clearArea.png)](images/full_size_images/annotation-clearArea.png)

#### Create Parking Space <sub><sup>[top](#top)</sup></sub> {: #create-parking-space data-toc-label='Create Parking Space'}
- Select the `ParkingSpace` option under `Create Mode`
- Drag in the desired `Parent Object`
- A `TARGET_WAYPOINT` will appear in the center fo the scene. This is where the `TEMP_WAYPOINT` will be created.
- Position the `TARGET_WAYPOINT` to the corner of the parking space closest to an approaching vehicle
- Click `Waypoint` to create a `TEMP_WAYPOINT`
- Create 4 `TEMP_WAYPOINT` in the order shown below
- Click `Connect` to create the `MapParkingSpace`

NOTE: Apollo 5.0 requires some space between the edge of a lane and the parking space. To verify that there is space, select the `MapLane` that goes past the `MapParkingSpace` and click `Display Lane`. This will show the width of the lane facilitate adjustment of the lane to have a gap between the lane and parking space.

[![](images/annotation-parkingSpace.png)](images/full_size_images/annotation-parkingSpace.png)

#### Create Speed Bump <sub><sup>[top](#top)</sup></sub> {: #create-speed-bump data-toc-label='Create Speed Bump'}
- Select the `SpeedBump` option under `Create Mode`
- Drag in the desired `Parent Object`
- A `TARGET_WAYPOINT` will appear in the center fo the scene. This is where the `TEMP_WAYPOINT` will be created.
- Position the `TARGET_WAYPOINT` to one end of the speed bump
- Click `Waypoint` to create a `TEMP_WAYPOINT`
- Create 2 `TEMP_WAYPOINT` in the order shown below
- The `TEMP_WAYPOINT` should be wide enough to be outside the lane
- Click `Connect` to create the `MapSpeedBump`
- To verify that the `MapSpeedBump` is wide enough, select the `MapLane` that the `MapSpeedBump` crosses. Enable `Display Lane` to visualize the width of the lane. If necesary, select the `MapSpeedBump` and enable `Display Handles` to adjust the positions of the individual waypoints.

[![](images/annotation-speedBump.png)](images/full_size_images/annotation-speedBump.png)

## Export Map Annotations <sub><sup>[top](#top)</sup></sub> {: #export-map-annotations data-toc-label='Export Map Annotations'}
[![](images/annotation-export.png)](images/full_size_images/annotation-export.png)

HD Map Annotations may be exported in a variety of formats. Current supported formats are:
* Apollo HD Map
* Autoware Vector Map
* Lanelet2 Map

To export a map:
- Open the `HD Map Export` tool in `Unity`: `Simulator` -> `Export HD Map`
- Select the desired format from the dropdown `Export Format`
- Enter the desired save location of the exported map
- Click `Export` to create the exported map

## Import Map Annotations <sub><sup>[top](#top)</sup></sub> {: #import-map-annotations data-toc-label='Import Map Annotations'}
[![](images/annotation-import.png)](images/full_size_images/annotation-import.png)

The simulator can import a variety of formats of annotated map. Current supported formats are:
 * Lanelet2 Map

To import a map:
- Open the `HD Map Export` tool in `Unity`: `Simulator` -> `Import HD Map`
- Select the format of the input map from the dropdown `Import Format`
- Select the file or folder that will be imported
- Click `Import` to import the map annotations into the simulator

Lanelet2 map importer Notes:
- Lanes will be automatically imported and grouped as `MapLaneSection` if possible
- Intersections with four-way traffic lights / stop signs or two-way stop signs can be imported and grouped under `MapIntersection`
- Left-turn lanes are automatically found and their corresponding lanes to yield are also obtained automatically to get NPCs working correctly
- For each `MapIntersection`, you need to manually adjust `X` and `Z` for the `Trigger Bounds` as explained in [annotate intersection part](#annotate-intersections).
- Remember to check objects under `Intersections` are grouped correctly. 

## Map Formats <sub><sup>[top](#top)</sup></sub> {: #map-formats data-toc-label='Map Formats'}
For more information on the map formats, please see the links below:

- [Apollo HD](https://github.com/ApolloAuto/apollo/issues/4048)
- [Autoware Vector](https://tools.tier4.jp/vector_map_builder/user_guide/)
- [Lanelet2](https://github.com/fzi-forschungszentrum-informatik/Lanelet2)
