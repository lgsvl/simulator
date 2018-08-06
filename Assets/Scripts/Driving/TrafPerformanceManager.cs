using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TrafPerformanceManager : MonoBehaviour
{
    static TrafPerformanceManager instance;

    public List<Camera> focusCameras;

    [Header("FPS Optimize")]
    public bool optimizeDistantCarRender = true;
    private bool optimizeDistantCarRender_pre = true;
    public bool optimizeDistantCarPhysics = true;
    private bool optimizeDistantCarPhysics_pre = true;

    [Header("Assisting Traffic")]
    public bool autoAssistingTraffic = true;
    public bool silentAssisting = true; //NPC car will be killed and respawned only when player is far away and can not see it

    [Space(10)]
    private HashSet<CarAIController> AICarSet;
    public TrafSpawner trafficSpawner;
    [System.NonSerialized]
    public float performanceCheckInterval = 1.0f;
    [System.NonSerialized]
    public float lightPerformanceCheckInterval = 1.5f;
    [System.NonSerialized]
    public float carRendDistanceThreshold = 225.0f;
    [System.NonSerialized]
    public float lightIndirectDistanceThreshold = 200.0f;
    [System.NonSerialized]
    public float carSimDistanceThreshold = 200.0f;

    //Detect field change
    void OnValidate()
    {
        if (Application.isPlaying)
        {
            if (optimizeDistantCarRender_pre != optimizeDistantCarRender
                || optimizeDistantCarPhysics_pre != optimizeDistantCarPhysics)
            {
                if (optimizeDistantCarRender || optimizeDistantCarPhysics)
                { UpdateCarPerformanceBehavior(true); }
                else
                { UpdateCarPerformanceBehavior(false); }
            }

            optimizeDistantCarRender_pre = optimizeDistantCarRender;
            optimizeDistantCarPhysics_pre = optimizeDistantCarPhysics;
        }
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    void Start()
    {
        int validFocusCams = 0;
        foreach (var cam in focusCameras)
        {
            if (cam != null)
            {
                ++validFocusCams;
            }
        }
        if (validFocusCams < 1)
        {
            focusCameras = new List<Camera>(GameObject.FindGameObjectsWithTag("DuckieCam").Select(go => go.GetComponent<Camera>()));
            if (focusCameras.Count < 1)
            {
                focusCameras = new List<Camera>() { Camera.main };
            }
        }

        if (AICarSet != null)
        { AICarSet.Clear(); }
        else
        { AICarSet = new HashSet<CarAIController>(); }
    }

    public float DistanceToNearestPlayerCamera(Vector3 pos)
    {
        float minDist = 10000.0f;

        for (int i = 0; i < focusCameras.Count; i++)
        {
            var cam = focusCameras[i];
            if (cam == null)
            {
                focusCameras.RemoveAt(i);
                --i;
                continue;
            }
            var dist = Vector3.Distance(pos, cam.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
            }
        }
        return minDist;
    }

    public static TrafPerformanceManager GetInstance()
    {
        return instance;
    }

    public HashSet<CarAIController> GetCarSet()
    {
        return AICarSet;
    }

    public void AddAICar(CarAIController carAI)
    {        
        var AICar = carAI.GetComponent<CarAIController>();
        if (AICar != null)
        {
            AICarSet.Add(AICar);
        }        
    }

    public bool RemoveAICar(CarAIController carAI)
    {
        return AICarSet.Remove(carAI);        
    }

    void UpdateCarPerformanceBehavior(bool withOptimization)
    {
        if (AICarSet == null || AICarSet.Count < 1)
            return;

        Debug.Log("Current AI Car Number: " + AICarSet.Count);

        if (withOptimization)
        {
            foreach (var AICar in AICarSet)
            {
                if (AICar == null)
                    continue;

                if (AICar != null)
                {
                    AICar.CancelInvoke(nameof(AICar.UpdateCarPerformance));
                    AICar.CancelInvoke(nameof(AICar.UpdateCarPerformanceRenderOnly));
                    AICar.CancelInvoke(nameof(AICar.UpdateCarPerformancePhysicsOnly));
                    if (optimizeDistantCarRender && optimizeDistantCarPhysics)
                    {
                        AICar.InvokeRepeating(nameof(AICar.UpdateCarPerformance), Random.Range(0.0f, performanceCheckInterval), performanceCheckInterval);
                    }
                    else if (optimizeDistantCarRender)
                    {
                        AICar.SetCarSimNormal();
                        AICar.InvokeRepeating(nameof(AICar.UpdateCarPerformanceRenderOnly), Random.Range(0.0f, performanceCheckInterval), performanceCheckInterval);
                    }
                    else if (optimizeDistantCarPhysics)
                    {
                        AICar.SetCarInRenderRange();
                        AICar.InvokeRepeating(nameof(AICar.UpdateCarPerformancePhysicsOnly), Random.Range(0.0f, performanceCheckInterval), performanceCheckInterval);
                    }
                }
            }
        }
        else
        {
            foreach (var AICar in AICarSet)
            {
                if (AICar == null)
                    continue;

                if (AICar != null)
                {
                    AICar.CancelInvoke(nameof(AICar.UpdateCarPerformance));
                    AICar.CancelInvoke(nameof(AICar.UpdateCarPerformanceRenderOnly));
                    AICar.CancelInvoke(nameof(AICar.UpdateCarPerformancePhysicsOnly));
                    AICar.SetCarInRenderRange();
                    AICar.SetCarSimNormal();
                }
            }
        }
    }
}
