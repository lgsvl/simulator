/*
* MIT License
* 
* Copyright (c) 2017 Philip Tibom, Jonathan Jansson, Rickard Laurenius, 
* Tobias Alldén, Martin Chemander, Sherry Davar
* 
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
* 
* The above copyright notice and this permission notice shall be included in all
* copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
* SOFTWARE.
*/

using UnityEngine;

/// <summary>
/// Author: Philip Tibom
/// Ray casts and simulates individual lasers.
/// </summary>
public class Laser
{
    private int laserId;
    private Ray ray;
    private RaycastHit hit;
    private float rayDistance;
    private float verticalAngle;
    private GameObject parentObject;
    private RenderLine lineDrawer;
    private float offset;

    public Laser(GameObject parent, float verticalAngle, float distance, float offset, GameObject lineDrawer, int laserId)
    {
        this.laserId = laserId;
        parentObject = parent;
        this.offset = offset;
        this.verticalAngle = verticalAngle;
        rayDistance = distance;
        this.lineDrawer = lineDrawer.GetComponent<RenderLine>();
        lineDrawer.transform.position = parentObject.transform.position + (parentObject.transform.up * offset);
        ray = new Ray();
        UpdateRay();
    }

    // Should be called from Update(), for best performance.
    // This is only visual, for debugging.
    public void DrawRay(bool isHit)
    {
        if (isHit)
        {
            lineDrawer.DrawLine(hit.point);
        }
        else
        {
            lineDrawer.DrawLine(ray.GetPoint(rayDistance));
        }
    }

    public void DebugDrawRay(bool isHit)
    {
        float distance = rayDistance;
        if (isHit)
        {
            distance = hit.distance;
        }
        Debug.DrawRay(ray.origin, ray.direction * distance, Color.red);
    }

    // Should be called from FixedUpdate(), for best performance.
    public RaycastHit ShootRay(int bitmask)
    {
        // Perform raycast
        UpdateRay();

        if (Physics.Raycast(ray, out hit, rayDistance, bitmask))
        {
            //DrawRay(true);
            return hit;
        }
        else
        {
            //DrawRay(false);
            return new RaycastHit();
        }
    }

    // Update existing ray. Don't create 'new' ray object, that is heavy.
    private void UpdateRay()
    {
        ray.origin = parentObject.transform.position + (parentObject.transform.up * offset);
        ray.direction = parentObject.transform.TransformDirection(Quaternion.AngleAxis(verticalAngle, Vector3.right) * Vector3.forward);
    }

    public Ray GetRay()
    {
        return ray;
    }

    public float GetVerticalAngle()
    {
        return verticalAngle;
    }

    public int GetLaserId()
    {
        return laserId;
    }

    public RenderLine GetRenderLine()
    {
        return lineDrawer;
    }
}
