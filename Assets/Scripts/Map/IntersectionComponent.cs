/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntersectionComponent : MonoBehaviour
{
    public List<IntersectionTrafficLightSetComponent> lightGroups = new List<IntersectionTrafficLightSetComponent>();
    public List<IntersectionTrafficLightSetComponent> facingGroup = new List<IntersectionTrafficLightSetComponent>();
    public List<IntersectionTrafficLightSetComponent> oppFacingGroup = new List<IntersectionTrafficLightSetComponent>();
    private List<IntersectionTrafficLightSetComponent> currentTrafficLightSet = new List<IntersectionTrafficLightSetComponent>();
    private bool isFacing = false;
    private float m_yellowTime = 0f;
    private float m_allRedTime = 0f;
    private float m_activeTime = 0f;
    private Material m_yellowMat;
    private Material m_redMat;
    private Material m_greenMat;

    public void SetLightGroupData(float yellowTime, float allRedTime, float activeTime, Material yellow, Material red, Material green)
    {
        m_yellowTime = yellowTime;
        m_allRedTime = allRedTime;
        m_activeTime = activeTime;
        m_yellowMat = yellow;
        m_redMat = red;
        m_greenMat = green;
        isFacing = false;
        lightGroups.AddRange(transform.GetComponentsInChildren<IntersectionTrafficLightSetComponent>());

        foreach (var item in lightGroups)
        {
            foreach (var group in lightGroups)
            {
                float dot = Vector3.Dot(group.transform.TransformDirection(Vector3.right), item.transform.TransformDirection(Vector3.right)); // TODO not vector right usually

                if (dot < -0.7f) // facing
                {
                    if (!facingGroup.Contains(item) && !oppFacingGroup.Contains(item))
                        facingGroup.Add(item);
                    if (!facingGroup.Contains(group) && !oppFacingGroup.Contains(group))
                        facingGroup.Add(group);
                }
                else if (dot > -0.5f && dot < 0.5f) // perpendicular
                {
                    if (!facingGroup.Contains(item) && !oppFacingGroup.Contains(item))
                        facingGroup.Add(item);
                    if (!oppFacingGroup.Contains(group) && !facingGroup.Contains(group))
                        oppFacingGroup.Add(group);
                }
                else if (lightGroups.Count == 1) // same direction
                {
                    if (!facingGroup.Contains(item))
                        facingGroup.Add(item);
                }
            }
        }
        if (lightGroups.Count != facingGroup.Count + oppFacingGroup.Count)
            Debug.LogError("Error finding facing light sets, please check light set parent rotation");
    }

    public void StartTrafficLightLoop()
    {
        StartCoroutine(TrafficLightLoop());
    }

    private IEnumerator TrafficLightLoop()
    {
        yield return new WaitForSeconds(Random.Range(0, 5f));
        while (true)
        {
            yield return null;

            currentTrafficLightSet = isFacing ? facingGroup : oppFacingGroup;

            foreach (var state in currentTrafficLightSet)
            {
                state.SetLightColor(TrafficLightSetState.Green, m_greenMat);
            }

            yield return new WaitForSeconds(m_activeTime);

            foreach (var state in currentTrafficLightSet)
            {
                state.SetLightColor(TrafficLightSetState.Yellow, m_yellowMat);
            }

            yield return new WaitForSeconds(m_yellowTime);

            foreach (var state in currentTrafficLightSet)
            {
                state.SetLightColor(TrafficLightSetState.Red, m_redMat);
            }

            yield return new WaitForSeconds(m_allRedTime);

            isFacing = !isFacing;
        }
    }
}
