/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
*/

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Simulator.FMU
{
    public class ExampleVehicleFMU : VehicleFMU, IVehicleDynamics
    {
        public Rigidbody RB { get; set; }

        public float AccellInput { get; set; } = 0f;
        public float SteerInput { get; set; } = 0f;

        public bool HandBrake { get; set; } = false;
        public float CurrentRPM { get; set; } = 0f;
        public float CurrentGear { get; set; } = 1f;
        public bool Reverse { get; set; } = false;
        public float WheelAngle
        {
            get
            {
                if (UnitySolver && Axles != null && Axles.Count > 0 && Axles[0] != null)
                {
                    return (Axles[0].Left.steerAngle + Axles[0].Right.steerAngle) * 0.5f;
                }
                return 0.0f;
            }
        }
        public IgnitionStatus CurrentIgnitionStatus { get; set; }

        private VehicleController VehicleController;

        public void Awake()
        {
            RB = GetComponent<Rigidbody>();
            VehicleController = GetComponent<VehicleController>();

            if (UnitySolver)
            {
                RB.centerOfMass = CenterOfMass;
                foreach (var axle in Axles)
                {
                    axle.Left.ConfigureVehicleSubsteps(5f, 30, 10);
                    axle.Right.ConfigureVehicleSubsteps(5f, 30, 10);
                    axle.Left.wheelDampingRate = 1f;
                    axle.Right.wheelDampingRate = 1f;
                }
            }

            var modelVars = new Dictionary<string, uint>();
            for (int i = 0; i < FMUData.modelVariables.Count; i++)
            {
                modelVars.Add(FMUData.modelVariables[i].name, FMUData.modelVariables[i].valueReference);
            }

            fmu = new FMU(FMUData.modelName, FMUData.modelName, FMUData.GUID, modelVars, FMUData.Path, false);
            Init();
        }

        private void Update()
        {
            if (UnitySolver)
            {
                UpdateWheelVisuals();
            }
        }

        public void FixedUpdate()
        {
            GetInput();

            ApplyInput();

            if (FMUData.type == FMIType.CoSimulation)
            {
                fmu.DoStep(Time.time, Time.deltaTime); // co simulation only, model exchange FMU will fail, not needed in example fmu
            }

            if (UnitySolver)
            {
                ApplyUnitySolver();
            }
            else
            {
                ApplyNonUnitySolver();
            }
        }

        public bool ForceReset(Vector3 pos, Quaternion rot)
        {
            if (UnitySolver)
            {
                foreach (var axle in Axles)
                {
                    axle.Left.brakeTorque = Mathf.Infinity;
                    axle.Right.brakeTorque = Mathf.Infinity;
                    axle.Left.motorTorque = 0f;
                    axle.Right.motorTorque = 0f;
                    axle.Left.brakeTorque = 0f;
                    axle.Right.brakeTorque = 0f;
                }
            }

            foreach (var axle in Axles)
            {
                axle.Left.brakeTorque = Mathf.Infinity;
                axle.Right.brakeTorque = Mathf.Infinity;
                axle.Left.motorTorque = 0f;
                axle.Right.motorTorque = 0f;
            }
            return true;
        }

        private void UpdateWheelVisuals()
        {
            foreach (var axle in Axles)
            {
                ApplyLocalPositionToVisuals(axle.Left, axle.LeftVisual);
                ApplyLocalPositionToVisuals(axle.Right, axle.RightVisual);
            }
        }

        private void ApplyLocalPositionToVisuals(WheelCollider collider, GameObject visual)
        {
            if (visual == null || collider == null)
            {
                return;
            }

            collider.GetWorldPose(out Vector3 position, out Quaternion rotation);

            visual.transform.position = position;
            visual.transform.rotation = rotation;
        }

        private void Init()
        {
            fmu.Reset();
            fmu.SetupExperiment(Time.time);
            fmu.EnterInitializationMode();

            // set init values here
            fmu.SetReal(FMUData.modelVariables[0].name, Convert.ToDouble(FMUData.modelVariables[0].start)); //maxSteerAngle

            fmu.ExitInitializationMode();
        }

        private void ApplyInput()
        {
            fmu.SetReal(FMUData.modelVariables[1].name, SteerInput); // steerInput
            fmu.SetReal(FMUData.modelVariables[2].name, AccellInput); // accelInput
        }

        private void ApplyUnitySolver()
        {
            if (UnitySolver && Axles != null && Axles.Count > 0 && Axles[0] != null)
            {
                Axles[0].Left.steerAngle = (float)fmu.GetReal(FMUData.modelVariables[3].name); // steerOutput
                Axles[0].Right.steerAngle = (float)fmu.GetReal(FMUData.modelVariables[3].name); // steerOutput
            }

            foreach (var axle in Axles)
            {
                if (axle.Motor)
                {
                    axle.Left.motorTorque = (float)fmu.GetReal(FMUData.modelVariables[4].name); // accelOutput
                    axle.Right.motorTorque = (float)fmu.GetReal(FMUData.modelVariables[4].name); // accelOutput
                }
            }
        }

        private void ApplyNonUnitySolver()
        {
            // example index 0, 1 are input and 2..7 are transform outputs
            // there is no example fmu for this
            RB.MovePosition(new Vector3((float)fmu.GetReal(FMUData.modelVariables[2].name),
                                        (float)fmu.GetReal(FMUData.modelVariables[3].name),
                                        (float)fmu.GetReal(FMUData.modelVariables[4].name)));
            RB.MoveRotation(Quaternion.Euler(new Vector3((float)fmu.GetReal(FMUData.modelVariables[5].name),
                                                         (float)fmu.GetReal(FMUData.modelVariables[6].name),
                                                         (float)fmu.GetReal(FMUData.modelVariables[7].name))));
        }

        private void OnDestroy()
        {
            fmu.Dispose();
        }

        private void GetInput()
        {
            if (VehicleController != null)
            {
                SteerInput = VehicleController.SteerInput;
                AccellInput = VehicleController.AccelInput;
            }

            if (HandBrake)
            {
                AccellInput = -1.0f; // TODO better way using Accel and Brake
            }
        }

        public bool GearboxShiftUp()
        {
            return false;
        }

        public bool GearboxShiftDown()
        {
            return false;
        }

        public bool ShiftFirstGear()
        {
            return false;
        }

        public bool ShiftReverse()
        {
            return false;
        }

        public bool ToggleReverse()
        {
            return false;
        }

        public bool ShiftReverseAutoGearBox()
        {
            return false;
        }

        public bool ToggleIgnition()
        {
            return false;
        }

        public bool ToggleHandBrake()
        {
            return false;
        }

        public bool SetHandBrake(bool state)
        {
            return false;
        }
    }
}
