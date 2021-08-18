/**
 * Copyright (c) 2019-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Text.RegularExpressions; 

namespace Simulator.Map
{
    // Utility class that manages id auto generation for elements of IMapType.
    public static class IdGenerator
    {
        // For a collection of ElementType objects within the map.
        // Look for biggest id numerical suffix, increment by 1, appli given prefix and return result.
        public static string AutogenerateNextId<ElementType>(string prefix)
            where ElementType : IMapType 
        {
            return $"{prefix}{AutogenerateNextIdNumber<ElementType>()}";
        }

        // Get numerical id suffix in form of a string.
        // Result would be equal to fallbackValue if inputId does not end with number.
        public static string GetIdNumberString(string inputId, string fallbackValue = "")
        {
            var res = GetIdNumberInt(inputId);
            return res == -1 ? fallbackValue : res.ToString();
        }

        // Get numerical id suffix in form of an integer.
        // Result would be equal to fallbackValue if inputId does not end with number.
        public static int GetIdNumberInt(string inputId, int fallbackValue = -1)
        {
            if(string.IsNullOrEmpty(inputId))
                return fallbackValue;

            Match m = Regex.Match(inputId, @"\d+$");
            if(!m.Success)
                return fallbackValue;

            if(!int.TryParse(m.Value, out int res))
                return fallbackValue;

            return res;
        }

        // Finds the biggest id suffix number in ElementType collection stored in map, increments it by 1 and returns.
        // If there are no valid id number suffixes -1 will be returned.
        private static int AutogenerateNextIdNumber<ElementType>()
            where ElementType : IMapType 
        {
            var mapHolder = UnityEngine.Object.FindObjectOfType<MapHolder>();
            if (mapHolder == null)
                return -1;

            var existingSignals = mapHolder.transform.GetComponentsInChildren<ElementType>();
            int maxIdNum = -1;
            for (int i = 0; i < existingSignals.Length; i++)
            {
                var signal = existingSignals[i];
                maxIdNum = Math.Max(maxIdNum, GetIdNumberInt(signal.id));
            }
            return maxIdNum + 1;
        }
    }
}