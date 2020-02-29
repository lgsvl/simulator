/* Copyright (c) 2018, Dassault Systemes All rights reserved.
 * Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
 * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
 * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
 * Neither the name of the copyright holder nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING,
 * BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT
 * SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
 * HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.IO;
using System.Xml.Linq;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Simulator.FMU;

namespace Simulator.Editor
{
    public class FMUImporter
    {
        public static FMUData ImportFMU(string vehicleName)
        {
            FMUData data = null;
            string[] filters = { "FMUs", "fmu", "All files", "*" };
            string fmuPath = EditorUtility.OpenFilePanelWithFilters("Import FMU", "", filters);
            if (!string.IsNullOrEmpty(fmuPath))
            {
                data = new FMUData();
                var fmuName = Path.GetFileNameWithoutExtension(fmuPath);
                var unzipDir = Path.Combine(Application.dataPath, "External", "Vehicles", vehicleName, fmuName);

                ZipFile zip = new ZipFile(fmuPath);
                ExtractZipFile(fmuPath, unzipDir);
                XDocument doc = XDocument.Load(unzipDir + "/modelDescription.xml");
                if (doc != null)
                {
                    var root = doc.Root;
                    data.Version = (string)root.Attribute("fmiVersion");
                    data.GUID = (string)root.Attribute("guid");
                    data.modelName = (string)root.Attribute("modelName");

                    // TODO support multiple modelIdentifiers ME or CS
                    data.type = FMIType.ModelExchange;
                    foreach (var e in root.Elements("CoSimulation"))
                    {
                        data.type = FMIType.CoSimulation;
                    }

                    var variables = new List<ScalarVariable>();
                    foreach (var e in root.Element("ModelVariables").Elements("ScalarVariable"))
                    {
                        var v = new ScalarVariable
                        {
                            name = (string)e.Attribute("name"),
                            description = (string)e.Attribute("description"),
                            causality = (string)e.Attribute("causality"),
                            variability = (string)e.Attribute("variability"),
                            initial = (string)e.Attribute("initial"),
                            valueReference = (uint)e.Attribute("valueReference"),
                        };
                        variables.Add(v);

                        foreach (var el in e.Elements("Real"))
                        {
                            v.type = VariableType.Real;
                            v.start = (string)el.Attribute("start");
                        }

                        foreach (var el in e.Elements("Integer"))
                        {
                            v.type = VariableType.Integer;
                            v.start = (string)el.Attribute("start");
                        }

                        foreach (var el in e.Elements("Boolean"))
                        {
                            v.type = VariableType.Boolean;
                            v.start = (string)el.Attribute("start");
                        }

                        foreach (var el in e.Elements("String"))
                        {
                            v.type = VariableType.String;
                            v.start = (string)el.Attribute("start");
                        }
                    }
                    data.modelVariables = variables;
                    data.Name = fmuName;

                    AssetDatabase.Refresh();
                }
            }
            return data;
        }

        public static void ExtractZipFile(string archivePath, string outFolder, string password = null)
        {

            using (Stream fsInput = File.OpenRead(archivePath))
            using (ZipFile zf = new ZipFile(fsInput))
            {

                if (!String.IsNullOrEmpty(password))
                {
                    // AES encrypted entries are handled automatically
                    zf.Password = password;
                }

                foreach (ZipEntry zipEntry in zf)
                {
                    if (!zipEntry.IsFile)
                    {
                        // Ignore directories
                        continue;
                    }
                    String entryFileName = zipEntry.Name;
                    // to remove the folder from the entry:
                    //entryFileName = Path.GetFileName(entryFileName);
                    // Optionally match entrynames against a selection list here
                    // to skip as desired.
                    // The unpacked length is available in the zipEntry.Size property.

                    // Manipulate the output filename here as desired.
                    var fullZipToPath = Path.Combine(outFolder, entryFileName);
                    var directoryName = Path.GetDirectoryName(fullZipToPath);
                    if (directoryName.Length > 0)
                    {
                        Directory.CreateDirectory(directoryName);
                    }

                    // 4K is optimum
                    var buffer = new byte[4096];

                    // Unzip file in buffered chunks. This is just as fast as unpacking
                    // to a buffer the full size of the file, but does not waste memory.
                    // The "using" will close the stream even if an exception occurs.
                    using (var zipStream = zf.GetInputStream(zipEntry))
                    using (Stream fsOutput = File.Create(fullZipToPath))
                    {
                        StreamUtils.Copy(zipStream, fsOutput, buffer);
                    }
                }
            }
        }
    }
}
