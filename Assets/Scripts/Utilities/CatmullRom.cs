/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Simulator.Utilities
{
    public class CatmullRom
    {
        private float alpha = 0.5f; // small alpha gives sharper turns
        private Vector3[] points = new Vector3[4];
        public float t0, t1, t2, t3;
        public CatmullRom() { }

        public CatmullRom(float alpha)
        {
            this.alpha = alpha;
        }

        public void SetPoints(Vector3[] new_points)
        {
            // TODO: handle case where input size != 4 --> always forcing four points for now
            this.points = new_points;
            GetKnots(); // update the knots each time the points are updated.
        }

        public float CalcT(float t, Vector3 A, Vector3 B)
        {
            float r = Mathf.Sqrt(Mathf.Pow(A.x - B.x, 2.0f) + Mathf.Pow(A.y - B.y, 2.0f) + Mathf.Pow(A.z - B.z, 2.0f));
            return Mathf.Pow(r, this.alpha) + t;
        }

        public float CalcVehicleT(Vector3 pos)
        {
            float t = CalcT(this.t0, this.points[0], pos);

            switch (GetSegment(t))
            {
                case 0: return t;
                case 1: return CalcT(this.t1, this.points[1], pos);
                case 2: return CalcT(this.t2, this.points[2], pos);
                default: return 0.0f;
            }
        }

        public int GetSegment(float t)
        {
            if (t >= this.t0 && t < this.t1) { return 0; }
            else if (t >= this.t1 && t < this.t2) { return 1; }
            else if (t >= this.t2 && t < this.t3) { return 2; }
            else { return 42; } // out of range 
        }

        private void GetKnots()
        {
            this.t0 = 0f;
            this.t1 = CalcT(this.t0, this.points[0], this.points[1]);
            this.t2 = CalcT(this.t1, this.points[1], this.points[2]);
            this.t3 = CalcT(this.t2, this.points[2], this.points[3]);
        }

        public void SetKnots(float t0, float t1, float t2, float t3)
        {
            this.t0 = t0;
            this.t1 = t1;
            this.t2 = t2;
            this.t3 = t3;
        }

        public Vector3 GetPointOnSpline(float t)
        {
            Vector3 A1 = (this.t1 - t) / (this.t1 - this.t0) * this.points[0] + (t - this.t0) / (this.t1 - this.t0) * this.points[1];
            Vector3 A2 = (this.t2 - t) / (this.t2 - this.t1) * this.points[1] + (t - this.t1) / (this.t2 - this.t1) * this.points[2];
            Vector3 A3 = (this.t3 - t) / (this.t3 - this.t2) * this.points[2] + (t - this.t2) / (this.t3 - this.t2) * this.points[3];

            Vector3 B1 = (this.t2 - t) / (this.t2 - this.t0) * A1 + (t - this.t0) / (this.t2 - this.t0) * A2;
            Vector3 B2 = (this.t3 - t) / (this.t3 - this.t1) * A2 + (t - this.t1) / (this.t3 - this.t1) * A3;

            return ((this.t2 - t) / (this.t2 - this.t1) * B1) + ((t - this.t1) / (this.t2 - this.t1) * B2);
        }

        public List<Vector3> GetSplineWayPoints(int nPoints)
        {
            List<Vector3> r = new List<Vector3>();
            for (float t = t1; t < t2; t += (t2 - t1) / nPoints)
            {
                r.Add(GetPointOnSpline(t));
            }
            return r;
        }
    }
}
