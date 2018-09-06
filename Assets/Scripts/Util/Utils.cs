/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System;
using System.Collections;

public static class Utils
{
    public class MinHeap
    {
        private readonly int[] _elements;
        public int MaxSize
        {
            get; private set;
        }
        private int _size;
        public int Size
        {
            get
            {
                return _size;
            }
        }

        public MinHeap(int size)
        {
            _elements = new int[size];
            MaxSize = size;
        }

        private int GetLeftChildIndex(int elementIndex) => 2 * elementIndex + 1;
        private int GetRightChildIndex(int elementIndex) => 2 * elementIndex + 2;
        private int GetParentIndex(int elementIndex) => (elementIndex - 1) / 2;

        private bool HasLeftChild(int elementIndex) => GetLeftChildIndex(elementIndex) < _size;
        private bool HasRightChild(int elementIndex) => GetRightChildIndex(elementIndex) < _size;
        private bool IsRoot(int elementIndex) => elementIndex == 0;

        private int GetLeftChild(int elementIndex) => _elements[GetLeftChildIndex(elementIndex)];
        private int GetRightChild(int elementIndex) => _elements[GetRightChildIndex(elementIndex)];
        private int GetParent(int elementIndex) => _elements[GetParentIndex(elementIndex)];

        private void Swap(int firstIndex, int secondIndex)
        {
            var temp = _elements[firstIndex];
            _elements[firstIndex] = _elements[secondIndex];
            _elements[secondIndex] = temp;
        }

        public bool IsEmpty()
        {
            return _size == 0;
        }

        public int Peek()
        {
            if (_size == 0)
                throw new IndexOutOfRangeException();

            return _elements[0];
        }

        public int Pop()
        {
            if (_size == 0)
                throw new IndexOutOfRangeException();

            var result = _elements[0];
            _elements[0] = _elements[_size - 1];
            _size--;

            ReCalculateDown();

            return result;
        }

        public void Add(int element)
        {
            if (_size == _elements.Length)
                throw new IndexOutOfRangeException();

            _elements[_size] = element;
            _size++;

            ReCalculateUp();
        }

        private void ReCalculateDown()
        {
            int index = 0;
            while (HasLeftChild(index))
            {
                var smallerIndex = GetLeftChildIndex(index);
                if (HasRightChild(index) && GetRightChild(index) < GetLeftChild(index))
                {
                    smallerIndex = GetRightChildIndex(index);
                }

                if (_elements[smallerIndex] >= _elements[index])
                {
                    break;
                }

                Swap(smallerIndex, index);
                index = smallerIndex;
            }
        }

        private void ReCalculateUp()
        {
            var index = _size - 1;
            while (!IsRoot(index) && _elements[index] < GetParent(index))
            {
                var parentIndex = GetParentIndex(index);
                Swap(parentIndex, index);
                index = parentIndex;
            }
        }

        public override string ToString()
        {
            return string.Join(", ", _elements);
        }
    }

    public static Transform FindDeepChild(this Transform parent, string name)
    {
        var result = parent.Find(name);
        if (result != null)
        {
            return result;
        }

        foreach (Transform child in parent)
        {
            result = child.FindDeepChild(name);
            if (result != null)
            {
                return result;
            }
        }
        return null;
    }

    public static Type GetCollectionElement(this Type type)
    {
        if (type.IsArray)
        {
            return type.GetElementType();
        }
        if (type.IsGenericList())
        {
            return type.GetGenericArguments()[0];
        }
        return null;
    }

    public static bool IsCollectionType(this Type type) => (type.IsGenericList() || type.IsArray);

