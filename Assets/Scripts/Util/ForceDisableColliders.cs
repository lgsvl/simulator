using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForceDisableColliders : MonoBehaviour
{
    public bool YES = false;
    private void Awake()
    {
        if (YES)
        {
            var colliders = GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
            {
                col.enabled = false;
            }
        }
    }
}
