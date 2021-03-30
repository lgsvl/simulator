/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

public interface IVehicleActions
{
    HeadLightState CurrentHeadLightState { get; set; }
    WiperState CurrentWiperState { get; set; }
    bool LeftTurnSignal { get; set; }
    bool RightTurnSignal { get; set; }
    bool HazardLights { get; set; }
    bool BrakeLights { get; set; }
    bool FogLights { get; set; }
    bool ReverseLights { get; set; }
    bool InteriorLight { get; set; }

    void IncrementHeadLightState();

    void IncrementWiperState();
}
