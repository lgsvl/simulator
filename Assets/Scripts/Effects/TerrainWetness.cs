/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TerrainWetness : MonoBehaviour {

 //   private List<ReliefTerrain> terrainTiles;

    public void OnEnable()
    {
 //       terrainTiles = new List<ReliefTerrain>(GameObject.FindObjectsOfType<ReliefTerrain>());
        if (WetRoads.instance != null)
            WetRoads.instance.OnWetness += SetWetness;

  //      foreach (var t in terrainTiles)
  //      {
   //         t.globalSettingsHolder.Refresh();

   //     }
    }

    public void OnDisable()
    {
        if(WetRoads.instance != null)
            WetRoads.instance.OnWetness -= SetWetness;
    }

    void SetWetness(float wet)
    {
     //   if (terrainTiles.Count > 0)
     //   {
            /*if (terrainTiles[0].globalSettingsHolder.TERRAIN_GlobalWetness < 0.5f && wet > 0.5f)
            {
                terrainTiles[0].globalSettingsHolder.TERRAIN_GlobalWetness = 1f;
                terrainTiles[0].globalSettingsHolder.RefreshAll();
            }
            else if (terrainTiles[0].globalSettingsHolder.TERRAIN_GlobalWetness > 0.5f && wet < 0.5f)
            {
                terrainTiles[0].globalSettingsHolder.TERRAIN_GlobalWetness = 0f;
                terrainTiles[0].globalSettingsHolder.RefreshAll();
            }*/
    //        foreach (var t in terrainTiles)
      //      {
     //           t.globalSettingsHolder.TERRAIN_GlobalWetness = wet;
     //           t.GetComponent<Terrain>().materialTemplate.SetFloat("TERRAIN_GlobalWetness", wet);
      //          Shader.SetGlobalFloat("TERRAIN_GlobalWetness", wet);
  
      //      }

     //   }
    }
}
