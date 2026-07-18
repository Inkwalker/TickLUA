using System;
using System.Collections.Generic;
using System.IO;
using TickLUA.VM.Objects;

namespace TickLUA.VM.Serialization
{
    /// <summary>
    /// Binary serialization for the built-in Lua value types: nil, boolean,
    /// number, string and table. Tables may be nested, share subtables and
    /// contain reference cycles — identity is preserved through back-references,
    /// and metatables are included. Functions, coroutines and host-defined
    /// types are not data and cannot be serialized.
    /// </summary>
    public static class LuaValueSerializer
    {
        private const byte TagNil = 0;
        private const byte TagFalse = 1;
        private const byte TagTrue = 2;
        private const byte TagNumber = 3;
        private const byte TagString = 4;
        private const byte TagTable = 5;

        /// <summary>Back-reference to a table already emitted in this value, by first-appearance index.</summary>
        private const byte TagTableRef = 6;

        public static byte[] Serialize(LuaObject value)
        {
            using (var stream = new MemoryStream())
            {
                Serialize(value, stream);
                return stream.ToArray();
            }
        }

        public static void Serialize(LuaObject value, Stream stream)
        {
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true))
            {
                Write(writer, value);
            }
        }

        public static LuaObject Deserialize(byte[] data)
        {
            using (var stream = new MemoryStream(data))
            {
                return Deserialize(stream);
            }
        }

        public static LuaObject Deserialize(Stream stream)
        {
            using (var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true))
            {
                return Read(reader);
            }
        }

        /// <exception cref="NotSupportedException">The value (or something it references) is not a serializable built-in type.</exception>
        public static void Write(BinaryWriter writer, LuaObject value)
        {
            Write(writer, value, null);
        }

        /// <exception cref="BytecodeFormatException">The stream does not contain a well-formed value.</exception>
        public static LuaObject Read(BinaryReader reader)
        {
            return Read(reader, null);
        }

        private static void Write(BinaryWriter writer, LuaObject value, Dictionary<TableObject, int> seen_tables)
        {
            switch (value)
            {
                case null:
                case NilObject _:
                    writer.Write(TagNil);
                    break;

                case BooleanObject boolean:
                    writer.Write((bool)boolean ? TagTrue : TagFalse);
                    break;

                case NumberObject number:
                    writer.Write(TagNumber);
                    writer.Write(number.Value);
                    break;

                case StringObject str:
                    writer.Write(TagString);
                    writer.Write(str.Value);
                    break;

                case TableObject table:
                    if (seen_tables == null)
                        seen_tables = new Dictionary<TableObject, int>();

                    if (seen_tables.TryGetValue(table, out int id))
                    {
                        writer.Write(TagTableRef);
                        writer.Write(id);
                        break;
                    }

                    writer.Write(TagTable);

                    // Register before writing contents so self-references
                    // inside entries or the metatable resolve to this table.
                    seen_tables[table] = seen_tables.Count;

                    writer.Write(table.Elements.Count);
                    foreach (var pair in table.Elements)
                    {
                        Write(writer, pair.Key, seen_tables);
                        Write(writer, pair.Value, seen_tables);
                    }

                    Write(writer, table.Metatable ?? (LuaObject)NilObject.Nil, seen_tables);
                    break;

                default:
                    throw new NotSupportedException(
                        $"Values of type {value.GetType().Name} are not serializable");
            }
        }

        private static LuaObject Read(BinaryReader reader, List<TableObject> read_tables)
        {
            byte tag = reader.ReadByte();

            switch (tag)
            {
                case TagNil:
                    return NilObject.Nil;

                case TagFalse:
                    return BooleanObject.False;

                case TagTrue:
                    return BooleanObject.True;

                case TagNumber:
                    return new NumberObject(reader.ReadSingle());

                case TagString:
                    return new StringObject(reader.ReadString());

                case TagTable:
                {
                    if (read_tables == null)
                        read_tables = new List<TableObject>();

                    var table = new TableObject();

                    // Register before reading contents, mirroring Write.
                    read_tables.Add(table);

                    int count = reader.ReadInt32();
                    for (int i = 0; i < count; i++)
                    {
                        var key = Read(reader, read_tables);
                        var value = Read(reader, read_tables);
                        table.Elements[key] = value;
                    }

                    if (Read(reader, read_tables) is TableObject metatable)
                        table.Metatable = metatable;

                    return table;
                }

                case TagTableRef:
                {
                    int id = reader.ReadInt32();
                    if (read_tables == null || id < 0 || id >= read_tables.Count)
                        throw new BytecodeFormatException($"Invalid table back-reference: {id}");
                    return read_tables[id];
                }

                default:
                    throw new BytecodeFormatException($"Unknown value tag: {tag}");
            }
        }
    }
}
