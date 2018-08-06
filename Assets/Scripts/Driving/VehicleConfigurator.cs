/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System.Collections;
using System.Xml.Serialization;
using System.IO;

[System.Serializable]
public class VehicleSettings
{
    public float Mass;
    public bool FWD;
    public bool RWD;
    public float AirDragCoeff;
    public float AirDownForceCoeff;
    public float TireDragCoeff;
    public float MaxMotorTorque;
    public float MinRPM;
    public float MaxRPM;
    public float ShiftDelay;
    public float ShiftTime;
    public float FrontBrakeBias;
    public float RearBrakeBias;
    public float MaxBrakeTorque;
    public float ForwardFrictionStiffness;
    public float SidewaysFrictionStiffness;
    public float MaxSteeringAngle;
    public float AutoSteerAmount;
    public float TractionControlAmount;
    public float TractionControlSlipLimit;
    public float SuspensionSpringFront;
    public float SuspensionSpringRear;
    public float SuspensionDamperFront;
    public float SuspensionDamperRear;
    public float timestamp;
    public string timestring;

    public VehicleSettings()
    {

    }

    public VehicleSettings(VehicleConfigurator configurator) {
        Mass = configurator.Mass;
        FWD = configurator.FWD;
        RWD = configurator.RWD;
        AirDragCoeff = configurator.AirDragCoeff;
        AirDownForceCoeff = configurator.AirDownforceCoeff;
        TireDragCoeff = configurator.TireDragCoeff;
        MaxMotorTorque = configurator.MaxMotorTorque;
        MinRPM = configurator.MinRPM;
        MaxRPM = configurator.MaxRPM;
        ShiftDelay = configurator.ShiftDelay;
        ShiftTime = configurator.ShiftTime;
        FrontBrakeBias = configurator.FrontBrakeBias;
        RearBrakeBias = configurator.RearBrakeBias;
        MaxBrakeTorque = configurator.MaxBrakeTorque;
        ForwardFrictionStiffness = configurator.ForwardFrictionStiffness;
        SidewaysFrictionStiffness = configurator.SidewaysFrictionStiffness;
        MaxSteeringAngle = configurator.MaxSteeringAngle;
        AutoSteerAmount = configurator.AutoSteerAmount;
        TractionControlAmount = configurator.TractionControlAmount;
        TractionControlSlipLimit = configurator.TractionControlSlipLimit;
        SuspensionSpringFront = configurator.SuspensionSpringFront;
        SuspensionSpringRear = configurator.SuspensionSpringRear;
        SuspensionDamperFront = configurator.SuspensionDamperFront;
        SuspensionDamperRear = configurator.SuspensionDamperRear;

        timestamp = System.DateTime.UtcNow.Ticks;
        timestring = System.DateTime.Now.ToShortDateString() + " " + System.DateTime.Now.ToShortTimeString();
    }

    public void Apply(VehicleConfigurator configurator)
    {
        configurator.Mass = Mass;
        configurator.FWD = FWD;
        configurator.RWD = RWD;
        configurator.AirDragCoeff = AirDragCoeff;
        configurator.AirDownforceCoeff = AirDownForceCoeff;
        configurator.TireDragCoeff = TireDragCoeff;
        configurator.MaxMotorTorque = MaxMotorTorque;
        configurator.MinRPM = MinRPM;
        configurator.MaxRPM = MaxRPM;
        configurator.ShiftDelay = ShiftDelay;
        configurator.ShiftTime = ShiftTime;
        configurator.FrontBrakeBias = FrontBrakeBias;
        configurator.RearBrakeBias = RearBrakeBias;
        configurator.MaxBrakeTorque = MaxBrakeTorque;
        configurator.ForwardFrictionStiffness = ForwardFrictionStiffness;
        configurator.SidewaysFrictionStiffness = SidewaysFrictionStiffness;
        configurator.MaxSteeringAngle = MaxSteeringAngle;
        configurator.AutoSteerAmount = AutoSteerAmount;
        configurator.TractionControlAmount = TractionControlAmount;
        configurator.TractionControlSlipLimit = TractionControlSlipLimit;
        configurator.SuspensionSpringFront = SuspensionSpringFront;
        configurator.SuspensionSpringRear = SuspensionSpringRear;
        configurator.SuspensionDamperFront = SuspensionDamperFront;
        configurator.SuspensionDamperRear = SuspensionDamperRear;
    }
}


