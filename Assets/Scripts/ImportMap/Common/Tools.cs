using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;
using apollo.common;
using Vector3 = UnityEngine.Vector3;

namespace Util
{
    public class Boundary
    {
        public Boundary(List<PointENU> _left, List<PointENU> _right)
        {
            left = Tools.ListToVector(_left);
            right = Tools.ListToVector(_right);
        }

        public Vector2[] left;
        public Vector2[] right;
    }


    public class Polygon
    {
        public Polygon(List<PointENU> polygon)
        {
            points = Tools.ListToVector(polygon);
        }

        public Vector2[] points { get; set; }
    }



    public class Tools
    {
        public static string ByteToStr(byte[] array)
        {
            return array == null ? "" : Encoding.ASCII.GetString(array);
        }

        public static PointENU Transform(PointENU point)
        {
            return new PointENU
            {
                x = point.x - HDMapLoader.origin.x,
                y = point.y - HDMapLoader.origin.y,
                z = point.z
            };
        }

        public static List<PointENU> Transform(List<PointENU> origin)
        {
            List<PointENU> points = new List<PointENU>();
            foreach (var point in origin)
            {
                PointENU _point = new PointENU
                {
                    x = point.x - HDMapLoader.origin.x,
                    y = point.y - HDMapLoader.origin.y,
                    z = point.z
                };
                points.Add(_point);
            }

            return points;
        }

        public static Vector2[] ListToVector(List<PointENU> list)
        {
            Vector2[] arr = new Vector2[list.Count];
            int index = 0;
            foreach (var point in list)
            {
                arr[index] = new Vector2((float)point.x, (float)point.y);
                index++;
            }

            return arr;
        }

        public static Vector3 CoorExchange(Vector3 point)
        {
            var tmp = point.y;
            point.y = point.z;
            point.z = tmp;
            return point;
        }

        public static void ModifyZ(Polygon polygon, float offset)
        {

        } 

    }


    public class Triangulator
    {
        private List<Vector2> m_points = new List<Vector2>();

        public Triangulator(Vector2[] points)
        {
            m_points = new List<Vector2>(points);
        }

        public int[] Triangulate()
        {
            List<int> indices = new List<int>();

            int n = m_points.Count;
            if (n < 3)
                return indices.ToArray();

            int[] V = new int[n];
            if (Area() > 0)
            {
                for (int v = 0; v < n; v++)
                    V[v] = v;
            }
            else
            {
                for (int v = 0; v < n; v++)
                    V[v] = (n - 1) - v;
            }

            int nv = n;
            int count = 2 * nv;
            for (int v = nv - 1; nv > 2;)
            {
                if ((count--) <= 0)
                    return indices.ToArray();

                int u = v;
                if (nv <= u)
                    u = 0;
                v = u + 1;
                if (nv <= v)
                    v = 0;
                int w = v + 1;
                if (nv <= w)
                    w = 0;

                if (Snip(u, v, w, nv, V))
                {
                    int a, b, c, s, t;
                    a = V[u];
                    b = V[v];
                    c = V[w];
                    indices.Add(a);
                    indices.Add(b);
                    indices.Add(c);
                    for (s = v, t = v + 1; t < nv; s++, t++)
                        V[s] = V[t];
                    nv--;
                    count = 2 * nv;
                }
            }

            indices.Reverse();
            return indices.ToArray();
        }

        private float Area()
        {
            int n = m_points.Count;
            float A = 0.0f;
            for (int p = n - 1, q = 0; q < n; p = q++)
            {
                Vector2 pval = m_points[p];
                Vector2 qval = m_points[q];
                A += pval.x * qval.y - qval.x * pval.y;
            }
            return (A * 0.5f);
        }

        private bool Snip(int u, int v, int w, int n, int[] V)
        {
            int p;
            Vector2 A = m_points[V[u]];
            Vector2 B = m_points[V[v]];
            Vector2 C = m_points[V[w]];
            if (Mathf.Epsilon > (((B.x - A.x) * (C.y - A.y)) - ((B.y - A.y) * (C.x - A.x))))
                return false;
            for (p = 0; p < n; p++)
            {
                if ((p == u) || (p == v) || (p == w))
                    continue;
                Vector2 P = m_points[V[p]];
                if (InsideTriangle(A, B, C, P))
                    return false;
            }
            return true;
        }

        private bool InsideTriangle(Vector2 A, Vector2 B, Vector2 C, Vector2 P)
        {
            float ax, ay, bx, by, cx, cy, apx, apy, bpx, bpy, cpx, cpy;
            float cCROSSap, bCROSScp, aCROSSbp;

            ax = C.x - B.x; ay = C.y - B.y;
            bx = A.x - C.x; by = A.y - C.y;
            cx = B.x - A.x; cy = B.y - A.y;
            apx = P.x - A.x; apy = P.y - A.y;
            bpx = P.x - B.x; bpy = P.y - B.y;
            cpx = P.x - C.x; cpy = P.y - C.y;

            aCROSSbp = ax * bpy - ay * bpx;
            cCROSSap = cx * apy - cy * apx;
            bCROSScp = bx * cpy - by * cpx;

            return ((aCROSSbp >= 0.0f) && (bCROSScp >= 0.0f) && (cCROSSap >= 0.0f));
        }
    }
}

