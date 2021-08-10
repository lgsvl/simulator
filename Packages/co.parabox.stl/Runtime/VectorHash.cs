using System;
using UnityEngine;

namespace Parabox.Stl
{
    struct StlVector3 : IEquatable<StlVector3>
    {
        const float k_Resolution = 10000f;

        public float x;
        public float y;
        public float z;

        public StlVector3(Vector3 v)
        {
            x = v.x;
            y = v.y;
            z = v.z;
        }

        public StlVector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static explicit operator Vector3(StlVector3 vec)
        {
            return new Vector3(vec.x, vec.y, vec.z);
        }

        public static explicit operator StlVector3(Vector3 vec)
        {
            return new StlVector3(vec);
        }

        public bool Equals(StlVector3 other)
        {
            return Mathf.Approximately(x, other.x)
                && Mathf.Approximately(y, other.y)
                && Mathf.Approximately(z, other.z);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is StlVector3))
                return false;

            return Equals((StlVector3) obj);
        }

        public override int GetHashCode()
        {
            // https://stackoverflow.com/questions/720177/default-implementation-for-object-gethashcode/720282#720282
            unchecked
            {
                int hash = 27;

                hash = (13 * hash) + (x * k_Resolution).GetHashCode();
                hash = (13 * hash) + (y * k_Resolution).GetHashCode();
                hash = (13 * hash) + (z * k_Resolution).GetHashCode();

                return hash;
            }
        }

        public static bool operator == (StlVector3 a, StlVector3 b)
        {
            return a.Equals(b);
        }

        public static bool operator != (StlVector3 a, StlVector3 b)
        {
            return ! a.Equals(b);
        }
    }
}
