# Cluster Simulation Introduction [](#top)
### What a Cluster is [[top]] {: #what-a-cluster-is data-toc-label='What a Cluster is'}
A `Cluster` is a single unit of simulation. By default there is always local machine available as a cluster.

When a user launches the simulator, there is a command-line option available to select between simulation master and client. 
When Simulator is started as master (default mode) user can access Web UI and perform simulation tasks. 
When Simulator is started as a client (using command-line argument) simulator is not rendering anything, but expects connection from a master.

For a simulation to start, master and client should share the same maps, vehicles and simulation configuration.

Simulation master is responsible to provide all required configuration and client is responsible to download all asset bundles required to start the simulation. 
Simulation master and other clients are waiting for all nodes to download and cache required asset bundles.

Master is responsible for assigning sensors to each client for simulation (including itself). 
After sensors are assigned each client starts the simulation while synchronizing the position of vehicles.

See [Cluster Simulation Quickstart](cluster-simulation-quickstart.md) for details on running a Cluster Simulation.

### Why use a Cluster [[top]] {: #why-use-a-cluster data-toc-label='Why use a Cluster'}
Clusters allow for better performance when generating data from multiple sensors. 
Unity does not have multiple-GPU support so multiple instances of the simulator are required.

This also allows for large-scale simulations with multiple vehicles on the same map.

## Simulation Synchronization [[top]] {: #simulation-synchronization data-toc-label='Simulation Synchronization'}
For the proper functioning of the sensors distributed to the clients, it is required to synchronize the whole simulation environment. Simulator starts a simulation on the master and sends clients configuration required to set up the same map and objects on the map. When the simulation changes on master it sends updates messages to every client. Simulator by default synchronizes every `Rigidbody` in ego vehicles, NPCs, pedestrians and controllable objects instantiated by Simulator Managers.

### Components Synchronization [[top]] {: #components-synchronization data-toc-label='Components Synchronization'}
Every component that has to be synchronized between cluster machines requires messages sender on the master and messages receiver on the clients. Taking a vehicle with rigidbody as an example, Simulator adds `DistributedObject` component to the vehicle GameObject to synchronize *enable* and *disable* calls and `DistributedRigidbody` components which send required data to mock the state of `Rigidbody` on clients. 

Default implementation required each `DistributedObject` to have a unique path in the hierarchy - objects on the same hierarchy level require unique GameObject names. For more advanced solutions refer to [Distributed Objects](distributed-objects.md).

Due to the performance impact, the Simulator by default does not synchronize `Transform` components. To add this functionality refer to [Distributed Components](distributed-components.md).