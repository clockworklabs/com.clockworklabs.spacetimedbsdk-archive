using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace SpacetimeDB.BSATN
{
    public interface IStructuralReadWrite
    {
        void ReadFields(BinaryReader reader);

        void WriteFields(BinaryWriter writer);

        static T Read<T>(BinaryReader reader)
            where T : IStructuralReadWrite
        {
            // Note that unlike in memory-unsafe languages, uninitialized
            // object here just means fields set to default values
            // (e.g. reference fields are null).
            // First, this is a useful optimization as it allows to avoid
            // creating a temp object for each field in the default constructor
            // and then overriding it with yet another object in `ReadFields`.
            // Second, this allows to invoke `Read` even on types that don't
            // have a default constructor but still must be deserializable.
            var result = (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
            result.ReadFields(reader);
            return result;
        }

        public static byte[] ToBytes<RW, T>(RW rw, T value)
            where RW : IReadWrite<T>
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            rw.Write(writer, value);
            return stream.ToArray();
        }

        public static byte[] ToBytes<T>(T value)
            where T : IStructuralReadWrite
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            value.WriteFields(writer);
            return stream.ToArray();
        }
    }

    public interface IReadWrite<T>
    {
        T Read(BinaryReader reader);

        void Write(BinaryWriter writer, T value);

        AlgebraicType GetAlgebraicType(ITypeRegistrar registrar);
    }

    public readonly struct Enum<T> : IReadWrite<T>
        where T : struct, Enum
    {
        private static T Validate(T value)
        {
            if (!Enum.IsDefined(typeof(T), value))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    $"Value {value} is out of range of enum {typeof(T).Name}"
                );
            }
            return value;
        }

        public T Read(BinaryReader reader) =>
            Validate((T)Enum.ToObject(typeof(T), reader.ReadByte()));

        public void Write(BinaryWriter writer, T value) =>
            writer.Write(Convert.ToByte(Validate(value)));

        public AlgebraicType GetAlgebraicType(ITypeRegistrar registrar) =>
            registrar.RegisterType<T>(
                (_) =>
                    new AlgebraicType.Sum(
                        Enum.GetNames(typeof(T))
                            .Select(name => new AggregateElement(name, AlgebraicType.Unit))
                            .ToArray()
                    )
            );
    }

    public readonly struct RefOption<Inner, InnerRW> : IReadWrite<Inner?>
        where Inner : class
        where InnerRW : IReadWrite<Inner>, new()
    {
        private static readonly InnerRW innerRW = new();

        public Inner? Read(BinaryReader reader) =>
            reader.ReadBoolean() ? null : innerRW.Read(reader);

        public void Write(BinaryWriter writer, Inner? value)
        {
            writer.Write(value is null);
            if (value is not null)
            {
                innerRW.Write(writer, value);
            }
        }

        public AlgebraicType GetAlgebraicType(ITypeRegistrar registrar) =>
            AlgebraicType.MakeOption(innerRW.GetAlgebraicType(registrar));
    }

    // This implementation is nearly identical to RefOption. The only difference is the constraint on T.
    // Yes, this is dumb, but apparently you can't have *really* generic `T?` because,
    // despite identical bodies, compiler will desugar it to very different
    // types based on whether the constraint makes it a reference type or a value type.
    public readonly struct ValueOption<Inner, InnerRW> : IReadWrite<Inner?>
        where Inner : struct
        where InnerRW : IReadWrite<Inner>, new()
    {
        private static readonly InnerRW innerRW = new();

        public Inner? Read(BinaryReader reader) =>
            reader.ReadBoolean() ? null : innerRW.Read(reader);

        public void Write(BinaryWriter writer, Inner? value)
        {
            writer.Write(!value.HasValue);
            if (value.HasValue)
            {
                innerRW.Write(writer, value.Value);
            }
        }

        public AlgebraicType GetAlgebraicType(ITypeRegistrar registrar) =>
            AlgebraicType.MakeOption(innerRW.GetAlgebraicType(registrar));
    }

    public readonly struct Bool : IReadWrite<bool>
    {
        public bool Read(BinaryReader reader) => reader.ReadBoolean();

        public void Write(BinaryWriter writer, bool value) => writer.Write(value);

        public AlgebraicType GetAlgebraicType(ITypeRegistrar registrar) =>
            new BuiltinType.Bool(default);
    }

    public readonly struct U8 : IReadWrite<byte>
    {
        public byte Read(BinaryReader reader) => reader.ReadByte();

        public void Write(BinaryWriter writer, byte value) => writer.Write(value);

        public AlgebraicType GetAlgebraicType(ITypeRegistrar registrar) =>
            new BuiltinType.U8(default);
    }

    public readonly struct U16 : IReadWrite<ushort>
    {
        public ushort Read(BinaryReader reader) => reader.ReadUInt16();

        public void Write(BinaryWriter writer, ushort value) => writer.Write(value);

        public AlgebraicType GetAlgebraicType(ITypeRegistrar registrar) =>
            new BuiltinType.U16(default);
    }

    public readonly struct U32 : IReadWrite<uint>
    {
        public uint Read(BinaryReader reader) => reader.ReadUInt32();

        public void Write(BinaryWriter writer, uint value) => writer.Write(value);

        public AlgebraicType GetAlgebraicType(ITypeRegistrar registrar) =>
            new BuiltinType.U32(default);
    }

    public readonly struct U64 : IReadWrite<ulong>
    {
        public ulong Read(BinaryReader reader) => reader.ReadUInt64();

        public void Write(BinaryWriter writer, ulong value) => writer.Write(value);

        public AlgebraicType GetAlgebraicType(ITypeRegistrar registrar) =>
            new BuiltinType.U64(default);
    }

