# Distributed Messages [](#top)
`DistributedMessage` is a class that is used to exchange data between peers in the distributed system. Every message contains the `Content` where the actual data is stored and meta-data like address key, message type, and timestamp.

### Address Key [[top]] {: #address-key data-toc-label='Address Key'}
`AddressKey` determines which object will receive the message on other simulations. `DistributedComponent` uses own key as an address key so the message will be received by corresponding components on other machines. `AddressKey` has to be set when sending a new message.

### Content [[top]] {: #content data-toc-label='Content'}
`Content` contains the actual data of the message in the `BytesStack` object. Note that poping data required reverse order than pushing.

### Message Type [[top]] {: #message-type data-toc-label='Message Type'}
`DistributedMessageType` determines how the message will be handled in the UDP protocol. This type has to be set when sending a new message.
Available distributed message types:

* *ReliableUnordered* packets won't be dropped, won't be duplicated, can arrive without order.
* *Sequenced* packets can be dropped, won't be duplicated, will arrive in order.
* *ReliableOrdered* packets won't be dropped, won't be duplicated, will arrive in order.
* *ReliableSequenced* packets can be dropped (except the last one), won't be duplicated, will arrive in order.
* *Unreliable* packets can be dropped, can be duplicated, can arrive without order.

*ReliableOrdered* messages are the most reliable, but may add significant delays, this type is best for the initialization messages like add new NPC command. *Unreliable* packets have the lowest delay between sending and handling a message but are not reliable. Snapshots are sent as *unreliable*, an incoming snapshot will override the data and delayed packets will not be used.

### Timestamp [[top]] {: #timestamp data-toc-label='Timestamp'}
The distributed system adds UTC timestamp to every message right before sending it, there is no need to set timestamp value when sending a message. A timestamp of the received message is set before passing the message to the addressed object. This timestamp is already corrected on the client by the value of connection latency and shows the approximate UTC DateTime when the message has been sent by the master.

## Bytes Stack [[top]] {: #bytes-stack data-toc-label='Bytes Stack'}
`BytesStack` is the message's content where the data is stored. Every data has to be represented as a set of bytes and has to be pushed to the stack. `BytesStack` class supports *byte*, *int*, *uint*, *long*, *float*, *double*, *bool* and *string* values. Operations that can be executed on those values are:

* *Push* - adds the value on the top of the stack;
* *Fetch* - reads the data from the top of the stack and does not remove it from the stack;
* *Pop* - reads the data from the top of the stack and removed it from the stack, note that *Pop* calls have to be called in reverse order than *Push*.

## Byte Compression [[top]] {: #byte-compression data-toc-label='Byte Compression'}
`ByteCompression` class adds extension methods to the `BytesStack` which add different values to the stack with limited bytes count:

* *CompressFloatToInt* and *DecompressFloatFromInt*;
* *PushEnum* and *PopEnum*;
* *PushCompressedColor* and *PopDecompressedColor*;
* *PushCompressedVector3* and *PushCompressedVector3*;
* *PushUncompressedVector3* and *PopUncompressedVector3*;
* *PushCompressedPosition* and *PopDecompressedPosition* - position bounds are limited to the map bounds;
* *PushCompressedRotation* and *PopDecompressedRotation*.