using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PaletteComponent : MonoBehaviour
{
    public FixedJoint fJoint;
    private bool isHooked = false;
    

    public void AttachToTugBot(Rigidbody rb)
    {
        if (isHooked) return;

        isHooked = true;
        fJoint.connectedBody = rb;
    }

    public void ReleaseTugBot()
    {
        isHooked = false;
        fJoint.connectedBody = null;
    }
}
