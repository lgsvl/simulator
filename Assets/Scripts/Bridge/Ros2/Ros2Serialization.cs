/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;

namespace Simulator.Bridge.Ros2
{
    public struct PartialByteArray
    {
        public byte[] Array;
        public int Length;
    }

    public static class Ros2Serialization
    {
        public static int GetLength<T>(T message)
        {
            return Cache<T>.GetLength(message);
        }

        public static int Serialize<T>(T message, byte[] dst, int offset)
        {
            unsafe
            {
                fixed (byte* ptr = dst)
                {
                    var pointer = new IntPtr(ptr) + offset;
                    var end = Cache<T>.Serialize(message, pointer);

                    long written = (byte*)end.ToPointer() - (ptr + offset);
                    return (int)written;
                }
            }
        }

        public static T Unserialize<T>(ArraySegment<byte> data)
        {
            unsafe
            {
                fixed (byte* ptr = data.Array)
                {
                    var reader = new Helper.Reader()
                    {
                        Src = new IntPtr(ptr) + data.Offset,
                        Count = data.Count,
                    };
                    return Cache<T>.Unserialize(reader);
                }
            }
        }

        static class Cache<T>
        {
            public static Func<T, int> GetLength;
            public static Func<T, IntPtr, IntPtr> Serialize;
            public static Func<Helper.Reader, T> Unserialize;

            static Cache()
            {
                var message = Expression.Parameter(typeof(T), "message");
                var src = Expression.Parameter(typeof(IntPtr), "src");
                var reader = Expression.Parameter(typeof(Helper.Reader), "reader");

                var getLengthBody = Helper.GetLength(typeof(T), message);
                GetLength = Expression.Lambda<Func<T, int>>(getLengthBody, message).Compile();

                var serializeBody = Helper.Serialize(typeof(T), message, src);
                Serialize = Expression.Lambda<Func<T, IntPtr, IntPtr>>(serializeBody, message, src).Compile();

                var unserializeBody = Helper.Unserialize(typeof(T), reader);
                Unserialize = Expression.Lambda<Func<Helper.Reader, T>>(unserializeBody, reader).Compile();
            }
        }

        static class Helper
        {
            // ============= GetLength

            static int GetStringLength(string str)
            {
                return sizeof(int) + (str == null ? 0 : Encoding.UTF8.GetByteCount(str));
            }

            static int GetArrayLength<T>(T[] array)
            {
                int length = sizeof(int);
                if (array != null)
                {
                    var getLength = Cache<T>.GetLength;
                    for (int i = 0; i < array.Length; i++)
                    {
                        length += getLength(array[i]);
                    }
                }
                return length;
            }

            static int GetListLength<T>(IList<T> list)
            {
                int length = sizeof(int);
                if (list != null)
                {
                    var getLength = Cache<T>.GetLength;
                    length += list.Sum(getLength);
                }
                return length;
            }

            static int GetSimpleLength(Type type)
            {
                if (type == typeof(bool) || type == typeof(byte) || type == typeof(sbyte))
                {
                    return 1;
                }
                else if (type == typeof(short) || type == typeof(ushort))
                {
                    return 2;
                }
                else if (type == typeof(int) || type == typeof(uint) || type == typeof(float))
                {
                    return 4;
                }
                else if (type == typeof(long) || type == typeof(ulong) || type == typeof(double))
                {
                    return 8;
                }
                else if (type.IsEnum)
                {
                    return GetSimpleLength(type.GetEnumUnderlyingType());
                }
                else
                {
                    return 0;
                }
            }

