using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using UnityEngine.Rendering;

namespace Parabox.Stl
{
	/// <summary>
	/// Import mesh assets from an STL file.
	/// </summary>
	public static class Importer
	{
        const int MaxFacetsPerMesh16 = UInt16.MaxValue / 3;
        const int MaxFacetsPerMesh32 = int.MaxValue / 3;

		struct Facet
		{
			public Vector3 normal;
			public Vector3 a, b, c;

			public Facet(Vector3 normal, Vector3 a, Vector3 b, Vector3 c)
			{
				this.normal = normal;
				this.a = a;
				this.b = b;
				this.c = c;
			}

			public override string ToString()
			{
				return string.Format("{0:F2}: {1:F2}, {2:F2}, {3:F2}", normal, a, b, c);
			}
		}

		/// <summary>
		/// Import an STL file.
		/// </summary>
		/// <param name="path">The path to load STL file from.</param>
		/// <returns></returns>
		public static Mesh[] Import(string path, CoordinateSpace space = CoordinateSpace.Right, UpAxis axis = UpAxis.Y, bool smooth = false, IndexFormat indexFormat = IndexFormat.UInt16)
		{
			IEnumerable<Facet> facets = null;

			if( IsBinary(path) )
			{
				try
				{
					facets = ImportBinary(path);
				}
				catch(System.Exception e)
				{
					UnityEngine.Debug.LogWarning(string.Format("Failed importing mesh at path {0}.\n{1}", path, e.ToString()));
					return null;
				}
			}
			else
			{
				facets = ImportAscii(path);
			}

			if(smooth)
				return ImportSmoothNormals(facets, space, axis, indexFormat);

			return ImportHardNormals(facets, space, axis, indexFormat);
		}

		static IEnumerable<Facet> ImportBinary(string path)
		{
			Facet[] facets;

            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader br = new BinaryReader(fs, new ASCIIEncoding()))
                {
                    // read header
                    br.ReadBytes(80);

                    uint facetCount = br.ReadUInt32();
                    facets = new Facet[facetCount];

                    for(uint i = 0; i < facetCount; i++)
                        facets[i] = br.GetFacet();
                }
            }

