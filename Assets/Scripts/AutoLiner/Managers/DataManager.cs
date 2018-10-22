/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml; //Needed for XML functionality
using System.Xml.Serialization; //Needed for XML Functionality
using System.IO;
using System.Xml.Linq;

#region openSCENARIO data
[System.Serializable]
public class OpenScenarioData
{
    public Dictionary<string, string> fileHeader = new Dictionary<string, string>();
    [System.Serializable]
    public struct FileHeader
    {
        public string revMajor;
        public string revMinor;
        public string date;
        public string description;
        public string author;
    };
    public FileHeader fileHeaderData;

    public Dictionary<string, string> catalogs = new Dictionary<string, string>();
    [System.Serializable]
    public struct Catalogs
    {
        public string vehicleCatalog;
        public string driverCatalog;
        public string pedestrianCatalog;
        public string pedestrianControllerCatalog;
        public string miscObjectCatalog;
        public string environmentCatalog;
        public string maneuverCatalog;
        public string trajectoryCatalog;
        public string routeCatalog;
    };
    public Catalogs catalogsData;

    public Dictionary<string, string> roadNetwork = new Dictionary<string, string>();
    [System.Serializable]
    public struct RoadNetwork
    {
        public string logics;
        public string sceneGraph;
        public string biDirectional;
        public string laneCount;
    };
    public RoadNetwork roadNetworkData;

    public List<Dictionary<string, string>> entities = new List<Dictionary<string, string>>();
    [System.Serializable]
    public struct Entities
    {
        public string name;
        public string catalogName;
        public string entryName;
        public string controllerCatalogName;
        public string controllerEntryName;
    };
    public List<Entities> entitiesData;

    public List<Dictionary<string, string>> storyboardInit = new List<Dictionary<string, string>>();
    [System.Serializable]
    public struct StoryboardInit
    {
        public string name;  // object
        public string dynamics;
        public string rate;
        public string speed;

        
        public string routeRefCatalogName;
        public string routeRefEntryName;
        public string routePositionLaneCoordPathS;
        public string routePositionLaneCoordLaneId;

        public string positionX;
        public string positionY;
        public string positionZ;
        public string positionHeading; // rot z
        public string positionPitch; // rot x
        public string positionRoll; // rot y
    };
    public List<StoryboardInit> storyboardInitData;

    public List<Dictionary<string, string>> storyboardStory = new List<Dictionary<string, string>>();
    [System.Serializable]
    public struct StoryboardStory
    {
        public string storyName; // multiple
        public string storyOwner; // can be absent
        public List<StoryboardStoryAct> storyboardStoryActs;
    };

    [System.Serializable]
    public struct StoryboardStoryAct
    {
        public string actName; // multiple
        public string sequenceName;
        public string sequenceExecutions;
        public string actor; // can be empty
        public string maneuverName; // can be absent entirely e.g. no <Maneuver, Event, Action>
        public List<StoryboardStoryActManeuverEvent> storyboardStoryManeuverEvents;

        public string sequenceConditionName;
        public string sequenceConditionDelay;
        public string sequenceConditionEdge;
        public string sequenceConditionSimulationTimeValue;
        public string sequenceConditionSimulationRule;
    };
    [System.Serializable]
    public struct StoryboardStoryActManeuverEvent
    {
        public string eventName; // multiple
        public string eventPriority;
        public List<StoryboardStoryActManeuverEventAction> storyboardStoryActManeuverEventActions;

        public string eventActionConditionName;
        public string eventActionConditionDelay;
        public string eventActionConditionEdge;
        //by entity
        public string eventActionConditionTriggeringEntitiesRule;
        public string eventActionConditionTriggeringEntityName;
        public string eventActionConditionEntityDistanceValue;
        public string eventActionConditionEntityDistanceFreespace;
        public string eventActionConditionEntityDistanceAlongRoute;
        public string eventActionConditionEntityDistanceRule;
        public string eventActionConditionEntityDistancePositionObject;
        public string eventActionConditionEntityDistancePositionDX;
        public string eventActionConditionEntityDistancePositionDY;
        // by state
        public string eventActionConditionAtStartType;
        public string eventActionConditionAtStartName;
        public string eventActionConditionAfterTerminationType;
        public string eventActionConditionAfterTerminationName;
        public string eventActionConditionAfterTerminationRule;