            public static Expression GetLength(Type type, Expression value)
            {
                Expression body;

                int simpleSize = GetSimpleLength(type);
                if (simpleSize != 0)
                {
                    body = Expression.Constant(simpleSize);
                }
                else if (type == typeof(string))
                {
                    var getLength = typeof(Helper).GetMethod(nameof(GetStringLength), BindingFlags.NonPublic | BindingFlags.Static);
                    body = Expression.Call(
                        method: getLength,
                        arg0: value);
                }
                else if (type == typeof(PartialByteArray))
                {
                    var lengthField = typeof(PartialByteArray).GetField(nameof(PartialByteArray.Length));
                    body = Expression.Add(
                        Expression.Constant(sizeof(int)),
                        Expression.Field(value, lengthField));
                }
                else if (type.IsArray)
                {
                    var elemType = type.GetElementType();
                    int elemSize = GetSimpleLength(elemType);
                    if (elemSize == 0)
                    {
                        var getLength = typeof(Helper).GetMethod(nameof(GetArrayLength), BindingFlags.NonPublic | BindingFlags.Static);
                        body = Expression.Call(
                            method: getLength.MakeGenericMethod(elemType),
                            arg0: value);
                    }
                    else
                    {
                        // 4 + (array == null ? 0 : elemSize * array.Length)
                        body = Expression.Add(
                            Expression.Constant(sizeof(int)),
                            Expression.Condition(
                                Expression.Equal(value, Expression.Constant(null, typeof(object))),
                                Expression.Constant(0),
                                Expression.Multiply(
                                    Expression.Constant(elemSize),
                                    Expression.ArrayLength(value))));
                    }
                }
                else if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(List<>)))
                {
                    var elemType = type.GetGenericArguments()[0];
                    var getLength = typeof(Helper).GetMethod(nameof(GetListLength), BindingFlags.NonPublic | BindingFlags.Static);
                    body = Expression.Call(
                        method: getLength.MakeGenericMethod(elemType),
                        arg0: value);
                }
                else
                {
                    Expression length = Expression.Constant(0);

                    var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
                    for (int i = 0; i < fields.Length; i++)
                    {
                        var field = fields[i];
                        var fieldType = field.FieldType;
                        var fieldValue = Expression.Field(value, field);
                        length = Expression.Add(length, GetLength(fieldType, fieldValue));
                    }

                    if (type.IsValueType)
                    {
                        body = length;
                    }
                    else
                    {
                        body = Expression.Condition(
                            Expression.Equal(value, Expression.Constant(null, typeof(object))),
                            Expression.Constant(0),
                            length);
                    }
                }

                return body;
            }

            // ============= Serialize

            static IntPtr WriteBool(IntPtr dst, bool value)
            {
                return WriteValue(dst, (byte)(value ? 1 : 0));
            }

            static IntPtr WriteValue<T>(IntPtr dst, T value) where T : unmanaged
            {
                unsafe
                {
                    var ptr = (T*)dst.ToPointer();
                    *ptr = value;
                }
                return dst + UnsafeUtility.SizeOf<T>();
            }

            static IntPtr WriteString(IntPtr dst, string str)
            {
                if (str == null || str.Length == 0)
                {
                    dst = WriteValue(dst, 0);
                }
                else
                {
                    unsafe
                    {
                        fixed (char* s = str)
                        {
                            var ptr = (byte*)dst.ToPointer();
                            int byteCount = Encoding.UTF8.GetBytes(s, str.Length, ptr + sizeof(int), int.MaxValue);
                            WriteValue(dst, byteCount);
                            dst += sizeof(int);
                            dst += byteCount;
                        }
                    }
                }
                return dst;
            }

            static IntPtr WriteValueArray<T>(IntPtr dst, T[] array, int length) where T : unmanaged
            {
                dst = WriteValue(dst, length);
                if (length != 0 && array != null)
                {
                    int byteCount = length * UnsafeUtility.SizeOf<T>();
                    unsafe
                    {
                        fixed (T* src = array)
                        {
                            Unsafe.CopyBlock(dst.ToPointer(), src, (uint)byteCount);
                        }
                    }
                    dst += byteCount;
                }
                return dst;
            }

            static IntPtr WriteArray<T>(IntPtr dst, T[] array)
            {
                if (array == null)
                {
                    dst = WriteValue(dst, 0);
                }
                else
                {
                    var serialize = Cache<T>.Serialize;
                    dst = WriteValue(dst, array.Length);
                    for (int i = 0; i < array.Length; i++)
                    {
                        dst = serialize(array[i], dst);
                    }
                }
                return dst;
            }

            static IntPtr WriteList<T>(IntPtr dst, IList<T> list)
            {
                if (list == null)
                {
                    dst = WriteValue(dst, 0);
                }
                else
                {
                    var serialize = Cache<T>.Serialize;
                    dst = WriteValue(dst, list.Count);
                    for (int i = 0; i < list.Count; i++)
                    {
                        dst = serialize(list[i], dst);
                    }
                }
                return dst;
            }

