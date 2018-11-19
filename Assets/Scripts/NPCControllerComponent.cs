using System.Collections.Generic;
using UnityEngine;

public class NPCControllerComponent : MonoBehaviour
{
    #region vars
    // physics
    private Vector3 lastRBPosition;
    private Rigidbody rb;
    private Vector2 normalSpeedRange = new Vector2(8.0f, 25.0f);
    private float normalSpeed = 20.0f;
    private float targetSpeed;
    private float currentSpeed;
    private float currentSpeed_measured;
    private float followSpeed;
    private float targetTurn;
    private float currentTurn = 0f;
    private RaycastHit groundHit;
    private int groundHitBitmask = -1;

    // inputs
    public const float maxTurn = 75f;

    // targeting
    // TODO need extra data class for HD map data
    public Transform frontCenter;
    public Transform frontLeft;
    public Transform frontRight;
    private Vector3 currentTarget;
    private int currentIndex = 0;
    //public Vector3 currentTargetTangent;
    //private Vector3 nextTarget; for intersection checks or new node waypoint[0]
    private List<Vector3> laneData = new List<Vector3>();

    // wheels TODO spawn instead
    private Transform wheelFL;
    private Transform wheelFR;
    private Transform wheelRL;
    private Transform wheelRR;
    private float theta = 0f;
    private float newX = 0f;
    private float lastX = 0f;
    private float radius = 0.32f;

    private bool doRaycast;
    private float nextRaycast = 0f;
    private const float frontRaycastDistance = 40f;
    private const float hardStopDistance = 1.75f;
    private const float heavyStopDistance = 4.5f;
    private const float safeReactionTime = 2.0f;
    static int carCheckBlockBitmask = -1;
    private const float speedAdjustRate = 2.0f;
    private const float turnAdjustRate = 3.0f;
    private RaycastHit frontClosestHitInfo = new RaycastHit();
    private bool detectFront;

    private bool isInit = false;
    #endregion

    #region mono
    private void Awake()
    {
        if (carCheckBlockBitmask == -1)
        {
            carCheckBlockBitmask = ~(1 << LayerMask.NameToLayer("Ground And Road") | 1 << LayerMask.NameToLayer("PlayerConstrain") | 1 << LayerMask.NameToLayer("Sensor Effects"));
        }
    }

    private void Start()
    {
        Init();
    }

    private void Update()
    {
        if (!isInit) return;

        DeterminTargetSpeed();
        DeterminTargetTurn();
        AdjustToTargetSpeedAndTurn(Time.deltaTime);
        WheelMovement();        
        EvaluateTarget();
    }

    private void FixedUpdate()
    {
        if (!isInit) return;

        CalculateSpeed(Time.fixedDeltaTime);
        NPCMove();
        NPCTurn();
    }
    #endregion

    #region init
    private void Init()
    {
        rb = GetComponent<Rigidbody>();
        groundHitBitmask = 1 << LayerMask.NameToLayer("Ground And Road") | 1 << LayerMask.NameToLayer("Road Shoulder");

        CreateFrontTransforms();
        GetWheelComponents();
        isInit = true;

        normalSpeed = Random.Range(normalSpeedRange.x, normalSpeedRange.y);
        currentSpeed = 0f;
        currentSpeed_measured = currentSpeed;
        targetSpeed = normalSpeed;
        followSpeed = normalSpeed;
        doRaycast = false;
        nextRaycast = 0f;
        detectFront = false;
    }

    private void CreateFrontTransforms()
    {
        if (frontCenter != null && frontLeft != null && frontRight != null)
        {
            //Debug.Log("All front transforms were setup manually, skip creating front transforms");
            return;
        }

        RaycastHit frontHit;
        Collider col = GetComponent<Collider>();
        if (Physics.Raycast(col.bounds.center + transform.forward * 20f, -transform.forward, out frontHit, 25f, 1 << LayerMask.NameToLayer("NPC"))) // TODO set for any collider extends in world pos
        {
            if (frontHit.collider.name == this.gameObject.name)
            {
                // set up nose transform
                GameObject go = new GameObject("FrontCenter");
                go.transform.position = frontHit.point;
                go.transform.SetParent(transform);

                frontCenter = go.transform;
                frontCenter.localRotation = Quaternion.identity;
            }
        }

        if (frontCenter == null) return;

        if (Physics.Raycast(frontCenter.position + transform.right * 20f - transform.forward * 0.1f, -transform.right, out frontHit, 25f, 1 << LayerMask.NameToLayer("NPC"))) // TODO set for any collider extends in world pos
        {
            if (frontHit.collider.name == this.gameObject.name)
            {
                // set up nose transform
                GameObject go = new GameObject("FrontRight");
                go.transform.position = frontHit.point;
                go.transform.SetParent(transform);
                frontRight = go.transform;
                frontRight.localRotation = Quaternion.identity;
            }
        }
        if (Physics.Raycast(frontCenter.position + -transform.right * 20f - transform.forward * 0.1f, transform.right, out frontHit, 25f, 1 << LayerMask.NameToLayer("NPC"))) // TODO set for any collider extends in world pos
        {
            if (frontHit.collider.name == this.gameObject.name)
            {
                // set up nose transform
                GameObject go = new GameObject("FrontLeft");
                go.transform.position = frontHit.point;
                go.transform.SetParent(transform);
                frontLeft = go.transform;
                frontLeft.localRotation = Quaternion.identity;
            }
        }
    }

