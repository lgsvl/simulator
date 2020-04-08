/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Editor.PointCloud.Trees
{
    using System;
    using System.IO;
    using System.IO.MemoryMappedFiles;
    using System.Text;

    public class PlyPointProcessor : PointProcessor
    {
        private readonly DefaultHeaderData header;

        public PlyPointProcessor(string filePath) : base(filePath)
        {
            header = ReadHeader();
        }

        private DefaultHeaderData ReadHeader()
        {
            var result = new DefaultHeaderData();

            using (var file = MemoryMappedFile.CreateFromFile(FilePath, FileMode.Open))
            {
                int elementOffset = 0;

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
                                var format = line.Split(new[] {' '});
                                if (format[1] != "binary_little_endian" || format[2] != "1.0")
                                {
                                    throw new Exception($"Unsupported PLY format: {line}");
                                }
                            }
                            else if (line.StartsWith("property"))
                            {
                                var props = line.Split(new[] {' '}, 3);
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
                                        result.Elements.Add(new PointElement() {Type = type.Value, Name = name.Value, Offset = elementOffset});
                                    }

                                    elementOffset += PointElement.GetSize(type.Value);
                                }
                            }
                            else if (line.StartsWith("element vertex"))
                            {
                                var vertex = line.Split(new[] {' '}, 3);
                                result.DataCount = long.Parse(vertex[2]);
                            }
                            else if (line.StartsWith("end_header"))
                            {
                                break;
                            }
                        }

                        result.DataOffset = stream.Position;
                    }
                }

                result.DataStride = elementOffset;
            }

            return result;
        }

        ///<inheritdoc/>
        public override PointCloudBounds CalculateBounds()
        {
            return CalculateBoundsDefault(header);
        }

        ///<inheritdoc/>
        public override PointCloudVerticalHistogram GenerateHistogram(PointCloudBounds bounds)
        {
            return GenerateHistogramDefault(header, bounds);
        }

        ///<inheritdoc/>
        public override bool ConvertPoints(NodeProcessorDispatcher target, TransformationData transformationData)
        {
            return ConvertPointsDefault(header, target, transformationData);
        }
    }
}