/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System.Collections;

public static class Functional {

    public static IEnumerator WatchFor(System.Func<bool> watch, System.Action onTrue)
    {
        while(!watch())
        {
            yield return null;
        }

        onTrue();
    }


    public static IEnumerator DoAfter(float delay, System.Action action)
    {
        yield return new WaitForSeconds(delay);
        action();
    }

    public static IEnumerator DoTimed(float time, System.Action<float> action)
    {
        float startTime = Time.time;
        while(Time.time - startTime < time)
        {
            action((Time.time - startTime) / time);
            yield return null;
        }
        action(1f);
    }
}
