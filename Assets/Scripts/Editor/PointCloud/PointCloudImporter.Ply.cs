/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using UnityEditor.Experimental.AssetImporters;
using Simulator.PointCloud;

namespace Simulator.Editor.PointCloud
{
    public partial class PointCloudImporter : ScriptedImporter
    {
        PointCloudData ImportPly(AssetImportContext context)
        {
            using (var file = MemoryMappedFile.CreateFromFile(context.assetPath, FileMode.Open))
            {
                long dataOffset;
                long dataCount = 0;
                int elementOffset = 0;
                var properties = new List<string>();
                var elements = new List<PointElement>();

                using (var view = file.CreateViewStream(0, 4096, MemoryMappedFileAccess.Read))
                {
                    var buffer = new byte[4096];
                    int length = view.Read(buffer, 0, buffer.Length);
                    using (var stream = new MemoryStream(buffer, 0, length, false))
                    {
                        var byteLine = new byte[128];
                        bool first = true;

                        while (true)
                        {
                            int byteCount = stream.Read(byteLine, 0, byteLine.Length);
                            int index = -1;
                            for (int i = 0; i < byteCount; i++)
                            {
                                if (byteLine[i] == '\n' || byteLine[i] == '\r')
                                {
                                    index = i;
                                    break;
                                }
                            }
                            if (index == -1)
                            {
                                throw new Exception("Bad PLY file");
                            }
                            var line = Encoding.ASCII.GetString(byteLine, 0, index);
                            byte next = byteLine[index + 1];
                            stream.Position -= byteLine.Length - (index + (next == '\r' || next == '\n' ? 2 : 1));

                            if (first && line != "ply")
                            {
                                throw new Exception("Bad PLY file format");
                            }
                            first = false;

                            if (line.StartsWith("format"))
                            {
                                var format = line.Split(new[] { ' ' });
                                if (format[1] != "binary_little_endian" || format[2] != "1.0")
                                {
                                    throw new Exception($"Unsupported PLY format: {line}");
                                }
                            }
                            else if (line.StartsWith("property"))
                            {
                                var props = line.Split(new[] { ' ' }, 3);
                                if (props[1] != "list")
                                {
                                    PointElementType? type = null;
                                    PointElementName? name = null;

                                    if (props[1] == "uint8") type = PointElementType.Byte;
                                    if (props[1] == "float32") type = PointElementType.Float;
                                    if (props[1] == "float64") type = PointElementType.Double;

                                    if (props[1] == "uchar") type = PointElementType.Byte;
                                    if (props[1] == "float") type = PointElementType.Float;
                                    if (props[1] == "double") type = PointElementType.Double;

                                    if (props[2] == "x") name = PointElementName.X;
                                    if (props[2] == "y") name = PointElementName.Y;
                                    if (props[2] == "z") name = PointElementName.Z;
                                    if (props[2] == "red") name = PointElementName.R;
                                    if (props[2] == "green") name = PointElementName.G;
                                    if (props[2] == "blue") name = PointElementName.B;
                                    if (props[2] == "intensity") name = PointElementName.I;
                                    if (props[2] == "scalar_intensity") name = PointElementName.I;

                                    if (!type.HasValue)
                                    {
                                        throw new Exception($"PLY file has unsupported property type {props[1]}");
                                    }

                                    if (name.HasValue)
                                    {
                                        elements.Add(new PointElement() { Type = type.Value, Name = name.Value, Offset = elementOffset });
                                    }

                                    elementOffset += PointElement.GetSize(type.Value);
                                }
                            }
                            else if (line.StartsWith("element vertex"))
                            {
                                var vertex = line.Split(new[] { ' ' }, 3);
                                dataCount = long.Parse(vertex[2]);
                            }
                            else if (line.StartsWith("end_header"))
                            {
                                break;
                            }
                        }
                        dataOffset = stream.Position;
                    }
                }

                int dataStride = elementOffset;
                using (var view = file.CreateViewAccessor(dataOffset, dataCount * dataStride, MemoryMappedFileAccess.Read))
                {
                    return ImportPoints(context, view, dataCount, dataStride, elements);
                }
            }
        }
    }
}
