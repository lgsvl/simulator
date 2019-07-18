# <a name="top"></a> Configuration File and Command Line Parameters

## Configuration File <sub><sup>[top](#top)</sup></sub> {: #configuration-file data-toc-label='Configuration File'}
To use a custom `config.yml` file, include it in the same folder as `Simulator.exe` or modify the one in the root directory of the cloned project.

Simulator configuration file includes parameters shared between different users and allows administrator to setup deployment specific parameters. Simulator uses YAML format for storing data in configuration file. See a table below to find out all supported parameters:

|Parameter Name|Type|Default Value|Description|
|:-:|:-:|:-:|:-:|
|hostname|string|localhost|Name of the HTTP server host. Simulator should respond to queries only related to the hostname provided. Star (*) can be used as a wildcard to match any domain.|
|port|integer|8080|Port number used by HTTP server to host WebUI.|
|headless|bool|false|Whether or not simulator should work in headless mode only. If parameter is set to true - non headless simulation should fail to start, WebUI should not allow to create non-headless simulations.|
|slave|bool|false|Whether or not simulator should work in slave mode only. If parameter is set to true - HTTP server does not start and user only can run this instance as a part of cluster.|
|read_only|bool|false|Whether or not the user is allowed to change anything in the database. This mode is used to run Simulator in public demo mode.|
|api_hostname|string|localhost|Name of the Python API host. By default it equal to hostname. Python API should respond to queries only related to the api_hostname provided. Star (*) can be used as a wildcard to match any domain.|
|api_port|integer|8181|Port number used by Python API to connect.|
|cloud_url|string|TBD|Cloud URL points to a simulator API endpoint in the cloud and is responsible for user authentication, new user registration and sharing content. Simulator uses our public API endpoint by default, but that could be changed for private on-premise deployment.|

## Command Line Parameters <sub><sup>[top](#top)</sup></sub> {: #command-line-parameters data-toc-label='Command Line Parameters'}
Simulator accepts provided command line parameters during start. Command line parameters overrides values from Configuration File. Only most important parameters can be provided via command line, see more details about Simulator Configuration File in the section related to configuration file. List of supported command line parameters can be found below:

|Parameter Name|Type|Default Value|Description|
|:-:|:-:|:-:|:-:|
|--hostname or -h|string|localhost|Name of the HTTP server host. Simulator should respond to queries only related to the hostname provided. Star (*) can be used as a wildcard to match any domain.|
|--port or -p|integer|8080|Port number used by HTTP server to host WebUI.|
|--slave or -s|none||Whether or not simulator should work in slave mode only. If parameter is present - HTTP server does not start and user only can run this instance as a part of cluster.|
|--master or -m|none||Whether or not simulator should work in master mode. If parameter is present - HTTP server starts and user can use WebUI regardless of what was specified in Simulator Configuration File.|
|--username or -u|string||Provides username for authentication with the cloud.|
|--password or -p|string||Provides password for authentication with the cloud.|
|--agree|none||Accepts the license agreement and forces to skip it in Web UI for specified user. This parameter is optional and can only be used only when username has been provided.|
