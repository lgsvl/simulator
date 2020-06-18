/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Core.Tests.Editor.Shared.Messaging.Data
{
    using System;
    using System.Linq;
    using System.Text;
    using Core.Messaging.Data;
    using NUnit.Framework;
    using NUnit.Framework.Constraints;
    using UnityEngine;

    /// <summary>
    /// Tests for the <see cref="BytesStack"/> class
    /// </summary>
    [TestFixture]
    public class BytesStackTests
    {
        /// <summary>
        /// Example structure used for testing object serialization
        /// </summary>
        [Serializable]
        private struct SampleStructData
        {
            /// <summary>
            /// Test int value
            /// </summary>
            public int intValue;

            /// <summary>
            /// Test bool value
            /// </summary>
            public bool boolValue;

            /// <summary>
            /// Test string value
            /// </summary>
            public string stringValue;

            /// <summary>
            /// Test float value
            /// </summary>
            public float floatValue;

            /// <inheritdoc/>
            public override bool Equals(object obj)
            {
                if (obj == null)
                    return false;
                if (!(obj is SampleStructData))
                    return false;
                var second = (SampleStructData) obj;
                if (intValue != second.intValue)
                    return false;
                if (boolValue != second.boolValue)
                    return false;
                if (stringValue != second.stringValue)
                    return false;
                if (Math.Abs(floatValue - second.floatValue) > Mathf.Epsilon)
                    return false;
                return true;
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                return intValue.GetHashCode() ^ boolValue.GetHashCode() ^ stringValue.GetHashCode() ^
                       floatValue.GetHashCode();
            }
        }

        /// <summary>
        /// Example class used for testing object serialization
        /// </summary>
        [Serializable]
        private struct SampleClassData
        {
            /// <summary>
            /// Test int value
            /// </summary>
            public int intValue;

            /// <summary>
            /// Test bool value
            /// </summary>
            public bool boolValue;

            /// <summary>
            /// Test string value
            /// </summary>
            public string stringValue;

            /// <summary>
            /// Test float value
            /// </summary>
            public float floatValue;

            /// <inheritdoc/>
            public override bool Equals(object obj)
            {
                if (obj == null)
                    return false;
                if (!(obj is SampleClassData))
                    return false;
                var second = (SampleClassData) obj;
                if (intValue != second.intValue)
                    return false;
                if (boolValue != second.boolValue)
                    return false;
                if (stringValue != second.stringValue)
                    return false;
                if (Math.Abs(floatValue - second.floatValue) > Mathf.Epsilon)
                    return false;
                return true;
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                return intValue.GetHashCode() ^ boolValue.GetHashCode() ^ stringValue.GetHashCode() ^
                       floatValue.GetHashCode();
            }
        }
        
        /// <summary>
        /// Example class used for testing object serialization
        /// </summary>
        [Serializable]
        private struct SampleNestedClassData
        {
            /// <summary>
            /// Test nested structure value
            /// </summary>
            public SampleStructData nestedStruct;

            /// <summary>
            /// Test nested class value
            /// </summary>
            public SampleClassData nestedClass;

            /// <inheritdoc/>
            public override bool Equals(object obj)
            {
                if (obj == null)
                    return false;
                if (!(obj is SampleNestedClassData))
                    return false;
                var second = (SampleNestedClassData) obj;
                if (!nestedStruct.Equals(second.nestedStruct))
                    return false;
                if (!nestedClass.Equals(second.nestedClass))
                    return false;
                return true;
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                return nestedStruct.GetHashCode() ^ nestedClass.GetHashCode();
            }
        }

        /// <summary>
        /// Tests the push and pop methods using whole bytes array and single byte by byte
        /// </summary>
        /// <param name="testCase">Bytes array to be tested</param>
        [TestCase(new byte[] {255, 0, 128, 64, 32})]
        [TestCase(new byte[] { })]
        [TestCase(new byte[] {0})]
        [TestCase(new byte[] {0, 0, 0, 0})]
        public void PushPeekPopBytesTests(byte[] testCase)
        {
            var bytesStack = new BytesStack(testCase.Length);

            //Test bytes array at once
            bytesStack.PushBytes(testCase);
            var result = bytesStack.PeekBytes(testCase.Length);
            Assert.True(result.SequenceEqual(testCase),
                "Peek of the bytes array at once returns different data than was pushed.");
            result = bytesStack.PopBytes(testCase.Length);
            Assert.True(result.SequenceEqual(testCase),
                "Pop of the bytes array at once returns different data than was pushed.");

            //Test bytes array one by one
            for (var i = 0; i < testCase.Length; i++)
                bytesStack.PushByte(testCase[i]);
            for (var i = testCase.Length - 1; i >= 0; i--)
                result[i] = bytesStack.PeekByte(testCase.Length - 1 - i);
            Assert.True(result.SequenceEqual(testCase),
                "Pop of the bytes array one by one returns different data than was pushed.");
            for (var i = testCase.Length - 1; i >= 0; i--)
                result[i] = bytesStack.PopByte();
            Assert.True(result.SequenceEqual(testCase),
                "Pop of the bytes array one by one returns different data than was pushed.");
        }

        /// <summary>
        /// Tests push and pop methods for integers
        /// </summary>
        /// <param name="value">Integer value to be tested</param>
        [TestCase(4)]
        [TestCase(0)]
        [TestCase(-4)]
        [TestCase(int.MinValue)]
        [TestCase(int.MaxValue)]
        public void PushPeekPopIntTests(int value)
        {
            var bytesCount = ByteCompression.RequiredBytes(value);
            var bytesStack = new BytesStack(bytesCount);
            bytesStack.PushInt(value, bytesCount);
            var result = bytesStack.PeekInt(bytesCount);
            Assert.True(value == result, "PeekInt returns different value than was pushed.");
            result = bytesStack.PopInt(bytesCount);
            Assert.True(value == result, "PopInt returns different value than was pushed.");
            Assert.True(bytesStack.Count == 0, "BytesStack is not empty after PopInt alone integer.");
        }

        /// <summary>
        /// Tests push and pop methods for unsigned integers
        /// </summary>
        /// <param name="value">Integer value to be tested</param>
        [TestCase(4u)]
        [TestCase(0u)]
        [TestCase(uint.MinValue)]
        [TestCase(uint.MaxValue)]
        public void PushPeekPopIntTests(uint value)
        {
            var bytesCount = ByteCompression.RequiredBytes(value);
            var bytesStack = new BytesStack(bytesCount);
            bytesStack.PushUint(value, bytesCount);
            var result = bytesStack.PeekUint(bytesCount);
            Assert.True(value == result, "PeekUint returns different value than was pushed.");
            result = bytesStack.PopUint(bytesCount);
            Assert.True(value == result, "PopUint returns different value than was pushed.");
            Assert.True(bytesStack.Count == 0, "BytesStack is not empty after PopUint alone integer.");
        }

        /// <summary>
        /// Tests push and pop methods for long
        /// </summary>
        /// <param name="value">Long value to be tested</param>
        [TestCase(4)]
        [TestCase(0)]
        [TestCase(-4)]
        [TestCase(15784963903102968L)]
        [TestCase(long.MinValue)]
        [TestCase(long.MaxValue)]
        public void PushPeekPopLongTests(long value)
        {
            var bytesCount = ByteCompression.RequiredBytes(value);
            var bytesStack = new BytesStack(bytesCount);
            bytesStack.PushLong(value, bytesCount);
            var result = bytesStack.PeekLong(bytesCount);
            Assert.True(value == result, "PeekLong returns different value than was pushed.");
            result = bytesStack.PopLong(bytesCount);
            Assert.True(value == result, "PopLong returns different value than was pushed.");
            Assert.True(bytesStack.Count == 0, "BytesStack is not empty after PopLong alone long.");
        }

        /// <summary>
        /// Tests push and pop methods for floats
        /// </summary>
        /// <param name="value">Float value to be tested</param>
        [TestCase(723672.235253f)]
        [TestCase(0.0001f)]
        [TestCase(0.0f)]
        [TestCase(-0.0001f)]
        [TestCase(-234512.4231f)]
        [TestCase(float.MinValue)]
        [TestCase(float.MaxValue)]
        public void PushPeekPopFloatTests(float value)
        {
            var bytesStack = new BytesStack(4);
            bytesStack.PushFloat(value);
            var result = bytesStack.PeekFloat();
            const float tolerance = 0.00001f;
            Assert.True(Math.Abs(value - result) < tolerance, "PeekFloat returns different value than was pushed.");
            result = bytesStack.PopFloat();
            Assert.True(Math.Abs(value - result) < tolerance, "PopFloat returns different value than was pushed.");
            Assert.True(bytesStack.Count == 0, "BytesStack is not empty after PopFloat alone float.");
        }

        /// <summary>
        /// Tests push and pop methods for double
        /// </summary>
        /// <param name="value">Double value to be tested</param>
        [TestCase(723672.235253f)]
        [TestCase(0.0001f)]
        [TestCase(0.0f)]
        [TestCase(-0.0001f)]
        [TestCase(-234512.4231f)]
        [TestCase(double.MinValue)]
        [TestCase(double.MaxValue)]
        public void PushPeekPopDoubleTests(double value)
        {
            var bytesStack = new BytesStack(8);
            bytesStack.PushDouble(value);
            var result = bytesStack.PeekDouble();
            const float tolerance = 0.00001f;
            Assert.True(Math.Abs(value - result) < tolerance, "PeekDouble returns different value than was pushed.");
            result = bytesStack.PopDouble();
            Assert.True(Math.Abs(value - result) < tolerance, "PopDouble returns different value than was pushed.");
            Assert.True(bytesStack.Count == 0, "BytesStack is not empty after PopDouble alone float.");
        }

        /// <summary>
        /// Tests push and pop methods for booleans
        /// </summary>
        /// <param name="value">Bool value to be tested</param>
        [TestCase(true)]
        [TestCase(false)]
        public void PushPeekPopBoolTests(bool value)
        {
            var bytesStack = new BytesStack(1);
            bytesStack.PushBool(value);
            var result = bytesStack.PeekBool();
            Assert.True(value == result, "PeekBool returns different value than was pushed.");
            result = bytesStack.PopBool();
            Assert.True(value == result, "PopBool returns different value than was pushed.");
            Assert.True(bytesStack.Count == 0, "BytesStack is not empty after PopBool alone bool.");
        }

        /// <summary>
        /// Tests push and pop methods for strings
        /// </summary>
        /// <param name="value">String value to be tested</param>
        [TestCase("TestValue")]
        [TestCase("")]
        [TestCase(null)]
        [TestCase("TestWithSlashes//\\\\/\\")]
        [TestCase("TestWithBrackets()[]{}<>")]
        [TestCase("TestWithDigits0123456789")]
        public void PushPeekPopStringTests(string value)
        {
            PushPeekPopStringTest(value, Encoding.UTF8);
        }

        /// <summary>
        /// Tests push and pop methods for strings using given encoding
        /// </summary>
        /// <param name="value">String value to be tested</param>
        /// <param name="encoding">Used encoding</param>
        private static void PushPeekPopStringTest(string value, Encoding encoding)
        {
            var bytesStack = new BytesStack(4 + (value?.Length ?? 0));
            bytesStack.PushString(value, encoding);
            var result = bytesStack.PeekString(encoding);
            Assert.True(value == result,
                $"PeekString returns different value than was pushed when using {encoding} encoding.");
            result = bytesStack.PopString(encoding);
            Assert.True(value == result,
                $"PopString returns different value than was pushed when using {encoding} encoding.");
            Assert.True(bytesStack.Count == 0, "BytesStack is not empty after PopString alone string.");
        }

        /// <summary>
        /// Tests push and pop methods for objects
        /// </summary>
        [TestCase]
        public void PushPeekPopObjectTests()
        {
            var structSample = new SampleStructData()
            {
                intValue = int.MaxValue,
                boolValue = true,
                stringValue = "TestWithDigits0123456789",
                floatValue = float.MaxValue
            };
            PushPeekPopObjectTest(structSample);
            
            var classSample = new SampleClassData()
            {
                intValue = int.MinValue,
                boolValue = false,
                stringValue = "TestWithSlashes//\\\\/\\",
                floatValue = float.MinValue
            };
            PushPeekPopObjectTest(classSample);

            var nestedSample = new SampleNestedClassData()
            {
                nestedStruct = structSample,
                nestedClass = classSample
            };
            PushPeekPopObjectTest(nestedSample);
        }

        /// <summary>
        /// Tests push and pop methods for a single object
        /// </summary>
        /// <param name="value">Object value to be tested</param>
        public void PushPeekPopObjectTest(object value)
        {
            var bytesStack = new BytesStack(1);
            bytesStack.PushObject(value);
            var result = bytesStack.PeekObject();
            Assert.True(value.Equals(result), "PeekObject returns different value than was pushed.");
            result = bytesStack.PopObject();
            Assert.True(value.Equals(result), "PopObject returns different value than was pushed.");
            Assert.True(bytesStack.Count == 0, "BytesStack is not empty after PopObject alone bool.");
        }
    }
}