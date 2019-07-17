using UnityEngine;

using apollo.hdmap;
using System.Collections.Generic;

public class MaterialHelper
{
    public static string SEP = $"{System.IO.Path.DirectorySeparatorChar}";

    private static readonly string MATERIAL_PATH = 
        $"Materials{SEP}SingleLane{SEP}";

    public static string JUNCTION_MATERIAL_PATH =
        $"Materials{SEP}";

    public static string CROSSWALK_MATERIAL_PATH =
        $"Materials{SEP}CrossWalk{SEP}";

    // TODO(fanghaowang): Check the material is render well
    public static Dictionary<string, string> MATERIAL = new Dictionary<string, string>()
    {
        ["DOTTED_WHITE,DOTTED_WHITE"] = MATERIAL_PATH + "DOTTED_WHITE_DOTTED_WHITE",
        ["SOLID_WHITE,SOLID_WHITE"] = MATERIAL_PATH + "SOLID_WHITE_SOLID_WHITE",
        ["SOLID_WHITE,DOTTED_WHITE"] = MATERIAL_PATH + "SOLID_WHITE_DOTTED_WHITE",
        ["DOTTED_WHITE,SOLID_WHITE"] = MATERIAL_PATH + "DOTTED_WHITE_SOLID_WHITE",
        ["UNKNOWN,UNKNOWN"] = MATERIAL_PATH + "SOLID_WHITE_SOLID_WHITE",
        ["SOLID_YELLOW,SOLID_WHITE"] = MATERIAL_PATH + "SOLID_YELLOW_SOLID_WHITE",
        ["SOLID_WHITE,SOLID_YELLOW"] = MATERIAL_PATH + "SOLID_WHITE_SOLID_YELLOW",
        ["Junction"] = JUNCTION_MATERIAL_PATH + "street_asphalt",
        ["Crosswalk"] = CROSSWALK_MATERIAL_PATH + "CrossWalk",
    };



    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }


    private string CreateKey(LaneBoundaryType.Type leftBoundaryType,
        LaneBoundaryType.Type rightBoundaryType)
    {
        return leftBoundaryType.ToString() + "," + rightBoundaryType.ToString();
    }

    private string GetValue(string key)
    {
        string filename = null;

        MATERIAL.TryGetValue(key, out filename);

        return filename == null ? $"{MATERIAL_PATH}DEFAULT" : filename;
    }

    /*
     * @Input: string 
     * @Output: Material class
     * 
     */
    public Material GetMaterial(LaneBoundaryType.Type leftBoundaryType,
        LaneBoundaryType.Type rightBoundaryType)
    {
        string key = CreateKey(leftBoundaryType, rightBoundaryType);
        return GetMaterial(key);
    }



    public Material GetMaterial(string type)
    {
        string value = GetValue(type);
        Material material = Resources.Load<Material>(value);
        if (material == null)
        {
            Debug.LogError("materials not found!");
        }

        return material;
    }

}