import saze
import sys
import lgsvl
from lgsvl import Vector

def get_pedesrian_waypoints(pedestrian):
    waypoints = []
    ped_pos = pedestrian.transform.position

    wp1 = lgsvl.WalkWaypoint(lgsvl.Vector(ped_pos.x, ped_pos.y, ped_pos.z), 0)
    wp2 = lgsvl.WalkWaypoint(lgsvl.Vector(ped_pos.x-40, ped_pos.y, ped_pos.z), 0)
    waypoints.append(wp1)
    waypoints.append(wp2)

    return waypoints

def spawn_pedestrian(sim, pos, name):
    state = lgsvl.AgentState()
    spawns = sim.get_spawn()
    state.transform = sim.map_point_on_lane(pos)
    state.transform.position.z -= 3.5
    #state.transform.position.z += 3.5
    state.transform.rotation.y = 270
    ped = sim.add_agent(name, lgsvl.AgentType.PEDESTRIAN, state)
    return ped

def get_ped_event(ped):
    ped_waypoints = get_pedesrian_waypoints(ped)
    def event_func():
        ped.follow(ped_waypoints, False)
    event = saze.Event(func = event_func, params = None, only_once = True)
    return event

def get_main_callback(sim, ped, npc1, npc2, gps_sensor):

    ego_trigger_point = Vector(-15,0,0)
    ped_trigger_thrs = 25
    npc2_trigger_thrs = 30

    ped_waypoints = get_pedesrian_waypoints(ped)
    ped_event = saze.get_pedestrian_event(ped, ped_waypoints)

    npc1_end_pos = Vector(-45,0,16)
    npc1_event = saze.get_npc_event(sim, npc1, [npc1_end_pos], [15])

    npc2_wp1 = Vector(-13,0,37.5)
    npc2_wp2 = Vector(-17.5,0,30)
    npc2_wp3 = Vector(-17.5,0,-30)
    npc2_event = saze.get_npc_event(sim, npc2, [npc2_wp1, npc2_wp2, npc2_wp3], [15, 10, 15])

    def callback():
        gps_data = gps_sensor.data
        ego_tr = sim.map_from_gps(latitude = gps_data.latitude,\
                            longitude = gps_data.longitude,\
                            )
        dist = (ego_tr.position - ego_trigger_point).norm()
        #event.trigger()
        if dist < ped_trigger_thrs:
            ped_event.trigger()
            npc1_event.trigger()
        if dist < npc2_trigger_thrs:
            npc2_event.trigger()
    return callback

def main():
    map_name = "Shalun"
    app_tag = "Scenario 1"

    ego_spawn_pos = Vector(-75, 0, -40)
    npc1_spawn_pos = Vector(7, 0, 16)
    npc2_spawn_pos = Vector(10, 0, 48)
    ped1_spawn_pos = Vector(-7, 0, 8)

    sim = saze.open_simulator(map_name)
    saze.print_msg(app_tag, "Simulator opened")
    ego = saze.spawn_ego(sim, ego_spawn_pos)
    saze.print_msg(app_tag, "Ego vehicle spawned")
    gps_sensor = saze.get_gps_sensor(ego)
    saze.print_msg(app_tag,"GPS sensor ready")

    npc1 = saze.spawn_npc(sim, npc1_spawn_pos, car_type = "Sedan")
    npc2 = saze.spawn_npc(sim, npc2_spawn_pos, car_type = "Sedan")
    ped = spawn_pedestrian(sim, ped1_spawn_pos, name = "Bob")

    callback = get_main_callback(sim, ped, npc1, npc2, gps_sensor)
    sim.run_with_callback(callback)

if __name__=="__main__":
    main()
