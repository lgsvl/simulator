using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TugBotFollowCameraComponent : MonoBehaviour
{
    public Transform cameraPositionHolder;
    public List<Transform> camPositions = new List<Transform>();
    public Transform target;
    public int currentIndex = 0;

    private void Start()
    {
        if (cameraPositionHolder == null) return;
        foreach (Transform child in cameraPositionHolder)
        {
            camPositions.Add(child);
        }
    }

    private void Update()
    {
        if (camPositions.Count == 0) return;
        if (target == null) target = transform.root;
        if (target == null) return;

        if (Input.GetKeyDown(KeyCode.V))
        {
            currentIndex = currentIndex == camPositions.Count - 1 ? 0 : currentIndex + 1;
        }

        transform.position = Vector3.Lerp(transform.position, camPositions[currentIndex].position, Time.deltaTime * 5f);
        transform.LookAt(target);
    }
}
