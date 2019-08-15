# Web UI Simulations Tab Explanation

A `Simulation` can be in the following states. 

- A `Simulation` will have a `Valid` status if it can be run
- A `Simulation` can become `Invalid` for several reasons:
	- A `Map` or `Vehicle` has become `Invalid` since the `Simulation` was created
	- A `Vehicle` with a bridge is missing a `Bridge Connection String`

### How to Add/Edit a Simulation
1. Click the `Add new` button or the pencil icon
2. The dialogue that opens has 4 tabs which change the parameters of the Simulation:
	1. `General`
		- `Simulation Name`: The name of the Simulation
		- `Select Cluster`: From the dropdown, select the cluster of computers that will run the Simulation
		- `API Only`: Check this if the Simulation will be controlled through the Python API. Checking this will disable most other options as they will be set through the API
		- `Headless Mode`: Check this if it is not necessary to render the Simulator in the main window. Checking this will improve performance.

		[![](images/web-simulation-general.png)](images/full_size_images/web-simulation-general.png)

	2. `Map & Vehicles`
		- `Select Map`: From the dropdown, choose the map that will be used
		- `Select Vehicle`: From the dropdown, choose the vehicle that will be spawned
			- `Bridge Connection String`: If the chosen vehicle has a Bridge Type, an IP:port must be provided to the bridge host
		- `+`: Adds an additional vehicle. Vehicles will spawn in `Spawn Info` positions of the map in order
		- `Interactive Mode`: Check this to enable Simulation controls

		[![](images/web-simulation-mapVehicle.png)](images/full_size_images/web-simulation-mapVehicle.png)

	3. `Traffic`
		- `Use Predefined Seed`: Check this and enter a seed [int] which will be used deterministically control NPCs
		- `Enable NPC`: Check this to have NPC vehicles spawn at the beginning of the Simulation
		- `Enable Pedestrians`: Check this to have Pedestrians spawn at the beginning of the Simulation

		[![](images/web-simulation-traffic.png)](images/full_size_images/web-simulation-traffic.png)

	4. `Weather`
		- `Time of Day`: Set the time of day for the Simulation
		- `Rain`: [0-1] set how much rain should fall
		- `Wetness`: [0-1] set how wet the roads should be
		- `Fog`: [0-1] set thick fog there should be
		- `Cloudiness`: [0-1] set how much cloud cover thee should be

		[![](images/web-simulation-weather.png)](images/full_size_images/web-simulation-weather.png)

