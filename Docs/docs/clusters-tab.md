# Web UI Clusters Tab Explanation [](#top)

### What a Cluster is [[top]] {: #what-a-cluster-is data-toc-label='What a Cluster is'}
A Cluster allows connecting multiple machines rendering the same simulation, but different sensors on each machine, this allows to run multiple sensors with better performance. Master establishes a connection with each defined client, divides required sensors between every machine and sends the state of every Simulation element to connected clients synchronizing the environment.

For more details about the cluster, simulations refer to the [Cluster Simulation Guide](cluster-simulation.md).


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
