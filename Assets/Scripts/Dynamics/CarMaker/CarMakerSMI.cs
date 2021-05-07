/**
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 * 
 * Copyright (c) 2019-2021 LG Electronics, Inc.
 * This software contains code licensed as described in LICENSE.
 */

using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.VFX;

namespace Simulator.Sensors
{
    public class CarMakerDynamicsSensorEventArgs
    {
        public Vector3[] UpVector;
        public float[] ZValue;
        public float SteerInput, AccellInput;
    }

    public class CarMakerSMI : MonoBehaviour, IVehicleDynamics
    {
        private Rigidbody RB;
        public Vector3 Velocity => RB.velocity;
        public Vector3 AngularVelocity => RB.angularVelocity;
        public Transform BaseLink { get { return BaseLinkTransform; } }
        public Transform BaseLinkTransform;

        public float AccellInput { get; set; } = 0f;
        public float SteerInput { get; set; } = 0f;
        public bool HandBrake { get; set; } = false;
        public float CurrentRPM { get; set; } = 0f;
        public float CurrentGear { get; set; } = 1f;
        public bool Reverse { get; set; } = false;
        public float WheelAngle { get; set; } = 0f;
        public float Speed { get => AgentSpeed; }
        public float MaxSteeringAngle { get; set; } = 39.4f;
        public IgnitionStatus CurrentIgnitionStatus { get; set; } = IgnitionStatus.On;

        private IAgentController Controller;
        private bool IsInitVehicleSMI = false;

        public float CraOffsetZ = 2.5f;
        public WheelCollider FL;
        public WheelCollider FR;
        public WheelCollider RL;
        public WheelCollider RR;
        private WheelCollider[] AgentTires = new WheelCollider[4];

        public delegate void CarMakerDynamicsEventHandler(object sender, CarMakerDynamicsSensorEventArgs e);
        public event CarMakerDynamicsEventHandler CarMakerDynamicsEvent;

        private bool IsPose = false;

        private Vector3 AgentPosition = Vector3.zero;
        public float AgentHeading = 0;
        public float AgentRoll = 0;
        public float AgentPitch = 0;
        public float AgentSpeed = 0;

        private void Start()
        {
            InitVehicleSMI();
        }

        private void Update()
        {
            GetInput();
        }

        private void FixedUpdate()
        {
            if (!IsInitVehicleSMI)
                return;

            CarMakerMove();
            PutEgoOnGround();
            SendDataToSensor();
        }

        private void SendDataToSensor()
        {
            Vector3[] aUpVector = {AgentTires[0].transform.up, AgentTires[1].transform.up, AgentTires[2].transform.up, AgentTires[3].transform.up};
            float[] aZValue = {AgentTires[0].transform.position.y, AgentTires[1].transform.position.y, AgentTires[2].transform.position.y, AgentTires[3].transform.position.y};

            CarMakerDynamicsEvent?.Invoke(this, new CarMakerDynamicsSensorEventArgs() { UpVector = aUpVector, ZValue = aZValue, SteerInput = SteerInput, AccellInput = AccellInput });
        }

        public void SetVehiclePose(Vector3 aPos, float aHeading, float aRoll, float aPitch, float aSpeed)
        {
            AgentPosition = aPos;
            AgentHeading = aHeading;
            AgentRoll = aRoll;
            AgentPitch = aPitch;
            AgentSpeed = aSpeed;
            IsPose = true;
        }

        private void CarMakerMove()
        {
            if (!IsPose)
                return;

            float posRight = AgentPosition.x + CraOffsetZ * Mathf.Sin(AgentHeading * Mathf.Deg2Rad);
            float posForward = AgentPosition.y + CraOffsetZ * Mathf.Cos(AgentHeading * Mathf.Deg2Rad);

            // smooth too slow
            //var targetDir = (new Vector3(posRight, AgentPosition.z, posForward) - RB.position).normalized;
            //RB.MovePosition(RB.position + (targetDir * Time.fixedDeltaTime));
            //RB.MoveRotation(Quaternion.Euler(new Vector3(AgentRoll, AgentHeading, AgentPitch)));

            RB.transform.position = new Vector3(posRight, AgentPosition.z, posForward);
            RB.transform.rotation = Quaternion.Euler(AgentRoll, AgentHeading, AgentPitch);

            RB.velocity = Vector3.zero;
            RB.angularVelocity = Vector3.zero;
        }