    public static bool IsGenericList(this Type type) => type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(List<>));

    public static bool IsNullable(this Type type) => Nullable.GetUnderlyingType(type) != null;

    public static object TypeDefaultValue(this Type type)
    {
        if (type.IsValueType)
            return Activator.CreateInstance(type);

        return null;
    }

    public static void RemoveAdjacentDuplicates(this IList list)
    {
        if (list.Count < 1)
        {
            return;
        }
        IList results = new List<object>();
        results.Add(list[0]);
        foreach (var e in list)
        {
            if (results[results.Count - 1] != e)
            {
                results.Add(e);
            }
        }
        list.Clear();
        foreach (var r in results)
        {
            list.Add(r);
        }
    }

    // Calculate the distance between
    // point pt and the segment p1 --> p2.
    public static float FindDistanceToSegment(Vector2 pt, Vector2 p1, Vector2 p2, out Vector2 closest)
    {
        float dx = p2.x - p1.x;
        float dy = p2.y - p1.y;
        if ((dx == 0) && (dy == 0))
        {
            // It's a point not a line segment.
            closest = p1;
            dx = pt.x - p1.x;
            dy = pt.y - p1.y;
            return (float)Mathf.Sqrt(dx * dx + dy * dy);
        }

        // Calculate the t that minimizes the distance.
        float t = ((pt.x - p1.x) * dx + (pt.y - p1.y) * dy) / (dx * dx + dy * dy);

        // See if this represents one of the segment's
        // end points or a point in the middle.
        if (t < 0)
        {
            closest = new Vector2(p1.x, p1.y);
            dx = pt.x - p1.x;
            dy = pt.y - p1.y;
        }
        else if (t > 1)
        {
            closest = new Vector2(p2.x, p2.y);
            dx = pt.x - p2.x;
            dy = pt.y - p2.y;
        }
        else
        {
            closest = new Vector2(p1.x + t * dx, p1.y + t * dy);
            dx = pt.x - closest.x;
            dy = pt.y - closest.y;
        }

        return (float)Mathf.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// Test whether two line segments intersect. If so, calculate the intersection point.
    /// </summary>
    /// <param name="a1">Vector to the start point of a.</param>
    /// <param name="a2">Vector to the end point of a.</param>
    /// <param name="b1">Vector to the start point of b.</param>
    /// <param name="b2">Vector to the end point of b.</param>
    /// <param name="intersection">The point of intersection, if any.</param>
    /// <param name="considerOverlapAsIntersect">Do we consider overlapping lines as intersecting?
    /// </param>
    /// <returns>True if an intersection point was found.</returns>
    public static bool LineSegementsIntersect(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2, out Vector2 intersection, bool considerCollinearOverlapAsIntersect = false)
    {
        intersection = new Vector2();

        var r = a2 - a1;
        var s = b2 - b1;
        var rxs = Cross(r, s);
        var qpxr = Cross(b1 - a1, r);

        // If r x s = 0 and (b1 - a1) x r = 0, then the two lines are collinear.
        if (Math.Abs(rxs) < 0.0001f && Math.Abs(qpxr) < 0.0001f)
        {
            // 1. If either  0 <= (b1 - a1) * r <= r * r or 0 <= (a1 - b1) * s <= * s
            // then the two lines are overlapping,
            if (considerCollinearOverlapAsIntersect)
            {
                if ((0 <= Vector2.Dot(b1 - a1, r) && Vector2.Dot(b1 - a1, r) <= Vector2.Dot(r, r)) || (0 <= Vector2.Dot(a1 - b1, s) && Vector2.Dot(a1 - b1, s) <= Vector2.Dot(s, s)))
                {
                    return true;
                }
            }

            // 2. If neither 0 <= (b1 - a1) * r = r * r nor 0 <= (a1 - b1) * s <= s * s
            // then the two lines are collinear but disjoint.
            // No need to implement this expression, as it follows from the expression above.
            return false;
        }

        // 3. If r x s = 0 and (b1 - a1) x r != 0, then the two lines are parallel and non-intersecting.
        if (Math.Abs(rxs) < 0.0001f && !(Math.Abs(qpxr) < 0.0001f))
            return false;

        // t = (b1 - a1) x s / (r x s)
        var t = Cross(b1 - a1, s) / rxs;

        // u = (b1 - a1) x r / (r x s)

        var u = Cross(b1 - a1, r) / rxs;

        // 4. If r x s != 0 and 0 <= t <= 1 and 0 <= u <= 1
        // the two line segments meet at the point a1 + t r = b1 + u s.
        if (!(Math.Abs(rxs) < 0.0001f) && (0 <= t && t <= 1) && (0 <= u && u <= 1))
        {
            // We can calculate the intersection point using either t or u.
            intersection = a1 + t * r;

            // An intersection was found.
            return true;
        }

        // 5. Otherwise, the two line segments are not parallel but do not intersect.
        return false;
    }

    public static float Cross(Vector2 v1, Vector2 v2)
    {
        return v1.x * v2.y - v1.y * v2.x;
    }

    public static bool CurveSegmentsIntersect(List<Vector2> a, List<Vector2> b, out List<Vector2> intersections)
    {
        intersections = new List<Vector2>();
        for (int i = 0; i < a.Count - 1; i++)
        {
            for (int j = 0; j < b.Count - 1; j++)
            {
                Vector2 intersect;
                if (LineSegementsIntersect(a[i], a[i + 1], b[j], b[j + 1], out intersect))
                {
                    intersections.Add(intersect);
                }
            }
        }
        return intersections.Count > 0;
    }

    //compute the s-coordinates of p's nearest point on line segments, p does not have to be on the segment
    public static float GetNearestSCoordinate(Vector2 p, List<Vector2> lineSegments, out float totalLength)
    {
        totalLength = 0;
        if (lineSegments.Count < 2)
        {
            return 0;
        }

        float minDistToSeg = float.MaxValue;
        float sCoord = 0;
        for (int i = 0; i < lineSegments.Count - 1; i++)
        {
            Vector2 closestPt;
            float distToSeg = FindDistanceToSegment(p, lineSegments[i], lineSegments[i + 1], out closestPt);
            float segLeng = (lineSegments[i + 1] - lineSegments[i]).magnitude;
            totalLength += segLeng;
            if (distToSeg < minDistToSeg)
            {
                minDistToSeg = distToSeg;
                sCoord = totalLength - segLeng + (closestPt - lineSegments[i]).magnitude;
            }
        }

        return sCoord;
    }
    
    public static float GetCurveLength(List<Vector2> lineSegments)
    {
        if (lineSegments.Count < 2)
        {
            return 0;
        }
        float totalLength = 0;
        for (int i = 0; i < lineSegments.Count - 1; i++)
        {
            totalLength += (lineSegments[i] - lineSegments[i + 1]).magnitude;
        }
        return totalLength;
    }
}

