/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */
using System;
using System.Xml.Linq;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Collections.Generic;

public class SDFLight : SDFParserBase
{
    public SDFLight(SDFDocument document)
    {
        this.document = document;
    }

    public readonly SDFDocument document;

    public override GameObject Parse(XElement lightElement, GameObject parent)
    {
        GameObject lightObject = CreateChildObject(lightElement, parent);
        var light = lightObject.AddComponent<Light>();

        lightObject.isStatic = ParseIntBool(lightElement.Element("static"), lightObject.isStatic);
        light.enabled = !lightObject.isStatic;

        light.type = lightElement.Attribute("type")?.Value switch
        {
            "directional" => LightType.Directional,
            "point" => LightType.Point,
            "spot" => LightType.Spot,
            _ => LightType.Point
        };

        foreach (var childElement in lightElement.Elements())
        {
            switch (childElement.Name.ToString())
            {
                case "cast_shadows":
                    light.shadows = ParseIntBool(childElement, false) ? LightShadows.Hard : LightShadows.None;
                    break;
                case "diffuse":
                    light.color = ParseColor(childElement);
                    break;
                case "attenuation":
                    ParseAttenuation(childElement, light);
                    break;
                case "direction":
                    light.transform.rotation = Quaternion.LookRotation(ParseSDFVector(childElement), Vector3.up);
                    break;
                default:
                    Debug.LogWarning("unhandled element: " + childElement + " within " + lightElement);
                    break;
            }
        }
        return lightObject;
    }

    private void ParseAttenuation(XElement attenuationElement, Light light)
    {
        foreach (var childElement in attenuationElement.Elements())
        {
            switch (childElement.Name.ToString())
            {
                case "range":
                    light.range = ParseSingle(childElement, 10.0f);
                    break;
                default:
                    Debug.LogWarning("unhandled element: " + childElement + " within " + attenuationElement);
                    break;
            }
        }
    }
}

