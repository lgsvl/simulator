/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(VehicleController))]
[RequireComponent(typeof(GpsDevice))]
public class CanBus : MonoBehaviour, Ros.IRosClient
{
    public ROSTargetEnvironment targetEnv;
    // VehicleController controller;

    public string ApolloTopic = "/apollo/canbus/chassis";
    
    public float Frequency = 10f;

    public bool PublishMessage = false;

    int seq;
    float NextSend;

    Ros.Bridge Bridge;

    Rigidbody mainRigidbody;
    VehicleController controller;
    VehicleInputController input_controller;
    GpsDevice gps;

    private void Start()
    {
        NextSend = Time.time + 1.0f / Frequency;
        controller = GetComponent<VehicleController>();
        input_controller = GetComponent<VehicleInputController>();
        gps = GetComponentInChildren<GpsDevice>();
        mainRigidbody = GetComponent<Rigidbody>();
    }

    public void OnRosBridgeAvailable(Ros.Bridge bridge)
    {
        Bridge = bridge;
        Bridge.AddPublisher(this);
    }

    public void OnRosConnected()
    {
        if (targetEnv == ROSTargetEnvironment.AUTOWARE)
        {
            Debug.Log("CAN bus not implemented in Autoware (yet). Nothing to publish.");
        }

        if (targetEnv == ROSTargetEnvironment.APOLLO)
        {
            Bridge.AddPublisher<Ros.Apollo.ChassisMsg>(ApolloTopic);
        }

        seq = 0;
    }

    void Update()
    {
        if (targetEnv != ROSTargetEnvironment.APOLLO && targetEnv != ROSTargetEnvironment.AUTOWARE)
        {
            return;
        }

        if (Bridge == null || Bridge.Status != Ros.Status.Connected || !PublishMessage)
        {
            return;
        }

        if (Time.time < NextSend)
        {
            return;
        }

        if (targetEnv == ROSTargetEnvironment.APOLLO)
        {
            Vector3 vel = mainRigidbody.velocity;
            Vector3 eul = mainRigidbody.rotation.eulerAngles;

            float dir;
            if (eul.y >= 0) dir = 45 * Mathf.Round((eul.y % 360) / 45.0f);
            else dir = 45 * Mathf.Round((eul.y % 360 + 360) / 45.0f);

            // (TODO) check for leap second issues.
            var gps_time = DateTimeOffset.FromUnixTimeSeconds((long) gps.measurement_time).DateTime.ToLocalTime();
            
            var apolloMessage = new Ros.Apollo.ChassisMsg()
            {
                engine_started = true,
                engine_rpm = controller.currentRPM,
                speed_mps = vel.magnitude,
                odometer_m = 0,
                fuel_range_m = 0,
                throttle_percentage = input_controller.throttle * 100,
                brake_percentage = input_controller.brake * 100,
                steering_percentage = - controller.steerInput * 100,
                // steering_torque_nm
                parking_brake = controller.handbrakeApplied,
                high_beam_signal = (controller.headlightMode == 2),
                low_beam_signal = (controller.headlightMode == 1),
                left_turn_signal = controller.leftTurnSignal,
                right_turn_signal = controller.rightTurnSignal,
                // horn
                wiper = (controller.wiperStatus != 0),
                // disengage_status
                driving_mode = Ros.Apollo.Chassis.DrivingMode.COMPLETE_AUTO_DRIVE,
                // error_code
                // gear_location 
                // steering_timestamp
                // signal
                // engage_advice              
               
                chassis_gps = new Ros.Apollo.Chassis.ChassisGPS()
                {
                    latitude = gps.latitude_orig,
                    longitude = gps.longitude_orig,
                    gps_valid = gps.PublishMessage,
                    year = gps_time.Year,
                    month = gps_time.Month,
                    day = gps_time.Day,
                    hours = gps_time.Hour,
                    minutes = gps_time.Minute,
                    seconds = gps_time.Second,
                    compass_direction = dir,
                    pdop = 0.1,
                    is_gps_fault = false,
                    is_inferred = false,
                    altitude = gps.height,
                    heading = eul.y,
                    hdop = 0.1,
                    vdop = 0.1,
                    quality = Ros.Apollo.Chassis.GpsQuality.FIX_3D,
                    num_satellites = 15,
                    gps_speed = vel.magnitude,

                },

                header = new Ros.ApolloHeader()
                {
                    timestamp_sec = Ros.Time.Now().secs,
                    module_name = "chassis",
                    sequence_num = seq++,
                },
            };
            Bridge.Publish(ApolloTopic, apolloMessage);
        }        
    }
}