#if NET7_0_OR_GREATER
    public readonly struct U128 : IReadWrite<System.UInt128>
    {
        public System.UInt128 Read(BinaryReader reader) =>
            new(reader.ReadUInt64(), reader.ReadUInt64());

        public void Write(BinaryWriter writer, System.UInt128 value)
        {
            writer.Write((ulong)(value >> 64));
            writer.Write((ulong)value);
        }

        public AlgebraicType GetAlgebraicType(ITypeRegistrar registrar) =>
            new BuiltinType.U128(default);
    }
#endif

    public readonly struct I8 : IReadWrite<sbyte>
    {
        public sbyte Read(BinaryReader reader) => reader.ReadSByte();

        public void Write(BinaryWriter writer, sbyte value) => writer.Write(value);

        public AlgebraicType GetAlgebraicType(ITypeRegistrar registrar) =>
            new BuiltinType.I8(default);
    }

    public readonly struct I16 : IReadWrite<short>
    {
        public short Read(BinaryReader reader) => reader.ReadInt16();

        public void Write(BinaryWriter writer, short value) => writer.Write(value);

        public AlgebraicType GetAlgebraicType(ITypeRegistrar registrar) =>
            new BuiltinType.I16(default);
    }

    public readonly struct I32 : IReadWrite<int>
    {
        public int Read(BinaryReader reader) => reader.ReadInt32();

        public void Write(BinaryWriter writer, int value) => writer.Write(value);

        public AlgebraicType GetAlgebraicType(ITypeRegistrar registrar) =>
            new BuiltinType.I32(default);
    }

    public readonly struct I64 : IReadWrite<long>
    {
        public long Read(BinaryReader reader) => reader.ReadInt64();

        public void Write(BinaryWriter writer, long value) => writer.Write(value);

        public AlgebraicType GetAlgebraicType(ITypeRegistrar registrar) =>
            new BuiltinType.I64(default);
    }

#if NET7_0_OR_GREATER
    public readonly struct I128 : IReadWrite<System.Int128>
    {
        public System.Int128 Read(BinaryReader reader) =>
            new(reader.ReadUInt64(), reader.ReadUInt64());

        public void Write(BinaryWriter writer, System.Int128 value)
        {
            writer.Write((long)(value >> 64));
            writer.Write((long)value);
        }

        public AlgebraicType GetAlgebraicType(ITypeRegistrar registrar) =>
            new BuiltinType.I128(default);
    }
