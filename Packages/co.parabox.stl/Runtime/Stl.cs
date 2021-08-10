using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Parabox.Stl
{
	/// <summary>
	/// Describes the file format of an STL file.
	/// </summary>
	public enum FileType
	{
		Ascii,
		Binary
	};

	public enum CoordinateSpace
	{
		Left,
		Right
	}

	public enum UpAxis
	{
		X,
		Y,
		Z
	}

	/// <summary>
	/// Export STL files from Unity mesh assets.
	/// </summary>
	static class Stl
	{
		public static Vector3 ToCoordinateSpace(Vector3 point, CoordinateSpace space)
		{
			if(space == CoordinateSpace.Left)
				return new Vector3(-point.y, point.z, point.x);

	    	return new Vector3(point.z, -point.x, point.y);
		}
	}
}
