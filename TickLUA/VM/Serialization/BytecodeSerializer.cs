using System.IO;
using TickLUA.VM.Objects;

namespace TickLUA.VM.Serialization
{
    /// <summary>
    /// Binary serialization of compiled bytecode (a <see cref="LuaFunction"/>
    /// tree). The stream starts with a "TLUA" magic and the compiler version
    /// that produced the bytecode; deserialization rejects streams whose
    /// version does not match <see cref="LuaFunction.CurrentCompilerVersion"/>,
    /// since the instruction encoding may have changed between versions.
    /// </summary>
    public static class BytecodeSerializer
    {
        private static readonly byte[] Magic = { (byte)'T', (byte)'L', (byte)'U', (byte)'A' };

        public static byte[] Serialize(LuaFunction function)
        {
            using (var stream = new MemoryStream())
            {
                Serialize(function, stream);
                return stream.ToArray();
            }
        }

        public static void Serialize(LuaFunction function, Stream stream)
        {
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true))
            {
                writer.Write(Magic);
                writer.Write(function.CompilerVersion);
                WriteFunction(writer, function);
            }
        }

        /// <exception cref="BytecodeFormatException">The stream is not TickLUA bytecode, was produced by an incompatible compiler version, or is corrupted.</exception>
        public static LuaFunction Deserialize(byte[] data)
        {
            using (var stream = new MemoryStream(data))
            {
                return Deserialize(stream);
            }
        }

        /// <exception cref="BytecodeFormatException">The stream is not TickLUA bytecode, was produced by an incompatible compiler version, or is corrupted.</exception>
        public static LuaFunction Deserialize(Stream stream)
        {
            using (var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true))
            {
                try
                {
                    foreach (byte expected in Magic)
                    {
                        if (reader.ReadByte() != expected)
                            throw new BytecodeFormatException("Not a TickLUA bytecode stream");
                    }

                    ushort version = reader.ReadUInt16();
                    if (version != LuaFunction.CurrentCompilerVersion)
                        throw new BytecodeFormatException(
                            $"Bytecode compiler version {version} is not compatible with this VM " +
                            $"(expected {LuaFunction.CurrentCompilerVersion})");

                    return ReadFunction(reader, version);
                }
                catch (EndOfStreamException ex)
                {
                    throw new BytecodeFormatException("Truncated bytecode stream", ex);
                }
            }
        }

        private static void WriteFunction(BinaryWriter writer, LuaFunction function)
        {
            writer.Write(function.Name != null);
            if (function.Name != null)
                writer.Write(function.Name);

            writer.Write(function.HasVarargs);
            writer.Write(function.ParameterCount);
            writer.Write(function.RegisterCount);

            writer.Write(function.Instructions.Count);
            foreach (var instruction in function.Instructions)
                writer.Write(instruction.Raw);

            writer.Write(function.Meta.Lines.Count);
            foreach (ushort line in function.Meta.Lines)
                writer.Write(line);

            writer.Write(function.Constants.Count);
            foreach (var constant in function.Constants)
                LuaValueSerializer.Write(writer, constant);

            writer.Write(function.Upvalues.Count);
            foreach (var upvalue in function.Upvalues)
            {
                writer.Write(upvalue.Name != null);
                if (upvalue.Name != null)
                    writer.Write(upvalue.Name);
                writer.Write(upvalue.IsLocalToParent);
                writer.Write(upvalue.Index);
            }

            writer.Write(function.NestedFunctions.Count);
            foreach (var nested in function.NestedFunctions)
                WriteFunction(writer, nested);
        }

        private static LuaFunction ReadFunction(BinaryReader reader, ushort version)
        {
            string name = reader.ReadBoolean() ? reader.ReadString() : null;
            bool has_varargs = reader.ReadBoolean();
            int parameter_count = reader.ReadInt32();
            int register_count = reader.ReadInt32();

            int instruction_count = reader.ReadInt32();
            var instructions = new Instruction[instruction_count];
            for (int i = 0; i < instruction_count; i++)
                instructions[i] = Instruction.FromRaw(reader.ReadUInt32());

            var meta = new LuaFunction.Metadata();
            int line_count = reader.ReadInt32();
            for (int i = 0; i < line_count; i++)
                meta.Lines.Add(reader.ReadUInt16());

            int constant_count = reader.ReadInt32();
            var constants = new LuaObject[constant_count];
            for (int i = 0; i < constant_count; i++)
                constants[i] = LuaValueSerializer.Read(reader);

            int upvalue_count = reader.ReadInt32();
            var upvalues = new LuaFunction.UpvalueDef[upvalue_count];
            for (int i = 0; i < upvalue_count; i++)
            {
                string upvalue_name = reader.ReadBoolean() ? reader.ReadString() : null;
                bool is_local = reader.ReadBoolean();
                byte index = reader.ReadByte();
                upvalues[i] = new LuaFunction.UpvalueDef(upvalue_name, is_local, index);
            }

            var function = new LuaFunction(name, instructions, constants, upvalues, meta, register_count)
            {
                HasVarargs = has_varargs,
                ParameterCount = parameter_count,
                CompilerVersion = version,
            };

            int nested_count = reader.ReadInt32();
            for (int i = 0; i < nested_count; i++)
                function.NestedFunctions.Add(ReadFunction(reader, version));

            return function;
        }
    }
}
