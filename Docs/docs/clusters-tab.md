# Web UI Clusters Tab Explanation [](#top)

Clusters have not been implemented yet.

### What a Cluster is [[top]] {: #what-a-cluster-is data-toc-label='What a Cluster is'}
A `Cluster` is a single unit of simulation. By default there is always local machine available as a cluster.

When a user launches  the simulator, there is a command line option available to select between simulation master and slave. 
When Simulator is started as master (default mode) user can access Web UI and perform simulation tasks. 
When Simulator is started as a slave (using command line argument) simulator is not rendering anything, but expects connection from master.

For a simulation to start, Master and Slave should share the same maps, vehicles and simulation configuration.

Simulation master is responsible to provide all required configuration and slave is responsible to download all asset bundles required to start simulation. 
Simulation master and other slaves are waiting for all nodes to download and cache required asset bundles.

Master is responsible for assigning sensors to each Slave for simulation (including itself). 
After sensors are assigned each slave starts the simulation while synchronizing position of vehicles.

See [Configuration File and Command Line Parameters](config-and-cmd-line-params.md) for details on how to setup the Simulator on each machine in a cluster.

### Why use a Cluster [[top]] {: #why-use-a-cluster data-toc-label='Why use a Cluster'}
Clusters allow for better performance when generating data from multiple sensors. 
Unity does not have multiple-gpu support so multiple instances of the simulator are required.

This also allows for large-scale simulations with multiple vehicles in the same map.



### How to Add a Cluster [[top]] {: #how-to-add-a-cluster data-toc-label='How to Add a Cluster'}
1. Click the `Add new` button
2. In the dialogue that opens, enter the name of the cluster and IPv4 of each machine that will be in the cluster. A blank list of IPs will default to the localhost.

[![](images/web-add-cluster.png)](images/full_size_images/web-add-cluster.png)

### How to Edit a Cluster [[top]] {: #how-to-edit-a-cluster data-toc-label='How to Edit a Cluster'}
1. Click the pencil icon
2. In the dialogue that opens, the name of the cluster can be changed and the list of IPs can be modified.

[![](images/web-edit-cluster.png)](images/full_size_images/web-edit-cluster.png)

## Copyright and License [[top]] {: #copyright-and-license data-toc-label='Copyright and License'}

Copyright (c) 2019 LG Electronics, Inc.

This software contains code licensed as described in LICENSE.
