using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PaletteComponent : MonoBehaviour
{
    public FixedJoint fJoint;
    public HingeJoint hJoint;

    public void AttachToTugBot(Vector3 jPos, Rigidbody rb)
    {
        hJoint.anchor = jPos;
        hJoint.connectedBody = rb;
    }

    public void ReleaseTugBot()
    {
        if (hJoint.connectedBody != null)
            hJoint.connectedBody = null;
    }
}
