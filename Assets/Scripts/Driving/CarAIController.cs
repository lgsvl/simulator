using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public struct TrafficNetworkInfoFrame
{
    [System.NonSerialized]
    public string userid;
    public double latitude;
    public double longitude;
    public string location;
    public double rotation;
    public double VehicleSpeed;
    public double VehicleSpeedOverGnd;
    public long capturedTimestamp;
    public long sentTimestamp;
    public bool NPC;

    public string ToJSON(bool pretty = false)
    {
        return JsonUtility.ToJson(this, pretty);
    }
}

[System.Serializable]
public struct TrafficNetworkEventFrame
{
    [System.NonSerialized]
    public string userid;
    public double latitude;
    public double longitude;
    public string location;
    public double rotation;
    public double VehicleSpeed;
    public double VehicleSpeedOverGnd;
    public long capturedTimestamp;
    public long sentTimestamp;
    public string EventId;
    public string EventDetected;
    public bool NPC;

    public string ToJSON(bool pretty = false)
    {
        return JsonUtility.ToJson(this, pretty);
    }
}

public class CarAIController : MonoBehaviour
{
    public bool DEBUG;

    private bool inited = false;

    public string carID = string.Empty;

    [System.NonSerialized]
    public List<Renderer> allRenderers;

    public enum HeadLightState { Off, Low, High, }
    public HeadLightState headlightState = HeadLightState.Off;

    public bool inAccident = false;

    bool flashLeft = false;
    bool flashRight = false;
    bool isWaiting = false;
    bool inHeavyFog = false;
    private const float fogDensityThreshold = 0.03f;
    public TrafAIMotor aiMotor;
    public List<Collider> carColliders;
    public List<Renderer> headLightRends;
    public List<Light> headSpotLights;
    public readonly static Color headBeamColor = new Color(1.0f, 1.0f, 1.0f);
    public const float lowBeamIntensity = 2.4f;
    public const float highBeamIntensity = 4.0f;
    public List<BrakeLightController> brakeLightControls;
    public List<Renderer> turnSignalLeft;
    public List<Renderer> turnSignalRight;
    private Renderer[] renderers;

    public float distToNearestPlayer;

    [System.NonSerialized]
    public bool inRenderRange = true;

    public enum SimUpdateRate { Low, Normal }
    public SimUpdateRate simUpdateRate;

    VFXManager vfxManager;

    void Start ()
    {
        if (vfxManager == null)
        {
            vfxManager = VFXManager.GetInstance<VFXManager>();
        }
        if (!inited)
            Init();
    }

    public void Init()
    {
        if (inited)
            return;
        inited = true;

        //Debug.Log("Car " + gameObject.name + " was initiated");

        allRenderers = GetComponentsInChildren<Renderer>().ToList<Renderer>();

        simUpdateRate = SimUpdateRate.Normal;

        inAccident = false;
        if (aiMotor == null)
            aiMotor = GetComponentInChildren<TrafAIMotor>();
        if (carColliders == null)
            carColliders = GetComponentsInChildren<Collider>().ToList<Collider>();
        if (renderers == null)
            renderers = GetComponentsInChildren<Renderer>();
        if (brakeLightControls.Count < 1)
        {
            brakeLightControls.AddRange(GetComponentsInChildren<BrakeLightController>());
        }
        turnSignalLeft.ForEach(r => SetLight(r, false));
        turnSignalRight.ForEach(r => SetLight(r, false));
        SetHeadLights(HeadLightState.Off);

        CheckTimeOfDayEvents();

        var interval = TrafPerformanceManager.GetInstance().performanceCheckInterval;
        if (TrafPerformanceManager.GetInstance().optimizeDistantCarRender
            && TrafPerformanceManager.GetInstance().optimizeDistantCarPhysics)
        { InvokeRepeating(nameof(UpdateCarPerformance), Random.Range(0.0f, interval), interval); }
        else if(TrafPerformanceManager.GetInstance().optimizeDistantCarRender)
        { InvokeRepeating(nameof(UpdateCarPerformanceRenderOnly), Random.Range(0.0f, interval), interval); }
        else if (TrafPerformanceManager.GetInstance().optimizeDistantCarPhysics)
        { InvokeRepeating(nameof(UpdateCarPerformancePhysicsOnly), Random.Range(0.0f, interval), interval); }
    }