    private void GetWheelComponents()
    {
        foreach (Transform child in this.transform)
        {
            if (child.name.Contains("FR"))
                wheelFR = child;
            if (child.name.Contains("FL"))
                wheelFL = child;
            if (child.name.Contains("RL"))
                wheelRL = child;
            if (child.name.Contains("RR"))
                wheelRR = child;
        }
    }

    private void WheelMovement()
    {
        if (!wheelFR || !wheelFL || !wheelRL || !wheelRR) return;

        theta = currentSpeed_measured * Time.deltaTime / radius;
        newX = lastX + theta * Mathf.Rad2Deg;
        lastX = newX;
        if (lastX > 360)
            lastX -= 360;

        wheelFR.localRotation = Quaternion.Euler(newX, currentTurn, 0);
        wheelFL.localRotation = Quaternion.Euler(newX, currentTurn, 0);
        wheelRL.localRotation = Quaternion.Euler(newX, 0, 0);
        wheelRR.localRotation = Quaternion.Euler(newX, 0, 0);
    }
    #endregion

    #region physics
    private void NPCMove()
    {
        if (rb == null) return;
        rb.MovePosition(rb.position + transform.forward * currentSpeed * Time.fixedDeltaTime);
    }

    private void NPCTurn()
    {
        if (Physics.Raycast(transform.position + Vector3.up * 5f, -Vector3.up, out groundHit, 25f, groundHitBitmask))
        {
            rb.MoveRotation(Quaternion.FromToRotation(Vector3.up, groundHit.normal) * Quaternion.Euler(0f, transform.eulerAngles.y + currentTurn * Time.deltaTime, 0f));

            // demo hack TODO more uniform way
            if (groundHit.collider.tag != "Bridge")
                NPCManager.Instance.DespawnBridgeNPC(this.gameObject);
        }
    }
    #endregion

    #region inputs
    private void DeterminTargetTurn()
    {
        float targetDist = Vector3.Distance(currentTarget, transform.position);
        Vector3 newTarget = currentTarget;
        // intersection tangent check // targetTangent = (currentEntry.waypoints[1] - currentEntry.waypoints[0]).normalized; otherwise targetTangent = Vector3.zero;
        //if (currentTargetTangent != Vector3.zero && targetDist > 6f)
        //{
        //    newTarget = currentTarget - (currentTargetTangent * (targetDist - 6f));
        //}
        var steerVector = (new Vector3(newTarget.x, transform.position.y, newTarget.z) - transform.position).normalized * Mathf.Clamp((currentSpeed_measured / 4f), 0f, 1f); // + dodgeVector
        var steer = Vector3.Angle(transform.forward, steerVector) * 1.5f;

        targetTurn = Mathf.Clamp((Vector3.Cross(transform.forward, steerVector).y < 0 ? -steer : steer), -maxTurn, maxTurn);
    }
    #endregion

    #region targeting
    public void SetLaneData(List<Vector3> data)
    {
        laneData = data;
        currentTarget = laneData[++currentIndex];
    }

    private bool HasReachedTarget()
    {
        if (!frontCenter || !frontLeft || !frontRight)
        {
            NPCManager.Instance.DespawnBridgeNPC(this.gameObject);
            return false;
        }
        var distToTarget = Vector3.Distance(new Vector3(frontCenter.position.x, 0f, frontCenter.position.z), new Vector3(currentTarget.x, 0f, currentTarget.z));
        return ((distToTarget < 8.0f) && Vector3.Dot(frontCenter.forward, currentTarget - frontCenter.position) < 0);
    }

