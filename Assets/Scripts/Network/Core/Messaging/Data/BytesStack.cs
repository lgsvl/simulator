/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Core.Messaging.Data
{
    using System;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Text;

    using UnityEngine;

    /// <summary>
    /// Managed bytes stack (LIFO) for sequential push and pop various data
    /// </summary>
    [Serializable]
    public class BytesStack
    {
        /// <summary>
        /// Default initial size of the data buffer
        /// </summary>
        private const int InitialSize = 32;

        /// <summary>
        /// The data buffer
        /// </summary>
        [SerializeField]
        private byte[] data;

        /// <summary>
        /// Current position on the buffer
        /// </summary>
        [SerializeField]
        private int position;

        /// <summary>
        /// Count of bytes current pushed in the stack
        /// </summary>
        public int Count => position;

        /// <summary>
        /// Current size of the stack data, automatically resized if needed
        /// </summary>
        public int StackSize => data.Length;

        /// <summary>
        /// The data buffer
        /// </summary>
        public byte[] RawData
        {
            get => data;
            private set => data = value;
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public BytesStack() : this(InitialSize) { }

        /// <summary>
        /// Constructor with initial data
        /// </summary>
        /// <param name="initialData">Initial data that will be pushed to the stack</param>
        /// <param name="copy">Should the data be copied</param>
        public BytesStack(byte[] initialData, bool copy)
        {
            if (copy)
            {
                data = new byte[initialData.Length];
                PushBytes(initialData);
            }
            else
            {
                data = initialData;
                position = initialData.Length;
            }
        }

        /// <summary>
        /// Constructor with initial buffer size
        /// </summary>
        /// <param name="initialSize">Initial buffer size</param>
        public BytesStack(int initialSize)
        {
            data = new byte[initialSize];
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="original">Original stack from where data will be copied</param>
        public BytesStack(BytesStack original) : this(original.GetDataCopy(), false) { }

        /// <summary>
        /// Resizes buffer if required size is greater than current size
        /// </summary>
        /// <param name="requiredSize">Required size of the buffer</param>
        public void ResizeIfRequired(int requiredSize)
        {
            var len = data.Length;
            if (len >= requiredSize) return;

            while (len < requiredSize) len *= 2;
            Array.Resize(ref data, len);
        }

        /// <summary>
        /// Resets stack pointer without removing the buffer
        /// </summary>
        public void Reset()
        {
            position = 0;
        }

        /// <summary>
        /// Returns a copy of the data
        /// </summary>
        /// <returns>Copy of the data</returns>
        public byte[] GetDataCopy()
        {
            var copy = new byte[position];
            Buffer.BlockCopy(data, 0, copy, 0, position);
            return copy;
        }

        /// <summary>
        /// Push bytes array data on top of the stack
        /// </summary>
        /// <param name="addedData">Data that will be added on top of the stack</param>
        public void PushBytes(byte[] addedData)
        {
            ResizeIfRequired(position + addedData.Length);
            Buffer.BlockCopy(addedData, 0, data, position, addedData.Length);
            position += addedData.Length;
        }

        /// <summary>
        /// Push bytes array data on top of the stack
        /// </summary>
        /// <param name="addedData">Data that will be added on top of the stack</param>
        /// <param name="dataPosition">Data position in the array</param>
        /// <param name="count">Bytes count in the array</param>
        public void PushBytes(byte[] addedData, int dataPosition, int count)
        {
            ResizeIfRequired(position + count);
            Buffer.BlockCopy(addedData, dataPosition, data, position, count);
            position += count;
        }

        /// <summary>
        /// Push single byte on top of the stack
        /// </summary>
        /// <param name="addedData">Data that will be added on top of the stack</param>
        public void PushByte(byte addedData)
        {
            ResizeIfRequired(position + 1);
            data[position++] = addedData;
        }

        /// <summary>
        /// Get copy of the data from top of the stack without moving the stack pointer
        /// </summary>
        /// <param name="bytesCount">Bytes count that will be returned</param>
        /// <param name="offset">Offset from the stack top </param>
        /// <returns>Copy of the data from top of the stack</returns>
        /// <exception cref="ArgumentException">Peek call exceeds the bytes stack size</exception>
        public byte[] PeekBytes(int bytesCount, int offset = 0)
        {
            if (bytesCount > Count) throw new IndexOutOfRangeException("Peek call exceeds the bytes stack size.");

            var result = new byte[bytesCount];
            for (var i = 0; i < bytesCount; i++) result[i] = data[position - bytesCount - offset + i];
            return result;
        }

        /// <summary>
        /// Get copy of the data from top of the stack while moving the stack pointer
        /// </summary>
        /// <param name="bytesCount">Bytes count that will be returned</param>
        /// <returns>Copy of the data from top of the stack</returns>
        public byte[] PopBytes(int bytesCount)
        {
            var result = PeekBytes(bytesCount);
            position -= bytesCount;
            return result;
        }

        /// <summary>
        /// Returns a single byte from top of the stack while moving the stack pointer
        /// </summary>
        /// <returns>Single byte from top of the stack</returns>
        public byte PopByte()
        {
            if (position <= 0) throw new IndexOutOfRangeException("Stack is empty.");

            return data[--position];
        }

        /// <summary>
        /// Returns a single byte from top of the stack while moving the stack pointer
        /// </summary>
        /// <param name="offset">Offset from the stack top </param>
        /// <returns>Single byte from top of the stack</returns>
        public byte PeekByte(int offset)
        {
            if (position <= 0) throw new IndexOutOfRangeException("Stack is empty.");

            return data[position - 1 - offset];
        }

        /// <summary>
        /// Push value as little endian to the buffer using given bytes count
        /// </summary>
        /// <param name="value">Value to be pushed, must use little endian encoding</param>
        /// <param name="bytesCount">Bytes count used to push this data</param>
        public void PushInt(int value, int bytesCount = 4)
        {
            PushByte((byte)(value));
            if (bytesCount <= 1) return;

            PushByte((byte)(value >> 8));
            if (bytesCount <= 2) return;

            PushByte((byte)(value >> 16));

            if (bytesCount > 3) PushByte((byte)(value >> 24));
        }

        /// <summary>
        /// Pop value encoded as little endian from the buffer using given bytes count
        /// </summary>
        /// <param name="bytesCount">Bytes count data uses in the buffer</param>
        /// <returns>Value returned from the buffer</returns>
        public int PopInt(int bytesCount = 4)
        {
            var result = 0;
            for (var i = 0; i < bytesCount; i++)
            {
                result <<= 8;
                result |= PopByte();
            }

            return result;
        }

        /// <summary>
        /// Peek value encoded as little endian from the buffer using given bytes count
        /// </summary>
        /// <param name="bytesCount">Bytes count data uses in the buffer</param>
        /// <param name="offset">Offset from the stack top</param>
        /// <returns>Value returned from the buffer</returns>
        public int PeekInt(int bytesCount = 4, int offset = 0)
        {
            var result = 0;
            for (var i = 0; i < bytesCount; i++)
            {
                result <<= 8;
                result |= data[position - 1 - offset - i];
            }

            return result;
        }

        /// <summary>
        /// Push value as little endian to the buffer using given bytes count
        /// </summary>
        /// <param name="value">Value to be pushed, must use little endian encoding</param>
        /// <param name="bytesCount">Bytes count used to push this data</param>
        public void PushUint(uint value, int bytesCount = 4)
        {
            PushByte((byte)(value));
            if (bytesCount <= 1) return;

            PushByte((byte)(value >> 8));
            if (bytesCount <= 2) return;

            PushByte((byte)(value >> 16));
            if (bytesCount > 3) PushByte((byte)(value >> 24));
        }

        /// <summary>
        /// Pop value encoded as little endian from the buffer using given bytes count
        /// </summary>
        /// <param name="bytesCount">Bytes count data uses in the buffer</param>
        /// <returns>Value returned from the buffer</returns>
        public uint PopUint(int bytesCount = 4)
        {
            uint result = 0;
            for (var i = 0; i < bytesCount; i++)
            {
                result <<= 8;
                result |= PopByte();
            }

            return result;
        }

        /// <summary>
        /// Peek value encoded as little endian from the buffer using given bytes count
        /// </summary>
        /// <param name="bytesCount">Bytes count data uses in the buffer</param>
        /// <param name="offset">Offset from the stack top</param>
        /// <returns>Value returned from the buffer</returns>
        public uint PeekUint(int bytesCount = 4, int offset = 0)
        {
            uint result = 0;
            for (var i = 0; i < bytesCount; i++)
            {
                result <<= 8;
                result |= data[position - 1 - offset - i];
            }

            return result;
        }

        /// <summary>
        /// Push value as little endian to the buffer using given bytes count
        /// </summary>
        /// <param name="value">Value to be pushed, must use little endian encoding</param>
        /// <param name="bytesCount">Bytes count used to push this data</param>
        public void PushLong(long value, int bytesCount = 8)
        {
            ResizeIfRequired(position + bytesCount);
            PushByte((byte)(value));
            while (--bytesCount > 0)
                data[position++] = (byte)(value >>= 8);
        }

        /// <summary>
        /// Pop value encoded as little endian from the buffer using given bytes count
        /// </summary>
        /// <param name="bytesCount">Bytes count data uses in the buffer</param>
        /// <returns>Value returned from the buffer</returns>
        public long PopLong(int bytesCount = 8)
        {
            var result = 0L;
            for (var i = 0; i < bytesCount; i++)
            {
                result <<= 8;
                result |= PopByte();
            }

            return result;
        }

        /// <summary>
        /// Peek value encoded as little endian from the buffer using given bytes count
        /// </summary>
        /// <param name="bytesCount">Bytes count data uses in the buffer</param>
        /// <param name="offset">Offset from the stack top</param>
        /// <returns>Value returned from the buffer</returns>
        public long PeekLong(int bytesCount = 8, int offset = 0)
        {
            var result = 0L;
            for (var i = 0; i < bytesCount; i++)
            {
                result <<= 8;
                result |= data[position - 1 - offset - i];
            }

            return result;
        }

        /// <summary>
        /// Push value encoded by <see cref="BitConverter"/> to the buffer
        /// </summary>
        /// <param name="value">Value to be pushed</param>
        public void PushFloat(float value)
        {
            unsafe
            {
                PushInt(*(int*) &value, 8);
            }
        }

        /// <summary>
        /// Pop value encoded by <see cref="BitConverter"/> from the buffer
        /// </summary>
        /// <returns>Value returned from the buffer</returns>
        public float PopFloat()
        {
            position -= 4;
            return BitConverter.ToSingle(data, position);
        }

        /// <summary>
        /// Peek value encoded by <see cref="BitConverter"/> from the buffer
        /// </summary>
        /// <param name="offset">Offset from the stack top</param>
        /// <returns>Value returned from the buffer</returns>
        public float PeekFloat(int offset = 0)
        {
            return BitConverter.ToSingle(data, position - 4 - offset);
        }

        /// <summary>
        /// Push value encoded by <see cref="BitConverter"/> to the buffer
        /// </summary>
        /// <param name="value">Value to be pushed</param>
        public void PushDouble(double value)
        {
            unsafe
            {
                PushLong(*(long*) &value, 8);
            }
        }

        /// <summary>
        /// Pop value encoded by <see cref="BitConverter"/> from the buffer
        /// </summary>
        /// <returns>Value returned from the buffer</returns>
        public double PopDouble()
        {
            position -= 8;
            return BitConverter.ToDouble(data, position);
        }

        /// <summary>
        /// Peek value encoded by <see cref="BitConverter"/> from the buffer
        /// </summary>
        /// <param name="offset">Offset from the stack top</param>
        /// <returns>Value returned from the buffer</returns>
        public double PeekDouble(int offset = 0)
        {
            return BitConverter.ToDouble(data, position - 8 - offset);
        }

        /// <summary>
        /// Push bool value to the buffer
        /// </summary>
        /// <param name="value">Bool value to be pushed</param>
        public void PushBool(bool value)
        {
            PushInt(value ? 1 : 0, 1);
        }

        /// <summary>
        /// Pop bool value from the buffer
        /// </summary>
        /// <returns>Bool value returned from the buffer</returns>
        public bool PopBool()
        {
            return PopInt(1) != 0;
        }

        /// <summary>
        /// Peek bool value from the buffer
        /// </summary>
        /// <param name="offset">Offset from the stack top</param>
        /// <returns>Value returned from the buffer</returns>
        public bool PeekBool(int offset = 0)
        {
            return PeekInt(1, offset) != 0;
        }

        /// <summary>
        /// Push value to the buffer, uses UTF8 encoding
        /// </summary>
        /// <param name="value">Value to be pushed</param>
        public void PushString(string value)
        {
            PushString(value, Encoding.UTF8);
        }

        /// <summary>
        /// Push value to the buffer
        /// </summary>
        /// <param name="value">Value to be pushed</param>
        /// <param name="encoding">Encoding used for this string</param>
        public void PushString(string value, Encoding encoding)
        {
            if (value == null)
            {
                PushInt(-1);
                return;
            }

            if (string.IsNullOrEmpty(value))
            {
                PushInt(0);
                return;
            }

            PushBytes(encoding.GetBytes(value));
            PushInt(value.Length);
        }

        /// <summary>
        /// Pop string from the buffer, uses UTF8 encoding
        /// </summary>
        /// <returns>String decoded from the stack</returns>
        public string PopString()
        {
            return PopString(Encoding.UTF8);
        }

        /// <summary>
        /// Pop string from the buffer
        /// </summary>
        /// <param name="encoding">Encoding used for this string</param>
        /// <returns>String decoded from the stack</returns>
        public string PopString(Encoding encoding)
        {
            var length = PopInt();
            if (length > position) throw new IndexOutOfRangeException("Cannot decode string from the stack.");

            switch (length)
            {
                case -1: return null;
                case 0: return "";
                default:
                    position -= length;
                    return encoding.GetString(data, position, length);
            }
        }

        /// <summary>
        /// Peek string from the buffer, uses UTF8 encoding
        /// </summary>
        /// <param name="offset">Offset from the stack top </param>
        /// <returns>String decoded from the stack</returns>
        public string PeekString(int offset = 0)
        {
            return PeekString(Encoding.UTF8, offset);
        }

        /// <summary>
        /// Peek string from the buffer
        /// </summary>
        /// <param name="encoding">Encoding used for this string</param>
        /// <param name="offset">Offset from the stack top </param>
        /// <returns>String decoded from the stack</returns>
        public string PeekString(Encoding encoding, int offset = 0)
        {
            var length = PeekInt(4, offset);
            if (length > position - 4 - offset)
                throw new IndexOutOfRangeException("Cannot decode string from the stack.");

            switch (length)
            {
                case -1: return null;
                case 0: return "";
                default: return encoding.GetString(data, position - 4 - offset - length, length);
            }
        }

        /// <summary>
        /// Push object value to the buffer, method uses performance heavy serializer!
        /// </summary>
        /// <param name="value">Object value to be pushed</param>
        public void PushObject(object value)
        {
            if(value == null)
                return;
            var bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, value);
                var array = ms.ToArray();
                PushBytes(array);
                PushInt(array.Length);
            }
        }

        /// <summary>
        /// Pop object value from the buffer, method uses performance heavy serializer!
        /// </summary>
        /// <returns>Object value returned from the buffer</returns>
        public object PopObject()
        {
            var bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                var arrBytes = PopBytes(PopInt());
                ms.Write(arrBytes, 0, arrBytes.Length);
                ms.Seek(0, SeekOrigin.Begin);
                var obj = bf.Deserialize(ms);

                return obj;
            }
        }

        /// <summary>
        /// Peek object value from the buffer
        /// </summary>
        /// <param name="offset">Offset from the stack top</param>
        /// <returns>Value returned from the buffer</returns>
        public object PeekObject(int offset = 0)
        {
            var bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                var arrBytes = PeekBytes(PeekInt(4, offset), offset + 4);
                ms.Write(arrBytes, 0, arrBytes.Length);
                ms.Seek(0, SeekOrigin.Begin);
                var obj = bf.Deserialize(ms);

                return obj;
            }
        }

        /// <summary>
        /// Returns max byte count required to encode the string
        /// </summary>
        /// <param name="value">String to encode</param>
        /// <returns>Max byte count required to encode the string</returns>
        public static int GetMaxByteCount(string value)
        {
            return GetMaxByteCount(value, Encoding.UTF8);
        }

        /// <summary>
        /// Returns max byte count required to encode the string
        /// </summary>
        /// <param name="value">String to encode</param>
        /// <param name="encoding">Encoding used for this string</param>
        /// <returns>Max byte count required to encode the string</returns>
        public static int GetMaxByteCount(string value, Encoding encoding)
        {
            if (string.IsNullOrEmpty(value)) return 4;
            return 4 + encoding.GetMaxByteCount(value.Length);
        }
    }
}