        private void InitVehicleSMI()
        {
            RB = GetComponent<Rigidbody>();
            RB.isKinematic = true;
            Controller = GetComponent<IAgentController>();

            var wheelColliders = GetComponentsInChildren<WheelCollider>().ToList();
            foreach (var wc in wheelColliders)
            {
                if (wc.gameObject.name == "FL")
                {
                    AgentTires[0] = wc;
                }
                if (wc.gameObject.name == "FR")
                {
                    AgentTires[1] = wc;
                }
                if (wc.gameObject.name == "RL")
                {
                    AgentTires[2] = wc;
                }
                if (wc.gameObject.name == "RR")
                {
                    AgentTires[3] = wc;
                }
            }
            var tireSpray = GetComponentsInChildren<VisualEffect>();
            foreach (var item in tireSpray)
            {
                item.enabled = false;
            }
            IsInitVehicleSMI = true;
        }

        private bool PutEgoOnGround()
        {
            RaycastHit hit;
            Vector3[] hitPoints = new Vector3[4];
            Vector3 avgPoint = Vector3.zero;
            bool isHitAll = true;
            int layerMask = 1 << LayerMask.NameToLayer("Default");

            for (int i = 0; i < 4; i++)
            {
                var tire = AgentTires[i];
                Ray tireRay = new Ray(tire.transform.position, -tire.transform.up);
                bool isHit = Physics.Raycast(tireRay, out hit, 100f, layerMask, QueryTriggerInteraction.Ignore);
                if (isHit)
                {
                    hitPoints[i] = hit.point;
                    avgPoint += hit.point;
                }
                else
                {
                    isHitAll = false;
                    break;
                }
            }

            if (!isHitAll)
            {
                return false;
            }

            Vector3 egoNewForward = (hitPoints[1] - hitPoints[3]).normalized;
            Vector3 egoNewLeft = (hitPoints[2] - hitPoints[3]).normalized;
            Vector3 egoNewUp = Vector3.Cross(egoNewLeft, egoNewForward);

            Vector3 egoPos = RB.transform.localPosition;
            RB.transform.rotation = Quaternion.LookRotation(egoNewForward, egoNewUp);
            RB.transform.localPosition = new Vector3(egoPos.x, avgPoint.y / 4f + 0.05f, egoPos.z);

            return true;
        }

        private void GetInput()
        {
            if (Controller != null)
            {
                SteerInput = Controller.SteerInput;
                AccellInput = Controller.AccelInput;
            }

            if (HandBrake)
            {
                AccellInput = -1.0f; // TODO better way using Accel and Brake
            }
        }

        public bool GearboxShiftUp()
        {
            return true;
        }

        public bool GearboxShiftDown()
        {
            return true;
        }

        public bool ShiftFirstGear()
        {
            return true;
        }

        public bool ShiftReverse()
        {
            return true;
        }

        public bool ToggleReverse()
        {
            return true;
        }

        public bool ShiftReverseAutoGearBox()
        {
            return true;
        }

        public bool ToggleIgnition()
        {
            return true;
        }

        public bool ToggleHandBrake()
        {
            return true;
        }

        public bool SetHandBrake(bool state)
        {
            return true;
        }

        public bool ForceReset(Vector3 pos, Quaternion rot)
        {
            return true;
        }
    }

    public class CMSignal
    {
        public int StartPosChangeCode;
        public int StartRouteObjId;
        public float StartPositionX;
        public float StartPositionY;
        public float StartPositionZ;
        public float StartHeading;
    }

    public class ControlSim
    {
        public float Brake;
        public float Steer;
        public float Accel;
        public int Gear;
        public int ControlState;
        public float StartPosS;
        public float StartPosT;
        public float StartYaw;
    }

    public class TireCP
    {
        public float TireFrontLeftZ;
        public float TireFrontRightZ;
        public float TireRearLeftZ;
        public float TireRearRightZ;
        public float TireFrontVectorX, TireFrontVectorY, TireFrontVectorZ;
        public float TireRearVectorX, TireRearVectorY, TireRearVectorZ;
        public float TireFriction;
    }

