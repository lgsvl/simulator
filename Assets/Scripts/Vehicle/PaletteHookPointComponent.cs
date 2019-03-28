using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PaletteHookPointComponent : MonoBehaviour
{
    private TugbotHookComponent tugbotHookC;

    private void OnTriggerStay(Collider other)
    {
        if (other.transform.root.tag == "Player")
        {
            tugbotHookC = other.transform.root.GetComponent<TugbotHookComponent>();

            if (tugbotHookC != null && tugbotHookC.IsHooked)
            {
                GetComponentInParent<PaletteComponent>().AttachToTugBot(tugbotHookC.hookRigidbody.position, tugbotHookC.hookRigidbody);
            }
        }
    }
}
