/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Web
{
    using System;
    using System.IO;
    using System.Text;
    using UnityEngine;
    using System.Collections.Generic;
    using System.Linq;

    public class MissedTemplateParameterError : Exception
    {
        public MissedTemplateParameterError(string parameterName) : base(parameterName) { }
    }

    public class SimulationConfigUtils
    {
        public class InternalTemplateAliases
        {
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

                if (namedParameters.TryGetValue("SIMULATOR_MAP", out parameter) ||
                   namedParameters.TryGetValue("LGSVL__MAP", out parameter))
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
                if (namedParameters.TryGetValue("SIMULATOR_RAIN", out parameter) ||
                    namedParameters.TryGetValue("LGSVL__ENVIRONMENT_RAIN", out parameter))
                {
                    simData.Rain = (float)parameter.GetValue<double>();
                }

                if (namedParameters.TryGetValue("SIMULATOR_FOG", out parameter) ||
                    namedParameters.TryGetValue("LGSVL__ENVIRONMENT_FOG", out parameter))
                {
                    simData.Fog = (float)parameter.GetValue<double>();
                }

                if (namedParameters.TryGetValue("SIMULATOR_WETNESS", out parameter) ||
                    namedParameters.TryGetValue("LGSVL__ENVIRONMENT_WETNESS", out parameter))
                {
                    simData.Wetness = (float)parameter.GetValue<double>();
                }

                if (namedParameters.TryGetValue("SIMULATOR_CLOUDINESS", out parameter) ||
                    namedParameters.TryGetValue("LGSVL__ENVIRONMENT_CLOUDINESS", out parameter))
                {
                    simData.Cloudiness = (float)parameter.GetValue<double>();
                }

                if (namedParameters.TryGetValue("SIMULATOR_DAMAGE", out parameter) ||
                    namedParameters.TryGetValue("LGSVL__ENVIRONMENT_DAMAGE", out parameter))
                {
                    simData.Damage = (float)parameter.GetValue<double>();
                }

                // Boolean flags
                if (namedParameters.TryGetValue("SIMULATOR_USE_TRAFFIC", out parameter) ||
                    namedParameters.TryGetValue("LGSVL__SPAWN_TRAFFIC", out parameter))
                {
                    simData.UseTraffic = parameter.GetValue<bool>();
                }

                if (namedParameters.TryGetValue("SIMULATOR_USE_BICYCLISTS", out parameter) ||
                    namedParameters.TryGetValue("LGSVL__SPAWN_BICYCLES", out parameter))
                {
                    simData.UseBicyclists = parameter.GetValue<bool>();
                }

                if (namedParameters.TryGetValue("SIMULATOR_USE_PEDESTRIANS", out parameter) ||
                    namedParameters.TryGetValue("LGSVL__SPAWN_PEDESTRIANS", out parameter))
                {
                    simData.UsePedestrians = parameter.GetValue<bool>();
                }

                // time of day
                if (namedParameters.TryGetValue("SIMULATOR_TIME_OF_DAY", out parameter) ||
                    namedParameters.TryGetValue("LGSVL__TIME_OF_DAY", out parameter))
                {
                    simData.TimeOfDay = parameter.GetValue<DateTime>();
                }

                if (namedParameters.TryGetValue("SIMULATOR_SEED", out parameter) ||
                    namedParameters.TryGetValue("LGSVL__RANDOM_SEED", out parameter))
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
                    }
                    else if (parameter.ParameterType == "map")
                    {
                        environment.Add(parameter.VariableName, parameter.GetValue<MapData>().Id);
                    }
                    else if (parameter.ParameterType == "vehicles")
                    {
                        var i = 0;
                        foreach (var vehicle in parameter.GetValue<VehicleData[]>())
                        {
                            environment.Add($"LGSVL__VEHICLE_{i}", vehicle.Id);
                            if (vehicle.Bridge != null && !String.IsNullOrEmpty(vehicle.Bridge.ConnectionString))
                            {
                                var parts = vehicle.Bridge.ConnectionString.Split(':');
                                // ipv6 not supported - ipv6 has many : in the address part so its harder to find the port
                                // unfortunately we do not have IPEndpoint.TryParse in .net core 2
                                if (parts.Length == 1)
                                {
                                    if (i == 0)
                                    {
                                        environment.Add("BRIDGE_HOST", vehicle.Bridge.ConnectionString); // legacy - vse_runner.py only supports one vehicle right now
                                    }
                                    environment.Add($"LGSVL__AUTOPILOT_{i}_HOST", vehicle.Bridge.ConnectionString);
                                }
                                else if (parts.Length == 2)
                                {
                                    if (i == 0)
                                    {
                                        environment.Add("BRIDGE_HOST", parts[0]); // legacy
                                        environment.Add("BRIDGE_PORT", parts[1]); // legacy
                                    }
                                    environment.Add($"LGSVL__AUTOPILOT_{i}_HOST", parts[0]);
                                    environment.Add($"LGSVL__AUTOPILOT_{i}_PORT", parts[1]);
                                }
                                else
                                {
                                    Debug.LogWarning("malformed ipv4 connection string: " + vehicle.Bridge.ConnectionString);
                                }
                            }
                            i++;
                        }
                    }
                    else
                    {
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

        public static void UpdateTestCaseEnvironment(TemplateData template, IDictionary<string, string> environment)
        {
            // Important note: Variable names are subject of further updates.
            ExtractEnvironmentVariables(template, environment);

            environment.Add("SIMULATOR_TC_RUNTIME", template.Alias);

            if (!environment.ContainsKey("LGSVL__SIMULATOR_HOST"))
            {
                var hostname = Config.ApiHost == "*" ? "localhost" : Config.ApiHost;
                if ((Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor) && !string.IsNullOrWhiteSpace(template.Alias))
                {
                    hostname = "host.docker.internal";
                    Debug.Log("(Docker on windows specific) Overriding LGSVL__SIMULATOR_HOST to host.docker.internal");
                    // previous method of taking the ip of the "vEthernet (WSL)" device stopped working for an unkown reason
                    // above hostname is from https://docs.docker.com/desktop/windows/networking/ where it says:

                    // "I want to connect from a container to a service on the host
                    // "The host has a changing IP address (or none if you have no network access)."
                    // "We recommend that you connect to the special DNS name host.docker.internal which resolves to the internal IP address used by the host."
                    // "This is for development purpose and will not work in a production environment outside of Docker Desktop for Windows."

                    // however we are only using this hostname on windows safeguarded above
                }

                environment.Add("SIMULATOR_HOST", hostname);
                environment.Add("LGSVL__SIMULATOR_HOST", hostname);
            }

            if (!environment.ContainsKey("LGSVL__SIMULATOR_PORT"))
            {
                environment.Add("SIMULATOR_PORT", Config.ApiPort.ToString());
                environment.Add("LGSVL__SIMULATOR_PORT", Config.ApiPort.ToString());
            }
        }

        static public bool IsInternalTemplate(TemplateData template)
        {
            return template.Alias == InternalTemplateAliases.API_ONLY || template.Alias == InternalTemplateAliases.RANDOM_TRAFFIC;
        }

        private static string GetSimulationVolumesPath(string simulationId)
        {
            return Path.Combine(
                Application.temporaryCachePath,
                "TestCaseRuntimeVolumes",
                simulationId);
        }
    }
}
