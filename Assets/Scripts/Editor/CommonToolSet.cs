using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class CommonToolSet : UnityEditor.Editor
{
    [MenuItem("Window/Custom Editor Tools/Number of objects selected")]
    static void OutputNumber()
    {
        GameObject[] selection = Selection.gameObjects;
        Debug.Log("Number of game objects selected: " + selection.Length);
    }

    [MenuItem("Window/Custom Editor Tools/Select All Renderers Under Selection")]
    static void SelectAllRenderersUnderSelection()
    {
        GameObject[] selection = Selection.gameObjects;
        List<Renderer> newRenderers = new List<Renderer>();
        foreach (var go in selection)
        {
            newRenderers.AddRange(go.GetComponentsInChildren<Renderer>(true));
        }

        Selection.objects = newRenderers.ToArray();
    }
}