#endif

    public readonly struct F32 : IReadWrite<float>
    {
        public float Read(BinaryReader reader) => reader.ReadSingle();

        public void Write(BinaryWriter writer, float value) => writer.Write(value);

        public AlgebraicType GetAlgebraicType(ITypeRegistrar registrar) =>
            new BuiltinType.F32(default);
    }

    public readonly struct F64 : IReadWrite<double>
    {
        public double Read(BinaryReader reader) => reader.ReadDouble();

        public void Write(BinaryWriter writer, double value) => writer.Write(value);

        public AlgebraicType GetAlgebraicType(ITypeRegistrar registrar) =>
            new BuiltinType.F64(default);
    }

    readonly struct Enumerable<Element, ElementRW> : IReadWrite<IEnumerable<Element>>
        where ElementRW : IReadWrite<Element>, new()
    {
        private static readonly ElementRW elementRW = new();

        public IEnumerable<Element> Read(BinaryReader reader)
        {
            var count = reader.ReadInt32();
            for (var i = 0; i < count; i++)
            {
                yield return elementRW.Read(reader);
            }
        }

        public void Write(BinaryWriter writer, IEnumerable<Element> value)
        {
            writer.Write(value.Count());
            foreach (var element in value)
            {
                elementRW.Write(writer, element);
            }
        }

        public AlgebraicType GetAlgebraicType(ITypeRegistrar registrar) =>
            new BuiltinType.Array(elementRW.GetAlgebraicType(registrar));
    }

    public readonly struct Array<Element, ElementRW> : IReadWrite<Element[]>
        where ElementRW : IReadWrite<Element>, new()
    {
        private static readonly Enumerable<Element, ElementRW> enumerable = new();

        public Element[] Read(BinaryReader reader) => enumerable.Read(reader).ToArray();

        public void Write(BinaryWriter writer, Element[] value) => enumerable.Write(writer, value);

        public AlgebraicType GetAlgebraicType(ITypeRegistrar registrar) =>
            enumerable.GetAlgebraicType(registrar);
    }

    // Special case for byte arrays that can be dealt with more efficiently.
    public readonly struct ByteArray : IReadWrite<byte[]>
    {
        public static readonly ByteArray Instance = new();

        public byte[] Read(BinaryReader reader) => reader.ReadBytes(reader.ReadInt32());

        public void Write(BinaryWriter writer, byte[] value)
        {
            writer.Write(value.Length);
            writer.Write(value);
        }

        public AlgebraicType GetAlgebraicType(ITypeRegistrar registrar) =>
            new BuiltinType.Array(new BuiltinType.U8(default));
    }

    // String is a special case of byte array with extra checks.
    public readonly struct String : IReadWrite<string>
    {
        public string Read(BinaryReader reader) =>
            Encoding.UTF8.GetString(ByteArray.Instance.Read(reader));

        public void Write(BinaryWriter writer, string value) =>
            ByteArray.Instance.Write(writer, Encoding.UTF8.GetBytes(value));

        public AlgebraicType GetAlgebraicType(ITypeRegistrar registrar) =>
            new BuiltinType.String(default);
    }

    public readonly struct List<Element, ElementRW> : IReadWrite<List<Element>>
        where ElementRW : IReadWrite<Element>, new()
    {
        private static readonly Enumerable<Element, ElementRW> enumerable = new();

        public List<Element> Read(BinaryReader reader) => enumerable.Read(reader).ToList();

        public void Write(BinaryWriter writer, List<Element> value) =>
            enumerable.Write(writer, value);

        public AlgebraicType GetAlgebraicType(ITypeRegistrar registrar) =>
            enumerable.GetAlgebraicType(registrar);
    }

    public readonly struct Dictionary<Key, Value, KeyRW, ValueRW>
        : IReadWrite<Dictionary<Key, Value>>
        where Key : notnull
        where KeyRW : IReadWrite<Key>, new()
        where ValueRW : IReadWrite<Value>, new()
    {
        private static readonly KeyRW keyRW = new();
        private static readonly ValueRW valueRW = new();

        public Dictionary<Key, Value> Read(BinaryReader reader)
        {
            var count = reader.ReadInt32();
            var result = new Dictionary<Key, Value>(count);
            for (var i = 0; i < count; i++)
            {
                result.Add(keyRW.Read(reader), valueRW.Read(reader));
            }
            return result;
        }

        public void Write(BinaryWriter writer, Dictionary<Key, Value> value)
        {
            writer.Write(value.Count);
            foreach (var (key, val) in value)
            {
                keyRW.Write(writer, key);
                valueRW.Write(writer, val);
            }
        }

        public AlgebraicType GetAlgebraicType(ITypeRegistrar registrar) =>
            new BuiltinType.Map(
                new(keyRW.GetAlgebraicType(registrar), valueRW.GetAlgebraicType(registrar))
            );
    }
}