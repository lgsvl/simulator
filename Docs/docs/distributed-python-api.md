# Distributed Python API [](#top)
Cluster Simulation performs changes only on the master simulation, clients' simulations apply the changes received from the master and don't require to react on every API command. Only selected commands are distributed to the clients, for example, *AddAgent*, *LoadScene* and *Reset* commands.

## Command Setup [[top]] {: #command-setup data-toc-label='Command Setup'}
If command should be distributed to the clients it has to implement the `IDistributedObject` interface. Master simulation can modify the arguments that will be sent to the clients inside the `Execute` methods.