			return facets;
		}

        static Facet GetFacet(this BinaryReader binaryReader)
        {
			Facet facet = new Facet(
				binaryReader.GetVector3(),	// Normal
				binaryReader.GetVector3(),	// A
				binaryReader.GetVector3(),	// B
				binaryReader.GetVector3()	// C
				);

            binaryReader.ReadUInt16(); // padding

            return facet;
        }

        static Vector3 GetVector3(this BinaryReader binaryReader)
        {
			return new Vector3(
				binaryReader.ReadSingle(),
				binaryReader.ReadSingle(),
				binaryReader.ReadSingle() );
        }

		const int SOLID = 1;
		const int FACET = 2;
		const int OUTER = 3;
		const int VERTEX = 4;
		const int ENDLOOP = 5;
		const int ENDFACET = 6;
		const int ENDSOLID = 7;
		const int EMPTY = 0;

		static int ReadState(string line)
		{
			if(line.StartsWith("solid"))
				return SOLID;
			else if(line.StartsWith("facet"))
				return FACET;
			else if(line.StartsWith("outer"))
				return OUTER;
			else if(line.StartsWith("vertex"))
				return VERTEX;
			else if(line.StartsWith("endloop"))
				return ENDLOOP;
			else if(line.StartsWith("endfacet"))
				return ENDFACET;
			else if(line.StartsWith("endsolid"))
				return ENDSOLID;
			else
				return EMPTY;
		}

		static IEnumerable<Facet> ImportAscii(string path)
		{
			List<Facet> facets = new List<Facet>();

			using(StreamReader sr = new StreamReader(path))
			{
				string line;
				int state = EMPTY, vertex = 0;
				Vector3 normal = Vector3.zero;
				Vector3 a = Vector3.zero, b = Vector3.zero, c = Vector3.zero;
				bool exit = false;

				while(sr.Peek() > 0 && !exit)
				{
					line = sr.ReadLine().Trim();
					state = ReadState(line);

					switch(state)
					{
						case SOLID:
							continue;

						case FACET:
							normal = StringToVec3(line.Replace("facet normal ", ""));
						break;

						case OUTER:
							vertex = 0;
						break;

						case VERTEX:
                            // maintain counter-clockwise orientation of vertices:
                            if (vertex == 0)
								a = StringToVec3(line.Replace("vertex ", ""));
							else if(vertex == 2)
								c = StringToVec3(line.Replace("vertex ", ""));
                            else if (vertex == 1)
								b = StringToVec3(line.Replace("vertex ", ""));
                            vertex++;
						break;

						case ENDLOOP:
						break;

						case ENDFACET:
							facets.Add(new Facet(normal, a, b, c));
						break;

						case ENDSOLID:
							exit = true;
						break;

						case EMPTY:
						default:
						break;

					}
				}
			}

			return facets;
		}

		static Vector3 StringToVec3(string str)
		{
			string[] split = str.Trim().Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
			Vector3 v = new Vector3();

			float.TryParse(split[0], out v.x);
			float.TryParse(split[1], out v.y);
			float.TryParse(split[2], out v.z);

            return v;
		}

		/// <summary>
		/// Determine whether this file is a binary stl format or not.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		static bool IsBinary(string path)
		{
			// http://stackoverflow.com/questions/968935/compare-binary-files-in-c-sharp
			FileInfo file = new FileInfo(path);

			if(file.Length < 130)
				return false;

			var isBinary = false;

			using(FileStream f0 = file.OpenRead())
			{
				using(BufferedStream bs0 = new BufferedStream(f0))
				{
					for(long i = 0; i < 80; i++)
					{
					    var readByte = bs0.ReadByte();
					    if (readByte == 0x0)
					    {
					        isBinary = true;
					        break;
					    }
					}
				}
			}

            if (!isBinary)
            {
                using (FileStream f0 = file.OpenRead())
                {
                    using (BufferedStream bs0 = new BufferedStream(f0))
                    {
                        var byteArray = new byte[6];

                        for (var i = 0; i < 6; i++)
                        {
                            byteArray[i] = (byte)bs0.ReadByte();
                        }

                        var text = Encoding.UTF8.GetString(byteArray);
                        isBinary = text != "solid ";
                    }
                }
            }

			return isBinary;
		}

		static Mesh[] ImportSmoothNormals(IEnumerable<Facet> faces, CoordinateSpace modelCoordinateSpace, UpAxis modelUpAxis, IndexFormat indexFormat)
		{
			var facets = faces as Facet[] ?? faces.ToArray();
			int maxVertexCount = indexFormat == IndexFormat.UInt32 ? MaxFacetsPerMesh32 * 3 : MaxFacetsPerMesh16 * 3;
			int triangleCount = facets.Length * 3;

			Dictionary<StlVector3, Vector3> smoothNormals = new Dictionary<StlVector3, Vector3>(triangleCount / 2);

			// In case meshes are split, we need to calculate smooth normals first
			foreach(var face in faces)
			{
				var x = (StlVector3) face.a;
				var y = (StlVector3) face.b;
				var z = (StlVector3) face.c;
				var normal = face.normal;

				if(smoothNormals.ContainsKey(x))
					smoothNormals[x] += normal;
				else
					smoothNormals.Add(x, normal);

				if(smoothNormals.ContainsKey(y))
					smoothNormals[y] += normal;
				else
					smoothNormals.Add(y, normal);

				if(smoothNormals.ContainsKey(z))
					smoothNormals[z] += normal;
				else
					smoothNormals.Add(z, normal);
			}

			List<Mesh> meshes = new List<Mesh>();

			List<Vector3> pos = new List<Vector3>(Math.Min(maxVertexCount, triangleCount));
			List<Vector3> nrm = new List<Vector3>(Math.Min(maxVertexCount, triangleCount));
			List<int> tri = new List<int>(triangleCount);
			Dictionary<StlVector3, int> map = new Dictionary<StlVector3, int>();
			int vertex = 0;
			Vector3[] points = new Vector3[3];

			foreach(var face in facets)
			{
				if(vertex + 3 > maxVertexCount)
				{
                    var mesh = new Mesh
                    {
                        vertices = pos.ToArray(),
                        normals = nrm.ToArray(),
                        indexFormat = indexFormat
                    };
                    if(modelCoordinateSpace == CoordinateSpace.Right)
						tri.Reverse();
					mesh.triangles = tri.ToArray();
					meshes.Add(mesh);

					vertex = 0;

					pos.Clear();
					nrm.Clear();
					tri.Clear();
					map.Clear();
				}

				points[0] = face.a;
				points[1] = face.b;
				points[2] = face.c;

				for(int i = 0; i < 3; i++)
				{
					int index = -1;
					var hash = (StlVector3) points[i];

					if(!map.TryGetValue(hash, out index))
					{
						if(modelCoordinateSpace == CoordinateSpace.Right)
						{
							pos.Add(Stl.ToCoordinateSpace(points[i], CoordinateSpace.Left));
							nrm.Add(Stl.ToCoordinateSpace(smoothNormals[hash].normalized, CoordinateSpace.Left));
						}
						else
						{
							pos.Add(points[i]);
							nrm.Add(smoothNormals[hash].normalized);
						}

						tri.Add(vertex);
						map.Add(hash, vertex++);
					}
					else
					{
						tri.Add(index);
					}
				}
			}

			if(vertex > 0)
			{
                var mesh = new Mesh
                {
                    vertices = pos.ToArray(),
                    normals = nrm.ToArray(),
                    indexFormat = indexFormat
                };
                if(modelCoordinateSpace == CoordinateSpace.Right)
					tri.Reverse();
				mesh.triangles = tri.ToArray();
				meshes.Add(mesh);

				vertex = 0;

				pos.Clear();
				nrm.Clear();
				tri.Clear();
				map.Clear();
			}

			return meshes.ToArray();
		}

		static Mesh[] ImportHardNormals(IEnumerable<Facet> faces, CoordinateSpace modelCoordinateSpace, UpAxis modelUpAxis, IndexFormat indexFormat)
		{
			var facets = faces as Facet[] ?? faces.ToArray();
			int faceCount = facets.Length, f = 0;
            int maxFacetsPerMesh = indexFormat == IndexFormat.UInt32 ? MaxFacetsPerMesh32 : MaxFacetsPerMesh16;
            int maxVertexCount = maxFacetsPerMesh * 3;
			Mesh[] meshes = new Mesh[faceCount / maxFacetsPerMesh + 1];

			for(int meshIndex = 0; meshIndex < meshes.Length; meshIndex++)
			{
				int len = System.Math.Min(maxVertexCount, (faceCount - f) * 3);
				Vector3[] v = new Vector3[len];
				Vector3[] n = new Vector3[len];
				int[] t = new int[len];

				for(int it = 0; it < len; it += 3)
				{
					v[it  ] = facets[f].a;
					v[it+1] = facets[f].b;
					v[it+2] = facets[f].c;

					n[it  ] = facets[f].normal;
					n[it+1] = facets[f].normal;
					n[it+2] = facets[f].normal;

					t[it  ] = it+0;
					t[it+1] = it+1;
					t[it+2] = it+2;

					f++;
				}

				if(modelCoordinateSpace == CoordinateSpace.Right)
				{
					for(int i = 0; i < len; i+=3)
					{
						v[i+0] = Stl.ToCoordinateSpace(v[i+0], CoordinateSpace.Left);
						v[i+1] = Stl.ToCoordinateSpace(v[i+1], CoordinateSpace.Left);
						v[i+2] = Stl.ToCoordinateSpace(v[i+2], CoordinateSpace.Left);

						n[i+0] = Stl.ToCoordinateSpace(n[i+0], CoordinateSpace.Left);
						n[i+1] = Stl.ToCoordinateSpace(n[i+1], CoordinateSpace.Left);
						n[i+2] = Stl.ToCoordinateSpace(n[i+2], CoordinateSpace.Left);

						var a = t[i+2];
						t[i+2] = t[i];
						t[i] = a;
					}
				}

                meshes[meshIndex] = new Mesh
                {
                    vertices = v,
                    normals = n,
                    triangles = t,
                    indexFormat = indexFormat
                };
            }

			return meshes;
		}
	}
}
