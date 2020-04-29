/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Tests.Runtime.Shared.Messaging.Data
{
	using System.Collections;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;

	using NUnit.Framework;

	using PetaPoco;

	using Simulator.Database;
	using Simulator.Network.Shared;

	using UnityEditor;

	using UnityEngine;
	using UnityEngine.SceneManagement;
	using UnityEngine.TestTools;

	using Debug = UnityEngine.Debug;

	/// <summary>
	/// Tests of running the cluster simulation
	/// </summary>
	[TestFixture]
	public class ClusterSimulationLaunchTest
	{
		/// <summary>
		/// Timeout in seconds used when trying to establish connection with clients
		/// </summary>
		private const float ClientsConnectTimeout = 30.0f;

		/// <summary>
		/// Time limit for a single python script test
		/// </summary>
		private const float PythonTestTimeLimit = 5 * 60.0f;

		/// <summary>
		/// Tests of running the cluster simulation
		/// </summary>
		/// <returns>IEnumerator</returns>
		[UnityTest]
		[Timeout(3600000)]
		public IEnumerator TestClusterSimulation()
		{
			yield return LoadLoaderScene();

			var clusters = ListModels<ClusterModel>();
			var testCluster = clusters.FirstOrDefault(cluster => cluster.Name == "TestCluster");
			Assert.IsNotNull(testCluster, "Cluster simulation tests require defined cluster named \"TestCluster\".");
			var maps = ListModels<MapModel>();
			var mapModels = maps as MapModel[] ?? maps.ToArray();
			Assert.IsTrue(mapModels.Any(), "Cluster simulation tests require at least one map.");
			var vehicles = ListModels<VehicleModel>();
			var vehicleModels = vehicles as VehicleModel[] ?? vehicles.ToArray();
			Assert.IsTrue(
				vehicleModels.Any(),
				"Cluster simulation tests require at least one vehicle without a bridge.");
			//Disable using bridge for the test purposes
			foreach (var vehicle in vehicleModels)
				vehicle.BridgeType = "";
			//Duplicate random vehicle
			var vehicleToDuplicate = vehicleModels.GetValue(Random.Range(0, vehicleModels.Length));
			vehicleModels.Append(vehicleToDuplicate);
			var vehiclesIds = vehicleModels.Select(model => model.Id).ToArray();
			for (var i = 0; i < mapModels.Length; i++)
			{
				var mapModel = mapModels[i];
				yield return RunSimulation(testCluster.Id, mapModel.Id, vehiclesIds, false);
				yield return RunSimulation(testCluster.Id, mapModel.Id, vehiclesIds, true);
			}
		}

		/// <summary>
		/// Tests of running the cluster simulation in API mode
		/// </summary>
		/// <returns>IEnumerator</returns>
		[UnityTest]
		[Timeout(3600000)]
		public IEnumerator TestApiClusterSimulation()
		{
			yield return LoadLoaderScene();

			var clusters = ListModels<ClusterModel>();
			var testCluster = clusters.FirstOrDefault(cluster => cluster.Name == "TestCluster");
			Assert.IsNotNull(testCluster, "Cluster simulation tests require defined cluster named \"TestCluster\".");
			yield return RunApiSimulation(testCluster.Id);
		}

		/// <summary>
		/// Loads the LoaderScene required for the simulations
		/// </summary>
		/// <returns>IEnumerator</returns>
		private IEnumerator LoadLoaderScene()
		{
			var loaderScene = SceneManager.GetSceneByName("LoaderScene");
			Assert.IsNotNull(loaderScene, "Scene named \"LoaderScene\" is required to perform Cluster Simulation.");
			var asyncOperation = SceneManager.LoadSceneAsync("LoaderScene");
			while (!asyncOperation.isDone) yield return null;
			while (Loader.Instance == null) yield return null;

			Assert.IsNotNull(
				Loader.Instance.Network.Master,
				"Simulator has to run in master mode to perform cluster simulation test.");
		}

		/// <summary>
		/// Lists models of given type from the database
		/// </summary>
		/// <typeparam name="T">Type of models to list</typeparam>
		/// <returns>Models of given type from the database</returns>
		private IEnumerable<T> ListModels<T>()
		{
			using (var db = DatabaseManager.Open())
			{
				var sql = Sql.Builder
				             .OrderBy("id");

				return db.Fetch<T>(sql);
			}
		}

		/// <summary>
		/// Runs a single simulation with passed parameters and tests it
		/// </summary>
		/// <param name="clusterId">Id of the cluster used in simulation</param>
		/// <param name="mapId">Id of the map used in simulation</param>
		/// <param name="vehicleIds">Ids of the vehicles used in simulation</param>
		/// <param name="interactive">Ids of the vehicles used in simulation</param>
		/// <returns>IEnumerator</returns>
		private IEnumerator RunSimulation(long clusterId, long mapId, long[] vehicleIds, bool interactive)
		{
			var vehicles = new ConnectionModel[vehicleIds.Length];
			for (var i = 0; i < vehicleIds.Length; i++)
			{
				var vehicleId = vehicleIds[i];
				vehicles[i] = new ConnectionModel()
				{
					Connection = "",
					Id = 0,
					Vehicle = vehicleId,
					Simulation = 0
				};
			}

			var simulationModel = new SimulationModel()
			{
				Cluster = clusterId,
				Headless = false,
				Interactive = interactive,
				Vehicles = vehicles,
				Map = mapId,
				ApiOnly = false,
				UsePedestrians = true,
				UseTraffic = true
			};
			yield return RunSimulation(simulationModel);
			yield return WaitForClients();
			yield return new WaitForSecondsRealtime(1.0f);
			if (interactive)
				SimulatorManager.Instance.UIManager.PauseButtonOnClick();
			yield return PerformPingsTests(10);

			Loader.StopAsync();
			while (Loader.Instance.CurrentSimulation != null) yield return null;
		}

		/// <summary>
		/// Starts the simulation asynchronously and waits until it's loaded
		/// </summary>
		/// <param name="simulationModel">SimulationModel used for the simulation</param>
		/// <returns>IEnumerator</returns>
		private IEnumerator RunSimulation(SimulationModel simulationModel)
		{
			Loader.StartAsync(simulationModel);
			while (Loader.Instance.CurrentSimulation == null) yield return null;
			while (Loader.Instance.CurrentSimulation.Status != "Running") yield return null;
		}

		/// <summary>
		/// Waits for the connection with clients
		/// </summary>
		/// <returns>IEnumerator</returns>
		private IEnumerator WaitForClients()
		{
			var connectingTime = 0.0f;
			var master = Loader.Instance.Network.Master;
			while (master.State != SimulationState.Running)
			{
				connectingTime += Time.unscaledDeltaTime;
				Assert.IsTrue(
					(connectingTime < ClientsConnectTimeout) || (master.State == SimulationState.Loading),
					"Cluster Simulation Test could not connect to the defined clients. Make sure that all clients defined in the TestCluster are running the Simulator in client mode.");
				yield return null;
			}
		}

		/// <summary>
		/// Performs the pings tests, requires a cluster simulation to be running
		/// </summary>
		/// <param name="pingsTests">Number of performed ping tests</param>
		/// <returns>IEnumerator</returns>
		private IEnumerator PerformPingsTests(int pingsTests)
		{
			var master = Loader.Instance.Network.Master;
			var failedPongTests = 0;
			for (int i = 0; i < pingsTests; i++)
			{
				var pingDuration = 0.0f;
				master.SendPing();
				while (!master.ReceivedAllPongs())
				{
					pingDuration += Time.unscaledDeltaTime;
					if (pingDuration >= ClientsConnectTimeout)
					{
						failedPongTests++;
						continue;
					}

					yield return null;
				}
			}

			if (failedPongTests > 0)
				Debug.LogWarning(
					$"Failed to complete {failedPongTests} out of {pingsTests} ping-pong tests during the test.");
		}

		/// <summary>
		/// Runs a single simulation with passed parameters and tests it
		/// </summary>
		/// <param name="clusterId">Id of the cluster used in simulation</param>
		/// <returns>IEnumerator</returns>
		private IEnumerator RunApiSimulation(long clusterId)
		{
			var simulationModel = new SimulationModel()
			{
				Cluster = clusterId,
				Headless = false,
				Interactive = false,
				Vehicles = null,
				Map = null,
				ApiOnly = true,
				UsePedestrians = true,
				UseTraffic = true
			};
			yield return RunSimulation(simulationModel);
			yield return new WaitForSecondsRealtime(1.0f);
			yield return RunPythonScriptTests();

			Loader.StopAsync();
			while (Loader.Instance.CurrentSimulation != null) yield return null;
		}

		/// <summary>
		/// Runs the Python script tests
		/// </summary>
		/// <returns>IEnumerator</returns>
		private IEnumerator RunPythonScriptTests()
		{
			var testsPath = $"{Application.dataPath}/External/Tests/";
			Assert.True(Directory.Exists(testsPath), $"Tests directory {testsPath} does not exists. Cannot complete tests of the simulation in API mode.");
			var tests = System.IO.Directory.GetFiles(testsPath, "*.py");
			Assert.True(tests.Length > 0, "No python scripts were found in the Assets/External/Tests/ directory. Cannot complete tests of the simulation in API mode.");
			foreach (var test in tests)
			{
				var scriptTime = 0.0f;
				var foo = new Process
				{
					StartInfo =
					{
						FileName = test,
						Arguments = "",
						WindowStyle = ProcessWindowStyle.Hidden
					}
				};
				foo.Start();
				yield return WaitForClients();
				while (!foo.HasExited)
				{
					scriptTime += Time.unscaledDeltaTime;
					Assert.True(
						scriptTime < PythonTestTimeLimit,
						$"Python script test has exceeded the time limit ({PythonTestTimeLimit}s), make sure test {test} does not require input. If required exceed the time limit {typeof(ClusterSimulationLaunchTest)}.PythonTestTimeLimit.");
					yield return null;
				}
			}
		}
	}
}
