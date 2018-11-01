using UnityEngine;

public class VehicleList : MonoBehaviour
{
    public RectTransform Panel;

    void Update ()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // only toggle visibility if there is at least one vehicle in scene
            if (Panel.transform.childCount > 2)
            {
                Panel.gameObject.SetActive(!Panel.gameObject.activeSelf);
            }
        }
    }
}
