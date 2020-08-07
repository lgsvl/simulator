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
    using System.IO;
    using System.Text;
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
            if (simData.Version == null)
            {
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
                    var seed = (int)parameter.GetValue<long>();

                    if (seed != 0)
                    {
                        simData.Seed = seed;
                    }
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
        public static void ExtractEnvironmentVariables(
            TemplateData template,
            IDictionary<string, string> environment)
        {
            foreach (var parameter in template.Parameters)
            {
                if (parameter.VariableType == "env")
                {
                    if (parameter.ParameterType == "boolean")
                    {
                        // Special case for bool -> 1/0
                        environment.Add(parameter.VariableName, parameter.GetValue<bool>() ? "1" : "0");
                    } else {
                        environment.Add(parameter.VariableName, parameter.GetValue<string>());
                    }
                }
            }
        }

        public static string SaveVolumes(string simulationId, TemplateData template)
        {
            Console.WriteLine($"[TC] Saving volumes for simulationId:{simulationId}");

            var volumePath = GetSimulationVolumesPath(simulationId);

            if (!Directory.Exists(volumePath))
            {
                Directory.CreateDirectory(volumePath);
            }

            foreach (var parameter in template.Parameters)
            {
                if (parameter.VariableType == "volume")
                {
                    Console.WriteLine($"[TC] Saving volume {parameter.Alias} '{parameter.ParameterName}' as '{parameter.VariableName}'");
                    var targetFilePath = Path.Combine(volumePath, parameter.VariableName);

                    using (FileStream fs = File.Create(targetFilePath))
                    {
                        byte[] content = new UTF8Encoding(true).GetBytes(parameter.GetValue<string>());
                        fs.Write(content, 0, content.Length);
                    }
                }
            }

            return volumePath;
        }

        public static void CleanupVolumes(string simulationId)
        {
            Console.WriteLine($"[TC] Removing volumes for simulationId:{simulationId}");

            var volumePath = GetSimulationVolumesPath(simulationId);
            try
            {
                Directory.Delete(volumePath, true);
            }
            catch (IOException e)
            {
                Debug.LogWarning($"[TC] Failed to remove folder {volumePath}: {e}");
            }
        }

        public static void UpdateTestCaseEnvironment(TemplateData template, IDictionary<string,string> environment)
        {
            // Important note: Variable names are subject of further updates.
            ExtractEnvironmentVariables(template, environment);

            environment.Add("SIMULATOR_TC_RUNTIME", template.Alias);

            String bridgeConnectionString;

            // Explode bridge connection string if provided
            if (environment.TryGetValue("BRIDGE_CONNECTION_STRING", out bridgeConnectionString))
            {
                string[] bridge = bridgeConnectionString.Split(':');

                if (bridge.Length == 2)
                {
                    environment.Add("BRIDGE_HOST", bridge[0]);
                    environment.Add("BRIDGE_PORT", bridge[1]);
                } else if (bridge.Length == 1)
                {
                    environment.Add("BRIDGE_HOST", bridge[1]);
                }
            }

            if (!environment.ContainsKey("SIMULATOR_HOST"))
            {
                environment.Add("SIMULATOR_HOST", Config.ApiHost);
            }

            if (!environment.ContainsKey("SIMULATOR_PORT"))
            {
                environment.Add("SIMULATOR_PORT", Config.ApiPort.ToString());
            }
        }

        static public bool IsInternalTemplate(TemplateData template)
        {
            return template.Alias == InternalTemplateAliases.API_ONLY || template.Alias == InternalTemplateAliases.RANDOM_TRAFFIC;
        }

        private static string GetSimulationVolumesPath(string simulationId)
        {
            // TODO: Application.temporaryCachePath is a better
            // but runtime wrapper script can't handle spaces in paths
            return Path.Combine(
                Path.GetTempPath(),
                "LGSVLSimulator",
                "TestCaseRuntimeVolumes",
                simulationId);
        }
    }
}
