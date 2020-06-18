/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Core.Messaging.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    /// <summary>
    /// Compression and decompression methods set used to minimize the packets sizes
    /// </summary>
    public static class ByteCompression
    {
        /// <summary>
        /// Default size of the compressed floats in bytes
        /// </summary>
        private const int DefaultBytesForCompressedFloat = 2;

        /// <summary>
        /// Default position bounds limiting the compression
        /// </summary>
        private static Bounds DefaultPositionBounds { get; } = new Bounds(new Vector3(0, 0, 0), new Vector3(4096.0f, 4096.0f, 4096.0f));

        /// <summary>
        /// Bounds for calculating position, compressing position out of bounds will cause an error
        /// </summary>
        public static Bounds PositionBounds { get; private set; } = DefaultPositionBounds;

        /// <summary>
        /// Minimum values of position (cached from bounds)
        /// </summary>
        private static Vector3 PositionMin { get; set; } = PositionBounds.min;

        /// <summary>
        /// Max values of position (cached from bounds)
        /// </summary>
        private static Vector3 PositionMax { get; set; } = PositionBounds.max;

        /// <summary>
        /// Required position precision
        /// </summary>
        public const float PositionPrecision = 0.001f;

        /// <summary>
        /// Required rotation precision
        /// </summary>
        public const float RotationPrecision = 2.0f / (1 << (DefaultBytesForCompressedFloat * 8));
        
        /// <summary>
        /// Required bytes to encode position.x maximum integer
        /// </summary>
        private static int positionXRequiredBytes;

        /// <summary>
        /// Required bytes to encode position.x maximum integer
        /// </summary>
        private static int PositionXRequiredBytes
        {
            get
            {
                if (positionXRequiredBytes == 0)
                    positionXRequiredBytes =
                        RequiredBytes(Mathf.CeilToInt((PositionMax.x - PositionMin.x) / PositionPrecision));
                return positionXRequiredBytes;
            }
        }

        /// <summary>
        /// Required bytes to encode position.y maximum integer
        /// </summary>
        private static int positionYRequiredBytes;

        /// <summary>
        /// Required bytes to encode position.y maximum integer
        /// </summary>
        private static int PositionYRequiredBytes
        {
            get
            {
                if (positionYRequiredBytes == 0)
                    positionYRequiredBytes =
                        RequiredBytes(Mathf.CeilToInt((PositionMax.y - PositionMin.y) / PositionPrecision));
                return positionYRequiredBytes;
            }
        }

        /// <summary>
        /// Required bytes to encode position.z maximum integer
        /// </summary>
        private static int positionZRequiredBytes;

        /// <summary>
        /// Required bytes to encode position.z maximum integer
        /// </summary>
        private static int PositionZRequiredBytes
        {
            get
            {
                if (positionZRequiredBytes == 0)
                    positionZRequiredBytes =
                        RequiredBytes(Mathf.CeilToInt((PositionMax.z - PositionMin.z) / PositionPrecision));
                return positionZRequiredBytes;
            }
        }

        /// <summary>
        /// Required bytes count to encode compressed position
        /// </summary>
        public static int PositionRequiredBytes =>
            PositionXRequiredBytes + PositionYRequiredBytes + PositionZRequiredBytes;

        /// <summary>
        /// Required bytes count to encode compressed rotation
        /// </summary>
        public static int RotationMaxRequiredBytes => 1 + 3 * DefaultBytesForCompressedFloat;

        /// <summary>
        /// Cached count of required bytes to encode selected enum type
        /// </summary>
        public static Dictionary<Type, int> EnumRequiredBytes = new Dictionary<Type, int>();

        /// <summary>
        /// Replaces position bounds, will change all the push and pop position results
        /// </summary>
        /// <param name="bounds">Bounds that will be set as position limit</param>
        public static void SetPositionBounds(Bounds bounds)
        {
            PositionBounds = bounds;
            positionXRequiredBytes = 0;
            positionYRequiredBytes = 0;
            positionZRequiredBytes = 0;
            PositionMin = bounds.min;
            PositionMax = bounds.max;
            Log.Info($"Position bounds changed to {bounds}.");
        }

        /// <summary>
        /// Count required bytes to encode the number, negative numbers always require whole int, zero requires 1 byte
        /// </summary>
        /// <param name="number">Number to analyze</param>
        /// <returns>Required bytes to encode the number</returns>
        public static int RequiredBytes(int number)
        {
            if (number == 0)
                return 1;
            if (number < 0)
                return sizeof(int);

            // Remove least significant byte until the value is zero
            var requiredBytes = 1;
            while ((BitConverter.IsLittleEndian ? (number >>= 8) : (number <<= 8)) != 0)
                requiredBytes++;
            return requiredBytes;
        }

        /// <summary>
        /// Count required bytes to encode the number, negative numbers always require whole long, zero requires 1 byte
        /// </summary>
        /// <param name="number">Number to analyze</param>
        /// <returns>Required bytes to encode the number</returns>
        public static int RequiredBytes(long number)
        {
            if (number == 0)
                return 1;
            if (number < 0)
                return sizeof(long);

            // Remove least significant byte until the value is zero
            var requiredBytes = 1;
            while ((BitConverter.IsLittleEndian ? (number >>= 8) : (number <<= 8)) != 0)
                requiredBytes++;
            return requiredBytes;
        }

        /// <summary>
        /// Count required bytes to encode the enum type
        /// </summary>
        /// <typeparam name="T">Enum type required to be encoded</typeparam>
        /// <returns>Required bytes to encode the enum type</returns>
        /// <exception cref="InvalidCastException">Type cannot be cast to enum</exception>
        public static int RequiredBytes<T>() where T : IComparable, IConvertible, IFormattable
        {
            var type = typeof(T);
            if (EnumRequiredBytes.TryGetValue(type, out var requiredBytes))
                return requiredBytes;

            if (!type.IsSubclassOf(typeof(Enum)))
                throw new
                    InvalidCastException
                    ("Cannot cast '" + type.FullName + "' to System.Enum.");

            requiredBytes = RequiredBytes(Enum.GetValues(type).Cast<int>().Last());
            EnumRequiredBytes.Add(type, requiredBytes);
            return requiredBytes;
        }

        /// <summary>
        /// Swap integer encoding between little and big endian
        /// </summary>
        /// <param name="value">Value to have encoding swapped</param>
        /// <returns>Value after swapping encoding</returns>
        public static int SwapEndianness(int value)
        {
            var b1 = (value) & 0xff;
            var b2 = (value >> 8) & 0xff;
            var b3 = (value >> 16) & 0xff;
            var b4 = (value >> 24) & 0xff;

            return b1 << 24 | b2 << 16 | b3 << 8 | b4;
        }

        /// <summary>
        /// Compress float to the integer 
        /// </summary>
        /// <param name="value">Value to be compressed</param>
        /// <param name="minValue">Minimum float value</param>
        /// <param name="maxValue">Maximum float value</param>
        /// <param name="bytesCount">Bytes used to encode the float as integer</param>
        /// <returns>Float value compressed to the integer</returns>
        public static int CompressFloatToInt(float value, float minValue, float maxValue, int bytesCount)
        {
            int intValue;
            //When using 4 bytes include the negative numbers
            if (bytesCount == 4)
            {
                var t = (value - minValue) / (maxValue - minValue);
                if (t < 0.5f)
                {
                    intValue = Mathf.RoundToInt((0.5f - t) * 2.0f * int.MinValue);
                    //Clamp the rounding near min-max boundaries
                    return intValue == int.MaxValue ? int.MinValue : intValue;
                }
                intValue = Mathf.RoundToInt((t - 0.5f) * 2.0f * int.MaxValue);
                //Clamp the rounding near min-max boundaries
                return intValue == int.MinValue ? int.MaxValue : intValue;
            }

            var maxIntValue = (1 << (8 * bytesCount)) - 1;
            intValue = Mathf.RoundToInt(maxIntValue * (value - minValue) / (maxValue - minValue));
            //Clamp the value after rounding
            return Mathf.Clamp(intValue, 0, maxIntValue);
        }

        /// <summary>
        /// Decompress the float from compressed integer
        /// </summary>
        /// <param name="value">Value to be decompressed</param>
        /// <param name="minValue">Minimum float value</param>
        /// <param name="maxValue">Maximum float value</param>
        /// <param name="bytesCount">Bytes used to encode the float as integer</param>
        /// <returns>Decompressed float value from the integer</returns>
        public static float DecompressFloatFromInt(int value, float minValue, float maxValue, int bytesCount)
        {
            //When using 4 bytes include the negative numbers
            if (bytesCount == 4)
            {
                if (value<0)
                    return ((float)value / int.MinValue) * (maxValue - minValue) + minValue;
                return ((float)value / int.MaxValue + 0.5f) * (maxValue - minValue) + minValue;
            }
            var maxIntValue = (1 << (8 * bytesCount)) - 1;
            return (float) value / maxIntValue * (maxValue - minValue) + minValue;
        }

        /// <summary>
        /// Pushes the enum integer value with minimum required bytes
        /// </summary>
        /// <param name="bytesStack">Buffer where enum is pushed</param>
        /// <param name="intValue">Enum value casted to integer</param>
        /// <typeparam name="T">Enum type</typeparam>
        public static void PushEnum<T>(this BytesStack bytesStack, int intValue)
            where T : IComparable, IConvertible, IFormattable
        {
            bytesStack.PushInt(intValue, RequiredBytes<T>());
        }

        /// <summary>
        /// Pops the enum value from the bytes stack
        /// </summary>
        /// <param name="bytesStack">Buffer where enum is pushed</param>
        /// <typeparam name="T">Enum type</typeparam>
        /// <returns>Enum value decompressed from the bytes stack</returns>
        public static T PopEnum<T>(this BytesStack bytesStack) where T : IComparable, IConvertible, IFormattable
        {
            var intValue = bytesStack.PopInt(RequiredBytes<T>());
            return (T) Enum.ToObject(typeof(T), intValue);
        }

        /// <summary>
        /// Compress color to integers and push it to the buffer
        /// </summary>
        /// <param name="bytesStack">Buffer where color will be pushed</param>
        /// <param name="color">Color to be compressed and pushed</param>
        /// <param name="bytesPerElement">Bytes count that will be used per each element</param>
        public static void PushCompressedColor(this BytesStack bytesStack, Color color, int bytesPerElement)
        {
            //Reverse order when writing to stack
            //Always use little-endian so compression and decompression are machines independent
            var a = CompressFloatToInt(color.a, 0.0f, 1.0f, bytesPerElement);
            if (!BitConverter.IsLittleEndian)
                a = SwapEndianness(a);
            bytesStack.PushInt(a, bytesPerElement);
            
            var b = CompressFloatToInt(color.b, 0.0f, 1.0f, bytesPerElement);
            if (!BitConverter.IsLittleEndian)
                b = SwapEndianness(b);
            bytesStack.PushInt(b, bytesPerElement);

            var g = CompressFloatToInt(color.g, 0.0f, 1.0f, bytesPerElement);
            if (!BitConverter.IsLittleEndian)
                g = SwapEndianness(g);
            bytesStack.PushInt(g, bytesPerElement);

            var r = CompressFloatToInt(color.r, 0.0f, 1.0f, bytesPerElement);
            if (!BitConverter.IsLittleEndian)
                r = SwapEndianness(r);
            bytesStack.PushInt(r, bytesPerElement);
        }

        /// <summary>
        /// Decompress color from the integers in the buffer
        /// </summary>
        /// <param name="bytesStack">Buffer where color is pushed</param>
        /// <param name="bytesPerElement">Bytes count that will be used per each element</param>
        /// <returns>Decompressed vector3</returns>
        public static Color PopDecompressedColor(this BytesStack bytesStack, int bytesPerElement)
        {
            //Always use little-endian so compression and decompression are machines independent
            var intR = bytesStack.PopInt(bytesPerElement);
            var intG = bytesStack.PopInt(bytesPerElement);
            var intB = bytesStack.PopInt(bytesPerElement);
            var intA = bytesStack.PopInt(bytesPerElement);

            return BitConverter.IsLittleEndian
                ? new Color(DecompressFloatFromInt(intR, 0.0f, 1.0f, bytesPerElement),
                    DecompressFloatFromInt(intG, 0.0f, 1.0f, bytesPerElement),
                    DecompressFloatFromInt(intB, 0.0f, 1.0f, bytesPerElement),
                    DecompressFloatFromInt(intA, 0.0f, 1.0f, bytesPerElement))
                : new Color(
                    DecompressFloatFromInt(SwapEndianness(intR), 0.0f, 1.0f, bytesPerElement),
                    DecompressFloatFromInt(SwapEndianness(intG), 0.0f, 1.0f, bytesPerElement),
                    DecompressFloatFromInt(SwapEndianness(intB), 0.0f, 1.0f, bytesPerElement),
                    DecompressFloatFromInt(SwapEndianness(intA), 0.0f, 1.0f, bytesPerElement));
        }

        /// <summary>
        /// Compress vector3 to integers and push it to the buffer
        /// </summary>
        /// <param name="bytesStack">Buffer where vector3 will be pushed</param>
        /// <param name="vector3">Vector3 to be compressed and pushed</param>
        /// <param name="minElementValue">Minimal value of the encoded vector3</param>
        /// <param name="maxElementValue">Maximal value of the encoded vector3</param>
        /// <param name="bytesPerElement">Bytes count that will be used per each element</param>
        public static void PushCompressedVector3(this BytesStack bytesStack, Vector3 vector3, float minElementValue,
            float maxElementValue, int bytesPerElement)
        {
            //Reverse order when writing to stack
            //Always use little-endian so compression and decompression are machines independent
            var z = CompressFloatToInt(vector3.z, minElementValue, maxElementValue, bytesPerElement);
            if (!BitConverter.IsLittleEndian)
                z = SwapEndianness(z);
            bytesStack.PushInt(z, bytesPerElement);

            var y = CompressFloatToInt(vector3.y, minElementValue, maxElementValue, bytesPerElement);
            if (!BitConverter.IsLittleEndian)
                y = SwapEndianness(y);
            bytesStack.PushInt(y, bytesPerElement);

            var x = CompressFloatToInt(vector3.x, minElementValue, maxElementValue, bytesPerElement);
            if (!BitConverter.IsLittleEndian)
                x = SwapEndianness(x);
            bytesStack.PushInt(x, bytesPerElement);
        }

        /// <summary>
        /// Decompress vector3 from the integers in the buffer
        /// </summary>
        /// <param name="bytesStack">Buffer where vector3 is pushed</param>
        /// <param name="minElementValue">Minimal value of the encoded vector3</param>
        /// <param name="maxElementValue">Maximal value of the encoded vector3</param>
        /// <param name="bytesPerElement">Bytes count that will be used per each element</param>
        /// <returns>Decompressed vector3</returns>
        public static Vector3 PopDecompressedVector3(this BytesStack bytesStack, float minElementValue,
            float maxElementValue, int bytesPerElement)
        {
            //Always use little-endian so compression and decompression are machines independent
            var intX = bytesStack.PopInt(bytesPerElement);
            var intY = bytesStack.PopInt(bytesPerElement);
            var intZ = bytesStack.PopInt(bytesPerElement);

            return BitConverter.IsLittleEndian
                ? new Vector3(DecompressFloatFromInt(intX, minElementValue, maxElementValue, bytesPerElement),
                    DecompressFloatFromInt(intY, minElementValue, maxElementValue, bytesPerElement),
                    DecompressFloatFromInt(intZ, minElementValue, maxElementValue, bytesPerElement))
                : new Vector3(
                    DecompressFloatFromInt(SwapEndianness(intX), minElementValue, maxElementValue, bytesPerElement),
                    DecompressFloatFromInt(SwapEndianness(intY), minElementValue, maxElementValue, bytesPerElement),
                    DecompressFloatFromInt(SwapEndianness(intZ), minElementValue, maxElementValue, bytesPerElement));
        }

        /// <summary>
        /// Pushes uncompressed vector3 to the buffer
        /// </summary>
        /// <param name="bytesStack">Buffer where vector3 will be pushed</param>
        /// <param name="vector3">Vector3 to be pushed</param>
        public static void PushUncompressedVector3(this BytesStack bytesStack, Vector3 vector3)
        {
            //Reverse order when writing to stack
            //Always use little-endian so compression and decompression are machines independent
            bytesStack.PushFloat(vector3.z);
            bytesStack.PushFloat(vector3.y);
            bytesStack.PushFloat(vector3.x);
        }

        /// <summary>
        /// Pops uncompressed vector3 from the buffer
        /// </summary>
        /// <param name="bytesStack">Buffer where vector3 is pushed</param>
        /// <returns>Decoded vector3</returns>
        public static Vector3 PopUncompressedVector3(this BytesStack bytesStack)
        {
            return new Vector3(bytesStack.PopFloat(), bytesStack.PopFloat(), bytesStack.PopFloat());
        }

        /// <summary>
        /// Compress a position.x value to integer
        /// </summary>
        /// <param name="x">Position.x value</param>
        /// <returns>Compressed position.x value to the integer</returns>
        /// <exception cref="ArgumentException">Position.x value exceeds preset min and max bounds</exception>
        private static int CompressPositionX(float x)
        {
            if (x < PositionMin.x-PositionPrecision || x > PositionMax.x+PositionPrecision)
                throw new ArgumentException(
                    $"Position X value {x} to be compressed exceeds preset min and max bounds <{PositionMin.x},{PositionMax.x}>.");
            //Snap to position bounds, if the difference is smaller than precision
            if (x < PositionMin.x)
                x = PositionMin.x;
            if (x > PositionMax.x)
                x = PositionMax.x;
            return CompressFloatToInt(x, PositionMin.x, PositionMax.x, PositionXRequiredBytes);
        }

        /// <summary>
        /// Decompress a position.x value from the integer
        /// </summary>
        /// <param name="x">Position.x value compressed to the integer</param>
        /// <returns>Decompressed position.x value from the integer</returns>
        /// <exception cref="ArgumentException">Position.x value exceeds 0 and max integer value bounds</exception>
        private static float DecompressPositionX(int x)
        {
            var maxIntValue = (1 << (8 * PositionXRequiredBytes)) - 1;
            if (x < 0 || x > maxIntValue)
                throw new ArgumentException(
                    $"Position X value {x} to be decompressed exceeds preset min and max bounds for integer <0,{maxIntValue}>.");
            return DecompressFloatFromInt(x, PositionMin.x, PositionMax.x, PositionXRequiredBytes);
        }

        /// <summary>
        /// Compress a position.y value to integer
        /// </summary>
        /// <param name="y">Position.y value</param>
        /// <returns>Compressed position.y value to the integer</returns>
        /// <exception cref="ArgumentException">Position.y value exceeds preset min and max bounds</exception>
        private static int CompressPositionY(float y)
        {
            if (y < PositionMin.y || y > PositionMax.y)
                throw new ArgumentException(
                    $"Position Y value {y} to be compressed exceeds preset min and max bounds <{PositionMin.y},{PositionMax.y}>.");
            //Snap to position bounds, if the difference is smaller than precision
            if (y < PositionMin.y)
                y = PositionMin.y;
            if (y > PositionMax.y)
                y = PositionMax.y;
            return CompressFloatToInt(y, PositionMin.y, PositionMax.y, PositionYRequiredBytes);
        }

        /// <summary>
        /// Decompress a position.y value from the integer
        /// </summary>
        /// <param name="y">Position.y value compressed to the integer</param>
        /// <returns>Decompressed position.y value from the integer</returns>
        /// <exception cref="ArgumentException">Position.y value exceeds 0 and max integer value bounds</exception>
        private static float DecompressPositionY(int y)
        {
            var maxIntValue = (1 << (8 * PositionYRequiredBytes)) - 1;
            if (y < 0 || y > maxIntValue)
                throw new ArgumentException(
                    $"Position Y value {y} to be decompressed exceeds preset min and max bounds for integer <0,{maxIntValue}>.");
            return DecompressFloatFromInt(y, PositionMin.y, PositionMax.y, PositionYRequiredBytes);
        }

        /// <summary>
        /// Compress a position.z value to integer
        /// </summary>
        /// <param name="z">Position.z value</param>
        /// <returns>Compressed position.z value to the integer</returns>
        /// <exception cref="ArgumentException">Position.z value exceeds preset min and max bounds</exception>
        private static int CompressPositionZ(float z)
        {
            if (z < PositionMin.z || z > PositionMax.z)
                throw new ArgumentException(
                    $"Position Z value {z} to be compressed exceeds preset min and max bounds <{PositionMin.z},{PositionMax.z}>.");
            //Snap to position bounds, if the difference is smaller than precision
            if (z < PositionMin.z)
                z = PositionMin.z;
            if (z > PositionMax.z)
                z = PositionMax.z;
            return CompressFloatToInt(z, PositionMin.z, PositionMax.z, PositionZRequiredBytes);
        }

        /// <summary>
        /// Decompress a position.z value from the integer
        /// </summary>
        /// <param name="z">Position.z value compressed to the integer</param>
        /// <returns>Decompressed position.z value from the integer</returns>
        /// <exception cref="ArgumentException">Position.z value exceeds 0 and max integer value bounds</exception>
        private static float DecompressPositionZ(int z)
        {
            var maxIntValue = (1 << (8 * PositionZRequiredBytes)) - 1;
            if (z < 0 || z > maxIntValue)
                throw new ArgumentException(
                    $"Position Z value {z} to be decompressed exceeds preset min and max bounds for integer <0,{maxIntValue}>.");
            return DecompressFloatFromInt(z, PositionMin.z, PositionMax.z, PositionZRequiredBytes);
        }

        /// <summary>
        /// Compress position to integers and push it to the buffer, required bytes count: <see cref="PositionRequiredBytes"/>
        /// </summary>
        /// <param name="bytesStack">Buffer where position will be pushed</param>
        /// <param name="position">Position to be compressed and pushed</param>
        public static void PushCompressedPosition(this BytesStack bytesStack, Vector3 position)
        {
            //Reverse order when writing to stack
            //Always use little-endian so compression and decompression are machines independent
            var z = CompressPositionZ(position.z);
            if (!BitConverter.IsLittleEndian)
                z = SwapEndianness(z);
            bytesStack.PushInt(z, PositionZRequiredBytes);

            var y = CompressPositionY(position.y);
            if (!BitConverter.IsLittleEndian)
                y = SwapEndianness(y);
            bytesStack.PushInt(y, PositionYRequiredBytes);

            var x = CompressPositionX(position.x);
            if (!BitConverter.IsLittleEndian)
                x = SwapEndianness(x);
            bytesStack.PushInt(x, PositionXRequiredBytes);
        }

        /// <summary>
        /// Decompress position from the integers in the buffer, required bytes count: <see cref="PositionRequiredBytes"/>
        /// </summary>
        /// <param name="bytesStack">Buffer where position is pushed</param>
        /// <returns>Decompressed position</returns>
        public static Vector3 PopDecompressedPosition(this BytesStack bytesStack)
        {
            //Always use little-endian so compression and decompression are machines independent
            var intX = bytesStack.PopInt(PositionXRequiredBytes);
            var intY = bytesStack.PopInt(PositionYRequiredBytes);
            var intZ = bytesStack.PopInt(PositionZRequiredBytes);

            return BitConverter.IsLittleEndian
                ? new Vector3(DecompressPositionX(intX), DecompressPositionY(intY), DecompressPositionZ(intZ))
                : new Vector3(DecompressPositionX(SwapEndianness(intX)), DecompressPositionY(SwapEndianness(intY)),
                    DecompressPositionZ(SwapEndianness(intZ)));
        }

        /// <summary>
        /// Compress rotation to integers and push it to the buffer, required bytes count: <see cref="RotationPrecision"/>
        /// </summary>
        /// <param name="bytesStack">Buffer where rotation will be pushed</param>
        /// <param name="rotation">Rotation to be compressed and pushed</param>
        public static void PushCompressedRotation(this BytesStack bytesStack, Quaternion rotation)
        {
            //Algorithm based on the "smallest three" method described at:
            //http://gafferongames.com/networked-physics/snapshot-compression/
            var maxIndex = (byte) 0;
            var maxValue = float.MinValue;
            var sign = 1f;

            // Find the maximum element in the quaternion
            for (var i = 0; i < 4; i++)
            {
                var element = rotation[i];
                var abs = Mathf.Abs(rotation[i]);
                if (!(abs > maxValue)) continue;
                // Maximum element is always compressed as positive, all other elements are negated if needed
                sign = (element < 0) ? -1 : 1;
                maxIndex = (byte) i;
                maxValue = abs;
            }

            // If the maximum element is approximately equal to 1.0f all other elements are approximately equal to 0.0f and does not have to be encoded
            if (Mathf.Approximately(maxValue, 1.0f))
            {
                //Use 4-7 indexes to determine which element is maximum and it is approximately equal to 1.0f
                bytesStack.PushInt(maxIndex + 4, 1);
                return;
            }

            //Reverse order when writing to stack
            // Compress and encode only smallest three Quaternion components as little endian integers
            if (maxIndex != 3)
            {
                var w = CompressFloatToInt(rotation.w * sign, -1.0f, 1.0f, DefaultBytesForCompressedFloat);
                if (!BitConverter.IsLittleEndian)
                    w = SwapEndianness(w);
                bytesStack.PushInt(w, DefaultBytesForCompressedFloat);
            }

            if (maxIndex != 2)
            {
                var z = CompressFloatToInt(rotation.z * sign, -1.0f, 1.0f, DefaultBytesForCompressedFloat);
                if (!BitConverter.IsLittleEndian)
                    z = SwapEndianness(z);
                bytesStack.PushInt(z, DefaultBytesForCompressedFloat);
            }

            if (maxIndex != 1)
            {
                var y = CompressFloatToInt(rotation.y * sign, -1.0f, 1.0f, DefaultBytesForCompressedFloat);
                if (!BitConverter.IsLittleEndian)
                    y = SwapEndianness(y);
                bytesStack.PushInt(y, DefaultBytesForCompressedFloat);
            }

            if (maxIndex != 0)
            {
                var x = CompressFloatToInt(rotation.x * sign, -1.0f, 1.0f, DefaultBytesForCompressedFloat);
                if (!BitConverter.IsLittleEndian)
                    x = SwapEndianness(x);
                bytesStack.PushInt(x, DefaultBytesForCompressedFloat);
            }

            bytesStack.PushInt(BitConverter.IsLittleEndian ? maxIndex : SwapEndianness(maxIndex), 1);
        }

        /// <summary>
        /// Decompress rotation from the integers in the buffer, required bytes count: <see cref="RotationPrecision"/>
        /// </summary>
        /// <param name="bytesStack">Buffer where rotation is pushed</param>
        /// <returns>Decompressed rotation</returns>
        public static Quaternion PopDecompressedRotation(this BytesStack bytesStack)
        {
            // Read the index of the maximum element
            var maxIndex = bytesStack.PopByte();

            // Indexed 4-7 determine that maximum element is approximately equal to 1.0f and other elements are not encoded
            // Other elements are approximately equal to 0.0f;
            if (maxIndex >= 4 && maxIndex <= 7)
            {
                var x = (maxIndex == 4) ? 1f : 0f;
                var y = (maxIndex == 5) ? 1f : 0f;
                var z = (maxIndex == 6) ? 1f : 0f;
                var w = (maxIndex == 7) ? 1f : 0f;

                return new Quaternion(x, y, z, w);
            }

            // Read and decompress the "smallest three" values
            var a = DecompressFloatFromInt(bytesStack.PopInt(DefaultBytesForCompressedFloat),
                -1.0f, 1.0f, DefaultBytesForCompressedFloat);
            var b = DecompressFloatFromInt(bytesStack.PopInt(DefaultBytesForCompressedFloat),
                -1.0f, 1.0f, DefaultBytesForCompressedFloat);
            var c = DecompressFloatFromInt(bytesStack.PopInt(DefaultBytesForCompressedFloat),
                -1.0f, 1.0f, DefaultBytesForCompressedFloat);
            // Count the maximum value
            var d = Mathf.Sqrt(1f - (a * a + b * b + c * c));

            // Reconstruct the quaternion from its elements
            if (maxIndex == 0)
                return new Quaternion(d, a, b, c);
            if (maxIndex == 1)
                return new Quaternion(a, d, b, c);
            if (maxIndex == 2)
                return new Quaternion(a, b, d, c);
            return new Quaternion(a, b, c, d);
        }
    }
}