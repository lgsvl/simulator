import saze
import sys
import lgsvl
from lgsvl import Vector

def print_msg(self, msg):
    print("Saze Scenario 0: {0}".format(msg))

def open_simulator():
    sim = lgsvl.Simulator(os.environ.get("SIMULATOR_HOST", "127.0.0.1"), 8181)
    if sim.current_scene == "SimpleMap":
        sim.reset()
    else:
        sim.load("SimpleMap")

    return sim

def spawn_ego(sim):
    spawns = sim.get_spawn()

    state = lgsvl.AgentState()
    state.transform = spawns[0]
    ego = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, state)

    return ego

def get_gps_sensor(ego):
    gps_sensor = None
    for sensor in ego.get_sensors():
        if sensor.name == "GPS":
            gps_sensor = sensor
    return gps_sensor

def spawn_npc(sim):
    npc_pos = Vector(-48,0,-103)
    npc = saze.spawn_npc(sim, npc_pos, "Sedan")
    return npc

def get_npc_event(sim, npc):
    waypoints_vec = []
    waypoints_vec.append(Vector(46.3, 0, -103))

    # Setup waypoints for the NPC to follow
    speed = 30
    waypoints = []
    for v in waypoints_vec:
        waypoints.append(lgsvl.DriveWaypoint(v, speed))

    # This function will be triggered
    def npc_event_func():
        npc.follow(waypoints)

    npc_event = saze.Event(func = npc_event_func, params = None, only_once = True)

    return npc_event

def get_main_callback(sim, npc, gps_sensor):
    ego_trigger_point = Vector(-27,0,-78)
    dist_thrs = 25

    npc_event = get_npc_event(sim, npc)
    def callback():
        gps_data = gps_sensor.data
        ego_tr = sim.map_from_gps(latitude = gps_data.latitude,\
                            longitude = gps_data.longitude)
        dist = (ego_tr.position - ego_trigger_point).norm()
        if dist < dist_thrs:
            npc_event.trigger()

    return callback

def main():
    app_tag = "Scenario 0"
    sim = saze.open_simulator("SimpleMap")
    saze.print_msg(app_tag, "Simulator opened")
    ego = saze.spawn_ego(sim)
    saze.print_msg(app_tag, "Ego vehicle spawned")
    gps_sensor = saze.get_gps_sensor(ego)
    saze.print_msg(app_tag,"GPS sensor ready")

    npc = spawn_npc(sim)
    saze.print_msg(app_tag,"NPC vehicle spawned")

    main_callback = get_main_callback(sim, npc, gps_sensor)
    sim.run_with_callback(main_callback)

if __name__=="__main__":
    main()
