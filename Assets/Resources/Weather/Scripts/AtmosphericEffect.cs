using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface AtmosphericEffect {
    void filterSkyParams(DayNightEventsController.lightParameters sky, Light celestialLight);
}
