# Sensors Distribution [](#top)
Distribution sensors between different machines are the advantage of the cluster simulation. Sensors are not synchronized between simulation, one sensor can be simulated only on one machine, but one machine can still simulate multiple sensors. 

## Sensor Setup [[top]] {: #sensor-setup data-toc-label='Sensor Setup'}
Sensors distributed to the clients will not be simulated on the master, those sensors will not affect the simulation (for example manual control sensor has to be simulated on the master) and API requests callbacks will be delayed. Due to these restrictions distribution is disabled by default and enabling it requires additional setup. Every sensor which can be distributed to clients has to override the `DistributionType` property with *LowLoad*, *HighLoad*, *UltraHighLoad* value according to its performance overhead.

## Sensors Load Balancing [[top]] {: #sensors-load-balancing data-toc-label='Sensors Load Balancing'}
The current load balancing algorithm divides sensors into groups by their type. Master distributes sensors by counting overhead sum and assigning each next sensor to the least overloaded machine.

* *UltraHighLoad* sensors are assigned first and adds 1.0 overhead to a machine. Sensors like `LIDAR`, which parses the camera images and performs complex maths operations, should be classified under this type.
* *HighLoad* sensors are assigned next and each sensor adds 0.1 overhead to a machine. Sensors like `RadarSensor`, which performs complex maths operations, should be classified under this type.
* *LowLoad* sensors are assigned last and each sensor adds 0.05 overhead to a machine. Sensors like `GpsSensor`, which performs simple maths operations, should be classified under this type.
* *DoNotDistribute* sensors are assigned only to the master machine. Sensors like `VehicleControlSensor`, which controls objects in a simulation, have to be classified under this type.

As the master machine requires more resources *UltraHighLoad* sensors that can be distributed will never be assigned to the master, additionally master starts the algorithm with 0.15 overhead.