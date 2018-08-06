using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SunController : MonoBehaviour
{
	void Update ()
    {
        GetComponent<Light>().enabled = Vector3.Dot(transform.forward, Vector3.up) < 0;
	}
}
