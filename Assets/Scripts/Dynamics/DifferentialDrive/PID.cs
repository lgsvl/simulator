/*
 * Copyright (c) 2021 LG Electronics Inc.
 *
 * SPDX-License-Identifier: MIT
 */

using System;

[Serializable]
public class PID
{
    private float _pGain, _iGain, _dGain;
    private float integral = 0;
    private float lastError = 0;
    private float _integralMax, _integralMin;
    private float _outputMax, _outputMin;

    public PID(in float pGain, in float iGain, in float dGain, in float integralMax = 100, in float integralMin = -100, in float outputMax = 1000, in float outputMin = -1000)
    {
        Change(pGain, iGain, dGain);
        this._integralMax = integralMax;
        this._integralMin = integralMin;
        this._outputMax = outputMax;
        this._outputMin = outputMin;
    }

    public void Change(in float pGain, in float iGain, in float dGain)
    {
        this._pGain = pGain;
        this._iGain = iGain;
        this._dGain = dGain;
    }

    public void Reset()
    {
        integral = 0;
        lastError = 0;
    }

    public float Update(in float target, in float actual, in float deltaTime)
    {
        var error = target - actual;
        integral += (error * deltaTime);

        // Limit iTerm so that the limit is meaningful in the output
        if (integral > _integralMax)
        {
            integral = _integralMax / _iGain;
        }
        else if (integral < _integralMin)
        {
            integral = _integralMin / _iGain;
        }

        var derive = (deltaTime == 0)? 0 : ((error - lastError) / deltaTime);
        lastError = error;

        var output = (error * _pGain) + (integral * _iGain) + (derive * _dGain);

        return UnityEngine.Mathf.Clamp(output, _outputMin, _outputMax);
    }
}