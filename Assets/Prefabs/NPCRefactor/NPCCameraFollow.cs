using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCCameraFollow : MonoBehaviour
{
    private Transform focus;
    public Vector3 offset;

    IEnumerator Start()
    {
        while (!MapManager.Instance.isInit && !NPCManager.Instance.isInit && NPCManager.Instance.currentPooledNPCs.Count < 0)
            yield return null;
        StartCoroutine(FollowRandomNPC());
    }

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.Space))
        {
            StopAllCoroutines();
            focus = NPCManager.Instance?.GetRandomActiveNPC();
            if (focus != null && focus.gameObject.activeInHierarchy)
            {
                transform.position = focus.position + offset;
                transform.SetParent(focus);
                transform.LookAt(focus);
            }
        }
    }

    IEnumerator FollowRandomNPC()
    {
        while (true)
        {
            focus = NPCManager.Instance?.GetRandomActiveNPC();
            if (focus != null && focus.gameObject.activeInHierarchy)
            {
                transform.position = focus.position + offset;
                transform.SetParent(focus);
                transform.LookAt(focus);
            }
            yield return new WaitForSeconds(10f);
        }
    }


}
