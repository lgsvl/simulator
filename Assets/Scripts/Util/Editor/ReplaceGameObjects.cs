using UnityEngine;
using UnityEditor;
using System.Collections;

/*
 * http://forum.unity3d.com/threads/24311-Replace-game-object-with-prefab/page2
 * */

public class ReplaceGameObjects : ScriptableWizard

{
    public GameObject useGameObject;

    public GameObject customParent;

    public bool isCustomRot = false;
    public Vector3 customRot = Vector3.zero;

    public bool isCustomScale = false;
    public Vector3 customScale = Vector3.zero;

    [MenuItem("SimulatorUtil/Replace GameObjects")]

    static void CreateWizard()
    {
        ScriptableWizard.DisplayWizard("Replace GameObjects", typeof(ReplaceGameObjects), "Replace");
    }

    void OnWizardCreate()
    {
        foreach (Transform t in Selection.transforms)
        {
            GameObject newObject = PrefabUtility.InstantiatePrefab(useGameObject) as GameObject;
            Undo.RegisterCreatedObjectUndo(newObject, "created prefab");
            Transform newT = newObject.transform;
            newObject.name = useGameObject.name;

            if (customParent != null)
                newT.SetParent(customParent.transform);
            else
                newT.SetParent(t.parent);

            newT.position = t.position;
            if (isCustomRot)
                newT.localRotation = Quaternion.Euler(customRot);
            else
                newT.localRotation = t.localRotation;

            //if (isCustomScale)
            //    newT.localScale = customScale;
            //else
            //    newT.localScale = t.localScale;

            
        }

        foreach (GameObject go in Selection.gameObjects)
        {
            Undo.DestroyObjectImmediate(go);
        }
    }
}
