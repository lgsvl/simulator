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
        if (self_collide == false)
        {
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

        for (int i = 0; i < links.Count; i++)
        {
            var link1 = links[i].GetComponent<LinkHelper>();
            for (int j = i + 1; j < links.Count; j++)
            {
                var link2 = links[j].GetComponent<LinkHelper>();
                bool shouldCollide = self_collide || link1.self_collide || link2.self_collide;
                /* model/self_collide :
                If set to true, all links in the model will collide with each other (except those connected by a joint). Can be overridden by the link or collision element self_collide property. Two links within a model will collide if link1.self_collide OR link2.self_collide. Links connected by a joint will never collide.
                */
                if (!shouldCollide)
                {
                    var link1Bodies = link1.GetComponentsInChildren<Collider>();
                    var link2Bodies = link2.GetComponentsInChildren<Collider>();
                    foreach (var body1 in link1Bodies)
                    {
                        foreach (var body2 in link2Bodies)
                        {
                            Physics.IgnoreCollision(body1, body2);
                        }
                    }
                }
            }
        }
    }
}
