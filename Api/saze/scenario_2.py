import saze
import sys
import lgsvl
from lgsvl import Vector

def get_jeep1_event(sim, npc):
    waypoint_vecs = []
    waypoint_vecs.append(Vector(26.6, 0, -48))
    #waypoint_vecs.append(Vector(32, 0, -36))
    waypoint_vecs.append(Vector(28, 0, -36))
    waypoint_vecs.append(Vector(28,0,5.5))
    waypoint_vecs.append(Vector(9,0,14))
    waypoint_vecs.append(Vector(-48,0,15))

    speeds = [10, 10, 5, 10, 10]

    return saze.get_npc_event(sim, npc, waypoint_vecs, speeds)


def get_truck1_event(sim, npc):
    waypoint_vecs = []
    waypoint_vecs.append(Vector(28,0,5.5))
    waypoint_vecs.append(Vector(9,0,14))
    waypoint_vecs.append(Vector(-48,0,15))

    speeds = [5, 7, 10]

    return saze.get_npc_event(sim, npc, waypoint_vecs, speeds)

def get_sedan1_event(sim, npc):
    waypoint_vecs = []
    waypoint_vecs.append(Vector(9,0,14))
    waypoint_vecs.append(Vector(-48,0,15))

    speeds = [5, 10]

    return saze.get_npc_event(sim, npc, waypoint_vecs, speeds)

def get_main_callback(sim, sedan1, truck1, jeep1, gps_sensor):

    event_s1 = get_sedan1_event(sim, sedan1)
    event_t1 = get_truck1_event(sim, truck1)
    event_j1 = get_jeep1_event(sim, jeep1)

    ego_trigger_point = Vector(32,0,-23)
    event_s1_thrs = 10

    def callback():
        gps_data = gps_sensor.data
        ego_tr = sim.map_from_gps(latitude = gps_data.latitude,\
                            longitude = gps_data.longitude,\
                            )
        #print(ego_tr)
        #event_s1.trigger()
        event_t1.trigger()
        event_j1.trigger()
        dist = (ego_tr.position - ego_trigger_point).norm()
        if dist < event_s1_thrs:
            event_s1.trigger()
            pass
    return callback

def main():
    map_name = "Shalun"
    app_tag = "Scenario 2"

    ego_spawn_pos = Vector(-75, 0, -40)
    #ego_spawn_pos = Vector(-10, 0, -47)
    #ego_spawn_pos = Vector(28, 0, -36)
    sedan1_spawn_pos = Vector(28, 0, 5.5)
    sedan2_spawn_pos = Vector(23, 0, 27)
    truck1_spawn_pos = Vector(28, 0, -15)
    jeep1_spawn_pos = Vector(11, 0, -48)

    sim = saze.open_simulator(map_name)
    saze.print_msg(app_tag, "Simulator opened")
    ego = saze.spawn_ego(sim, ego_spawn_pos)
    saze.print_msg(app_tag, "Ego vehicle spawned")
    gps_sensor = saze.get_gps_sensor(ego)
    saze.print_msg(app_tag,"GPS sensor ready")

    sedan1 = saze.spawn_npc(sim, sedan1_spawn_pos, car_type = "Sedan")
    sedan2 = saze.spawn_npc(sim, sedan2_spawn_pos, car_type = "Sedan")
    truck1 = saze.spawn_npc(sim, truck1_spawn_pos, car_type = "DeliveryTruck")
    #truck2 = None
    jeep1 = saze.spawn_npc(sim, jeep1_spawn_pos, car_type = "Jeep")

    callback = get_main_callback(sim, sedan1, truck1, jeep1, gps_sensor)
    sim.run_with_callback(callback)

if __name__=="__main__":
    main()
