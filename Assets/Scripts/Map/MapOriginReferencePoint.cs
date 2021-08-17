using UnityEngine;
using Simulator.Map;

public class MapOriginReferencePoint : MonoBehaviour
{
    public double latitue;
    public double longitude;

    void OnDrawGizmosSelected()
    {
        Gizmos.DrawSphere(transform.position, 0.1f);
        var origin = FindObjectOfType<MapOrigin>();
        var realPos = origin.LatLongToPosition(latitue, longitude);
        realPos.y = transform.position.y;
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(realPos, 0.1f);
        Gizmos.DrawLine(transform.position, realPos);
    }
}
