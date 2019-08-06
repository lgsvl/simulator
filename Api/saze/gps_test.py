import saze

def get_main_callback(sim, gps_sensor):
    def gps_callback():
        gps_data = gps_sensor.data
        tr = sim.map_from_gps(latitude = gps_data.latitude,\
                            longitude = gps_data.longitude,\
                            northing = gps_data.northing,\
                            easting = gps_data.easting)
        print(tr)

    return gps_callback

def main():
    app_tag = "GPS Test"
    sim = saze.open_simulator("Shalun")
    saze.print_msg(app_tag, "Simulator opened")
    ego = saze.spawn_ego(sim)
    saze.print_msg(app_tag, "Ego vehicle spawned")
    gps_sensor = saze.get_gps_sensor(ego)
    saze.print_msg(app_tag,"GPS sensor ready")

    main_callback = get_main_callback(sim, gps_sensor)
    sim.run_with_callback(main_callback)

if __name__=="__main__":
    main()
