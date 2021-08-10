using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Parabox.Stl
{
	public static class Exporter
	{
		/// <summary>
		/// Export a hierarchy of GameObjects to path with file type.
		/// </summary>
		/// <param name="path"></param>
		/// <param name="gameObjects"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public static bool Export(string path, GameObject[] gameObjects, FileType type)
		{
			Mesh[] meshes = CreateWorldSpaceMeshesWithTransforms(gameObjects.Select(x => x.transform).ToArray());
			bool success = false;

			if(meshes != null && meshes.Length > 0)
			{
				if(!string.IsNullOrEmpty(path))
					success = Exporter.WriteFile(path, meshes, type);
			}

			for(int i = 0; meshes != null && i < meshes.Length; i++)
				Object.DestroyImmediate(meshes[i]);

			return success;
		}

		/**
		 * Extracts a list of mesh values with their relative transformations intact.
		 */
		private static Mesh[] CreateWorldSpaceMeshesWithTransforms(IList<Transform> transforms)
		{
			if(transforms == null || transforms.Count < 1)
				return null;

			// move root node to center of selection
			Vector3 p = Vector3.zero;

			for(int i = 0; i < transforms.Count; i++)
				p += transforms[i].position;
			Vector3 mesh_center = p / (float) transforms.Count;

			GameObject root = new GameObject();
			root.name = "ROOT";
			root.transform.position = mesh_center;

			// copy all transforms to new root gameobject
			foreach(Transform t in transforms)
			{
				GameObject go = (GameObject) GameObject.Instantiate(t.gameObject);
				go.transform.SetParent(t.parent, false);
				go.transform.SetParent(root.transform, true);
			}

			// move root to 0,0,0 so mesh transformations are relative to origin
			root.transform.position = Vector3.zero;

			// create new meshes by iterating the root node and transforming vertex & normal
			// values (ignoring all other mesh attributes since STL doesn't care about them)
			List<MeshFilter> mfs = root.GetComponentsInChildren<MeshFilter>().Where(x => x.sharedMesh != null).ToList();
			int meshCount = mfs.Count;
			Mesh[] meshes = new Mesh[meshCount];

			for(int i = 0; i < meshCount; i++)
			{
				Transform t = mfs[i].transform;

				Vector3[] v = mfs[i].sharedMesh.vertices;
				Vector3[] n = mfs[i].sharedMesh.normals;

				for(int it = 0; it < v.Length; it++)
				{
					v[it] = t.TransformPoint(v[it]);
					n[it] = t.TransformDirection(n[it]);
				}

				Mesh m = new Mesh();

				m.name = mfs[i].name;
				m.vertices = v;
				m.normals = n;
				m.triangles = mfs[i].sharedMesh.triangles;

				meshes[i] = m;
			}

			// Cleanup
			GameObject.DestroyImmediate(root);

			return meshes;
		}

		/// <summary>
		/// Write a mesh file to STL format.
		/// </summary>
		/// <param name="path"></param>
		/// <param name="mesh"></param>
		/// <param name="type"></param>
		/// <param name="convertToRightHandedCoordinates"></param>
		/// <returns></returns>
		public static bool WriteFile(string path, Mesh mesh, FileType type = FileType.Ascii, bool convertToRightHandedCoordinates = true)
		{
			return WriteFile(path, new Mesh[] { mesh }, type, convertToRightHandedCoordinates);
		}

		/**
		 *	Write a collection of mesh assets to an STL file.
		 *	No transformations are performed on meshes in this method.
		 *	Eg, if you want to export a set of a meshes in a transform
		 *	hierarchy the meshes should be transformed prior to this call.
		 *
		 *	string path - Where to write the file.
		 *	IList<Mesh> meshes - The mesh assets to write.
		 *	FileType type - How to format the file (in ASCII or binary).
		 */
		public static bool WriteFile(string path, IList<Mesh> meshes, FileType type = FileType.Ascii, bool convertToRightHandedCoordinates = true)
		{
			try
			{
				switch(type)
				{
					case FileType.Binary:
					{
						// http://paulbourke.net/dataformats/stl/
						// http://www.fabbers.com/tech/STL_Format
						using (BinaryWriter writer = new BinaryWriter(File.Open(path, FileMode.Create), new ASCIIEncoding()))
						{
							// 80 byte header
							writer.Write(new byte[80]);

							uint totalTriangleCount = (uint) (meshes.Sum(x => x.triangles.Length) / 3);

							// unsigned long facet count (4 bytes)
							writer.Write( totalTriangleCount );

							foreach(Mesh mesh in meshes)
							{
								Vector3[] v = mesh.vertices;
								Vector3[] n = mesh.normals;

								if(convertToRightHandedCoordinates)
								{
									for(int i = 0, c = v.Length; i < c; i++)
									{
										v[i] = Stl.ToCoordinateSpace(v[i], CoordinateSpace.Right);
										n[i] = Stl.ToCoordinateSpace(n[i], CoordinateSpace.Right);
									}
								}

								int[] t = mesh.triangles;
								int triangleCount = t.Length;
								if(convertToRightHandedCoordinates)
									System.Array.Reverse(t);

								for(int i = 0; i < triangleCount; i += 3)
								{
									int a = t[i], b = t[i+1], c = t[i+2];

									Vector3 avg = AvgNrm(n[a], n[b], n[c]);

									writer.Write(avg.x);
									writer.Write(avg.y);
									writer.Write(avg.z);

									writer.Write(v[a].x);
									writer.Write(v[a].y);
									writer.Write(v[a].z);

									writer.Write(v[b].x);
									writer.Write(v[b].y);
									writer.Write(v[b].z);

									writer.Write(v[c].x);
									writer.Write(v[c].y);
									writer.Write(v[c].z);

									// specification says attribute byte count should be set to 0.
									writer.Write( (ushort)0 );
								}
							}
						}
					}
					break;

					default:
						string model = WriteString(meshes);
						File.WriteAllText(path, model);
						break;
				}
			}
			catch(System.Exception e)
			{
				UnityEngine.Debug.LogError(e.ToString());
				return false;
			}

			return true;
		}

		/**
		 *	Write a Unity mesh to an ASCII STL string.
		 */
		public static string WriteString(Mesh mesh, bool convertToRightHandedCoordinates = true)
		{
			return WriteString(new Mesh[] { mesh }, convertToRightHandedCoordinates);
		}

		/**
		 * Write a set of meshes to an ASCII string in STL format.
		 */
		public static string WriteString(IList<Mesh> meshes, bool convertToRightHandedCoordinates = true)
		{
			StringBuilder sb = new StringBuilder();

			string name = meshes.Count == 1 ? meshes[0].name : "Composite Mesh";

			sb.AppendLine(string.Format("solid {0}", name));

			foreach(Mesh mesh in meshes)
			{
				Vector3[] v = mesh.vertices;
				Vector3[] n = mesh.normals;
				int[] t = mesh.triangles;

				if(convertToRightHandedCoordinates)
				{
					for(int i = 0, c = v.Length; i < c; i++)
					{
						v[i] = Stl.ToCoordinateSpace(v[i], CoordinateSpace.Right);
						n[i] = Stl.ToCoordinateSpace(n[i], CoordinateSpace.Right);
					}

					System.Array.Reverse(t);
				}

				int triLen = t.Length;

				for(int i = 0; i < triLen; i+=3)
				{
					int a = t[i];
					int b = t[i+1];
					int c = t[i+2];

					Vector3 nrm = AvgNrm(n[a], n[b], n[c]);

					sb.AppendLine(string.Format("facet normal {0} {1} {2}", nrm.x, nrm.y, nrm.z));

					sb.AppendLine("outer loop");

					sb.AppendLine(string.Format("\tvertex {0} {1} {2}", v[a].x, v[a].y, v[a].z));
					sb.AppendLine(string.Format("\tvertex {0} {1} {2}", v[b].x, v[b].y, v[b].z));
					sb.AppendLine(string.Format("\tvertex {0} {1} {2}", v[c].x, v[c].y, v[c].z));

					sb.AppendLine("endloop");

					sb.AppendLine("endfacet");
				}
			}

			sb.AppendLine(string.Format("endsolid {0}", name));

			return sb.ToString();
		}

		/**
		 *	Average of 3 vectors.
		 */
		private static Vector3 AvgNrm(Vector3 a, Vector3 b, Vector3 c)
		{
			return new Vector3(
				(a.x + b.x + c.x) / 3f,
				(a.y + b.y + c.y) / 3f,
				(a.z + b.z + c.z) / 3f );
		}

	}
}
