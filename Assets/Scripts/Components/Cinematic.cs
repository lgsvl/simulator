/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;

public class Cinematic : MonoBehaviour
{
    public enum CinematicType
    {
        Start,
        Stop
    };

    public CinematicType CurrentCinematicType = CinematicType.Start;
}