        public bool isComplete;

    };
    [System.Serializable]
    public struct StoryboardStoryActManeuverEventAction
    {
        public string actionName; // multiple
        public string actionType; // routing e.g. requires external xml file parsed, lateral e.g. lane change, longitudinal e.g. speed

        //public string actionRoutingType;
        // TODO parse  additional xml

        public string actionLateralTypeDynamics;
        public string actionLateralTypeTime;
        public string actionLateralTypeTarget;
        public string actionLateralTypeTargetValue;

        public string actionLongitudinalTypeDynamics;
        public string actionLongitudinalTypeTime;
        public string actionLongitudinalTypeTarget;

        public string actionPositionTypeObject;
        public string actionPositionTypeDLane;
        public string actionPositionTypeDS;
        public string actionPositionTypeOrientationType;
        public string actionPositionTypeOrientationH;

        public bool isComplete;
    };
    public List<StoryboardStory> storyboardStoryData;
}
#endregion

public class DataManager : MonoBehaviour
{
    #region Singelton
    private static DataManager _instance = null;
    public static DataManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = GameObject.FindObjectOfType<DataManager>();
                if (_instance == null)
                    Debug.LogError("<color=red>DataManager" +
                        " Not Found!</color>");
            }
            return _instance;
        }
    }
    #endregion

    #region vars
    public string folderPath { get; } = "Assets/Resources/XMLFiles";

    private string trajectoryCatalogFilePath = "Assets/Resources/XMLFiles/TrajectoryCatalog/TrajectoryCatalog.xosc";
    private string routeCatalogFilePath = "Assets/Resources/XMLFiles/TrajectoryCatalog/RouteCatalog.xosc";

    private XDocument xmlDoc;

    private IEnumerable<XAttribute> scenarioHeader;
    private IEnumerable<XElement> scenarioCatalogs;
    private IEnumerable<XElement> scenarioRoadNetwork;
    private IEnumerable<XElement> scenarioEntities;
    private IEnumerable<XElement> scenarioInit;
    private IEnumerable<XElement> scenarioInitPrivate;
    private IEnumerable<XElement> scenarioStory;
    private IEnumerable<XElement> scenarioStoryActs;
    private IEnumerable<XElement> scenarioStoryEvents;
    private IEnumerable<XElement> scenarioStoryActions;
    
    public OpenScenarioData data = new OpenScenarioData();
    public List<OpenScenarioData.StoryboardStoryActManeuverEvent> storyEvents = new List<OpenScenarioData.StoryboardStoryActManeuverEvent>();
    //public List<OpenScenarioData.StoryboardStoryActManeuverEventAction> storyActions = new List<OpenScenarioData.StoryboardStoryActManeuverEventAction>();
    #endregion

    #region mono
    void Awake()
    {
        if (_instance == null)
            _instance = this;

        if (_instance != this)
            DestroyImmediate(gameObject);

        DontDestroyOnLoad(gameObject);
    }

    void OnApplicationQuit()
    {
        _instance = null;
        DestroyImmediate(gameObject);
    }
    #endregion

    #region parse data
    private void ParseOpenScenarioFile(string filePath)
    {
        ClearData();

        xmlDoc = XDocument.Load(filePath);
        scenarioHeader = xmlDoc.Root.Element("FileHeader").Attributes();
        scenarioCatalogs = xmlDoc.Descendants("Catalogs").Elements();
        scenarioRoadNetwork = xmlDoc.Descendants("RoadNetwork").Elements();
        scenarioEntities = xmlDoc.Descendants("Entities").Elements();

        scenarioInit = xmlDoc.Descendants("Init");
        scenarioInitPrivate = scenarioInit.Descendants("Private");

        scenarioStory = xmlDoc.Descendants("Story");
        scenarioStoryActs = scenarioStory.Descendants("Act");
        
        // fileheader
        foreach (var item in scenarioHeader)
        {
            data.fileHeader.Add(item.Name.ToString(), item.Value);
            if (item.Name.ToString() == "revMajor")
                data.fileHeaderData.revMajor = item.Value;
            if (item.Name.ToString() == "revMinor")
                data.fileHeaderData.revMinor = item.Value;
            if (item.Name.ToString() == "date")
                data.fileHeaderData.date = item.Value;
            if (item.Name.ToString() == "description")
                data.fileHeaderData.description = item.Value;
            if (item.Name.ToString() == "author")
                data.fileHeaderData.author = item.Value;
        }

        // catalogs // TODO needed?
        foreach (var item in scenarioCatalogs)
        {
            data.catalogs.Add(item.Name.ToString(), item.Element("Directory").Attribute("path").Value);
            if (item.Name.ToString() == "VehicleCatalog")
                data.catalogsData.vehicleCatalog = item.Element("Directory").Attribute("path").Value;
            if (item.Name.ToString() == "DriverCatalog")
                data.catalogsData.driverCatalog = item.Element("Directory").Attribute("path").Value;
            if (item.Name.ToString() == "PedestrianCatalog")
                data.catalogsData.pedestrianCatalog = item.Element("Directory").Attribute("path").Value;
            if (item.Name.ToString() == "PedestrianControllerCatalog")
                data.catalogsData.pedestrianControllerCatalog = item.Element("Directory").Attribute("path").Value;
            if (item.Name.ToString() == "MiscObjectCatalog")
                data.catalogsData.miscObjectCatalog = item.Element("Directory").Attribute("path").Value;
            if (item.Name.ToString() == "EnvironmentCatalog")
                data.catalogsData.environmentCatalog = item.Element("Directory").Attribute("path").Value;
            if (item.Name.ToString() == "ManeuverCatalog")
                data.catalogsData.maneuverCatalog = item.Element("Directory").Attribute("path").Value;
            if (item.Name.ToString() == "TrajectoryCatalog")
                data.catalogsData.trajectoryCatalog = item.Element("Directory").Attribute("path").Value;
            if (item.Name.ToString() == "RouteCatalog")
                data.catalogsData.routeCatalog = item.Element("Directory").Attribute("path").Value;
        }

        foreach (var item in scenarioRoadNetwork)
        {
            if (item.Attribute("filepath") != null)
                data.roadNetwork.Add(item.Name.ToString(), item.Attribute("filepath").Value);
            if (item.Attribute("value") != null)
                data.roadNetwork.Add(item.Name.ToString(), item.Attribute("value").Value);

            if (item.Name.ToString() == "Logics")
                data.roadNetworkData.logics = item.Attribute("filepath").Value;
            if (item.Name.ToString() == "SceneGraph")
                data.roadNetworkData.sceneGraph = item.Attribute("filepath").Value;
            if (item.Name.ToString() == "BiDirectional")
                data.roadNetworkData.biDirectional = item.Attribute("value").Value;
            if (item.Name.ToString() == "LaneCount")
                data.roadNetworkData.laneCount = item.Attribute("value").Value;
        }

        foreach (var item in scenarioEntities)
        {
            Dictionary<string, string> tempDict = new Dictionary<string, string>();
            tempDict.Add(item.Attribute("name").Name.ToString(), item.Attribute("name").Value);
            tempDict.Add(item.Element("CatalogReference").Attribute("catalogName").Name.ToString(), item.Element("CatalogReference").Attribute("catalogName").Value);
            tempDict.Add(item.Element("CatalogReference").Attribute("entryName").Name.ToString(), item.Element("CatalogReference").Attribute("entryName").Value);
            // TODO keyvaluepair ???
            tempDict.Add("controllerCatalogName", item.Element("Controller").Element("CatalogReference").Attribute("catalogName").Value);
            tempDict.Add("controllerEntryName", item.Element("Controller").Element("CatalogReference").Attribute("entryName").Value);
            data.entities.Add(tempDict);

            OpenScenarioData.Entities tempEntities = new OpenScenarioData.Entities();
            tempEntities.name = item.Attribute("name").Value;
            tempEntities.catalogName = item.Element("CatalogReference").Attribute("catalogName").Value;
            tempEntities.entryName = item.Element("CatalogReference").Attribute("entryName").Value;
            tempEntities.controllerCatalogName = item.Element("Controller").Element("CatalogReference").Attribute("catalogName").Value;
            tempEntities.controllerEntryName = item.Element("Controller").Element("CatalogReference").Attribute("entryName").Value;
            data.entitiesData.Add(tempEntities);
        }

        
        
        foreach (var itemA in scenarioInitPrivate)
        {
            Dictionary<string, string> tempDict = new Dictionary<string, string>();
            OpenScenarioData.StoryboardInit tempInit = new OpenScenarioData.StoryboardInit();

            tempDict.Add("name", itemA.Attribute("object").Value);
            tempInit.name = itemA.Attribute("object").Value;
            foreach (var itemB in itemA.Elements("Action"))
            {
                if (itemB.Element("Longitudinal") != null)
                {
                    tempDict.Add("dynamics", itemB.Element("Longitudinal").Element("Speed").Element("Dynamics").Attribute("shape").Value);
                    tempInit.dynamics = itemB.Element("Longitudinal").Element("Speed").Element("Dynamics").Attribute("shape").Value;
                    if (itemB.Element("Longitudinal").Element("Speed").Element("Dynamics").Attribute("rate") != null) // missing in some xml files
                    {
                        tempDict.Add("rate", itemB.Element("Longitudinal").Element("Speed").Element("Dynamics").Attribute("rate").Value);
                        tempInit.rate = itemB.Element("Longitudinal").Element("Speed").Element("Dynamics").Attribute("rate").Value;
                    }
                    tempDict.Add("speed", itemB.Element("Longitudinal").Element("Speed").Element("Target").Element("Absolute").Attribute("value").Value);
                    tempInit.speed = itemB.Element("Longitudinal").Element("Speed").Element("Target").Element("Absolute").Attribute("value").Value;
                }
                else
                {
                    if (itemB.Element("Position").Element("World") != null)
                    {
                        tempDict.Add("positionX", itemB.Element("Position").Element("World").Attribute("x").Value);
                        tempInit.positionX = itemB.Element("Position").Element("World").Attribute("x").Value;
                        tempDict.Add("positionY", itemB.Element("Position").Element("World").Attribute("y").Value);
                        tempInit.positionY = itemB.Element("Position").Element("World").Attribute("y").Value;
                        tempDict.Add("positionZ", itemB.Element("Position").Element("World").Attribute("z").Value);
                        tempInit.positionZ = itemB.Element("Position").Element("World").Attribute("z").Value;
                        tempDict.Add("positionHeading", itemB.Element("Position").Element("World").Attribute("h").Value);
                        tempInit.positionHeading = itemB.Element("Position").Element("World").Attribute("h").Value;
                        tempDict.Add("positionPitch", itemB.Element("Position").Element("World").Attribute("p").Value);
                        tempInit.positionPitch = itemB.Element("Position").Element("World").Attribute("p").Value;
                        tempDict.Add("positionRoll", itemB.Element("Position").Element("World").Attribute("r").Value);
                        tempInit.positionRoll = itemB.Element("Position").Element("World").Attribute("r").Value;
                    }
                    else
                    {
                        tempDict.Add("routeRefCatalogName", itemB.Element("Position").Element("Route").Element("RouteRef").Element("CatalogReference").Attribute("catalogName").Value);
                        tempInit.routeRefCatalogName = itemB.Element("Position").Element("Route").Element("RouteRef").Element("CatalogReference").Attribute("catalogName").Value;
                        tempDict.Add("routeRefEntryName", itemB.Element("Position").Element("Route").Element("RouteRef").Element("CatalogReference").Attribute("entryName").Value);
                        tempInit.routeRefEntryName = itemB.Element("Position").Element("Route").Element("RouteRef").Element("CatalogReference").Attribute("entryName").Value;
                        tempDict.Add("routePositionLaneCoordPathS", itemB.Element("Position").Element("Route").Element("Position").Element("LaneCoord").Attribute("pathS").Value);
                        tempInit.routePositionLaneCoordPathS = itemB.Element("Position").Element("Route").Element("Position").Element("LaneCoord").Attribute("pathS").Value;
                        tempDict.Add("routePositionLaneCoordLaneId", itemB.Element("Position").Element("Route").Element("Position").Element("LaneCoord").Attribute("laneId").Value);
                        tempInit.routePositionLaneCoordLaneId = itemB.Element("Position").Element("Route").Element("Position").Element("LaneCoord").Attribute("laneId").Value;
                    }   
                }
            }
            data.storyboardInit.Add(tempDict);
            data.storyboardInitData.Add(tempInit);
        }

        foreach (var itemA in scenarioStory)
        {
            //Debug.Log(itemA.ToString());
            OpenScenarioData.StoryboardStory tempStory = new OpenScenarioData.StoryboardStory();
            tempStory.storyboardStoryActs = new List<OpenScenarioData.StoryboardStoryAct>();
            tempStory.storyName = itemA.Attribute("name").Value;
            if (itemA.Attribute("owner") != null)
            {
                tempStory.storyOwner = itemA.Attribute("owner").Value;
            }

            foreach (var itemB in scenarioStoryActs)
            {
                //Debug.Log(itemB.ToString());
                OpenScenarioData.StoryboardStoryAct tempStoryAct = new OpenScenarioData.StoryboardStoryAct();
                tempStoryAct.storyboardStoryManeuverEvents = new List<OpenScenarioData.StoryboardStoryActManeuverEvent>();

                tempStoryAct.actName = itemB.Attribute("name").Value;
                tempStoryAct.sequenceName = itemB.Element("Sequence").Attribute("name").Value;
                tempStoryAct.sequenceExecutions = itemB.Element("Sequence").Attribute("numberOfExecutions").Value;
                if (itemB.Element("Sequence").Element("Actors").Element("Entity") != null)
                {
                    tempStoryAct.actor = itemB.Element("Sequence").Element("Actors").Element("Entity").Attribute("name").Value; // can be absent
                }
                if (itemB.Element("Sequence").Element("Maneuver") != null) // can be a null sequence
                {
                    tempStoryAct.maneuverName = itemB.Element("Sequence").Element("Maneuver").Attribute("name").Value;

                    scenarioStoryEvents = itemB.Element("Sequence").Element("Maneuver").Elements("Event");
                    foreach (var itemC in scenarioStoryEvents)
                    {
                        //Debug.Log(itemC.ToString());
                        OpenScenarioData.StoryboardStoryActManeuverEvent tempStoryActEvent = new OpenScenarioData.StoryboardStoryActManeuverEvent();
                        tempStoryActEvent.storyboardStoryActManeuverEventActions = new List<OpenScenarioData.StoryboardStoryActManeuverEventAction>();

                        tempStoryActEvent.eventName = itemC.Attribute("name").Value;
                        tempStoryActEvent.eventPriority = itemC.Attribute("priority").Value;

                        // actions
                        scenarioStoryActions = itemC.Elements("Action");
                        foreach (var itemD in scenarioStoryActions)
                        {
                            //Debug.Log(itemD.ToString());
                            OpenScenarioData.StoryboardStoryActManeuverEventAction tempStoryActEventAction = new OpenScenarioData.StoryboardStoryActManeuverEventAction();

                            tempStoryActEventAction.actionName = itemD.Attribute("name").Value;
                            if (itemD.Element("Private").Element("Routing") != null)
                            {
                                tempStoryActEventAction.actionType = itemD.Element("Private").Element("Routing").Name.ToString();//tempStoryActEventAction.actionRoutingType = itemD.Element("Private").Element("Routing");
                            }
                            else if (itemD.Element("Private").Element("Lateral") != null)
                            {
                                tempStoryActEventAction.actionType = itemD.Element("Private").Element("Lateral").Name.ToString();
                                tempStoryActEventAction.actionLateralTypeDynamics = itemD.Element("Private").Element("Lateral").Element("LaneChange").Element("Dynamics").Attribute("shape").Value;
                                tempStoryActEventAction.actionLateralTypeTime = itemD.Element("Private").Element("Lateral").Element("LaneChange").Element("Dynamics").Attribute("time").Value;
                                tempStoryActEventAction.actionLateralTypeTarget = itemD.Element("Private").Element("Lateral").Element("LaneChange").Element("Target").Element("Relative").Attribute("object").Value;
                                tempStoryActEventAction.actionLateralTypeTargetValue = itemD.Element("Private").Element("Lateral").Element("LaneChange").Element("Target").Element("Relative").Attribute("value").Value;
                            }
                            else if (itemD.Element("Private").Element("Longitudinal") != null)
                            {
                                tempStoryActEventAction.actionType = itemD.Element("Private").Element("Longitudinal").Name.ToString();
                                tempStoryActEventAction.actionLongitudinalTypeDynamics = itemD.Element("Private").Element("Longitudinal").Element("Speed").Element("Dynamics").Attribute("shape").Value;
                                if (itemD.Element("Private").Element("Longitudinal").Element("Speed").Element("Dynamics").Attribute("time") != null)
                                    tempStoryActEventAction.actionLongitudinalTypeTime = itemD.Element("Private").Element("Longitudinal").Element("Speed").Element("Dynamics").Attribute("time").Value; // can be absent
                                tempStoryActEventAction.actionLongitudinalTypeTarget = itemD.Element("Private").Element("Longitudinal").Element("Speed").Element("Target").Element("Absolute").Attribute("value").Value;
                            }
                            else
                            {
                                tempStoryActEventAction.actionType = itemD.Element("Private").Element("Position").Name.ToString();
                                tempStoryActEventAction.actionPositionTypeObject = itemD.Element("Private").Element("Position").Element("RelativeLane").Attribute("object").Value;
                                tempStoryActEventAction.actionPositionTypeDLane = itemD.Element("Private").Element("Position").Element("RelativeLane").Attribute("dLane").Value;
                                tempStoryActEventAction.actionPositionTypeDS = itemD.Element("Private").Element("Position").Element("RelativeLane").Attribute("ds").Value;
                                tempStoryActEventAction.actionPositionTypeOrientationType = itemD.Element("Private").Element("Position").Element("RelativeLane").Element("Orientation").Attribute("type").Value;
                                tempStoryActEventAction.actionPositionTypeOrientationH = itemD.Element("Private").Element("Position").Element("RelativeLane").Element("Orientation").Attribute("h").Value;
                            }
                            tempStoryActEventAction.isComplete = false;
                            tempStoryActEvent.storyboardStoryActManeuverEventActions.Add(tempStoryActEventAction);
                        }

                        tempStoryActEvent.eventActionConditionName = itemC.Element("Conditions").Element("Start").Element("ConditionGroup").Element("Condition").Attribute("name").Value;
                        tempStoryActEvent.eventActionConditionDelay = itemC.Element("Conditions").Element("Start").Element("ConditionGroup").Element("Condition").Attribute("delay").Value;
                        tempStoryActEvent.eventActionConditionEdge = itemC.Element("Conditions").Element("Start").Element("ConditionGroup").Element("Condition").Attribute("edge").Value;

                        if (itemC.Element("Conditions").Element("Start").Element("ConditionGroup").Element("Condition").Element("ByEntity") != null)
                        {
                            tempStoryActEvent.eventActionConditionTriggeringEntitiesRule = itemC.Element("Conditions").Element("Start").Element("ConditionGroup").Element("Condition").Element("ByEntity").Element("TriggeringEntities").Attribute("rule").Value;
                            tempStoryActEvent.eventActionConditionTriggeringEntityName = itemC.Element("Conditions").Element("Start").Element("ConditionGroup").Element("Condition").Element("ByEntity").Element("TriggeringEntities").Element("Entity").Attribute("name").Value;
                            tempStoryActEvent.eventActionConditionEntityDistanceValue = itemC.Element("Conditions").Element("Start").Element("ConditionGroup").Element("Condition").Element("ByEntity").Element("EntityCondition").Element("Distance").Attribute("value").Value;
                            tempStoryActEvent.eventActionConditionEntityDistanceFreespace = itemC.Element("Conditions").Element("Start").Element("ConditionGroup").Element("Condition").Element("ByEntity").Element("EntityCondition").Element("Distance").Attribute("freespace").Value;
                            tempStoryActEvent.eventActionConditionEntityDistanceAlongRoute = itemC.Element("Conditions").Element("Start").Element("ConditionGroup").Element("Condition").Element("ByEntity").Element("EntityCondition").Element("Distance").Attribute("alongRoute").Value;
                            tempStoryActEvent.eventActionConditionEntityDistanceRule = itemC.Element("Conditions").Element("Start").Element("ConditionGroup").Element("Condition").Element("ByEntity").Element("EntityCondition").Element("Distance").Attribute("rule").Value;
                            tempStoryActEvent.eventActionConditionEntityDistancePositionObject = itemC.Element("Conditions").Element("Start").Element("ConditionGroup").Element("Condition").Element("ByEntity").Element("EntityCondition").Element("Distance").Element("Position").Element("RelativeObject").Attribute("object").Value;
                            tempStoryActEvent.eventActionConditionEntityDistancePositionDX = itemC.Element("Conditions").Element("Start").Element("ConditionGroup").Element("Condition").Element("ByEntity").Element("EntityCondition").Element("Distance").Element("Position").Element("RelativeObject").Attribute("dx").Value;
                            tempStoryActEvent.eventActionConditionEntityDistancePositionDY = itemC.Element("Conditions").Element("Start").Element("ConditionGroup").Element("Condition").Element("ByEntity").Element("EntityCondition").Element("Distance").Element("Position").Element("RelativeObject").Attribute("dy").Value;
                        }
                        else
                        {
                            if (itemC.Element("Conditions").Element("Start").Element("ConditionGroup").Element("Condition").Element("ByState").Element("AtStart") != null)
                            {
                                tempStoryActEvent.eventActionConditionAtStartType = itemC.Element("Conditions").Element("Start").Element("ConditionGroup").Element("Condition").Element("ByState").Element("AtStart").Attribute("type").Value;
                                tempStoryActEvent.eventActionConditionAtStartName = itemC.Element("Conditions").Element("Start").Element("ConditionGroup").Element("Condition").Element("ByState").Element("AtStart").Attribute("name").Value;
                            }
                            else
                            {
                                tempStoryActEvent.eventActionConditionAfterTerminationType = itemC.Element("Conditions").Element("Start").Element("ConditionGroup").Element("Condition").Element("ByState").Element("AfterTermination").Attribute("type").Value;
                                tempStoryActEvent.eventActionConditionAfterTerminationName = itemC.Element("Conditions").Element("Start").Element("ConditionGroup").Element("Condition").Element("ByState").Element("AfterTermination").Attribute("name").Value;
                                tempStoryActEvent.eventActionConditionAfterTerminationRule = itemC.Element("Conditions").Element("Start").Element("ConditionGroup").Element("Condition").Element("ByState").Element("AfterTermination").Attribute("rule").Value;
                            }
                            
                        }
                        tempStoryActEvent.isComplete = false;
                        tempStoryAct.storyboardStoryManeuverEvents.Add(tempStoryActEvent);
                    }
                }
                tempStoryAct.sequenceConditionName = itemB.Element("Conditions").Element("Start").Element("ConditionGroup").Element("Condition").Attribute("name").Value;
                tempStoryAct.sequenceConditionDelay = itemB.Element("Conditions").Element("Start").Element("ConditionGroup").Element("Condition").Attribute("delay").Value;
                tempStoryAct.sequenceConditionEdge = itemB.Element("Conditions").Element("Start").Element("ConditionGroup").Element("Condition").Attribute("edge").Value;
                tempStoryAct.sequenceConditionSimulationTimeValue = itemB.Element("Conditions").Element("Start").Element("ConditionGroup").Element("Condition").Element("ByValue").Element("SimulationTime").Attribute("value").Value;
                tempStoryAct.sequenceConditionSimulationRule = itemB.Element("Conditions").Element("Start").Element("ConditionGroup").Element("Condition").Element("ByValue").Element("SimulationTime").Attribute("rule").Value;

                tempStory.storyboardStoryActs.Add(tempStoryAct);
            }

            data.storyboardStoryData.Add(tempStory);
        }
    }
    #endregion

    #region methods
    public void LoadData(string fileName)
    {
        ParseOpenScenarioFile(folderPath + "/" + fileName);
        // TODO add to init data
        for (int i = 0; i < data.storyboardStoryData.Count; i++)
        {
            for (int j = 0; j < data.storyboardStoryData[i].storyboardStoryActs.Count; j++)
            {
                for (int k = 0; k < data.storyboardStoryData[i].storyboardStoryActs[j].storyboardStoryManeuverEvents.Count; k++)
                {
                    storyEvents.Add(data.storyboardStoryData[i].storyboardStoryActs[j].storyboardStoryManeuverEvents[k]);
                    //for (int l = 0; l < data.storyboardStoryData[i].storyboardStoryActs[j].storyboardStoryManeuverEvents[k].storyboardStoryActManeuverEventActions.Count; l++)
                    //{
                    //    storyActions.Add(data.storyboardStoryData[i].storyboardStoryActs[j].storyboardStoryManeuverEvents[k].storyboardStoryActManeuverEventActions[l]);
                    //}
                }
            }
        }
    }

    public OpenScenarioData.RoadNetwork GetRoadNetworkData()
    {
        return data.roadNetworkData;
    }

    public List<OpenScenarioData.StoryboardInit> GetInitVehicleData()
    {
        return data.storyboardInitData;
    }

    public void ClearData()
    {
        data = new OpenScenarioData
        {
            fileHeaderData = new OpenScenarioData.FileHeader(),
            catalogsData = new OpenScenarioData.Catalogs(),
            roadNetworkData = new OpenScenarioData.RoadNetwork(),
            entitiesData = new List<OpenScenarioData.Entities>(),
            storyboardInitData = new List<OpenScenarioData.StoryboardInit>(),
            storyboardStoryData = new List<OpenScenarioData.StoryboardStory>()
        };
        storyEvents.Clear();
        //storyActions.Clear();
    }
    #endregion
}
