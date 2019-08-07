import saze
import sys
import lgsvl
from lgsvl import Vector

def get_pedesrian_waypoints(pedestrian):
    waypoints = []
    ped_pos = pedestrian.transform.position

    wp1 = lgsvl.WalkWaypoint(lgsvl.Vector(ped_pos.x, ped_pos.y, ped_pos.z), 0)
    wp2 = lgsvl.WalkWaypoint(lgsvl.Vector(ped_pos.x, ped_pos.y, ped_pos.z + 12), 0)
    waypoints.append(wp1)
    waypoints.append(wp2)

    return waypoints

def get_npc1_event(sim, npc):
    waypoint_vecs = []
    waypoint_vecs.append(Vector(-72,0,38))

    speeds = [15]

    return saze.get_npc_event(sim, npc, waypoint_vecs, speeds)

def get_npc2_event(sim, npc):
    waypoint_vecs = []
    waypoint_vecs.append(Vector(13,0,38))
    waypoint_vecs.append(Vector(-72,0,38))


    speeds = [5, 10]

    return saze.get_npc_event(sim, npc, waypoint_vecs, speeds)

def get_main_callback(sim, ped, npc1, npc2, gps_sensor):

    ped_waypoints = get_pedesrian_waypoints(ped)
    ped_event = saze.get_pedestrian_event(ped, ped_waypoints)

    npc1_event = get_npc1_event(sim, npc1)
    npc2_event = get_npc2_event(sim, npc2) # same event

    ego_trigger_point = Vector(9,0, 34)
    trigger_thrs = 65

    def callback():
        gps_data = gps_sensor.data
        ego_tr = sim.map_from_gps(latitude = gps_data.latitude,\
                            longitude = gps_data.longitude,\
                            )
        #print(ego_tr)
        dist = (ego_tr.position - ego_trigger_point).norm()
        if dist < trigger_thrs:
            ped_event.trigger()
            npc1_event.trigger()
            npc2_event.trigger()
    return callback

def main():
    map_name = "Shalun"
    app_tag = "Scenario 3"

    ego_spawn_pos = Vector(-80, 0, -30)
    #ego_spawn_pos = Vector(63.5, 0, -27)

    truck_pos = Vector(9,0, 34)
    truck_offset = Vector(0, 0, -3)

    npc1_pos = Vector(43, 0, 37)
    npc2_pos = Vector(48, 0, 37)

    ped_pos = Vector(15, 0, 34)
    ped_offset = Vector(0, 0, -5)
    ped_rot = Vector(0, 270, 0)

    sim = saze.open_simulator(map_name)
    saze.print_msg(app_tag, "Simulator opened")
    ego = saze.spawn_ego(sim, ego_spawn_pos)
    saze.print_msg(app_tag, "Ego vehicle spawned")
    gps_sensor = saze.get_gps_sensor(ego)
    saze.print_msg(app_tag,"GPS sensor ready")

    truck = saze.spawn_npc(sim, truck_pos, car_type = "DeliveryTruck", offset = truck_offset)
    npc1 = saze.spawn_npc(sim, npc1_pos, car_type = "Sedan")
    npc2 = saze.spawn_npc(sim, npc2_pos, car_type = "Sedan")

    ped1 = saze.spawn_pedestrian(sim, ped_pos, "Bob", offset = ped_offset, rotation = ped_rot)

    callback = get_main_callback(sim, ped1, npc1, npc2, gps_sensor)
    sim.run_with_callback(callback)

if __name__=="__main__":
    main()