    public class PoseCmd
    {
        public double PoseX;
        public double PoseY;
        public double PoseZ;
        public double PoseH;
        public double Roll;
        public double Pitch;
        public double Yaw;
        public double SetSpeed;
        public double[] ReservedF;
        public int[] ReservedI;
    }

    public class CarMakerDataConv
    {
        public static char ElementSeparator = ',';
        public static char HeaderSeparator = ':';

        public static string CMSignalToString(CMSignal aCMSignal)
        {
            var sb = new StringBuilder(4096);
            sb.Append(aCMSignal.StartPosChangeCode.ToString());
            sb.Append(ElementSeparator);
            sb.Append(aCMSignal.StartRouteObjId.ToString());
            sb.Append(ElementSeparator);
            sb.Append(aCMSignal.StartPositionX.ToString());
            sb.Append(ElementSeparator);
            sb.Append(aCMSignal.StartPositionY.ToString());
            sb.Append(ElementSeparator);
            sb.Append(aCMSignal.StartPositionZ.ToString());
            sb.Append(ElementSeparator);
            sb.Append(aCMSignal.StartHeading.ToString());

            return sb.ToString();
        }

        public static string ControlSimToString(ControlSim aControlSim)
        {
            var sb = new StringBuilder(4096);
            sb.Append(aControlSim.Brake.ToString());
            sb.Append(ElementSeparator);
            sb.Append(aControlSim.Steer.ToString());
            sb.Append(ElementSeparator);
            sb.Append(aControlSim.Accel.ToString());
            sb.Append(ElementSeparator);
            sb.Append(aControlSim.Gear.ToString());
            sb.Append(ElementSeparator);
            sb.Append(aControlSim.ControlState.ToString());
            sb.Append(ElementSeparator);
            sb.Append(aControlSim.StartPosS.ToString());
            sb.Append(ElementSeparator);
            sb.Append(aControlSim.StartPosT.ToString());
            sb.Append(ElementSeparator);
            sb.Append(aControlSim.StartYaw.ToString());

            return sb.ToString();
        }

        public static string TireCPToString(TireCP aChassis)
        {
            var sb = new StringBuilder(4096);
            sb.Append(aChassis.TireFrontLeftZ.ToString());
            sb.Append(ElementSeparator);
            sb.Append(aChassis.TireFrontRightZ.ToString());
            sb.Append(ElementSeparator);
            sb.Append(aChassis.TireRearLeftZ.ToString());
            sb.Append(ElementSeparator);
            sb.Append(aChassis.TireRearRightZ.ToString());
            sb.Append(ElementSeparator);
            sb.Append(aChassis.TireFrontVectorX.ToString());
            sb.Append(ElementSeparator);
            sb.Append(aChassis.TireFrontVectorY.ToString());
            sb.Append(ElementSeparator);
            sb.Append(aChassis.TireFrontVectorZ.ToString());
            sb.Append(ElementSeparator);
            sb.Append(aChassis.TireRearVectorX.ToString());
            sb.Append(ElementSeparator);
            sb.Append(aChassis.TireRearVectorY.ToString());
            sb.Append(ElementSeparator);
            sb.Append(aChassis.TireRearVectorZ.ToString());
            sb.Append(ElementSeparator);
            sb.Append(aChassis.TireFriction.ToString());
            return sb.ToString();
        }

        public static PoseCmd StringToPoseCmd(string aData)
        {
            PoseCmd pose = new PoseCmd();
            var headerAndContent = aData.Split(HeaderSeparator);
            string header = headerAndContent[0];
            string content = headerAndContent[1];

            var headerElements = header.Split(ElementSeparator);
            double recvTime = double.Parse(headerElements[0]);
            double sentTime = double.Parse(headerElements[1]);
            int subscribersCount = int.Parse(headerElements[2]);

            var contentElements = content.Split(ElementSeparator);
            pose.PoseX = float.Parse(contentElements[0]);
            pose.PoseY = float.Parse(contentElements[1]);
            pose.PoseZ = float.Parse(contentElements[2]);
            pose.Roll = float.Parse(contentElements[3]);
            pose.Pitch = float.Parse(contentElements[4]);
            pose.Yaw = float.Parse(contentElements[5]);
            pose.SetSpeed = float.Parse(contentElements[6]);
            pose.PoseH = pose.Yaw;

            return pose;
        }
    }
}
