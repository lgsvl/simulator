using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulatorManager : MonoBehaviour
{
    #region Singleton
    private static SimulatorManager _instance = null;
    public static SimulatorManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = GameObject.FindObjectOfType<SimulatorManager>();
                if (_instance == null)
                    Debug.LogError("<color=red>SimulatorManager Not Found!</color>");
            }
            return _instance;
        }
    }
    #endregion

    #region vars
    public GameObject dashUIPrefab; // TODO dev hack
    private List<GameObject> activeVehicles = new List<GameObject>();

    private GameObject currentActive = null;
    #endregion

    #region mono
    private void Awake()
    {
        if (_instance == null)
            _instance = this;

        if (_instance != this)
        {
            DestroyImmediate(gameObject);
        }
        else
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    private void Start()
    {
        activeVehicles.Clear();
    }

    private void OnApplicationQuit()
    {
        _instance = null;
        DestroyImmediate(gameObject);
    }
    #endregion

    #region active vehicles
    public void AddActiveVehicle(GameObject go)
    {
        if (go == null) return;
        activeVehicles.Add(go);
    }

    public void RemoveActiveVehicle(GameObject go)
    {
        if (go == null) return;
        activeVehicles.Remove(go);
    }

    public void SetCurrentActiveVehicle(GameObject go)
    {
        if (go == null) return;
        currentActive = go;
        go?.GetComponent<VehicleController>().SetDashUIState();
    }

    public bool CheckCurrentActiveVehicle(GameObject go)
    {
        return go == currentActive;
    }

    public GameObject GetCurrentActiveVehicle()
    {
        return currentActive;
    }
    #endregion

    public void SpawnDashUI()
    {
        Instantiate(dashUIPrefab);
    }
}
