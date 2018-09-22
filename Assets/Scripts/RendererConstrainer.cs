using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class RendererConstrainer : MonoBehaviour
{
    private List<Renderer> activeRenderers = new List<Renderer>();
    bool isHidden = false;
    private List<Renderer> hiddenRenderers = new List<Renderer>();

    public void ReloadAllActiveRenderers()
    {
        ShowHiddenRenderers();
        ClearAll();
        foreach (var rend in FindObjectsOfType<Renderer>())
        {
            if (rend.enabled)
            {
                activeRenderers.Add(rend);
            }
        }
    }

    public void ToggleActiveRenderersOutsideBox()
    {
        if (isHidden)
        {
            ShowHiddenRenderers();
            isHidden = false;
        }
        else
        {
            HideOutsideActiveRenderers();
            isHidden = true;
        }
    }

    public void ShowHiddenRenderers()
    {
        Undo.RecordObjects(hiddenRenderers.ToArray(), "Changes");
        foreach (var rend in hiddenRenderers)
        {
            rend.enabled = true;
        }
        hiddenRenderers.Clear();
    }

    public void HideOutsideActiveRenderers()
    {
        Undo.RecordObjects(hiddenRenderers.ToArray(), "Changes");
        hiddenRenderers.Clear();
        var selfRend = GetComponent<Renderer>();
        foreach (var rend in activeRenderers)
        {
            if (!selfRend.bounds.Intersects(rend.bounds))
            {
                rend.enabled = false;
                hiddenRenderers.Add(rend);
            }
        }
    }

    public void ClearAll()
    {
        activeRenderers.Clear();
        hiddenRenderers.Clear();
        isHidden = false;
    }
}
