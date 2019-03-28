using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PaletteComponent : MonoBehaviour
{
    public FixedJoint fJoint;

    public void AttachToTugBot(Vector3 jPos, Rigidbody rb)
    {
        fJoint.anchor = jPos;
        fJoint.connectedBody = rb;
    }

    public void ReleaseTugBot()
    {
        fJoint.connectedBody = null;
    }
}
