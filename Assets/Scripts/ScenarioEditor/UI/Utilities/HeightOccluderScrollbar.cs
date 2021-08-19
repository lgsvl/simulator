/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.Utilities
{
    using System;
    using System.Collections.Generic;
    using Elements;
    using Managers;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.UI;

    /// <summary>
    /// Height occluder scrollbar for limiting the objects visibility
    /// </summary>
    public class HeightOccluderScrollbar : MonoBehaviour
    {
        /// <summary>
        /// Object for quick enabling and disabling components of a single game object
        /// </summary>
        private class ConcealableObject
        {
            /// <summary>
            /// Game object that will be enabled and disabled
            /// </summary>
            private GameObject gameObject;

            /// <summary>
            /// Cached collider component of this concealable object
            /// </summary>
            private Collider collider;

            /// <summary>
            /// Cached mesh renderer component of this concealable object
            /// </summary>
            private MeshRenderer meshRenderer;

            /// <summary>
            /// Cached skinned mesh renderer component of this concealable object
            /// </summary>
            private SkinnedMeshRenderer skinnedMeshRenderer;

            /// <summary>
            /// Cached line renderer component of this concealable object
            /// </summary>
            private LineRenderer lineRenderer;

            /// <summary>
            /// Initial Y value of the gameobject position
            /// </summary>
            private float initialPositionY;

            /// <summary>
            /// Bounds centered on the initial gameobject position
            /// </summary>
            private Bounds bounds;

            /// <summary>
            /// Cached collider component of this concealable object
            /// </summary>
            public Collider Collider => collider;

            /// <summary>
            /// Position Y required to hide this concealable object
            /// </summary>
            public float HeightToHide => gameObject.transform.position.y - initialPositionY + bounds.center.y + bounds.extents.y;
            

            /// <summary>
            /// Enables or disables components in the linked gameobject
            /// </summary>
            /// <param name="enabled">Should components be enabled</param>
            public void SetEnabled(bool enabled)
            {
                if (Collider != null)
                    Collider.enabled = enabled;
                if (meshRenderer != null)
                    meshRenderer.enabled = enabled;
                if (skinnedMeshRenderer != null)
                    skinnedMeshRenderer.enabled = enabled;
                if (lineRenderer != null)
                    lineRenderer.enabled = enabled;
            }

            /// <summary>
            /// Finds recursively objects that will be shown and hidden and they are added to the scene objects list
            /// </summary>
            /// <param name="sceneObjects">List where found concealable objects are added</param>
            /// <param name="objectToCheck">Game object and its children are checked and added as concealable if needed</param>
            public static void FindObjectsRecursively(List<ConcealableObject> sceneObjects, GameObject objectToCheck)
            {
                ConcealableObject concealableObject = null;

                // Set the mesh renderer if it is enabled
                var objectRenderer = objectToCheck.GetComponent<MeshRenderer>();
                if (objectRenderer != null && objectRenderer.enabled)
                {
                    concealableObject = new ConcealableObject
                    {
                        gameObject = objectToCheck, meshRenderer = objectRenderer, bounds = objectRenderer.bounds
                    };
                }

                // Set the skinned mesh renderer if it is enabled
                var objectSkinnedRenderer = objectToCheck.GetComponent<SkinnedMeshRenderer>();
                if (objectSkinnedRenderer != null && objectSkinnedRenderer.enabled)
                {
                    if (concealableObject == null)
                    {
                        concealableObject = new ConcealableObject {gameObject = objectToCheck, bounds = objectSkinnedRenderer.bounds};
                    }
                    else
                    {
                        concealableObject.bounds.Encapsulate(objectSkinnedRenderer.bounds);
                    }
                    concealableObject.skinnedMeshRenderer = objectSkinnedRenderer;
                }

                // Set the collider if it is enabled
                var objectCollider = objectToCheck.GetComponent<Collider>();
                if (objectCollider != null && objectCollider.enabled)
                {
                    if (concealableObject == null)
                    {
                        concealableObject = new ConcealableObject {gameObject = objectToCheck, bounds = objectCollider.bounds};
                    }
                    else
                    {
                        concealableObject.bounds.Encapsulate(objectCollider.bounds);
                    }
                    concealableObject.collider = objectCollider;
                }

                // Set the line renderer if it is enabled
                var objectLineRenderer = objectToCheck.GetComponent<LineRenderer>();
                if (objectLineRenderer != null && objectLineRenderer.enabled)
                {
                    if (concealableObject == null)
                    {
                        concealableObject = new ConcealableObject {gameObject = objectToCheck, bounds = objectLineRenderer.bounds};
                    }
                    else
                    {
                        concealableObject.bounds.Encapsulate(objectLineRenderer.bounds);
                    }
                    concealableObject.lineRenderer = objectLineRenderer;
                }

                // Add the concealable object if it was created
                if (concealableObject != null)
                {
                    concealableObject.initialPositionY = concealableObject.gameObject.transform.position.y;
                    sceneObjects.Add(concealableObject);
                }

                // Add children recursively
                for (var i = 0; i < objectToCheck.transform.childCount; i++)
                {
                    var child = objectToCheck.transform.GetChild(i).gameObject;
                    FindObjectsRecursively(sceneObjects, child);
                }
            }
        }

        /// <summary>
        /// Object for quick enabling and disabling components of a single scenario element (multiple game objects)
        /// </summary>
        private class ConcealableScenarioElement
        {
            /// <summary>
            /// Scenario element that is linked to this object
            /// </summary>
            private readonly ScenarioElement scenarioElement;

            /// <summary>
            /// Concealable objects included in this scenario element
            /// </summary>
            private readonly List<ConcealableObject> componentsList = new List<ConcealableObject>();

            /// <summary>
            /// Is this scenario element currently enabled
            /// </summary>
            private bool isEnabled = true;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="element">Scenario element that is linked to this object</param>
            public ConcealableScenarioElement(ScenarioElement element)
            {
                scenarioElement = element;
                Recache();
                scenarioElement.ModelChanged += ScenarioElementOnModelChanged;
            }

            /// <summary>
            /// Destructor
            /// </summary>
            ~ConcealableScenarioElement()
            {
                scenarioElement.ModelChanged -= ScenarioElementOnModelChanged;
            }

            /// <summary>
            /// Element invoked when the scenario element's model is changed
            /// </summary>
            /// <param name="changedElement">Changed scenario element</param>
            private void ScenarioElementOnModelChanged(ScenarioElement changedElement)
            {
                Recache();
            }

            /// <summary>
            /// Recache concealable game objects in the scenario element
            /// </summary>
            public void Recache()
            {
                componentsList.Clear();
                ConcealableObject.FindObjectsRecursively(componentsList, scenarioElement.gameObject);
            }

            /// <summary>
            /// Enables or disables components in the linked scenario element
            /// </summary>
            /// <param name="enable">Should components in the scenario element be enabled</param>
            public void SetEnabled(bool enable)
            {
                if (enable == isEnabled || !scenarioElement.IsEditableOnMap)
                    return;
                for (var i = 0; i < componentsList.Count; i++)
                {
                    var concealableObject = componentsList[i];
                    concealableObject.SetEnabled(enable);
                }

                // Deselect scenario element if it is hidden
                if (!enable && ScenarioManager.Instance.SelectedElement == scenarioElement)
                    ScenarioManager.Instance.SelectedElement = null;
                isEnabled = enable;
            }
        }

        /// <summary>
        /// Count in to how many steps the cache is divided
        /// </summary>
        private const int CacheSteps = 100;

        /// <summary>
        /// Persistence data key for height occluder scrollbar state
        /// </summary>
        private static string HeightOccluderScrollbarStateKey =
            "Simulator/ScenarioEditor/HeightOccluderScrollbar/State";

        /// <summary>
        /// Scrollbar reference which handles the height occluder
        /// </summary>
        [SerializeField]
        private Scrollbar scrollbar;

        /// <summary>
        /// Current height occluder step
        /// </summary>
        private int currentStep;

        /// <summary>
        /// Is the height occluder scrollbar currently enabled
        /// </summary>
        private bool isEnabled;

        /// <summary>
        /// Is the height occluder scrollbar initialized
        /// </summary>
        private bool isInitialized;

        /// <summary>
        /// Scene which objects are currently precached
        /// </summary>
        private Scene? precachedScene;

        /// <summary>
        /// Minimal Y position that is available on the scene (min is set by the lowest collider) 
        /// </summary>
        private float sceneMinY;

        /// <summary>
        /// Maximum X position that is available on the scene
        /// </summary>
        private float sceneMaxY;

        /// <summary>
        /// Cached concealable objects that are available in the precached scene
        /// </summary>
        private readonly List<ConcealableObject>[] sceneCachedObjects = new List<ConcealableObject>[CacheSteps];

        /// <summary>
        /// Concealable scenario elements that are added to the scenario
        /// </summary>
        private readonly Dictionary<ScenarioElement, ConcealableScenarioElement> scenarioElements =
            new Dictionary<ScenarioElement, ConcealableScenarioElement>();

        /// <summary>
        /// Is the height occluder scrollbar currently enabled
        /// </summary>
        public bool IsEnabled
        {
            get => isEnabled;
            set
            {
                if (isEnabled == value)
                    return;
                isEnabled = value;
                var intValue = value ? 1 : 0;
                PlayerPrefs.SetInt(HeightOccluderScrollbarStateKey, intValue);
                gameObject.SetActive(value);
            }
        }

        /// <summary>
        /// Initialization method
        /// </summary>
        public void Initialize()
        {
            if (isInitialized)
                return;
            scrollbar.numberOfSteps = CacheSteps;
            scrollbar.value = 1.0f;
            currentStep = CacheSteps - 1;
            var scenarioManager = ScenarioManager.Instance;
            scenarioManager.ScenarioElementActivated += ScenarioManagerOnScenarioElementActivated;
            scenarioManager.ScenarioElementDeactivated += ScenarioManagerOnScenarioElementDeactivated;
            var mapManager = scenarioManager.GetExtension<ScenarioMapManager>();
            PrecacheMap();
            mapManager.MapChanged += MapManagerOnMapChanged;
            isEnabled = PlayerPrefs.GetInt(HeightOccluderScrollbarStateKey, 0) != 0;
            gameObject.SetActive(isEnabled);
            isInitialized = true;
        }

        /// <summary>
        /// Deinitialization method
        /// </summary>
        public void Deinitialize()
        {
            if (!isInitialized)
                return;
            var scenarioManager = ScenarioManager.Instance;
            scenarioManager.ScenarioElementActivated -= ScenarioManagerOnScenarioElementActivated;
            scenarioManager.ScenarioElementDeactivated -= ScenarioManagerOnScenarioElementDeactivated;
            var mapManager = scenarioManager.GetExtension<ScenarioMapManager>();
            mapManager.MapChanged -= MapManagerOnMapChanged;
            isInitialized = false;
        }

        /// <summary>
        /// Method invoked when the scenario element is activated
        /// </summary>
        /// <param name="element">Activated scenario element</param>
        /// <exception cref="ArgumentException">Exception invoked when the cache is corrupted</exception>
        private void ScenarioManagerOnScenarioElementActivated(ScenarioElement element)
        {
            if (scenarioElements.ContainsKey(element))
            {
                throw new ArgumentException(
                    $"Scenario element {element.name} has already been added to the {nameof(HeightOccluderScrollbar)} cache.");
            }

            scenarioElements.Add(element, new ConcealableScenarioElement(element));
        }

        /// <summary>
        /// Method invoked when the scenario element is deactivated
        /// </summary>
        /// <param name="element">Deactivated scenario element</param>
        /// <exception cref="ArgumentException">Exception invoked when the cache is corrupted</exception>
        private void ScenarioManagerOnScenarioElementDeactivated(ScenarioElement element)
        {
            // Ignore deactivated elements that were not activated
            if (!scenarioElements.ContainsKey(element))
            {
                return;
            }

            scenarioElements.Remove(element);
        }

        /// <summary>
        /// Method invoked when the map is changed
        /// </summary>
        /// <param name="loadedMap"></param>
        private void MapManagerOnMapChanged(ScenarioMapManager.MapMetaData loadedMap)
        {
            scrollbar.SetValueWithoutNotify(1.0f);
            PrecacheMap();
        }

        /// <summary>
        /// Clears the height occluder cache and parameters
        /// </summary>
        private void ResetView()
        {
            for (var i = 0; i < sceneCachedObjects.Length; i++)
            {
                sceneCachedObjects[i] = null;
            }

            scenarioElements.Clear();

            sceneMinY = float.MaxValue;
            sceneMaxY = float.MinValue;
        }

        /// <summary>
        /// Precache all the concealable objects on the loaded scene
        /// </summary>
        private void PrecacheMap()
        {
            var mapManager = ScenarioManager.Instance.GetExtension<ScenarioMapManager>();
            var map = mapManager.LoadedScene;
            if (map == precachedScene)
                return;
            precachedScene = null;

            ResetView();
            if (map == null)
                return;

            var sceneObjects = new List<ConcealableObject>();
            var rootGameObjects = map.Value.GetRootGameObjects();
            for (var i = 0; i < rootGameObjects.Length; i++)
            {
                var rootGameObject = rootGameObjects[i];
                ConcealableObject.FindObjectsRecursively(sceneObjects, rootGameObject);
            }

            // Find min and max Y
            for (var i = 0; i < sceneObjects.Count; i++)
            {
                var concealableObject = sceneObjects[i];
                var y = concealableObject.HeightToHide;
                //Limit min scene Y to the lowest collider position
                if (y < sceneMinY && concealableObject.Collider != null)
                    sceneMinY = y;
                if (y > sceneMaxY)
                    sceneMaxY = y;
            }

            // Insert concealable objects at proper cache position
            var rangeY = sceneMaxY - sceneMinY;
            var stepRange = rangeY / CacheSteps;
            for (var i = 0; i < sceneObjects.Count; i++)
            {
                var cachedObject = sceneObjects[i];
                var y = cachedObject.HeightToHide;
                // If concealable object is under the lowest collider do not cache it
                if (y < sceneMinY)
                    continue;
                var index = Mathf.Clamp(Mathf.FloorToInt((y - sceneMinY) / stepRange),
                    0,
                    sceneCachedObjects.Length - 1);
                var list = sceneCachedObjects[index];
                if (list == null)
                {
                    list = new List<ConcealableObject>();
                    sceneCachedObjects[index] = list;
                }

                list.Add(cachedObject);
            }
        }

        /// <summary>
        /// Method invoked when the scrollbar changes its value
        /// </summary>
        /// <param name="scrollbarValue">Current scrollbar value</param>
        public void OnScrollbarValueChanged(float scrollbarValue)
        {
            var step = Mathf.Clamp(Mathf.RoundToInt(scrollbarValue * CacheSteps), 1, CacheSteps - 1);
            var yLimit = sceneMinY + scrollbarValue * (sceneMaxY - sceneMinY);

            // Limit static height occluder
            if (step < currentStep)
            {
                for (var i = step; i <= currentStep; i++)
                {
                    var objects = sceneCachedObjects[i];
                    if (objects == null)
                        continue;
                    for (var index = 0; index < objects.Count; index++)
                    {
                        var concealableObject = objects[index];
                        concealableObject.SetEnabled(false);
                    }
                }
            }
            else if (step > currentStep)
            {
                for (var i = currentStep; i <= step; i++)
                {
                    var objects = sceneCachedObjects[i];
                    if (objects == null)
                        continue;
                    for (var index = 0; index < objects.Count; index++)
                    {
                        var concealableObject = objects[index];
                        concealableObject.SetEnabled(true);
                    }
                }
            }

            // Limit dynamic scenario elements view
            foreach (var scenarioElement in scenarioElements)
            {
                var y = scenarioElement.Key.transform.position.y;
                // Show scenario element if it's the last step (show everything)
                // or scenario element is below current limit
                // or it is selected
                var enable = step == CacheSteps - 1 || y <= yLimit ||
                             ScenarioManager.Instance.SelectedElement == scenarioElement.Key;
                scenarioElement.Value.SetEnabled(enable);
            }

            currentStep = step;
        }
    }
}