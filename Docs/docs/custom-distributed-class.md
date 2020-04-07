# Custom Distributed Class [](#top)
Distributed system supports sending distributed messages from any class implementing `IMessageSender` to the objects of classes `IMessageReceiver`. A custom class can implement both interfaces, address key property will be shared.

## Address Key [[top]] {: #address-key data-toc-label='Address Key'}
`Key` property of the `IMessageSender` and `IMessageReceiver` is an address for the distributed messages. This key must be globally unique and deterministic, on every machine in every Simulation run the key of an object has to return the same value.

## Registration [[top]] {: #registration data-toc-label='Registration'}
Every object that is going to send or receive distributed messages has to register itself in the `MessagesManager`. In the Simulator `MessagesManager` instance is available in the: `Loader.Instance.Network.MessagesManager`, this property has a null reference if the simulation does not use a cluster. Whenever an object is ready to send or receive distributed messages it has to register itself with the `RegisterObject` method of the `MessagesManager` and when an object will no longer send or receiver messages it has to unregister itself with `UnregisterObject` method of the `MessagesManager`. Messages received for the unregistered address key will be stored and passed to proper objects after registration.

## Message Sender [[top]] {: #message-sender data-toc-label='Message Sender'}
`IMessageSender` implementation requires:

* `UnicastMessage`, a basic implementation invokes `UnicastMessage` method of an `MessagesManager` instance;
* `BroadcastMessage`, a basic implementation invokes `BroadcastMessage` method of an `MessagesManager` instance;
* `UnicastInitialMessages`, requires sending every data required for the object initialization using `UnicastMessage` method.

## Message Receiver [[top]] {: #message-receiver data-toc-label='Message Receiver'}
`IMessageReceiver` implementation requires:

* `ReceiveMessage`, parses every incomming data after object is registered.