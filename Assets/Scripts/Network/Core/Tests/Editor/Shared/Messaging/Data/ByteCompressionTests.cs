/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Core.Tests.Editor.Shared.Messaging.Data
{
	using Core.Messaging.Data;

	using NUnit.Framework;

	using UnityEngine;

	/// <summary>
	/// Tests for the <see cref="ByteCompression"/> clas
	/// </summary>
	[TestFixture]
	public class ByteCompressionTests
	{
		/// <summary>
		/// Tests for the <see cref="ByteCompression.RequiredBytes"/> method
		/// </summary>
		[Test]
		public void RequiredBytesTest()
		{
			var value = 0;
			Assert.True(
				ByteCompression.RequiredBytes(value) == 1,
				$"Required bytes in {typeof(ByteCompression).Name} returns invalid bytes count for value {value}.");
			value = 1;
			Assert.True(
				ByteCompression.RequiredBytes(value) == 1,
				$"Required bytes in {typeof(ByteCompression).Name} returns invalid bytes count for value {value}.");
			value = 0xFF;
			Assert.True(
				ByteCompression.RequiredBytes(value) == 1,
				$"Required bytes in {typeof(ByteCompression).Name} returns invalid bytes count for value {value}.");
			value = 0x100;
			Assert.True(
				ByteCompression.RequiredBytes(value) == 2,
				$"Required bytes in {typeof(ByteCompression).Name} returns invalid bytes count for value {value}.");
			value = 0x1FF;
			Assert.True(
				ByteCompression.RequiredBytes(value) == 2,
				$"Required bytes in {typeof(ByteCompression).Name} returns invalid bytes count for value {value}.");
			value = 0x10000;
			Assert.True(
				ByteCompression.RequiredBytes(value) == 3,
				$"Required bytes in {typeof(ByteCompression).Name} returns invalid bytes count for value {value}.");
			value = 0x1FFFF;
			Assert.True(
				ByteCompression.RequiredBytes(value) == 3,
				$"Required bytes in {typeof(ByteCompression).Name} returns invalid bytes count for value {value}.");
			value = 0x1FFFFFF;
			Assert.True(
				ByteCompression.RequiredBytes(value) == 4,
				$"Required bytes in {typeof(ByteCompression).Name} returns invalid bytes count for value {value}.");
			value = int.MaxValue;
			Assert.True(
				ByteCompression.RequiredBytes(value) == 4,
				$"Required bytes in {typeof(ByteCompression).Name} returns invalid bytes count for value {value}.");
			value = -1;
			Assert.True(
				ByteCompression.RequiredBytes(value) == 4,
				$"Required bytes in {typeof(ByteCompression).Name} returns invalid bytes count for value {value}.");
		}

		/// <summary>
		/// Tests for the <see cref="ByteCompression.CompressFloatToInt"/> and <see cref="ByteCompression.DecompressFloatFromInt"/> methods
		/// </summary>
		/// <param name="value">Float value to be tested</param>
		/// <param name="minValue">Minimum float value</param>
		/// <param name="maxValue">Maximum float value</param>
		/// <param name="bytesCount">Bytes used to encode the float as integer</param>
		[TestCase(723672.235253f, 0.0f, 999999.0f, 4)]
		[TestCase(0.0001f, 0.0f, 1.0f, 2)]
		[TestCase(1.0f, 0.0f, 1.0f, 4)]
		[TestCase(0.0f, -1.0f, 1.0f, 2)]
		[TestCase(-0.0001f, -1.0f, 1.0f, 3)]
		[TestCase(-234512.4231f, -999999.0f, 999999.0f, 4)]
		[TestCase(float.MinValue, float.MinValue, float.MaxValue, 4)]
		[TestCase(float.MaxValue, float.MinValue, float.MaxValue, 4)]
		public void FloatCompressionTests(float value, float minValue, float maxValue, int bytesCount)
		{
			var precision = (maxValue - minValue) / (1 << (bytesCount * 8));
			var bytesStack = new BytesStack(ByteCompression.PositionRequiredBytes);
			bytesStack.PushInt(ByteCompression.CompressFloatToInt(value, minValue, maxValue, bytesCount), bytesCount);
			var resultFloat = ByteCompression.DecompressFloatFromInt(
				bytesStack.PopInt(bytesCount),
				minValue,
				maxValue,
				bytesCount);
			Assert.True(
				Mathf.Abs(value - resultFloat) <= precision,
				$"Compression-decompression operation of the float value result exceeds precision. Tested float: {value}, result float: {resultFloat}, precision {precision}.");
		}

		/// <summary>
		/// Tests for the <see cref="ByteCompression.PushCompressedColor"/> and <see cref="ByteCompression.PopDecompressedColor"/> methods
		/// </summary>
		/// <param name="r">Red value of tested color</param>
		/// <param name="g">Green value of tested color</param>
		/// <param name="b">Blue value of tested color</param>
		/// <param name="a">Alpha value of tested color</param>
		/// <param name="bytesPerElement">Bytes count that will be used per each element</param>
		[TestCase(0.0f, 0.0f, 0.0f, 0.0f, 1)]
		[TestCase(1.0f, 1.0f, 1.0f, 1.0f, 1)]
		[TestCase(0.5f, 0.0f, 0.0f, 1.0f, 4)]
		[TestCase(1.0f, 0.25f, 0.5f, 0.0f, 2)]
		[TestCase(0.06346f, 0.25436f, 0.5346346f, 0.9346346f, 3)]
		public void ColorCompressionTests(float r, float g, float b, float a, int bytesPerElement)
		{
			var color = new Color(r, g, b, a);
			var precision = (1.0f - 0.0f) / (1 << (bytesPerElement * 8));
			var bytesStack = new BytesStack(ByteCompression.PositionRequiredBytes);
			bytesStack.PushCompressedColor(color, bytesPerElement);
			var resultVector3 = bytesStack.PopDecompressedColor(bytesPerElement);
			Assert.True(
				Mathf.Abs(color.r - resultVector3.r) <= precision,
				$"Compression-decompression operation of the color value R result exceeds precision. Tested color: {color}, result color: {resultVector3}.");
			Assert.True(
				Mathf.Abs(color.g - resultVector3.g) <= precision,
				$"Compression-decompression operation of the color value G result exceeds precision. Tested color: {color}, result color: {resultVector3}.");
			Assert.True(
				Mathf.Abs(color.b - resultVector3.b) <= precision,
				$"Compression-decompression operation of the color value B result exceeds precision. Tested color: {color}, result color: {resultVector3}.");
			Assert.True(
				Mathf.Abs(color.a - resultVector3.a) <= precision,
				$"Compression-decompression operation of the color value A result exceeds precision. Tested color: {color}, result color: {resultVector3}.");
		}

		/// <summary>
		/// Tests for the <see cref="ByteCompression.PushCompressedVector3"/> and <see cref="ByteCompression.PopDecompressedVector3"/> methods
		/// </summary>
		/// <param name="x">X value of the tested vector3</param>
		/// <param name="y">Y value of the tested vector3</param>
		/// <param name="z">Z value of the tested vector3</param>
		/// <param name="minElementValue">Minimal value of the encoded vector3</param>
		/// <param name="maxElementValue">Maximal value of the encoded vector3</param>
		/// <param name="bytesPerElement">Bytes count that will be used per each element</param>
		[TestCase(0.0f, 0.0f, 0.0f, -0.1f, 1.0f, 2)]
		[TestCase(1.0f, 1.0f, 1.0f, -0.1f, 1.0f, 2)]
		[TestCase(-2000.0f, -2000.0f, -2.0f, -2000.0f, 2000.0f, 3)]
		[TestCase(2000.0f, 2000.0f, 20.0f, -2000.0f, 2000.0f, 3)]
		[TestCase(1234.373f, -872.457f, 8.769f, -2000.0f, 2000.0f, 3)]
		[TestCase(-845.85484588f, 125.3463466f, 3.34646f, -2000.0f, 2000.0f, 3)]
		public static void Vector3CompressionTest(
			float x,
			float y,
			float z,
			float minElementValue,
			float maxElementValue,
			int bytesPerElement)
		{
			var vector3 = new Vector3(x, y, z);
			var precision = (maxElementValue - minElementValue) / (1 << (bytesPerElement * 8));
			var bytesStack = new BytesStack(ByteCompression.PositionRequiredBytes);
			bytesStack.PushCompressedVector3(vector3, minElementValue, maxElementValue, bytesPerElement);
			var resultVector3 = bytesStack.PopDecompressedVector3(minElementValue, maxElementValue, bytesPerElement);
			Assert.True(
				Mathf.Abs(vector3.x - resultVector3.x) <= precision,
				$"Compression-decompression operation of the vector3 value X result exceeds precision. Tested vector3: {vector3}, result vector3: {resultVector3}.");
			Assert.True(
				Mathf.Abs(vector3.y - resultVector3.y) <= precision,
				$"Compression-decompression operation of the vector3 value Y result exceeds precision. Tested vector3: {vector3}, result vector3: {resultVector3}.");
			Assert.True(
				Mathf.Abs(vector3.z - resultVector3.z) <= precision,
				$"Compression-decompression operation of the vector3 value Z result exceeds precision. Tested vector3: {vector3}, result vector3: {resultVector3}.");
		}

		/// <summary>
		/// Tests for the <see cref="ByteCompression.PushCompressedPosition"/> and <see cref="ByteCompression.PopDecompressedPosition"/> methods
		/// </summary>
		/// <param name="x">X value of the tested position</param>
		/// <param name="y">Y value of the tested position</param>
		/// <param name="z">Z value of the tested position</param>
		[TestCase(0.0f, 0.0f, 0.0f)]
		[TestCase(1.0f, 1.0f, 1.0f)]
		[TestCase(-2000.0f, -2.0f, -2000.0f)]
		[TestCase(2000.0f, 20.0f, 2000.0f)]
		[TestCase(1234.373f, -8.72457f, 876.9f)]
		[TestCase(-845.85484588f, 125.3463466f, 3.34646f)]
		public static void PositionCompressionTest(float x, float y, float z)
		{
			var defaultPositionBounds = ByteCompression.PositionBounds;
			try
			{
				var position = new Vector3(x, y, z);
				TestSinglePosition(position);
				var maxElementSize = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y), Mathf.Abs(z)) * 2;
				ByteCompression.SetPositionBounds(
					new Bounds(Vector3.zero, new Vector3(maxElementSize, maxElementSize, maxElementSize)));
				TestSinglePosition(position);
				ByteCompression.SetPositionBounds(new Bounds(position, Vector3.one));
				TestSinglePosition(position);
				ByteCompression.SetPositionBounds(
					new Bounds(
						new Vector3(-maxElementSize / 3, maxElementSize / 3, -maxElementSize / 5),
						new Vector3(2 * maxElementSize, 2 * maxElementSize, 2 * maxElementSize)));
				TestSinglePosition(position);
			}
			finally
			{
				ByteCompression.SetPositionBounds(defaultPositionBounds);
			}

		}

		/// <summary>
		/// Tests <see cref="ByteCompression.PushCompressedPosition"/> and <see cref="ByteCompression.PopDecompressedPosition"/> methods on a single position
		/// </summary>
		/// <param name="position">Position to be tested</param>
		private static void TestSinglePosition(Vector3 position)
		{
			var bytesStack = new BytesStack(ByteCompression.PositionRequiredBytes);
			bytesStack.PushCompressedPosition(position);
			var resultPosition = bytesStack.PopDecompressedPosition();
			Assert.True(
				Mathf.Abs(position.x - resultPosition.x) <= ByteCompression.PositionPrecision,
				$"Compression-decompression operation of the position value X result exceeds position precision. Tested position: {position}, result position: {resultPosition}.");
			Assert.True(
				Mathf.Abs(position.y - resultPosition.y) <= ByteCompression.PositionPrecision,
				$"Compression-decompression operation of the position value Y result exceeds position precision. Tested position: {position}, result position: {resultPosition}.");
			Assert.True(
				Mathf.Abs(position.z - resultPosition.z) <= ByteCompression.PositionPrecision,
				$"Compression-decompression operation of the position value Z result exceeds position precision. Tested position: {position}, result position: {resultPosition}.");
		}

		/// <summary>
		/// Tests for the <see cref="ByteCompression.PushCompressedPosition"/> and <see cref="ByteCompression.PopDecompressedPosition"/> methods
		/// </summary>
		/// <param name="x">X value of the tested position</param>
		/// <param name="y">Y value of the tested position</param>
		/// <param name="z">Z value of the tested position</param>
		[TestCase(0.0f, 0.0f, 0.0f)]
		[TestCase(1.0f, 1.0f, 1.0f)]
		[TestCase(-2000.0f, -2.0f, -2000.0f)]
		[TestCase(2000.0f, 20.0f, 2000.0f)]
		[TestCase(1234.373f, -8.72457f, 876.9f)]
		[TestCase(-845.85484588f, 125.3463466f, 3.34646f)]
		[TestCase(float.MaxValue, float.MinValue, 0.0f)]
		public static void UncompressedPositionTest(float x, float y, float z)
		{
			var position = new Vector3(x, y, z);
			var bytesStack = new BytesStack(3 * 4);
			bytesStack.PushUncompressedVector3(position);
			var resultPosition = bytesStack.PopUncompressedVector3();
			Assert.True(
				Mathf.Abs(position.x - resultPosition.x) <= Mathf.Epsilon,
				$"Decoding operation of the position value X result exceeds epsilon. Tested position: {position}, result position: {resultPosition}.");
			Assert.True(
				Mathf.Abs(position.y - resultPosition.y) <= Mathf.Epsilon,
				$"Decoding operation of the position value Y result exceeds epsilon. Tested position: {position}, result position: {resultPosition}.");
			Assert.True(
				Mathf.Abs(position.z - resultPosition.z) <= Mathf.Epsilon,
				$"Decoding operation of the position value Z result exceeds epsilon. Tested position: {position}, result position: {resultPosition}.");
		}

		/// <summary>
		/// Tests for the <see cref="ByteCompression.PushCompressedRotation"/> and <see cref="ByteCompression.PopDecompressedRotation"/> methods
		/// </summary>
		[Test]
		public void RotationsCompressionTests()
		{
			RotationCompressionTest(new Quaternion(1.0f, 0.0f, 0.0f, 0.0f));
			RotationCompressionTest(new Quaternion(-1.0f, 0.0f, 0.0f, 0.0f));
			RotationCompressionTest(new Quaternion(0.0f, 1.0f, 0.0f, 0.0f));
			RotationCompressionTest(new Quaternion(0.0f, -1.0f, 0.0f, 0.0f));
			RotationCompressionTest(new Quaternion(0.0f, 0.0f, 1.0f, 0.0f));
			RotationCompressionTest(new Quaternion(0.0f, 0.0f, -1.0f, 0.0f));
			RotationCompressionTest(new Quaternion(0.0f, 0.0f, 0.0f, 1.0f));
			RotationCompressionTest(new Quaternion(0.0f, 0.0f, 0.0f, -1.0f));

			RotationCompressionTest(Quaternion.Euler(0.0f, 0.0f, 0.0f));
			RotationCompressionTest(Quaternion.Euler(-15.0f, 30.0f, -45.0f));
			RotationCompressionTest(Quaternion.Euler(60.0f, -120.0f, -240.0f));
			RotationCompressionTest(Quaternion.Euler(53.532523f, 326.623623f, -3.623663f));
			RotationCompressionTest(Quaternion.Euler(-326.6326236f, 236.236623f, -233.21421421f));
		}

		/// <summary>
		/// Single compress and decompress rotation test
		/// </summary>
		/// <param name="rotation">Rotation to be tested</param>
		private static void RotationCompressionTest(Quaternion rotation)
		{
			var bytesStack = new BytesStack(ByteCompression.RotationMaxRequiredBytes);
			bytesStack.PushCompressedRotation(rotation);
			var resultRotation = bytesStack.PopDecompressedRotation();
			Assert.True(
				AreQuaternionsApproximate(rotation, resultRotation, ByteCompression.RotationPrecision),
				$"Compression-decompression operation of the rotation result exceeds rotation precision. Tested rotation: {rotation}, result rotation: {resultRotation}.");
		}

		/// <summary>
		/// Checks if quaternions are approximately equal
		/// </summary>
		/// <param name="q1">First quaternion</param>
		/// <param name="q2">Second quaternion</param>
		/// <param name="precision">Precision of the approximation, 0 requires identical quaternions</param>
		/// <returns>Are quaternions approximately equal</returns>
		private static bool AreQuaternionsApproximate(Quaternion q1, Quaternion q2, float precision)
		{
			return Mathf.Abs(Quaternion.Dot(q1, q2)) >= 1 - precision;
		}
	}
}