public class VehicleConfigurator : MonoBehaviour {


    public void SaveSettings()
    {
        var car = vehicle.GetComponent<VehicleInfo>().vehicleShortName;
        var savedSettings = new VehicleSettings(this);

        var serializer = new XmlSerializer(typeof(VehicleSettings));

        using (var filestream = new FileStream(Application.streamingAssetsPath + "/SavedPresets/" + car +".xml", FileMode.Create))
        {
            var writer = new System.Xml.XmlTextWriter(filestream, System.Text.Encoding.Unicode);
            serializer.Serialize(writer, savedSettings);
        }
    }

    public void LoadSettings()
    {
        var car = vehicle.GetComponent<VehicleInfo>().vehicleShortName;

        var serializer = new XmlSerializer(typeof(VehicleSettings));
        try
        {
            using (var filestream = new FileStream(Application.streamingAssetsPath + "/SavedPresets/" + car + ".xml", FileMode.Open))
            {
                var reader = new System.Xml.XmlTextReader(filestream);
                var savedSettings = serializer.Deserialize(reader) as VehicleSettings;
                savedSettings.Apply(this);
            }
        }
        catch
        {
            Debug.LogWarning("Error loading vehicle settings from file: " + Application.streamingAssetsPath + "/SavedPresets/" + car + ".xml");
        }
    }

    public VehicleController vehicle;
    public Rigidbody rb;

    public void Init(GameObject car)
    {
        vehicle = car.GetComponent<VehicleController>();
        rb = car.GetComponent<Rigidbody>();
    }

    public float Mass
    {
        get
        {
            return rb.mass;
        }
        set
        {
            rb.mass = value;
        }
    }

    public bool FWD
    {
        get
        {
            return vehicle.axles[0].motor;
        }
        set
        {
            vehicle.axles[0].motor = value;
            vehicle.RecalcDrivingWheels();
        }
    }

    public bool RWD
    {
        get
        {
            return vehicle.axles[1].motor;
        }
        set
        {
            vehicle.axles[1].motor = value;
            vehicle.RecalcDrivingWheels();
        }
    }

    public float AirDragCoeff
    {
        get
        {
            return vehicle.airDragCoeff;
        }
        set
        {
            vehicle.airDragCoeff = value;
        }
    }

    public float AirDownforceCoeff
    {
        get
        {
            return vehicle.airDownForceCoeff;
        }
        set
        {
            vehicle.airDownForceCoeff = value;
        }
    }

    public float TireDragCoeff
    {
        get
        {
            return vehicle.tireDragCoeff;
        }
        set
        {
            vehicle.tireDragCoeff = value;
        }
    }

    public float MaxMotorTorque
    {
        get
        {
            return vehicle.maxMotorTorque;
        }
        set
        {
            vehicle.maxMotorTorque = value;
        }
    }

    public float MinRPM
    {
        get
        {
            return vehicle.minRPM;
        }
        set
        {
            vehicle.minRPM = value;
        }
    }

    public float MaxRPM
    {
        get
        {
            return vehicle.maxRPM;
        }
        set
        {
            vehicle.maxRPM = value;
        }
    }

    public float ShiftDelay
    {
        get
        {
            return vehicle.shiftDelay;
        }
        set
        {
            vehicle.shiftDelay = value;
        }
    }

    public float ShiftTime
    {
        get
        {
            return vehicle.shiftTime;
        }
        set
        {
            vehicle.shiftTime = value;
        }
    }

