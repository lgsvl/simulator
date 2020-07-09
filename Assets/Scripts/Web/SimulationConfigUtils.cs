/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Web 
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using UnityEngine;
    using System.Collections.Generic;

    public class MissedTemplateParameterError: Exception 
    {
        public MissedTemplateParameterError(string parameterName): base(parameterName) { }
    }

    public class SimulationConfigUtils
    {
        public class InternalTemplateAliases {
            public const String API_ONLY = "apiOnly";
            public const String RANDOM_TRAFFIC = "randomTraffic";
        }
        
        public static SimulationData ProcessKnownTemplates(ref SimulationData simData)
        {
            if (simData.Version == null) {
                Debug.Log("Got legacy Simulation Config request");
                return simData;
            }

            var namedParameters = new Dictionary<String, TemplateParameter>();
            var positionalParameters = new List<TemplateParameter>();

            foreach (var parameter in simData.Template.Parameters)
            {
                if (parameter.VariableName != null)
                {
                    namedParameters.Add(parameter.VariableName, parameter);
                }
                else
                {
                    positionalParameters.Add(parameter);
                }
            }

            // Process well-know parameters for internal templates
            // TODO Check parameter types
            if (simData.Template.Alias == InternalTemplateAliases.RANDOM_TRAFFIC)
            {
                TemplateParameter parameter;

                if (namedParameters.TryGetValue("SIMULATOR_MAP", out parameter))
                {
                    simData.Map = parameter.GetValue<MapData>();
                } 
                else
                {
                    throw new MissedTemplateParameterError("SIMULATOR_MAP");
                }

                if (namedParameters.TryGetValue("SIMULATOR_VEHICLES", out parameter)) 
                {
                    simData.Vehicles = parameter.GetValue<VehicleData[]>();
                }
                else
                {
                    throw new MissedTemplateParameterError("SIMULATOR_VEHICLES");
                }

                // Float/double values
                if (namedParameters.TryGetValue("SIMULATOR_RAIN", out parameter))
                {
                    simData.Rain = (float)parameter.GetValue<double>();
                }

                if (namedParameters.TryGetValue("SIMULATOR_FOG", out parameter))
                {
                    simData.Fog = (float)parameter.GetValue<double>();
                }

                if (namedParameters.TryGetValue("SIMULATOR_WETNESS", out parameter))
                {
                    simData.Wetness = (float)parameter.GetValue<double>();
                }

                if (namedParameters.TryGetValue("SIMULATOR_CLOUDINESS", out parameter))
                {
                    simData.Cloudiness = (float)parameter.GetValue<double>();
                }

                if (namedParameters.TryGetValue("SIMULATOR_DAMAGE", out parameter))
                {
                    simData.Damage = (float)parameter.GetValue<double>();
                }

                // Boolean flags
                if (namedParameters.TryGetValue("SIMULATOR_USE_TRAFFIC", out parameter))
                {
                    simData.UseTraffic = parameter.GetValue<bool>();
                }

                if (namedParameters.TryGetValue("SIMULATOR_USE_BICYCLISTS", out parameter))
                {
                    simData.UseBicyclists = parameter.GetValue<bool>();
                }

                if (namedParameters.TryGetValue("SIMULATOR_USE_PEDESTRIANS", out parameter))
                {
                    simData.UsePedestrians = parameter.GetValue<bool>();
                }

                // time of day
                if (namedParameters.TryGetValue("SIMULATOR_TIME_OF_DAY", out parameter))
                {
                    simData.TimeOfDay = parameter.GetValue<DateTime>();
                }

                if (namedParameters.TryGetValue("SIMULATOR_SEED", out parameter))
                {
                    simData.Seed = (int)parameter.GetValue<long>();
                }
            }
            else
            {
                TemplateParameter parameter;

                if (namedParameters.TryGetValue("SIMULATOR_API_ONLY", out parameter))
                {
                    simData.ApiOnly = parameter.GetValue<bool>();
                }
            }

            return simData;
        }
    }
}