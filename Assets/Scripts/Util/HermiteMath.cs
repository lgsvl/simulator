/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System.Collections;

public class HermiteMath {
    
    public static Vector3 HermiteVal(Vector3 p1, Vector3 p2, Vector3 t1, Vector3 t2, float s) {

        t1 = t1.normalized * Vector3.Distance(p1, p2);
        t2 = t2.normalized * Vector3.Distance(p1, p2);
        float h1 =  2*(s * s * s) - 3*(s*s) + 1;          
          float h2 = -2*(s * s * s) + 3*(s*s);              
          float h3 =   (s * s * s) - 2*(s*s) + s;         
          float h4 =   (s * s * s) -  (s*s);              
          return h1*p1 + h2*p2 + h3*t1 + h4*t2;
    }

}
