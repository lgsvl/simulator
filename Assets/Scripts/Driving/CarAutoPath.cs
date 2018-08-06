/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CarAutoPath : MonoBehaviour {

    public TrafficPathNode[] path;
    public List<RoadPathNode> pathNodes;

    // FOR COPYING OVER TO RA-LESS SYSTEM ONLY
   /*
  private int currentWaypointIndex;
  private RAPathNode currentNode;
  private Vector3 currentWaypoint;
  private RAPathNode nextNode;
  private int nextWaypointIndex;
  private Vector3 nextWaypoint;


  void Start () {
      currentWaypointIndex = 0;
      currentNode = path[currentWaypointIndex++];
      currentWaypoint = currentNode.transform.position;

      while (UpdateNextWaypoint() == false)
      {
          var curTan = currentNode.tangent == null ? Vector3.zero : currentNode.tangent.position;
          var newNode = new RoadPathNode()
          {
              isInintersection = currentNode.isIntersectionStart,
              tangent = curTan,
              position = currentNode.transform.position
          };

          pathNodes.Add(newNode);

          currentWaypoint = nextWaypoint;
          currentNode = nextNode;
          currentWaypointIndex = nextWaypointIndex;
      }
  }

  private bool UpdateNextWaypoint()
  {
      if (currentNode.isIntersection || currentNode.next == null)
      {
          nextWaypointIndex = currentWaypointIndex + 1;
          if (nextWaypointIndex >= path.Length)
              return true;

          nextNode = path[nextWaypointIndex];
          nextWaypoint = nextNode.transform.position;
      }
      else
      {
          nextNode = currentNode.next.GetComponent<RAPathNode>();
          nextWaypoint = nextNode.transform.position;
          nextWaypointIndex = currentWaypointIndex;
      }

      return false;
  }
  */
}

