# Simulator - NPC Map Navigation

![NPCSetup](images\NPCSetup.jpg)

NPC vehicles now use the MapSegmentBuilder classes to navigate annotated maps.



##### Map Manager

![](images\NPCSetup_MapManager.jpg)

Use SanFrancisco.scene as a template to build map data for NPCs.

1. Remove <span style='color:blue'>SFTraffic_New.prefab</span> from scene and any associated scripts, e.g., TrafPerformanceManager.cs, TrafInfoManager.cs and TrafSystem.cs.
2. Create a new GameObject at position Vector3.zero and Quaternion.identity, named <span style='color:blue'>Map</span>.
3. Add MapManager.cs component to this object and save as a <span style='color:blue'>prefab</span> in project assets.
4. The public field, SpawnLanesHolder, of MapManager.cs requires the <span style='color:blue'>MapLaneSegmentBuilder</span> holder transform.
5. The public field, IntersectionsHolder, of MapManager.cs requires the <span style='color:blue'>TrafficLights</span> holder transform.  This has intersection meshes and scripts for lights.
6. The public fields, Green, Yellow and Red, are materials for the segmentation camera system.  If using the Traffic meshes from SanFrancisco.scene, these need to be added here.  If not, IntersectionComponent.cs and associated scripts will need edited.



##### Map Lane and Intersection Grouping

![NPCSetup_IntersectionMapBuilder](images\NPCSetup_MapBuilderIntersectionGroup.jpg)

1. Create <span style='color:blue'>TrafficLanes</span> holder object as a child of <span style='color:blue'>Map.prefab</span>.
2. Place all <span style='color:blue'>MapLaneSegmentBuilder</span> objects into <span style='color:blue'>TrafficLanes</span> holder object for all non intersection lanes.
3. Create <span style='color:blue'>IntersectionLanes</span> holder object as a child of <span style='color:blue'>Map.prefab</span>.
4. Create a new <span style='color:blue'>Intersection</span> holder object as a child of <span style='color:blue'>IntersectionLanes</span> transform for each intersection annotation.  **Be sure its world position is in the center of each intersection.**
5. Place <span style='color:blue'>MapLaneSegmentBuilder</span> and <span style='color:blue'>MapStopLineSegmentBuilder</span> objects into <span style='color:blue'>Intersection</span> holder object for each intersection.



##### Map Intersection Builder

![NPCSetup_IntersectionMapBuilder](images\NPCSetup_IntersectionMapBuilder.jpg)

1. For each <span style='color:blue'>Intersection</span> holder, add the MapIntersectionBuilder.cs component.



##### Traffic Lights

![NPCSetup_TrafficLight01](images\NPCSetup_TrafficLight01.jpg)

1. Create a <span style='color:blue'>TrafficLights</span> holder object to hold all traffic light meshes or place all traffic meshes under the map annotation <span style='color:blue'>Intersections</span>.  Just be sure to have the root holder be in MapManager.cs IntersectionHolder public reference.
2. Create a <span style='color:blue'>Intersection</span> holder object.  **Be sure its world position is in the center of each intersection.**
3. Add IntersectionComponent.cs to each <span style='color:blue'>Intersection</span> holder object.



![NPCSetup_TrafficLight02](images\NPCSetup_TrafficLight02.jpg)

1. Place <span style='color:blue'>TrafficLightPole</span> facing it's corresponding <span style='color:blue'>StopLineSegmentBuilder</span> object.  **The transfom needs to be Z axis or gizmo arrow forward, parallel to the StopLineSegmentBuilder object Z axis or gizmo arrow forward.**
2. Add IntersectionTrafficLightSetComponent.cs.
3. Place as a child of the <span style='color:blue'>Intersection</span> holder object.
4. For opposite facing <span style='color:blue'>TrafficLightPoles</span> and <span style='color:blue'>StopLineSegmentBuilders</span>, **be sure to orient transforms in Z axis or gizmo arrow forward but perpendicular to other facing light poles and stoplines.**  



![NPCSetup_TrafficLight03](images\NPCSetup_TrafficLight03.jpg)

1. Add <span style='color:blue'>TrafficLight</span> meshes as children of the <span style='color:blue'>TrafficLightPole</span>.
2. Add IntersectionTrafficLightSetComponent.cs to each <span style='color:blue'>TrafficLight</span>.



##### StopLine and MapLaneSegmentBuilder overlap

![NPCSetup_StopLineAndLaneOverlap](images\NPCSetup_StopLineAndLaneOverlap.jpg)

**MapLaneSegmentBuilders final waypoint needs to be slightly overlapping the MapStopLineBuilder**