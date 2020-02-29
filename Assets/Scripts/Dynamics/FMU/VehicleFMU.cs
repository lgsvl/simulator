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
    public class VehicleFMU : MonoBehaviour
    {
        public bool UnitySolver = false;
        public List<AxleInfo> Axles;
        public Vector3 CenterOfMass = new Vector3(0f, 0.35f, 0f);

        public FMU fmu;
        public FMUData FMUData = null;
    }
}
