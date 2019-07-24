/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */
 
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Simulator.Map;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Schemas;


namespace Simulator.Editor
{
    public class OpenDriveMapImporter
    {
        MapOrigin MapOrigin;

        public void ImportOpenDriveMap(string filePath)
        {
            OpenDRIVE odr;
            var serializer = new XmlSerializer(typeof(OpenDRIVE));

            XmlTextReader reader = new XmlTextReader(filePath);
            odr = (OpenDRIVE)serializer.Deserialize(reader);

            if (Calculate(filePath))
            {
                Debug.Log("Successfully imported OpenDRIVE Map!\nNote if your map is incorrect, please check if you have set MapOrigin correctly.");
            }
            else
            {
                Debug.Log("Failed to import OpenDRIVE map.");
            }
        }

        public bool Calculate(string filePath)
        {
            return true;
        }

        /*
        static void ValidationCallback(object sender, ValidationEventArgs args)
        {
            if (args.Severity == XmlSeverityType.Warning)
                Console.Write("WARNING: ");
            else if (args.Severity == XmlSeverityType.Error)
                Console.Write("ERROR: ");

            Console.WriteLine(args.Message);
        }
        */

    }
}