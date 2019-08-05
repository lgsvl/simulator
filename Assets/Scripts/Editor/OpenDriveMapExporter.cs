/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */
 
using UnityEngine;
using Simulator.Map;
using System.IO;
using System.Xml.Serialization;
using Schemas;


namespace Simulator.Editor
{
    public class OpenDriveMapExporter
    {
        MapOrigin MapOrigin;
        MapManagerData MapAnnotationData;
        OpenDRIVE Map;

        public void ExportOpenDRIVEMap(string filePath)
        { 
            if (Calculate())
            {
                Export(filePath);
                Debug.Log("Successfully generated and exported OpenDRIVE Map!");
            }
            else
            {
                Debug.LogError("Failed to export OpenDRIVE Map!");
            }
        }
            

        public bool Calculate()
        {
            var mapOrigin = MapOrigin.Find();
            if (mapOrigin == null)
            {
                return false;
            }

            MapAnnotationData = new MapManagerData();
            MapAnnotationData.GetIntersections();
            MapAnnotationData.GetTrafficLanes();

            Map = new OpenDRIVE()
            {
                header = new OpenDRIVEHeader()
                {
                    revMajor = (ushort)1,
                    revMinor = (ushort)4,
                    name = "",
                    version = 1.00f,
                    date = System.DateTime.Now.ToString("ddd, MMM dd HH':'mm':'ss yyy"),
                    vendor = "LGSVL"
                }
            };

            return true;
        }

        void Export(string filePath)
        {
            var serializer = new XmlSerializer(typeof(OpenDRIVE));

            StreamWriter writer = new StreamWriter(filePath);
            serializer.Serialize(writer, Map);
        }
    }
}