using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PedestrianManager : MonoBehaviour
{
    #region Singelton
    private static PedestrianManager _instance = null;
    public static PedestrianManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = GameObject.FindObjectOfType<PedestrianManager>();
                if (_instance == null)
                    Debug.LogError("<color=red>PedestrianManager" + " Not Found!</color>");
            }
            return _instance;
        }
    }
    #endregion

    #region vars
    public bool isOptimizing = true;
    public int perPedSpawnerCount = 3;
    public GameObject pedPrefab;
    public List<GameObject> pedestrians;

    private List<PedestrianSpawnerComponent> pedSpawners = new List<PedestrianSpawnerComponent>();
    
    public GameObject mainPedGO { get; private set; }
    private PedestrianComponent mainPedC;

    private List<GameObject> npcGOs = new List<GameObject>();
    private float distToUser;
    private float pedRendDistanceThreshold = 150.0f;
    private int performanceUpdateRate = 60;
    private int frameCount = 0;

    // test
    public Transform target;
    public Transform mainPedSpawnT;
    public bool testToTarget = false;
    #endregion

    #region mono
    private void Start()
    {
        GetPedSpawners();
        StartCoroutine(SpawnNPCPeds());

        // called by demo manager class
        SpawnMainPed(mainPedSpawnT);
        
    }

    private void Update()
    {
        // test
        if (testToTarget)
        {
            testToTarget = false;
            SetMainPedDestination();
        }

        frameCount++;
        if (isOptimizing && frameCount >= performanceUpdateRate)
        {
            frameCount = 0;
            OptimizePedestrians();
        }
    }

    private void OnApplicationQuit()
    {
        _instance = null;
        DestroyImmediate(gameObject);
    }
    #endregion

    #region methods
    private void GetPedSpawners()
    {
        pedSpawners.Clear();
        pedSpawners = new List<PedestrianSpawnerComponent>(FindObjectsOfType<PedestrianSpawnerComponent>());
    }
    #endregion

    #region mainPed
    public void SetMainPedDestination()
    {
        mainPedC.SetPedDestination(target);
    }

    public void SetMainPedAnimation()
    {
        mainPedC.SetPedAnimation("EnterCar");
    }

    //private void SpawnMainPed(Transform spawnT = null)
    //{
    //    Vector3 tempV = Vector3.zero;
    //    if (spawnT != null)
    //        tempV = spawnT.position;

    //    mainPedGO = Instantiate(pedPrefab, tempV, Quaternion.identity, this.transform);
    //    Instantiate(pedestrians[(int)Random.Range(0, pedestrians.Count)], mainPedGO.transform);
    //    mainPedGO.name = "MainPedestrian";

    //    mainPedC = mainPedGO.GetComponent<PedestrianComponent>();
    //    if (mainPedC != null)
    //        mainPedC.InitPed(true);
    //}

    public PedestrianComponent SpawnMainPed(Transform spawnT = null)
    {
        if (mainPedGO != null)
            Destroy(mainPedGO);

        Vector3 tempV = Vector3.zero;
        if (spawnT != null)
            tempV = spawnT.position;

        mainPedGO = Instantiate(pedPrefab, tempV, Quaternion.identity, this.transform);
        Instantiate(pedestrians[(int)Random.Range(0, pedestrians.Count)], mainPedGO.transform);
        mainPedGO.name = "MainPedestrian";

        mainPedC = mainPedGO.GetComponent<PedestrianComponent>();
        if (mainPedC != null)
            mainPedC.InitPed(true);

        return mainPedC;
    }
    #endregion

    #region npcPed
    private IEnumerator SpawnNPCPeds()
    {
        npcGOs.Clear();
        for (int i = 0; i < pedSpawners.Count; i++)
        {
            for (int j = 0; j < perPedSpawnerCount; j++)
            {
                GameObject ped = Instantiate(pedPrefab, pedSpawners[i].GetPositionBetweenTargets(), Quaternion.identity, pedSpawners[i].transform);
                npcGOs.Add(ped);
                Instantiate(pedestrians[(int)Random.Range(0, pedestrians.Count)], ped.transform);
                PedestrianComponent pedC = ped.GetComponent<PedestrianComponent>();
                if (pedC != null)
                    pedC.InitPed();
                yield return new WaitForEndOfFrame();
            }
            yield return new WaitForEndOfFrame();
        }
    }
    #endregion

    #region optimize
    private void OptimizePedestrians()
    {
        for (int i = 0; i < npcGOs.Count; i++)
        {
            distToUser = TrafPerformanceManager.Instance.DistanceToNearestPlayerCamera(npcGOs[i].transform.position);
            npcGOs[i].SetActive(distToUser < pedRendDistanceThreshold);
        }
    }
    #endregion
}
