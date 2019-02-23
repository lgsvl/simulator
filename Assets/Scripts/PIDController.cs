// PID Controller for ego vehicle

using System;
using UnityEngine;

namespace Control
{
    public class PID
    {
        public float k_p;
        public float k_d;
        public float k_i;
        public float e_p;
        public float e_d;
        public float e_i;

        public PID()
        {
            e_p = 0f;
            e_d = 0f;
            e_i = 0f;
        }

        public PID(float kp, float kd, float ki)
        {
            k_p = kp;
            k_d = kd;
            k_i = ki;
            e_p = 0f;
            e_d = 0f;
            e_i = 0f;
        }

        public void SetKValues(float kp, float kd, float ki)
        {
            k_p = kp;
            k_d = kd;
            k_i = ki;
        }

        public void UpdateErrors(float dt, float current_value, float target_value)
        {
            // trying to maintain constant velocity
            // the feedback value shouuld be current velocity

            float prior_error = e_p;
            e_p = target_value - current_value;
            e_i += e_p * dt;
            e_d = (e_p - prior_error) / dt;
        }

        public float Run(float dt, float current_value, float target_value)
        {
            UpdateErrors(dt, current_value, target_value);
            return -k_p*e_p - k_d*e_d - k_i*e_i; 
        }
    }
} // namespace Control