    private void EvaluateTarget()
    {
        if (!HasReachedTarget()) return;

        if (currentIndex < laneData.Count - 1)
        {
            currentIndex++;
            currentTarget = laneData[currentIndex];
        }
        else
        {
            // TODO check for off lane
            NPCManager.Instance.DespawnBridgeNPC(this.gameObject);
        }
    }

    private void AdjustToTargetSpeedAndTurn(float delta)
    {
        currentSpeed += speedAdjustRate * delta * (targetSpeed - currentSpeed);
        currentTurn += turnAdjustRate * delta * (targetTurn - currentTurn);
    }

    private void DeterminTargetSpeed()
    {
        targetSpeed = normalSpeed; //always assume target speed is normal speed and then reduce as needed

        doRaycast = false;
        //check in front of us
        if (Time.time > nextRaycast)
        {
            doRaycast = true;
            nextRaycast = NextRaycastTime();
        }

        if (doRaycast && frontCenter != null && frontLeft != null && frontRight != null)
        {
            frontClosestHitInfo = new RaycastHit();

            RaycastHit hitInfo = new RaycastHit();
            float midHitDist = 1000f;
            float leftHitDist = 1000f;
            float rightHitDist = 1000f;
            float minHitDistance = 1000f;
            if (Physics.Raycast(frontCenter.position, frontCenter.forward, out hitInfo, frontRaycastDistance, carCheckBlockBitmask))
            {
                midHitDist = hitInfo.distance;
                if (hitInfo.distance < minHitDistance)
                {
                    minHitDistance = hitInfo.distance;
                    frontClosestHitInfo = hitInfo;
                }
            }

            if (Physics.Raycast(frontRight.position, frontRight.forward, out hitInfo, frontRaycastDistance, carCheckBlockBitmask))
            {
                rightHitDist = hitInfo.distance;
                if (hitInfo.distance < minHitDistance)
                {
                    minHitDistance = hitInfo.distance;
                    frontClosestHitInfo = hitInfo;
                }
            }

            if (Physics.Raycast(frontLeft.position, frontLeft.forward, out hitInfo, frontRaycastDistance, carCheckBlockBitmask))
            {
                leftHitDist = hitInfo.distance;
                if (hitInfo.distance < minHitDistance)
                {
                    minHitDistance = hitInfo.distance;
                    frontClosestHitInfo = hitInfo;
                }
            }

            detectFront = frontClosestHitInfo.collider != null;
        }

        followSpeed = targetSpeed; //always assume follow speed is the same as the previously calculated(if any) target speed
        var safeDistance = currentSpeed_measured * safeReactionTime; //2s reaction time

        if (detectFront && frontClosestHitInfo.collider != null)
        {
            float frontSpeed = normalSpeed;
            var aiCar = frontClosestHitInfo.collider.GetComponent<NPCControllerComponent>();
            var playerCar = frontClosestHitInfo.collider.GetComponentInParent<VehicleController>();

            if (aiCar != null)
            {
                frontSpeed = aiCar.currentSpeed_measured * Vector3.Dot(frontCenter.forward, aiCar.frontCenter.forward);
                if (frontSpeed < 1.5f)
                {
                    frontSpeed = 0f;
                }
            }
            else if (playerCar != null)
            {
                frontSpeed = playerCar.RB.velocity.magnitude * Vector3.Dot(frontCenter.forward, playerCar.transform.forward);
                if (frontSpeed < 1.5f)
                {
                    frontSpeed = 0f;
                }
            }

            followSpeed = frontSpeed; //when there is front car assume follow speed will be the same as front car speed

            var hitDist = frontClosestHitInfo.distance;
            if (hitDist < hardStopDistance)
            {
                followSpeed = 0f; //hard stop when too close
            }
            else if (hitDist < heavyStopDistance)
            {
                followSpeed = frontSpeed * 0.5f;
            }
            else if (hitDist > safeDistance)
            {
                followSpeed = targetSpeed; //if car is outside of safe distance set to target speed
            }
        }

        targetSpeed = Mathf.Min(targetSpeed, followSpeed); //Affect target speed
    }

    public float NextRaycastTime()
    {
        return Time.time + Random.Range(0.2f, 0.25f);
    }

    //can be improved later with rigidbody speed if using better physic system
    void CalculateSpeed(float delta)
    {
        currentSpeed_measured = ((rb.position - lastRBPosition) / delta).magnitude;
        lastRBPosition = rb.position;
    }
    #endregion
}