    public void ReSpawn()
    {
        //Debug.Log("Car is trying to respawn...");
        inited = false;
        if (TrafSpawner.GetInstance().Spawn(true, false, this))//Attempt a definite spawn
        {
            Debug.Log("Car respawned successfully.");
        }
        else
        {
            Debug.Log("Car failed to respawn.");
        }
    }

    public void ReSpawnSilent()
    {
        //Debug.Log("Car is trying to respawn...");
        inited = false;
        if (TrafSpawner.GetInstance().Spawn(true, true, this))//Attempt a definite spawn
        { 
            Debug.Log("Car respawned successfully in invisible area."); 
        }
        else
        {
            Debug.Log("Car failed to respawn silently.");
        }
    }

    public void UpdateCarPerformance()
    {        
        distToNearestPlayer = TrafPerformanceManager.GetInstance().DistanceToNearestPlayerCamera(transform.position);
        if (distToNearestPlayer < TrafPerformanceManager.GetInstance().carRendDistanceThreshold)
        {
            if (!inRenderRange)
            { SetCarInRenderRange(); }
        }
        else
        {
            if (inRenderRange)
            { SetCarOutOfRenderRange(); }
        }

        if (distToNearestPlayer < TrafPerformanceManager.GetInstance().carSimDistanceThreshold)
        { SetCarSimNormal(); }
        else
        { SetCarSimLow(); }
    }

    public void UpdateCarPerformanceRenderOnly()
    {
        distToNearestPlayer = TrafPerformanceManager.GetInstance().DistanceToNearestPlayerCamera(transform.position);
        if (distToNearestPlayer < TrafPerformanceManager.GetInstance().carRendDistanceThreshold)
        {
            if (!inRenderRange)
            { SetCarInRenderRange(); }
        }
        else
        {
            if (inRenderRange)
            { SetCarOutOfRenderRange(); }
        }
    }

    public void UpdateCarPerformancePhysicsOnly()
    {
        distToNearestPlayer = TrafPerformanceManager.GetInstance().DistanceToNearestPlayerCamera(transform.position);
        if (distToNearestPlayer < TrafPerformanceManager.GetInstance().carSimDistanceThreshold)
        { SetCarSimNormal(); }
        else
        { SetCarSimLow(); }
    }

    public void SetCarInRenderRange()
    {
        inRenderRange = true;
        foreach (var r in renderers)
        {
            r.enabled = true;
        }
        CheckTimeOfDayEvents();
    }

    public void RandomSelectCarPaintTexture()
    {
        var texRandomizer = GetComponent<TextureRandomizer>();
        if (texRandomizer != null)
        {
            texRandomizer.RandomSelectTexture();
        }
    }

    public void SetCarOutOfRenderRange()
    {
        inRenderRange = false;
        foreach (var r in renderers)
        {
            r.enabled = false;
        }
        SetHeadLights(HeadLightState.Off);
        headlightState = HeadLightState.Off;
    }

    public void SetCarSimNormal()
    {
        if (simUpdateRate != SimUpdateRate.Normal)
        {
            simUpdateRate = SimUpdateRate.Normal;
        }
    }

    public void SetCarSimLow()
    {
        if (simUpdateRate != SimUpdateRate.Low)
        {
            simUpdateRate = SimUpdateRate.Low;
            aiMotor.lowResTimestamp = Time.time;
            aiMotor.lowResPhysicsTimestamp = Time.fixedTime;
        }
    }

    void Update ()
    {
        if (inAccident)
            return;

        if (aiMotor != null && aiMotor.enabled)
        {
            if (aiMotor.brakeHard || aiMotor.currentSpeed < 0.05f)
            {
                SetBrakeLights(true);
            }
            else
            {
                SetBrakeLights(false);
            }

            if (aiMotor.leftTurn || aiMotor.rightTurn)
            {
                SetFlashing(aiMotor.leftTurn, aiMotor.rightTurn);
            }
            else
            {
                SetFlashing(false, false);
            }

            if (aiMotor.blockedByTraffic && !isWaiting)
            {
                isWaiting = true;
                StartCoroutine(BlockedDriverReaction());
            }
        }

        if (inHeavyFog)
        {
            if (RenderSettings.fogDensity < fogDensityThreshold)
            {
                inHeavyFog = false;
                OnFogClear();
            }
        }
        else
        {
            if (RenderSettings.fogDensity > fogDensityThreshold)
            {
                inHeavyFog = true;
                OnFog();
            }
        }
    }

