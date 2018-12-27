/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum PedestrainState
{
    None,
    Idle,
    Walking,
    Crossing
};

public class PedestrianComponent : MonoBehaviour
{
    private Transform target01;
    private Transform target02;
    public float idleTime = 0f;
    public float targetRange = 2f;

    private Vector3 currentTargetPos;
    private Transform currentTargetT;
    private NavMeshAgent agent;
    private Animator anim;
    private PedestrainState thisPedState = PedestrainState.None;

    public void InitPed()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();

        agent.avoidancePriority = (int)Random.Range(1, 100); // set to 0 for no avoidance

        target01 = transform.parent.GetComponent<PedestrianSpawnerComponent>().target01;
        target02 = transform.parent.GetComponent<PedestrianSpawnerComponent>().target02;
        currentTargetPos = GetRandomTargetPosition();

        agent.SetDestination(currentTargetPos);
        thisPedState = PedestrainState.Walking;
    }

    private void Update()
    {
        if (IsRandomIdle())
            StartCoroutine(ChangePedState());

        if (IsPedAtDestination())
            SetPedNextAction();

        SetAnimationControllerParameters();
    }

    public void SetPedDestination(Transform target)
    {
        if (agent == null || target == null) return;

        currentTargetT = target;
        currentTargetPos = target.position;
        agent.SetDestination(currentTargetPos);
        thisPedState = PedestrainState.Walking;
    }

    public void SetPedAnimation(string triggerName)
    {
        if (anim == null) return;

        AnimatorControllerParameter[] tempACP = anim.parameters;
        for (int i = 0; i < tempACP.Length; i++)
        {
            if (triggerName == tempACP[i].name)
                anim.SetTrigger(triggerName);
        }
    }

    private void SetPedNextAction()
    {
        if (!agent.enabled) return;
        
        currentTargetPos = GetRandomTargetPosition();
        agent.SetDestination(currentTargetPos);
        thisPedState = PedestrainState.Walking; 
    }

    private IEnumerator LookAtTarget() // demo pedestrian control
    {
        if (agent == null) yield break;

        Quaternion tempQ = agent.transform.rotation;
        float elapsedTime = 0f;
        float speed = 0.5f;
        while (elapsedTime < 0.5f)
        {
            agent.transform.rotation = Quaternion.Lerp(tempQ, currentTargetT.rotation, (elapsedTime/speed));
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator ChangePedState()
    {
        if (agent == null) yield break;

        thisPedState = PedestrainState.Idle;
        agent.isStopped = true;

        yield return new WaitForSeconds(Random.Range(idleTime * 0.5f, idleTime));

        agent.isStopped = false;
        thisPedState = PedestrainState.Walking;
    }

    private bool IsPedAtDestination()
    {
        if (!agent.pathPending && !agent.hasPath && thisPedState == PedestrainState.Walking)
            return true;
        return false;
    }

    private bool IsRandomIdle()
    {
        if ((int)Random.Range(0, 1000) < 1 && thisPedState == PedestrainState.Walking)
            return true;
        return false;
    }

    private void SetAnimationControllerParameters()
    {
        if (agent == null || anim == null) return;

        Vector3 s = agent.velocity.normalized; //agent.transform.InverseTransformDirection(agent.velocity).normalized;
        anim.SetFloat("speed", Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.z)));
        //anim.SetFloat("turn", s.z); TODO blend better animation
    }

    private Vector3 GetRandomTargetPosition()
    {
        Vector3 tempV = ((int)Random.Range(0, 2) == 0) ? target01.position : target02.position;
        
        int count = 0;
        bool isInNavMesh = false;
        while (!isInNavMesh || count > 10000)
        {
            Vector3 randomPoint = tempV + Random.insideUnitSphere * targetRange;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, 1.0f, NavMesh.AllAreas))
            {
                tempV = hit.position;
                isInNavMesh = true;
            }
            count++;
        }

        return tempV;
    }
}
