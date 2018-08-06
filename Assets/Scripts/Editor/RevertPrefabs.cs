using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// http://answers.unity3d.com/questions/144453/reverting-several-gameobjects-to-prefab-settings-a.html
/// </summary>
public class RevertPrefabInstance : UnityEditor.Editor
{
    [MenuItem("Tools/Revert Selected Prefabs %&#r")]
    static void Revert()
    {
        GameObject[] selection = Selection.gameObjects;

        if (selection.Length > 0)
        {
            int revertedCount = 0;
            Dictionary<UnityEngine.Object, bool> prefabsAlreadyReverted = new Dictionary<UnityEngine.Object, bool>();

            //we tell the user that we're about to do an expensive operation, because otherwise they may think the editor is hanging
            //additionally if the hotkey doesn't work (aka clash with another hotkey) they won't even know it failed to run!
            EditorUtility.DisplayDialog("Please wait", "Checking " + selection.Length + " objects and their children to revert prefab status -- this may take a while!", "OK");
            for (int i = 0; i < selection.Length; i++)
                RecursiveRevertPrefabInstances(selection[i], ref revertedCount, ref prefabsAlreadyReverted);

            //tell them we finished, because otherwise they have no easy way to notice if it takes a bit!
            EditorUtility.DisplayDialog("All done!", "Performed " + revertedCount + " reversions.", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Error", "Cannot revert to prefab - nothing selected.", "OK");
        }
    }

    /// <summary>
    /// This allows for both nested prefabs as well as simply going into object trees without having to expand the whole tree first.
    /// </summary>
    static void RecursiveRevertPrefabInstances(GameObject obj, ref int revertedCount, ref Dictionary<UnityEngine.Object, bool> prefabsAlreadyReverted)
    {
        if (obj == null)
            return;
        if (IsAPrefabNotYetReverted(obj, ref prefabsAlreadyReverted))
        {
            revertedCount++;
            PrefabUtility.RevertPrefabInstance(obj);
        }
        Transform trans = obj.transform;
        for (int i = 0; i < trans.childCount; i++)
            RecursiveRevertPrefabInstances(trans.GetChild(i).gameObject, ref revertedCount, ref prefabsAlreadyReverted);
    }

    /// <summary>
    /// This keeps us from reverting the same prefab over and over, which otherwise happens when we're doing checks for nested prefabs.
    /// </summary>
    static bool IsAPrefabNotYetReverted(GameObject obj, ref Dictionary<UnityEngine.Object, bool> prefabsAlreadyReverted)
    {
        bool wasValidAtEitherLevel = false;
        UnityEngine.Object prefab = PrefabUtility.GetPrefabParent(obj);
        if (prefab != null && !prefabsAlreadyReverted.ContainsKey(prefab))
        {
            wasValidAtEitherLevel = true;
            prefabsAlreadyReverted[prefab] = true;
        }
        prefab = PrefabUtility.GetPrefabObject(obj);
        if (prefab != null && !prefabsAlreadyReverted.ContainsKey(prefab))
        {
            wasValidAtEitherLevel = true;
            prefabsAlreadyReverted[prefab] = true;
        }
        return wasValidAtEitherLevel;
    }
}