    private void SetLight(Renderer rend, bool state)
    {
        if (state)
        {
            foreach (var mat in rend.materials)
            {
                mat.EnableKeyword("_EMISSION");
            }
        }
        else
        {
            foreach (var mat in rend.materials)
            {
                mat.DisableKeyword("_EMISSION");
            }
        }
    }

    private void SetHeadLightMat(Renderer rend, HeadLightState state)
    {
        if (state == HeadLightState.Off)
        {
            foreach (var mat in rend.materials)
            {
                mat.DisableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Color.black);
            }
        }
        else
        {
            foreach (var mat in rend.materials)
            {
                mat.EnableKeyword("_EMISSION");
            }

            if (state == HeadLightState.Low)
            {
                foreach (var mat in rend.materials)
                {
                    mat.SetColor("_EmissionColor", headBeamColor * lowBeamIntensity);
                }
            }
            else if (state == HeadLightState.High)
            {
                foreach (var mat in rend.materials)
                {
                    mat.SetColor("_EmissionColor", headBeamColor * highBeamIntensity);
                }
            }
        }
    }

    private void SetHeadSpotLight(Light light, HeadLightState state)
    {
        if (state == HeadLightState.Off)
        {
            light.enabled = false;
            light.intensity = 0.0f;
        }
        else
        {
            light.enabled = true;

            var t = light.transform;
            if (state == HeadLightState.Low)
            {
                light.enabled = true;
                light.intensity = 0.9f;
                light.range = 40.0f;
                light.spotAngle = 30.0f;
                t.localEulerAngles = new Vector3(10.0f, t.localEulerAngles.y, t.localEulerAngles.z);
            }
            else if (state == HeadLightState.High)
            {
                light.enabled = true;
                light.intensity = 3.0f;
                light.range = 100.0f;
                light.spotAngle = 70.0f;
                t.localEulerAngles = new Vector3(0.0f, t.localEulerAngles.y, t.localEulerAngles.z);
            }
        }
    }

    IEnumerator Flasher()
    {
        if (brakeLightControls.Count < 1) { yield break; }

        while (flashLeft || flashRight)
        {
            if (flashLeft)  turnSignalLeft.ForEach(r =>  SetLight(r, true));
            if (flashRight) turnSignalRight.ForEach(r => SetLight(r, true));

            yield return new WaitForSeconds(0.5f);

            turnSignalLeft.ForEach(r => SetLight(r, false));
            turnSignalRight.ForEach(r => SetLight(r, false));
            yield return new WaitForSeconds(0.5f);
        }
        yield break;
    }

    IEnumerator BlockedDriverReaction()
    {
        yield return new WaitForSeconds(Random.Range(2.0f, 5.0f));
        isWaiting = false;
        yield break;
    }

    void SetFlashing(bool left, bool right)
    {
        bool doStart = (flashLeft == false && flashRight == false) && (left || right);

        flashLeft = left;
        flashRight = right;

        if (doStart || inAccident)
        {
            StopCoroutine(Flasher());
            StartCoroutine(Flasher());
        }
    }

    void SetBrakeLights(bool state)
    {
        if (brakeLightControls.Count < 1) { return; }
        foreach (var brakeLightControl in brakeLightControls)
        {
            brakeLightControl.SetBrakeLight(state);
        }
    }

    void SetHeadLights(HeadLightState state)
    {
        headLightRends.ForEach(r => SetHeadLightMat(r, state));
        headSpotLights.ForEach(l => SetHeadSpotLight(l, state));
    }

    void CheckTimeOfDayEvents()
    {
        if (DayNightEventsController.Instance.currentPhase == DayNightEventsController.Phase.Sunrise)
            OnSunRise();
        if (DayNightEventsController.Instance.currentPhase == DayNightEventsController.Phase.Day)
            OnDay();
        if (DayNightEventsController.Instance.currentPhase == DayNightEventsController.Phase.Sunset)
            OnSunSet();
        if (DayNightEventsController.Instance.currentPhase == DayNightEventsController.Phase.Night)
            OnNight();
    }

    IEnumerator CollisionReaction()
    {
        yield return new WaitForSeconds(Random.Range(0.5f, 1.0f));
        SetFlashing(true, true);
        //yield return new WaitForSeconds(Random.Range(10.0f, 30.0f));
        //aiMotor.enabled = true;
        yield break;
    }

    private void OnCollisionEnter(Collision collision)
    {
        var carAI = collision.gameObject.GetComponent<CarAIController>();

        if (inAccident)        
            return;
        
        //Right now NPC car will not trigger accident upon collision of another one unless any one is in force collide mode
        if (carAI != null && !carAI.inAccident && !carAI.aiMotor.forceToCollideMode && !aiMotor.forceToCollideMode)        
            return;
        
        if (collision.gameObject.CompareTag("Player") || (carAI != null && carAI.inAccident) || (aiMotor.forceToCollideMode && (carAI != null || collision.gameObject.layer == LayerMask.NameToLayer("EnvironmentProp"))))
        {
            //accident happens
            Debug.Log("car collision happened at time: " + Time.time);
            SetCarInAccidentState();

            //Calculate and spawn collision VFX
            var normal = collision.contacts[0].normal;
            Vector3 normalVel = Vector3.Project(aiMotor.currentSpeed * aiMotor.nose.forward, normal);
            Vector3 otherNormalVel;
            if (carAI != null && carAI.inAccident)
            {
                otherNormalVel = Vector3.Project(carAI.GetComponent<Rigidbody>().velocity, -normal);
            }
            else 
            {
                otherNormalVel = Vector3.Project(carAI != null ? (carAI.aiMotor.currentSpeed * carAI.aiMotor.nose.forward) : Vector3.zero, -normal);
            }
            var VelDiff = normalVel - otherNormalVel;
            if (VelDiff.magnitude > 10f)
            {
                var extraVel = VelDiff.normalized * (VelDiff.magnitude - 10f);
                vfxManager.SpawnVFX(vfxManager.settings.carCollisionParticleVFX.sourceGo, collision.contacts[0].point, VFXManager.VFXType.CarCollisionParticles, extraVel, 0f, 5f);
            }
            
            StartCoroutine(CollisionReaction());
            if (aiMotor.forceToCollideMode)
            {
                aiMotor.GenerateEngineDamageSmoke();
            }
        }
    }

    public void SetCarInAccidentState()
    {
        inAccident = true;
        SetBrakeLights(true);
        aiMotor.currentSpeed = 0f;
        aiMotor.CancelInvoke("CheckHeight");
    }

    public void OnSunRise()
    {
        if (headlightState != HeadLightState.Off)
        {
            SetHeadLights(HeadLightState.Off);
            headlightState = HeadLightState.Off;
        }
    }
    public void OnDay()
    {
        if (headlightState != HeadLightState.Off)
        {
            SetHeadLights(HeadLightState.Off);
            headlightState = HeadLightState.Off;
        }
    }
    public void OnSunSet()
    {
        if (distToNearestPlayer > TrafPerformanceManager.GetInstance().carRendDistanceThreshold)
            return;

        if (headlightState != HeadLightState.Low)
        {
            SetHeadLights(HeadLightState.Low);
            headlightState = HeadLightState.Low;
        }
    }
    public void OnNight()
    {
        if (distToNearestPlayer > TrafPerformanceManager.GetInstance().carRendDistanceThreshold)
            return;

        if (headlightState != HeadLightState.Low)
        {
            SetHeadLights(HeadLightState.Low);
            headlightState = HeadLightState.Low;
        }
    }
    public void OnFog()
    {
        if (headlightState != HeadLightState.High)
        {
            SetHeadLights(HeadLightState.High);
            headlightState = HeadLightState.High;
        }
    }

    public void OnFogClear()
    {
        if (headlightState != HeadLightState.Off)
        {
            SetHeadLights(HeadLightState.Off);
            headlightState = HeadLightState.Off;
        }
    }

    void OnDisable()
    {
        CancelInvoke();
    }

    void OnDestroy()
    {
        //Debug.Log("Car " + gameObject.name + " was destroyed");
        CancelInvoke();
        if (TrafPerformanceManager.GetInstance().RemoveAICar(this))
        {
            --TrafSpawner.GetInstance().totalTrafficCarCount;
            var trafNetManager = TrafNetworkManager.GetInstance<TrafNetworkManager>();
            if (trafNetManager != null && trafNetManager.freeIdPool != null)
            {
                trafNetManager.freeIdPool.Enqueue(carID);
            }
        }        
    }
}
