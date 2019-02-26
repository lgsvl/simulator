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
        public float windup_guard;

        public PID()
        {
            this.e_p = 0f;
            this.e_d = 0f;
            this.e_i = 0f;
        }

        public PID(float kp, float kd, float ki)
        {
            this.k_p = kp;
            this.k_d = kd;
            this.k_i = ki;
            this.e_p = 0f;
            this.e_d = 0f;
            this.e_i = 0f;
        }

        public void SetKValues(float kp, float kd, float ki) 
        {
            this.k_p = kp;
            this.k_d = kd;
            this.k_i = ki;
        }

        public void ResetValues()
        {
            this.k_p = 0f;
            this.k_d = 0f;
            this.k_i = 0f;
            this.e_p = 0f;
            this.e_d = 0f;
            this.e_i = 0f;
            this.windup_guard = 0f;
        }
        
        public void SetWindupGuard(float limit)
        {
            this.windup_guard = limit;
        }
        
        public void UpdateErrors(float dt, float current_value, float target_value)
        {
            float prior_error = e_p;
            this.e_p = target_value - current_value;
            this.e_i += this.e_p * dt;
            
            // windup guard is only applied if the gaurd value is larger than 0
            if (this.windup_guard > 0f && Mathf.Abs(this.e_i) > this.windup_guard)
            {
                this.e_i = Mathf.Sign(this.e_i) * this.windup_guard;
            }

            this.e_d = (this.e_p - prior_error) / dt;
        }
        
        public float Run(float dt, float current_value, float target_value)
        {
            UpdateErrors(dt, current_value, target_value);
            return -this.k_p*this.e_p - this.k_d*this.e_d - this.k_i*this.e_i; 
        }
    }
} // namespace Control