            public static Expression Serialize(Type type, Expression value, Expression dst)
            {
                if (type == typeof(bool))
                {
                    var write = typeof(Helper).GetMethod(nameof(WriteBool), BindingFlags.NonPublic | BindingFlags.Static);
                    dst = Expression.Call(
                        method: write,
                        arg0: dst,
                        arg1: value);
                }
                else if (type == typeof(sbyte) || type == typeof(byte)
                    || type == typeof(short) || type == typeof(ushort)
                    || type == typeof(int) || type == typeof(uint)
                    || type == typeof(long) || type == typeof(ulong)
                    || type == typeof(float) || type == typeof(double)
                    || type.IsEnum)
                {
                    var elemType = type.IsEnum ? type.GetEnumUnderlyingType() : type;
                    var write = typeof(Helper).GetMethod(nameof(WriteValue), BindingFlags.NonPublic | BindingFlags.Static);
                    dst = Expression.Call(
                        method: write.MakeGenericMethod(elemType),
                        arg0: dst,
                        arg1: type.IsEnum ? Expression.Convert(value, elemType) : value);
                }
                else if (type == typeof(string))
                {
                    var serialize = typeof(Helper).GetMethod(nameof(WriteString), BindingFlags.NonPublic | BindingFlags.Static);
                    dst = Expression.Call(method: serialize, arg0: dst, arg1: value);
                }
                else if (type == typeof(PartialByteArray))
                {
                    var arrayField = typeof(PartialByteArray).GetField(nameof(PartialByteArray.Array));
                    var lengthField = typeof(PartialByteArray).GetField(nameof(PartialByteArray.Length));
                    var write = typeof(Helper).GetMethod(nameof(WriteValueArray), BindingFlags.NonPublic | BindingFlags.Static);
                    dst = Expression.Call(
                        method: write.MakeGenericMethod(typeof(byte)),
                        arg0: dst,
                        arg1: Expression.Field(value, arrayField),
                        arg2: Expression.Field(value, lengthField));
                }
                else if (type.IsArray)
                {
                    var elemType = type.GetElementType();
                    if (elemType == typeof(sbyte) || elemType == typeof(byte)
                        || elemType == typeof(short) || elemType == typeof(ushort)
                        || elemType == typeof(int) || elemType == typeof(uint)
                        || elemType == typeof(long) || elemType == typeof(ulong)
                        || elemType == typeof(float) || elemType == typeof(double)
                        || elemType.IsEnum)
                    {
                        var arrayLength = Expression.Condition(
                            Expression.Equal(value, Expression.Constant(null, typeof(object))),
                            Expression.Constant(0),
                            Expression.ArrayLength(value));

                        var write = typeof(Helper).GetMethod(nameof(WriteValueArray), BindingFlags.NonPublic | BindingFlags.Static);
                        dst = Expression.Call(
                            method: write.MakeGenericMethod(elemType),
                            arg0: dst,
                            arg1: value,
                            arg2: arrayLength);
                    }
                    else
                    {
                        var write = typeof(Helper).GetMethod(nameof(WriteArray), BindingFlags.NonPublic | BindingFlags.Static);
                        dst = Expression.Call(
                            method: write.MakeGenericMethod(elemType),
                            arg0: dst,
                            arg1: value);
                    }
                }
                else if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(List<>)))
                {
                    var elemType = type.GetGenericArguments()[0];
                    var write = typeof(Helper).GetMethod(nameof(WriteList), BindingFlags.NonPublic | BindingFlags.Static);
                    dst = Expression.Call(
                        method: write.MakeGenericMethod(elemType),
                        arg0: dst,
                        arg1: value);
                }
                else
                {
                    var writes = dst;

                    var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
                    for (int i = 0; i < fields.Length; i++)
                    {
                        var field = fields[i];
                        var fieldType = field.FieldType;

                        var fieldValue = Expression.Field(value, field);
                        writes = Serialize(fieldType, fieldValue, writes);
                    }

                    if (type.IsValueType)
                    {
                        dst = writes;
                    }
                    else
                    {
                        dst = Expression.Condition(
                            Expression.Equal(value, Expression.Constant(null, typeof(object))),
                            dst,
                            writes);
                    }
                }

                return dst;
            }

            // ============= Unserialize

            public class Reader
            {
                public IntPtr Src;
                public int Count;

                public bool ReadBool()
                {
                    return ReadValue<byte>() != 0;
                }

                public T ReadValue<T>() where T : unmanaged
                {
                    int byteCount = UnsafeUtility.SizeOf<T>();
                    if (Count < byteCount)
                    {
                        // NOTE: if are getting this exception, verify that your C# structure has correct fields!
                        // Compare with .msg file, including other referenced messages - make sure all the types match exactly
                        throw new InvalidDataException($"{Count} bytes remaining, need {byteCount} bytes");
                    }
                    unsafe
                    {
                        var ptr = (T*)Src;
                        Src += byteCount;
                        Count -= byteCount;
                        return *ptr;
                    }
                }

                public string ReadString()
                {
                    int byteCount = ReadValue<int>();
                    if (Count < byteCount)
                    {
                        // NOTE: if are getting this exception, verify that your C# structure has correct fields!
                        // Compare with .msg file, including other referenced messages - make sure all the types match exactly
                        throw new InvalidDataException($"{Count} bytes remaining, need {byteCount} bytes");
                    }
                    unsafe
                    {
                        var ptr = (byte*)Src;
                        Src += byteCount;
                        Count -= byteCount;
                        return Encoding.UTF8.GetString(ptr, byteCount);
                    }
                }

                public PartialByteArray ReadPartialByteArray()
                {
                    var arr = new PartialByteArray();
                    arr.Array = ReadValueArray<byte>();
                    arr.Length = arr.Array.Length;
                    return arr;
                }

                public T[] ReadValueArray<T>() where T : unmanaged
                {
                    int length = ReadValue<int>();
                    int byteCount = length * UnsafeUtility.SizeOf<T>();
                    if (Count < byteCount)
                    {
                        // NOTE: if are getting this exception, verify that your C# structure has correct fields!
                        // Compare with .msg file, including other referenced messages - make sure all the types match exactly
                        throw new InvalidDataException($"{Count} bytes remaining, need {byteCount} bytes");
                    }
                    var arr = new T[length];
                    unsafe
                    {
                        fixed (T* dst = arr)
                        {
                            UnsafeUtility.MemCpy(dst, Src.ToPointer(), byteCount);
                            Src += byteCount;
                        }
                    }
                    return arr;
                }

                public T[] ReadArray<T>()
                {
                    int length = ReadValue<int>();

                    var unserialize = Cache<T>.Unserialize;
                    var arr = new T[length];
                    for (int i = 0; i < length; i++)
                    {
                        arr[i] = unserialize(this);
                    }
                    return arr;
                }

                public List<T> ReadList<T>()
                {
                    int length = ReadValue<int>();

                    var unserialize = Cache<T>.Unserialize;
                    var list = new List<T>(length);
                    for (int i = 0; i < length; i++)
                    {
                        list.Add(unserialize(this));
                    }
                    return list;
                }

            } // class Reader

            public static Expression Unserialize(Type type, Expression reader)
            {
                Expression value;

                if (type == typeof(bool))
                {
                    var read = typeof(Reader).GetMethod(nameof(Reader.ReadBool));
                    value = Expression.Call(instance: reader, method: read);
                }
                else if (type == typeof(sbyte) || type == typeof(byte)
                    || type == typeof(short) || type == typeof(ushort)
                    || type == typeof(int) || type == typeof(uint)
                    || type == typeof(long) || type == typeof(ulong)
                    || type == typeof(float) || type == typeof(double)
                    || type.IsEnum)
                {
                    var elemType = type.IsEnum ? type.GetEnumUnderlyingType() : type;
                    var read = typeof(Reader).GetMethod(nameof(Reader.ReadValue));
                    value = Expression.Call(instance: reader, method: read.MakeGenericMethod(elemType));
                    if (type.IsEnum)
                    {
                        value = Expression.Convert(value, type);
                    }
                }
                else if (type == typeof(string))
                {
                    var read = typeof(Reader).GetMethod(nameof(Reader.ReadString));
                    value = Expression.Call(instance: reader, method: read);
                }
                else if (type == typeof(PartialByteArray))
                {
                    var read = typeof(Reader).GetMethod(nameof(Reader.ReadPartialByteArray));
                    value = Expression.Call(instance: reader, method: read);
                }
                else if (type.IsArray)
                {
                    var elemType = type.GetElementType();
                    if (elemType == typeof(sbyte) || elemType == typeof(byte)
                        || elemType == typeof(short) || elemType == typeof(ushort)
                        || elemType == typeof(int) || elemType == typeof(uint)
                        || elemType == typeof(long) || elemType == typeof(ulong)
                        || elemType == typeof(float) || elemType == typeof(double)
                        || elemType.IsEnum)
                    {
                        var read = typeof(Reader).GetMethod(nameof(Reader.ReadValueArray));
                        value = Expression.Call(instance: reader, method: read.MakeGenericMethod(elemType));
                    }
                    else
                    {
                        var read = typeof(Reader).GetMethod(nameof(Reader.ReadArray));
                        value = Expression.Call(instance: reader, method: read.MakeGenericMethod(elemType));
                    }
                }
                else if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(List<>)))
                {
                    var elemType = type.GetGenericArguments()[0];
                    var read = typeof(Reader).GetMethod(nameof(Reader.ReadList));
                    value = Expression.Call(instance: reader, method: read.MakeGenericMethod(elemType));
                }
                else
                {
                    var obj = Expression.Variable(type);

                    var body = new List<Expression>();
                    body.Add(Expression.Assign(obj, Expression.New(type)));

                    var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
                    for (int i = 0; i < fields.Length; i++)
                    {
                        var field = fields[i];
                        var fieldType = field.FieldType;

                        var fieldValue = Unserialize(fieldType, reader);
                        body.Add(Expression.Assign(Expression.Field(obj, field), fieldValue));
                    }

                    body.Add(obj);

                    value = Expression.Block(new[] { obj }, body);
                }

                return value;
            }

        } // static class Helper
    }
}
