# Cluster Simulation Quickstart [](#top)
## Start Simulators [[top]] {: #start-simulators data-toc-label='Start Simulators'}

Cluster Simulation requires a single Simulator application running in the master mode and at least one Simulator application running in the client mode on another machine. Each Simulator application has to be running and waiting for the simulation start.

See [Configuration File and Command-Line Parameters](config-and-cmd-line-params.md) for details on how to set up the Simulator on each machine in a cluster.

[![](images/distributed-awaiting-simulator.png)](images/full_size_images/distributed-awaiting-simulator.png)

## Prepare The Cluster [[top]] {: #prepare-the-cluster data-toc-label='Prepare The Cluster'}

Open the WebUI on the machine where the Simulator application is running in the master mode and prepare a cluster with all the clients' IP addresses. 

See [Clusters Tab](clusters-tab.md) for details on how to set up a cluster in the WebUI.

[![](images/distributed-local-cluster.png)](images/full_size_images/distributed-local-cluster.png)

## Prepare And Start A Simulation [[top]] {: #prepare-and-start-simulation data-toc-label='Prepare And Start A Simulation'}

Open the WebUI on the machine where the Simulator application is running in the master mode and prepare a simulation that uses prepared cluster with the clients' IP addresses. Start the simulation, the master will try to establish a connection with the clients if successful the simulation will start on each machine. Note that clients will download required maps and vehicles' asset bundles, this may cause an additional delay when starting a simulation.

[![](images/distributed-local-simulation.png)](images/full_size_images/distributed-local-simulation.png)