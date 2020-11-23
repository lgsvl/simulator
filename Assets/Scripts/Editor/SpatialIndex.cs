using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Simulator.Editor
{
    // TODO: this could be optimized further with a QuadTree
    public class SpatialIndex<T> where T : struct
    {
        public delegate float SquaredDistanceFunc(float x, float y, float z, T candidate);
        public delegate Rect BoxFunc(T t);
        BoxFunc boxer;

        float gridSize = 10.0f;
        public SpatialIndex(float gridSize, BoxFunc box)
        {
            boxer = box;
            this.gridSize = gridSize;
        }

        struct GridIndex
        {
            public GridIndex(int _x, int _y)
            {
                this.x = _x;
                this.y = _y;
            }
            public int x;
            public int y;
        }

        class RectRingWalker
        {
            int radius;
            int centerX;
            int centerY;

            public RectRingWalker(float centerX, float centerY, int ring, float gridSize)
            {
                this.centerX = (int)Mathf.Floor(centerX / gridSize);
                this.centerY = (int)Mathf.Floor(centerY / gridSize);
                radius = ring;
            }

            public IEnumerator<GridIndex> GetEnumerator()
            {
                if (radius == 0)
                {
                    yield return new GridIndex(centerX, centerY);
                }
                else
                {
                    // TTTTT
                    // M   m
                    // M   m
                    // M   m
                    // BBBBB
                    for (int x = centerX - radius; x <= centerX + radius; x++)
                    {
                        yield return new GridIndex(x, centerY - radius); // T
                    }
                    for (int y = centerY - radius + 1; y <= centerY + radius - 1; y++)
                    {
                        yield return new GridIndex(centerX - radius, y); // M
                        yield return new GridIndex(centerX + radius, y); // m
                    }
                    for (int x = centerX - radius; x <= centerX + radius; x++)
                    {
                        yield return new GridIndex(x, centerY + radius); // B
                    }
                }
            }
        }
        class ScanlineWalker
        {
            int minX;
            int minY;
            int maxX;
            int maxY;

            public ScanlineWalker(Rect bounds, float gridSize)
            {
                minX = (int)Mathf.Floor(bounds.xMin / gridSize);
                minY = (int)Mathf.Floor(bounds.yMin / gridSize);
                maxX = (int)Mathf.Floor(bounds.xMax / gridSize) + 1;
                maxY = (int)Mathf.Floor(bounds.yMax / gridSize) + 1;
            }

            public IEnumerator<GridIndex> GetEnumerator()
            {
                GridIndex current;
                for (current.x = minX; current.x < maxX; current.x++)
                {
                    for (current.y = minY; current.y < maxY; current.y++)
                    {
                        yield return current;
                    }
                }
            }
        }
        public void Add(T t)
        {
            var bb = boxer(t);
            var walker = new ScanlineWalker(bb, gridSize);
            foreach (var index in walker)
            {
                HashSet<T> cellContents;
                if (grid.TryGetValue(index, out cellContents))
                {
                    cellContents.Add(t);
                }
                else
                {
                    cellContents = new HashSet<T>();
                    cellContents.Add(t);
                    grid.Add(index, cellContents);
                }
            }
        }

        public List<T> query(Rect bounds)
        {
            HashSet<T> result = new HashSet<T>();
            var walker = new ScanlineWalker(bounds, gridSize);
            foreach (var index in walker)
            {
                HashSet<T> cellContents;
                if (grid.TryGetValue(index, out cellContents))
                {
                    foreach (var t in cellContents)
                    {
                        result.UnionWith(cellContents);
                    }
                }
            }
            return result.ToList();
        }

        public bool nearest(float x, float y, float z, float maxRadius, SquaredDistanceFunc distFunc, out T result)
        {
            float minDistSquared = float.MaxValue;
            HashSet<T> seen = new HashSet<T>();
            result = default(T);
            // how man (rectangular) rings of tiles do we need to visit at most
            int maxRing = (int)Mathf.Ceil(maxRadius / gridSize) + 1;
            for (int r = 0; r < maxRing; r++)
            {
                // lower bound how close a point in this ring can be to the query point
                // since it could be right on the edge between ring 0 and 1, that gives us 0
                // so we always at least have to check ring 0 and 1 (9 tiles)
                float ringMinDist = (r - 1) * gridSize;
                var walker = new RectRingWalker(x, y, r, gridSize);
                // the next ring cannot contain a point that is closer to the one we already have, we can stop expanding
                if (ringMinDist * ringMinDist > minDistSquared) break;
                foreach (var index in walker)
                {
                    HashSet<T> cellContents;
                    if (grid.TryGetValue(index, out cellContents))
                    {
                        cellContents.ExceptWith(seen);
                        foreach (var candidate in cellContents)
                        {
                            float dist = distFunc(x, y, z, candidate);
                            if (dist < minDistSquared)
                            {
                                result = candidate;
                                minDistSquared = dist;
                            }
                        }
                        seen.UnionWith(cellContents);
                    }
                }
            }
            // we found the closest in all tiles visited but ti could still be outside
            // of the circular radius or we could not have found anything (minDistSquared==float.MaxValue)
            if (Mathf.Sqrt(minDistSquared) < maxRadius)
            {
                return true;
            }
            return false;
        }
        Dictionary<GridIndex, HashSet<T>> grid = new Dictionary<GridIndex, HashSet<T>>();
    }
}
