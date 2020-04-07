# Distributed Components [](#top)
`DistributedComponent` is an abstract class with the implementation of sending snapshots from the authoritative object to all connected peers. For example `DistributedTransform` synchronize transforms states in the clients' simulations to the corresponding transform state on the master basing on the position, rotation and scale sent in the snapshots. `DistributedTransform` component added to GameObject in the simulation will synchronize the transform, note that every `DistributedComponent` requires a `DistributedObject` added to the same GameObject or any parent GameObject. 

### Custom Distributed Component [[top]] {: #custom-distributed-component data-toc-label='Custom Distributed Component'}
Extending the `DistributedComponent` component requires the following implementations:

* `ComponentKey` property, if multiple components of this type are allowed in a single GameObject it has to be a unique key otherwise, it can be for example class name.
* `GetSnapshot` method which returns snapshot data inside a `ByteStack` object (refer to [Distributed Messages](distributed-messages.md#bytes-stack) for more information about `ByteStack`).
* `ApplySnapshot` method which parses and applies the snapshot data from the message content to the object. Note that the `ApplySnapshot` method has to pop data in reverse order than `GetSnapshot` is pushing data.

## Distributed Components With Deltas [[top]] {: #distributed-components-with-deltas data-toc-label='Distributed Components With Deltas'}
A snapshot includes data required to recreate the same state on the client. Sending the whole snapshot with a whole object's state when only a single element changes will contain too much redundant data. `DistributedComponentWithDeltas` extends the basic implementation with two methods:

* `SendDelta` which sends the message with passed delta data inside a `ByteStack` object.
* `ApplyDelta` abstract method which parses and applies the delta data from the message content to the object. Note that the `SendDelta` method has to pop data in reverse order than `ApplyDelta` is pushing data.

## Distributed Transform [[top]] {: #distributed-transform data-toc-label='Distributed Transform'}
`DistributedTransform` sends local position, local rotation and local scale of a transform component in the snapshots from the master to the clients. Only a single `DistributedTransform` component can be attached to a GameObject. Snapshots are send up to 60 times per second only if any element of the snapshot changes. The snapshots limit can be changed on the master by changing the `SnapshotsPerSecondLimit` property value

## Distributed Rigidbody [[top]] {: #distributed-rigidbody data-toc-label='Distributed Rigidbody'}
`DistributedRigidbody` sends the position and rotation of the rigidbody in the same GameObject from the master to the clients. With the default setting snapshots are just applied to the rigidbodies. It is possible to change `SimulationType` to *ExtrapolateVelocities*, with this setting `DistributedRigidbody` extrapolates received velocity and angular velocity. Applied position and rotation includes the corrections calculated from the extrapolated velocities. Snapshots are send up to 60 times per second only if rigidbody is not in sleeping mode. The snapshots limit can be changed on the master by changing the `SnapshotsPerSecondLimit` property value