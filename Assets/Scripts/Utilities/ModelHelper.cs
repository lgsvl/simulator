using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ModelHelper : MonoBehaviour
{
    public bool self_collide = false;
    public List<GameObject> links = new List<GameObject>();
    public List<ModelHelper> models = new List<ModelHelper>();

    void Awake()
    {
        if (self_collide == true)
        {
            // unity default is self collision - nothing to do
            return;
        }
        var childLinkBodies = links.SelectMany(t => t.GetComponentsInChildren<Collider>());

        var subModelBodies =
            models.SelectMany(m => m.links)
            .SelectMany(l => l.GetComponentsInChildren<Collider>());

        foreach (var body in childLinkBodies)
        {
            foreach (var sBody in subModelBodies)
            {
                Physics.IgnoreCollision(body, sBody);
            }
        }
    }
}