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
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    public class PcdPointProcessor : PointProcessor
    {
        private readonly DefaultHeaderData header;

        public PcdPointProcessor(string filePath) : base(filePath)
        {
            header = ReadHeader();
        }

        /// <summary>
        /// Reads header of associated file and stores results under <see cref="header"/>.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private DefaultHeaderData ReadHeader()
        {
            var result = new DefaultHeaderData();

            using (var file = MemoryMappedFile.CreateFromFile(FilePath, FileMode.Open))
            {
                string[] fields = null;
                string[] sizes = null;
                string[] types = null;

                using (var view = file.CreateViewStream(0, 4096, MemoryMappedFileAccess.Read))
                {
                    var buffer = new byte[4096];
                    int length = view.Read(buffer, 0, buffer.Length);
                    using (var stream = new MemoryStream(buffer, 0, length, false))
                    {
                        var byteLine = new byte[128];
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
                                throw new Exception("Bad PCD file format");
                            }

                            byte next = byteLine[index + 1];
                            stream.Position -= byteLine.Length - (index + (next == '\r' || next == '\n' ? 2 : 1));

                            var line = Encoding.ASCII.GetString(byteLine, 0, index);
                            line = new Regex("#.*$").Replace(line, "").Trim();

                            if (line.Length == 0)
                            {
                                continue;
                            }

                            if (line.StartsWith("VERSION"))
                            {
                                var version = line.Split(new[] {' '}, 2);
                                if (version[1] != "0.7")
                                {
                                    throw new Exception($"Unsupported PCD version: {version[1]}");
                                }
                            }
                            else if (line.StartsWith("FIELDS"))
                            {
                                fields = line.Split(' ').Skip(1).ToArray();
                            }
                            else if (line.StartsWith("SIZE"))
                            {
                                sizes = line.Split(' ').Skip(1).ToArray();
                            }
                            else if (line.StartsWith("TYPE"))
                            {
                                types = line.Split(' ').Skip(1).ToArray();
                            }
                            else if (line.StartsWith("COUNT"))
                            {
                                var counts = line.Split(' ');
                                if (counts.Skip(1).Any(c => c != "1"))
                                {
                                    throw new Exception($"Unsupported PCD counts, expected '1' for all fields");
                                }
                            }
                            else if (line.StartsWith("HEIGHT"))
                            {
                                var height = line.Split(new[] {' '}, 2);
                                if (height[1] != "1")
                                {
                                    throw new Exception($"Unsupported PCD height: {height[1]}");
                                }
                            }
                            else if (line.StartsWith("POINTS"))
                            {
                                var points = line.Split(new[] {' '}, 2);
                                result.DataCount = long.Parse(points[1]);
                            }
                            else if (line.StartsWith("DATA"))
                            {
                                var data = line.Split(new[] {' '}, 2);
                                if (data[1] != "binary")
                                {
                                    throw new Exception($"Unsupported PCD data format: {data[1]}");
                                }

                                break;
                            }
                        }

                        result.DataOffset = stream.Position;
                    }
                }

                if (fields == null) throw new Exception("PCD file is missing FIELDS");
                if (sizes == null) throw new Exception("PCD file is missing SIZE");
                if (types == null) throw new Exception("PCD file is missing TYPE");

                if (fields.Length != sizes.Length || fields.Length != types.Length)
                {
                    throw new Exception("PCD file has incorrect type specs");
                }

                int elementOffset = 0;

                for (int i = 0; i < fields.Length; i++)
                {
                    PointElementType? type = null;
                    PointElementName? name = null;

                    int elementSize = int.Parse(sizes[i]);

                    if (fields[i] == "x") name = PointElementName.X;
                    if (fields[i] == "y") name = PointElementName.Y;
                    if (fields[i] == "z") name = PointElementName.Z;
                    if (fields[i] == "intensity") name = PointElementName.I;
                    if (fields[i] == "scalar_intensity") name = PointElementName.I;

                    if (types[i] == "U" && elementSize == 1) type = PointElementType.Byte;
                    if (types[i] == "F" && elementSize == 4) type = PointElementType.Float;
                    if (types[i] == "D" && elementSize == 8) type = PointElementType.Double;

                    if (type.HasValue && name.HasValue)
                    {
                        result.Elements.Add(new PointElement()
                            {Type = type.Value, Name = name.Value, Offset = elementOffset});
                    }

                    elementOffset += elementSize;
                }

                result.DataStride = elementOffset;
            }

            return result;
        }

        /// <inheritdoc/>
        public override PointCloudBounds CalculateBounds()
        {
            return CalculateBoundsDefault(header);
        }

        /// <inheritdoc/>
        public override PointCloudVerticalHistogram GenerateHistogram(PointCloudBounds bounds)
        {
            return GenerateHistogramDefault(header, bounds);
        }

        /// <inheritdoc/>
        public override bool ConvertPoints(NodeProcessorDispatcher target, TransformationData transformationData)
        {
            return ConvertPointsDefault(header, target, transformationData);
        }
    }
}