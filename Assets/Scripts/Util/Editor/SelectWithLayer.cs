using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using DB = UnityEngine.Debug;

public class SelectWithLayer : ScriptableObject {
    static void SelectLayer(int layerNum) {
        var objs = Selection.GetFiltered(typeof(GameObject), SelectionMode.Deep);

        var list = new List<Object>(objs.Length);

        foreach (var obj in objs) {
            var go = obj as GameObject;

            if (go == null) continue;

            if (go.layer == layerNum) {
                list.Add(go);
            }

            Selection.objects = list.ToArray();
        }

        var layerName = LayerMask.LayerToName(layerNum);
        DB.Log(string.Format("Found {0} objects in layer \"{1}\"", list.Count, string.IsNullOrEmpty(layerName) ? layerNum.ToString() : layerName));
    }

    #region Can't figure out a smarter way to do this.
    [MenuItem("Tools/Select With Layer/0")]
    static void SelectLayer0() {
        SelectLayer(0);
    }
    [MenuItem("Tools/Select With Layer/1")]
    static void SelectLayer1() {
        SelectLayer(1);
    }
    [MenuItem("Tools/Select With Layer/2")]
    static void SelectLayer2() {
        SelectLayer(2);
    }
    [MenuItem("Tools/Select With Layer/3")]
    static void SelectLayer3() {
        SelectLayer(3);
    }
    [MenuItem("Tools/Select With Layer/4")]
    static void SelectLayer4() {
        SelectLayer(4);
    }
    [MenuItem("Tools/Select With Layer/5")]
    static void SelectLayer5() {
        SelectLayer(5);
    }
    [MenuItem("Tools/Select With Layer/6")]
    static void SelectLayer6() {
        SelectLayer(6);
    }
    [MenuItem("Tools/Select With Layer/7")]
    static void SelectLayer7() {
        SelectLayer(7);
    }
    [MenuItem("Tools/Select With Layer/8")]
    static void SelectLayer8() {
        SelectLayer(8);
    }
    [MenuItem("Tools/Select With Layer/9")]
    static void SelectLayer9() {
        SelectLayer(9);
    }
    [MenuItem("Tools/Select With Layer/10")]
    static void SelectLayer10() {
        SelectLayer(10);
    }
    [MenuItem("Tools/Select With Layer/11")]
    static void SelectLayer11() {
        SelectLayer(11);
    }
    [MenuItem("Tools/Select With Layer/12")]
    static void SelectLayer12() {
        SelectLayer(12);
    }
    [MenuItem("Tools/Select With Layer/13")]
    static void SelectLayer13() {
        SelectLayer(13);
    }
    [MenuItem("Tools/Select With Layer/14")]
    static void SelectLayer14() {
        SelectLayer(14);
    }
    [MenuItem("Tools/Select With Layer/15")]
    static void SelectLayer15() {
        SelectLayer(15);
    }
    [MenuItem("Tools/Select With Layer/16")]
    static void SelectLayer16() {
        SelectLayer(16);
    }
    [MenuItem("Tools/Select With Layer/17")]
    static void SelectLayer17() {
        SelectLayer(17);
    }
    [MenuItem("Tools/Select With Layer/18")]
    static void SelectLayer18() {
        SelectLayer(18);
    }
    [MenuItem("Tools/Select With Layer/19")]
    static void SelectLayer19() {
        SelectLayer(19);
    }
    [MenuItem("Tools/Select With Layer/20")]
    static void SelectLayer20() {
        SelectLayer(20);
    }
    [MenuItem("Tools/Select With Layer/21")]
    static void SelectLayer21() {
        SelectLayer(21);
    }
    [MenuItem("Tools/Select With Layer/22")]
    static void SelectLayer22() {
        SelectLayer(22);
    }
    [MenuItem("Tools/Select With Layer/23")]
    static void SelectLayer23() {
        SelectLayer(23);
    }
    [MenuItem("Tools/Select With Layer/24")]
    static void SelectLayer24() {
        SelectLayer(24);
    }
    [MenuItem("Tools/Select With Layer/25")]
    static void SelectLayer25() {
        SelectLayer(25);
    }
    [MenuItem("Tools/Select With Layer/26")]
    static void SelectLayer26() {
        SelectLayer(26);
    }
    [MenuItem("Tools/Select With Layer/27")]
    static void SelectLayer27() {
        SelectLayer(27);
    }
    [MenuItem("Tools/Select With Layer/28")]
    static void SelectLayer28() {
        SelectLayer(28);
    }
    [MenuItem("Tools/Select With Layer/29")]
    static void SelectLayer29() {
        SelectLayer(29);
    }
    [MenuItem("Tools/Select With Layer/30")]
    static void SelectLayer30() {
        SelectLayer(30);
    }
    [MenuItem("Tools/Select With Layer/31")]
    static void SelectLayer31() {
        SelectLayer(31);
    }
    #endregion
}
