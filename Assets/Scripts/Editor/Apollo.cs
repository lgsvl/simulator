/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;
using UnityEngine;
using System.Text;
using ApolloCommon = apollo.common;


namespace Simulator.Editor
{
    namespace Apollo
    {
        public static class HDMapUtil
        {
            //Convert coordinate to Apollo coordinate
            public static ApolloCommon.PointENU GetApolloCoordinates(Vector3 unityPos, double originEasting, double originNorthing, bool dim3D = true)
            {
                return GetApolloCoordinates(unityPos, originEasting, originNorthing, 0, dim3D);
            }

            public static ApolloCommon.PointENU GetApolloCoordinates(Vector3 unityPos, double originEasting, double originNorthing, float altitudeOffset, bool dim3D = true)
            {
                var pointENU = new ApolloCommon.PointENU()
                {
                    x = unityPos.x + originEasting, y = unityPos.z + originNorthing
                };
                if (dim3D)
                    pointENU.z = unityPos.y + altitudeOffset;

                return pointENU;
            }
        }
    }
}
