# <a name="top"></a>Bridge Connection UI
When in a non-Headless Simulation, a list of published and subscribed topics can be found in the Simulator menu (plug icon).

[![](images/bridge-ui.png)](images/full_size_images/bridge-ui.png)

At the top of the menu is the selected vehicle.

The bridge status can be: `Disconnected`, `Connecting`, or `Connected`

The bridge address is the same that was entered as the `Bridge Connection String` when creating the Simulation.

Each topic is then listed in the following format:

`PUB` or `SUB`: indicates if the Simulator publishes or subscribes to messages on this topic

`Topic`: is the topic that the messages are published/subscribed to

`Type`: is the message type on this topic

`Count`: is the total number of messages published/received when the bridge was connected