public static class StringBuilderExtension
{
    public static string Substring(this StringBuilder sb, int startIndex, int length)
    {
        return sb.ToString(startIndex, length);
    }

    public static StringBuilder Remove(this StringBuilder sb, char ch)
    {
        for (int i = 0; i < sb.Length;)
        {
            if (sb[i] == ch)
                sb.Remove(i, 1);
            else
                i++;
        }
        return sb;
    }

    public static StringBuilder RemoveFromEnd(this StringBuilder sb, int num)
    {
        return sb.Remove(sb.Length - num, num);
    }

    public static void Clear(this StringBuilder sb)
    {
        sb.Length = 0;
    }

    /// <summary>
    /// Trim left spaces of string
    /// </summary>
    /// <param name="sb"></param>
    /// <returns></returns>
    public static StringBuilder TrimLeft(this StringBuilder sb)
    {
        if (sb.Length != 0)
        {
            int length = 0;
            int num2 = sb.Length;
            while ((sb[length] == ' ') && (length < num2))
            {
                length++;
            }
            if (length > 0)
            {
                sb.Remove(0, length);
            }
        }
        return sb;
    }

    /// <summary>
    /// Trim right spaces of string
    /// </summary>
    /// <param name="sb"></param>
    /// <returns></returns>
    public static StringBuilder TrimRight(this StringBuilder sb)
    {
        if (sb.Length != 0)
        {
            int length = sb.Length;
            int num2 = length - 1;
            while ((sb[num2] == ' ') && (num2 > -1))
            {
                num2--;
            }
            if (num2 < (length - 1))
            {
                sb.Remove(num2 + 1, (length - num2) - 1);
            }
        }
        return sb;
    }

    /// <summary>
    /// Trim spaces around string
    /// </summary>
    /// <param name="sb"></param>
    /// <returns></returns>
    public static StringBuilder Trim(this StringBuilder sb)
    {
        if (sb.Length != 0)
        {
            int length = 0;
            int num2 = sb.Length;
            while ((sb[length] == ' ') && (length < num2))
            {
                length++;
            }
            if (length > 0)
            {
                sb.Remove(0, length);
                num2 = sb.Length;
            }
            length = num2 - 1;
            while ((sb[length] == ' ') && (length > -1))
            {
                length--;
            }
            if (length < (num2 - 1))
            {
                sb.Remove(length + 1, (num2 - length) - 1);
            }
        }
        return sb;
    }

    /// <summary>
    /// Get index of a char
    /// </summary>
    /// <param name="sb"></param>
    /// <param name="c"></param>
    /// <returns></returns>
    public static int IndexOf(this StringBuilder sb, char value)
    {
        return IndexOf(sb, value, 0);
    }

