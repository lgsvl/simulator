/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

public class ParkingSpawnsManager : SpawnsManager
{
    protected override void Initialize()
    {
        base.Initialize();

        spawnAreaType = SpawnAreaType.ParkingSpaces;
        spawnRadius = 2;
    }
}
