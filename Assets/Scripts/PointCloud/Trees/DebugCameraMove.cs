/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace PointCloud.Trees
{
    using UnityEngine;

    public class DebugCameraMove : MonoBehaviour
    {
        public Transform cameraTrans;
        
        private Vector3 pos1 = new Vector3(71.8f, -1.94f, -74.2f);
        private Vector3 pos2 = new Vector3(-40.1f, -1.94f, 48.8f);

        private float yRot1 = -105f;
        private float yRot2 = 25f;

        private float duration = 10f;

        private float timePassed = 0f;
        
        private void Update()
        {
            timePassed += Time.deltaTime;
            var p = Mathf.Clamp01(timePassed / duration);

            cameraTrans.position = Vector3.Lerp(pos1, pos2, p);
            var euler = cameraTrans.eulerAngles;
            euler.y = Mathf.Lerp(yRot1, yRot2, p);
            cameraTrans.eulerAngles = euler;

            if (timePassed > duration)
                timePassed = 0f;
        }
    }
}