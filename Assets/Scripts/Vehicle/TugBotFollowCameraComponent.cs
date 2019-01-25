using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TugBotFollowCameraComponent : MonoBehaviour
{
    
    public List<Transform> camPositions = new List<Transform>();
    private Transform target;
    private int currentIndex = 0;

    private void Start()
    {
        target = GameObject.FindGameObjectWithTag("Player").transform; // TODO better way
    }

    private void Update()
    {
        if (camPositions.Count == 0 || target == null) return;

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            currentIndex = currentIndex == camPositions.Count - 1 ? 0 : currentIndex + 1;
        }

        transform.position = Vector3.Lerp(transform.position, camPositions[currentIndex].position, Time.deltaTime * 5f);
        transform.LookAt(target);
    }
}
