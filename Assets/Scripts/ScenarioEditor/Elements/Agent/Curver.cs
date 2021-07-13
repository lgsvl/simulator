/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Elements.Agents
{
    using UnityEngine;
    using System.Collections;
    using System.Collections.Generic;
    public class Curver : MonoBehaviour
    {
        //arrayToCurve is original Vector3 array, smoothness is the number of interpolations.

        private Vector3[] smooth;
        public static Vector3[] MakeSmoothCurve(Vector3[] arrayToCurve, float smoothness)
        {
            List<Vector3> points;
            List<Vector3> curvedPoints;
            int pointsLength = 0;
            int curvedLength = 0;

            if (smoothness < 1.0f) smoothness = 1.0f;

            pointsLength = arrayToCurve.Length;

            curvedLength = (pointsLength * Mathf.RoundToInt(smoothness)) - 1;
            curvedPoints = new List<Vector3>(curvedLength);

            float t = 0.0f;
            for (int pointInTimeOnCurve = 0; pointInTimeOnCurve < curvedLength + 1; pointInTimeOnCurve++)
            {
                t = Mathf.InverseLerp(0, curvedLength, pointInTimeOnCurve);

                points = new List<Vector3>(arrayToCurve);

                for (int j = pointsLength - 1; j > 0; j--)
                {
                    for (int i = 0; i < j; i++)
                    {
                        points[i] = (1 - t) * points[i] + t * points[i + 1];
                    }
                }

                curvedPoints.Add(points[0]);
            }

            return (curvedPoints.ToArray());
        }

        private void Start()
        {

            Vector3[] positionArray = new[] { new Vector3(0f, 0f, 0f), new Vector3(10f, 0f, 0f), new Vector3(10f, 5f, 0f) };
            smooth = MakeSmoothCurve(positionArray, 5);
            for (int i = 0; i < smooth.Length - 1; i++)
            {
                Debug.Log(smooth[i]);

            }

        }
        private void Update()
        {
            for (int i = 0; i < smooth.Length - 1; i++)
            {
                Debug.DrawLine(smooth[i], smooth[i + 1], Color.red);

            }

        }
    }
}