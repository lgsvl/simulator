using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManualEventControl : UnitySingleton<ManualEventControl> {

    public VehicleController playerVehicleCtrl;
	// Update is called once per frame
	void Update ()
    {
        //if (Input.GetKeyDown(KeyCode.Keypad0))
        //{
        //    TriggerHardBrakingNearFront();
        //}
        //else if (Input.GetKeyDown(KeyCode.Keypad1))
        //{
        //    TriggerLaneSwerveNearFront();
        //}
        //else if (Input.GetKeyDown(KeyCode.Keypad2))
        //{
        //    TriggerAccidentFarFront();
        //}
        //else if (Input.GetKeyDown(KeyCode.Keypad3))
        //{
        //    TriggerHardBrakingFarFront();
        //}
        //else if (Input.GetKeyDown(KeyCode.Keypad4))
        //{
        //    TriggerLaneSwerveFarFront();
        //}
    }

    public void TriggerHardBrakingNearFront()
    {
        var cols = Physics.OverlapSphere(playerVehicleCtrl.carCenter.position, 20.0f);
        TrafAIMotor closestCar = null;
        float minDist = 1000f;
        foreach (var col in cols)
        {
            var carFound = col.GetComponentInParent<TrafAIMotor>();
            if (carFound != null)
            {
                if (Vector3.Dot(playerVehicleCtrl.carCenter.forward, carFound.nose.forward) > 0.7f &&
                    Vector3.Dot(playerVehicleCtrl.carCenter.forward, (carFound.nose.position - playerVehicleCtrl.carCenter.position).normalized) > 0.85f)
                {
                    var dist = Vector3.Distance(playerVehicleCtrl.carCenter.position, carFound.nose.position);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        closestCar = carFound;
                    }
                }
            }
        }

        if (closestCar != null)
        {
            closestCar.StartCoroutine(closestCar.HoldEmergencyHardBrakeState());
            //var carAI = closestCar.GetComponent<CarAIController>();
        }
    }

    public void TriggerLaneSwerveNearFront()
    {
        //This is rough pick, individual NPC car logic that cause danger to player will be handled separately in each NPC car's logic loop
        var cols = Physics.OverlapSphere(playerVehicleCtrl.carCenter.position, 20f);
        TrafAIMotor closestCar = null;
        float minDist = 1000f;
        foreach (var c in cols)
        {
            var carFound = c.GetComponentInParent<TrafAIMotor>();
            if (carFound != null)
            {
                var dist = Vector3.Distance(playerVehicleCtrl.carCenter.position, carFound.nose.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    closestCar = carFound;
                }
            }

            if (closestCar != null)
            {
                closestCar.triggerShiftToPlayer = true;
                closestCar.playerVehicleCtrl = playerVehicleCtrl;
            }
        }
    }

    public void TriggerAccidentFarFront()
    {
        //Find a car collision accident happen in the front direction is at around 80-150 meters
        //If there is any car that is less than 15 meters away from another closest collidable collider then let the car to rush into the closest collider and cause a collision
        //If the intentional collision can not be made after several seconds then make the car to have a single car accident (ex, like engine fire)
        var playerPos = playerVehicleCtrl ? playerVehicleCtrl.carCenter.position : transform.position;
        var playerFwd = playerVehicleCtrl ? playerVehicleCtrl.carCenter.forward : transform.forward;
        List<Collider> cols = FindCollidersInRangedDistanceInFront(playerPos, playerFwd, 100f, 200f, 30f, ColliderIsPartOfAICar);
        if (cols != null && cols.Count > 1)
        {       
            //Shuffle cols
            cols.Shuffle<Collider>();
        }

        foreach (var col in cols)
        {
            var aiMotor = col.GetComponentInParent<TrafAIMotor>();
            if (aiMotor != null)
            {
                if (aiMotor.IsInAccident())
                {
                    continue;
                }
                var colliders = FindCollidersInRangedDistanceInFront(aiMotor.transform.position, aiMotor.transform.forward, 1f, 25f, 125f);
                if (colliders != null && colliders.Count > 0)
                {
                    Collider closestCol = null;
                    float closestDist = 25f;
                    foreach (var c in colliders)
                    {
                        if (c == col)
                        {
                            continue;
                        }

                        var meshCol = c as MeshCollider;
                        if (meshCol != null && !meshCol.convex)
                        {
                            continue;
                        }

                        if (!(c.gameObject.layer == LayerMask.NameToLayer("EnvironmentProp")) && !c.GetComponentInParent<TrafAIMotor>())
                        {
                            continue;
                        }

                        var d = Vector3.Distance(aiMotor.transform.position, c.bounds.center);
                        if (d < closestDist)
                        {
                            closestDist = d;
                            closestCol = c;
                        }
                    }

                    if (closestCol != null)
                    {
                        //Setup special collision mode and rush into
                        aiMotor.ForceCollide(closestCol);
                        Debug.Log("Force Colliding!!!");
                        //Debug
                        {
                            var go = new GameObject("ForceCollide!!!");
                            go.AddComponent<DebugTracker>().sourceObj = aiMotor.gameObject;
                            go.transform.position = aiMotor.transform.position;
                            Destroy(go, 10f); //Debug object will be cleared automatically
                        }

                        break;
                    }
                    else
                    {
                        var carAI = aiMotor.GetComponent<CarAIController>();
                        carAI.SetCarInAccidentState();
                        Debug.Log("Engine Breakdown!!!");
                        aiMotor.EngineBreakDown();
                    }
                }
                else
                {
                    var carAI = aiMotor.GetComponent<CarAIController>();
                    carAI.SetCarInAccidentState();
                    Debug.Log("Engine Breakdown!!!");
                    aiMotor.EngineBreakDown();
                }
            }
        }
    }

    public void TriggerLaneSwerveFarFront()
    {
        var playerPos = playerVehicleCtrl ? playerVehicleCtrl.carCenter.position : transform.position;
        var playerFwd = playerVehicleCtrl ? playerVehicleCtrl.carCenter.forward : transform.forward;
        List<Collider> cols = FindCollidersInRangedDistanceInFront(playerPos, playerFwd, 100f, 200f, 30f, ColliderIsPartOfAICar);
        if (cols != null && cols.Count > 1)
        {
            //Shuffle cols
            cols.Shuffle<Collider>();
        }

        foreach (var col in cols)
        {
            var aiMotor = col.GetComponentInParent<TrafAIMotor>();
            if (aiMotor != null)
            {
                if (aiMotor.IsInAccident())
                {
                    continue;
                }

                aiMotor.StartCoroutine(aiMotor.HoldEmergencyHardBrakeState());
                break;
            }
        }
    }

    public void TriggerHardBrakingFarFront()
    {
        var playerPos = playerVehicleCtrl ? playerVehicleCtrl.carCenter.position : transform.position;
        var playerFwd = playerVehicleCtrl ? playerVehicleCtrl.carCenter.forward : transform.forward;
        List<Collider> cols = FindCollidersInRangedDistanceInFront(playerPos, playerFwd, 100f, 200f, 30f, ColliderIsPartOfAICar);
        if (cols != null && cols.Count > 1)
        {
            //Shuffle cols
            cols.Shuffle<Collider>();
        }

        foreach (var col in cols)
        {
            var aiMotor = col.GetComponentInParent<TrafAIMotor>();
            if (aiMotor != null)
            {
                if (aiMotor.IsInAccident())
                {
                    continue;
                }
                break;
            }
        }
    }

    public List<Collider> FindCollidersInRangedDistanceInFront(Vector3 origin, Vector3 normForward, float minDist = 100f, float maxDist = 200f, float viewAngle = 45f, System.Func<Collider, bool> condition = null)
    {
        var cols = Physics.OverlapSphere(origin, maxDist);
        List<Collider> retList = new List<Collider>();
        foreach (var col in cols)
        {
            if (condition != null)
            {
                if (!condition(col))
                {
                    continue;
                }
            }

            var OrigToColVec = col.transform.position - origin;
            if (OrigToColVec.magnitude < minDist)
            {
                continue;
            }

            if (Vector3.Angle(normForward, OrigToColVec.normalized) > (viewAngle * 0.5f))
            {
                continue;
            }

            retList.Add(col);
        }

        return retList;
    }

    bool ColliderIsPartOfAICar(Collider c)
    {
        if (c.GetComponentInParent<TrafAIMotor>())
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}

public static class ExtensionUtility
{
    static System.Random rand = new System.Random();
    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rand.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}