    /// <summary>
    /// Get index of a char starting from a given index
    /// </summary>
    /// <param name="sb"></param>
    /// <param name="c"></param>
    /// <param name="startIndex"></param>
    /// <returns></returns>
    public static int IndexOf(this StringBuilder sb, char value, int startIndex)
    {
        for (int i = startIndex; i < sb.Length; i++)
        {
            if (sb[i] == value)
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Get index of a string
    /// </summary>
    /// <param name="sb"></param>
    /// <param name="text"></param>
    /// <returns></returns>
    public static int IndexOf(this StringBuilder sb, string value)
    {
        return IndexOf(sb, value, 0, false);
    }

    /// <summary>
    /// Get index of a string from a given index
    /// </summary>
    /// <param name="sb"></param>
    /// <param name="text"></param>
    /// <param name="startIndex"></param>
    /// <returns></returns>
    public static int IndexOf(this StringBuilder sb, string value, int startIndex)
    {
        return IndexOf(sb, value, startIndex, false);
    }

    /// <summary>
    /// Get index of a string with case option
    /// </summary>
    /// <param name="sb"></param>
    /// <param name="text"></param>
    /// <param name="ignoreCase"></param>
    /// <returns></returns>
    public static int IndexOf(this StringBuilder sb, string value, bool ignoreCase)
    {
        return IndexOf(sb, value, 0, ignoreCase);
    }

    /// <summary>
    /// Get index of a string from a given index with case option
    /// </summary>
    /// <param name="sb"></param>
    /// <param name="text"></param>
    /// <param name="startIndex"></param>
    /// <param name="ignoreCase"></param>
    /// <returns></returns>
    public static int IndexOf(this StringBuilder sb, string value, int startIndex, bool ignoreCase)
    {
        int num3;
        int length = value.Length;
        int num2 = (sb.Length - length) + 1;
        if (ignoreCase == false)
        {
            for (int i = startIndex; i < num2; i++)
            {
                if (sb[i] == value[0])
                {
                    num3 = 1;
                    while ((num3 < length) && (sb[i + num3] == value[num3]))
                    {
                        num3++;
                    }
                    if (num3 == length)
                    {
                        return i;
                    }
                }
            }
        }
        else
        {
            for (int j = startIndex; j < num2; j++)
            {
                if (char.ToLower(sb[j]) == char.ToLower(value[0]))
                {
                    num3 = 1;
                    while ((num3 < length) && (char.ToLower(sb[j + num3]) == char.ToLower(value[num3])))
                    {
                        num3++;
                    }
                    if (num3 == length)
                    {
                        return j;
                    }
                }
            }
        }
        return -1;
    }

    /// <summary>
    /// Determine whether a string starts with a given text
    /// </summary>
    /// <param name="sb"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool StartsWith(this StringBuilder sb, string value)
    {
        return StartsWith(sb, value, 0, false);
    }

    /// <summary>
    /// Determine whether a string starts with a given text (with case option)
    /// </summary>
    /// <param name="sb"></param>
    /// <param name="value"></param>
    /// <param name="ignoreCase"></param>
    /// <returns></returns>
    public static bool StartsWith(this StringBuilder sb, string value, bool ignoreCase)
    {
        return StartsWith(sb, value, 0, ignoreCase);
    }

    /// <summary>
    /// Determine whether a string is begin with a given text
    /// </summary>
    /// <param name="sb"></param>
    /// <param name="value"></param>
    /// <param name="startIndex"></param>
    /// <param name="ignoreCase"></param>
    /// <returns></returns>
    public static bool StartsWith(this StringBuilder sb, string value, int startIndex, bool ignoreCase)
    {
        int length = value.Length;
        int num2 = startIndex + length;
        if (ignoreCase == false)
        {
            for (int i = startIndex; i < num2; i++)
            {
                if (sb[i] != value[i - startIndex])
                {
                    return false;
                }
            }
        }
        else
        {
            for (int j = startIndex; j < num2; j++)
            {
                if (char.ToLower(sb[j]) != char.ToLower(value[j - startIndex]))
                {
                    return false;
                }
            }
        }
        return true;
    }

    /// <summary>
    /// Determine whether a string is begin with a given text
    /// </summary>
    /// <param name="sb"></param>
    /// <param name="value"></param>
    /// <param name="startIndex"></param>
    /// <param name="ignoreCase"></param>
    /// <returns></returns>
    public static bool EndsWith(this StringBuilder sb, char value, bool ignoreSpace)
    {
        if (ignoreSpace)
        {
            for (int i = sb.Length - 1; i >= 0; i--)
            {
                if (sb[i] == ' ')
                {
                    continue;
                }
                if (sb[i] == value)
                {
                    return true;
                }
                else
                {
                    return false;
                }              
            }
        }
        else
        {
            if (sb[sb.Length - 1] == value)
            {
                return true;
            }
        }        
        return false;        
    }
}