    public float FrontBrakeBias
    {
        get
        {
            return vehicle.axles[0].brakeBias;
        }
        set
        {
            vehicle.axles[0].brakeBias = value;
        }
    }

    public float RearBrakeBias
    {
        get
        {
            return vehicle.axles[1].brakeBias;
        }
        set
        {
            vehicle.axles[1].brakeBias = value;
        }
    }

    public float MaxBrakeTorque
    {
        get
        {
            return vehicle.maxBrakeTorque;
        }
        set
        {
            vehicle.maxBrakeTorque = value;
        }
    }

    public float ForwardFrictionStiffness
    {
        get
        {
            return vehicle.axles[0].left.forwardFriction.stiffness;
        }
        set
        {
            var friction = vehicle.axles[0].left.forwardFriction;
            friction.stiffness = value;
            vehicle.axles[0].left.forwardFriction = friction;
            vehicle.axles[0].right.forwardFriction = friction;
            vehicle.axles[1].left.forwardFriction = friction;
            vehicle.axles[1].right.forwardFriction = friction;
        }
    }

    public float SidewaysFrictionStiffness
    {
        get
        {
            return vehicle.axles[0].left.sidewaysFriction.stiffness;
        }
        set
        {
            var friction = vehicle.axles[0].left.sidewaysFriction;
            friction.stiffness = value;
            vehicle.axles[0].left.sidewaysFriction = friction;
            vehicle.axles[0].right.sidewaysFriction = friction;
            vehicle.axles[1].left.sidewaysFriction = friction;
            vehicle.axles[1].right.sidewaysFriction = friction;
        }
    }

    public float MaxSteeringAngle
    {
        get
        {
            return vehicle.maxSteeringAngle;
        }
        set
        {
            vehicle.maxSteeringAngle = value;
        }
    }

    public float AutoSteerAmount
    {
        get
        {
            return vehicle.autoSteerAmount;
        }
        set
        {
            vehicle.autoSteerAmount = value;
        }

    }

    public float TractionControlAmount
    {
        get
        {
            return vehicle.tractionControlAmount;
        }
        set
        {
            vehicle.tractionControlAmount = value;
        }
    }

    public float TractionControlSlipLimit
    {
        get
        {
            return vehicle.tractionControlSlipLimit;
        }
        set
        {
            vehicle.tractionControlSlipLimit = value;
        }
    }


    public float SuspensionSpringFront
    {
        get
        {
            return vehicle.axles[0].left.suspensionSpring.spring;
        }
        set
        {
            var suspension = vehicle.axles[0].left.suspensionSpring;
            suspension.spring = value;
            vehicle.axles[0].left.suspensionSpring = suspension;
            vehicle.axles[0].right.suspensionSpring = suspension;
        }
    }

    public float SuspensionSpringRear
    {
        get
        {
            return vehicle.axles[1].left.suspensionSpring.spring;
        }
        set
        {
            var suspension = vehicle.axles[1].left.suspensionSpring;
            suspension.spring = value;
            vehicle.axles[1].left.suspensionSpring = suspension;
            vehicle.axles[1].right.suspensionSpring = suspension;
        }
    }


    public float SuspensionDamperFront
    {
        get
        {
            return vehicle.axles[0].left.suspensionSpring.damper;
        }
        set
        {
            var suspension = vehicle.axles[0].left.suspensionSpring;
            suspension.damper = value;
            vehicle.axles[0].left.suspensionSpring = suspension;
            vehicle.axles[0].right.suspensionSpring = suspension;
        }
    }

    public float SuspensionDamperRear
    {
        get
        {
            return vehicle.axles[1].left.suspensionSpring.damper;
        }
        set
        {
            var suspension = vehicle.axles[1].left.suspensionSpring;
            suspension.damper = value;
            vehicle.axles[1].left.suspensionSpring = suspension;
            vehicle.axles[1].right.suspensionSpring = suspension;
        }
    }
}
