using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class CommonToolSet : UnityEditor.Editor
{
    [MenuItem("Tools/Number of objects selected")]
    static void OutputNumber()
    {
        GameObject[] selection = Selection.gameObjects;
        Debug.Log("Number of game objects selected: " + selection.Length);
    }
}
