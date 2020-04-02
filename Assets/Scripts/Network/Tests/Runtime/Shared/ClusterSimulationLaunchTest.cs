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
	using System.Linq;

	using NUnit.Framework;

	using PetaPoco;

	using Simulator.Database;
	using Simulator.Network.Shared;

	using UnityEngine;
	using UnityEngine.SceneManagement;
	using UnityEngine.TestTools;

	/// <summary>
	/// Tests of running the cluster simulation
	/// </summary>
	[TestFixture]
	public class ClusterSimulationLaunchTest : MonoBehaviour
	{
		private const float ClientsConnectTimeout = 30.0f;

		/// <summary>
		/// Tests of running the cluster simulation
		/// </summary>
		/// <returns>IEnumerator</returns>
		[UnityTest]
		[Timeout(3600000)]
		public IEnumerator TestClusterSimulation()
		{
			var loaderScene = SceneManager.GetSceneByName("LoaderScene");
			Assert.IsNotNull(loaderScene, "Scene named \"LoaderScene\" is required to perform Cluster Simulation.");
			var asyncOperation = SceneManager.LoadSceneAsync("LoaderScene");
			while (!asyncOperation.isDone) yield return null;
			while (Loader.Instance == null) yield return null;

			Assert.IsNotNull(
				Loader.Instance.Network.Master,
				"Simulator has to run in master mode to perform cluster simulation test.");
			var clusters = ListModels<ClusterModel>();
			var testCluster = clusters.FirstOrDefault(cluster => cluster.Name == "TestCluster");
			Assert.IsNotNull(testCluster, "Cluster simulation tests require defined cluster named \"TestCluster\".");
			var maps = ListModels<MapModel>();
			var mapModels = maps as MapModel[] ?? maps.ToArray();
			Assert.IsTrue(mapModels.Any(), "Cluster simulation tests require at least one map.");
			var vehicles = ListModels<VehicleModel>().Where(vehicle => string.IsNullOrEmpty(vehicle.BridgeType));
			var vehicleModels = vehicles as VehicleModel[] ?? vehicles.ToArray();
			Assert.IsTrue(
				vehicleModels.Any(),
				"Cluster simulation tests require at least one vehicle without a bridge.");
			var vehiclesIds = vehicleModels.Select(model => model.Id).ToArray();
			for (var i = 0; i < mapModels.Length; i++)
			{
				var mapModel = mapModels[i];
				yield return RunSimulation(testCluster.Id, mapModel.Id, vehiclesIds);
			}
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
		/// <returns>IEnumerator</returns>
		private IEnumerator RunSimulation(long clusterId, long mapId, long[] vehicleIds)
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
				Interactive = false,
				Vehicles = vehicles,
				Map = mapId,
				ApiOnly = false,
				UsePedestrians = true,
				UseTraffic = true
			};
			Loader.StartAsync(simulationModel);
			while (Loader.Instance.CurrentSimulation == null) yield return null;
			while (Loader.Instance.CurrentSimulation.Status != "Running") yield return null;

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

			yield return new WaitForSeconds(1.0f);

			var pingsTests = 5;
			var failedPongTests = 0;
			for (int i = 0; i < pingsTests; i++)
			{
				connectingTime = 0.0f;
				master.SendPing();
				while (!master.ReceivedAllPongs())
				{
					connectingTime += Time.unscaledDeltaTime;
					if (connectingTime >= ClientsConnectTimeout)
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

			Loader.StopAsync();
			while (Loader.Instance.CurrentSimulation != null) yield return null;
		}
	}
}
