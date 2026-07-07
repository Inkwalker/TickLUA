using System;

namespace TickLUA.VM.Tools
{
    public static class BytecodePrinter
    {
        public static void ConsoleWrite(LuaFunction func, bool include_nested = true)
        {
            Console.WriteLine($"function {func.Name}:");
            for (int i = 0; i < func.Instructions.Count; i++)
            {
                var line = GetLineNumber(func.Meta, i);
                var inst = GetInstructionString(func.Meta, func.Instructions[i]).PadRight(28);
                var comment = GetComment(func, i);

                Console.WriteLine($"{line}   {inst} {comment}");
            }
            Console.WriteLine();

            if (include_nested) 
            { 
                foreach (var nested in func.NestedFunctions)
                {
                    ConsoleWrite(nested, include_nested);
                }
            }
        }

        private static string GetInstructionString(LuaFunction.Metadata meta, Instruction i)
        {
            var op = i.Opcode;

            var op_str = op.ToString().PadRight(16);

            switch (op)
            {
                case Opcode.NOP:
                    return op_str;
                case Opcode.LOAD_TRUE:
                case Opcode.LOAD_FALSE:
                case Opcode.LOAD_FALSE_SKIP:
                case Opcode.NEW_TABLE:
                case Opcode.CLOSE:
                    return $"{op_str} R{i.A}";
                case Opcode.LOAD_CONST:
                    return $"{op_str} R{i.A} C{i.Bx}";
                case Opcode.MOVE:
                case Opcode.NOT:
                case Opcode.UNM:
                case Opcode.LEN:
                    return $"{op_str} R{i.A} R{i.B}";
                case Opcode.LOAD_NIL:
                case Opcode.RETURN:
                case Opcode.VARARG:
                    return $"{op_str} R{i.A} {i.Bx}";
                case Opcode.TEST:
                    return $"{op_str} R{i.A} {i.B}";
                case Opcode.GET_UPVAL:
                case Opcode.SET_UPVAL:
                    return $"{op_str} R{i.A} UV{i.B}";
                case Opcode.CLOSURE:
                    return $"{op_str} R{i.A} F{i.Bx}";
                case Opcode.LOAD_INT:
                case Opcode.FORLOOP:
                case Opcode.FORPREP:
                    return $"{op_str} R{i.A} {i.BxSigned}";
                case Opcode.ADD:
                case Opcode.SUB:
                case Opcode.MUL:
                case Opcode.DIV:
                case Opcode.MOD:
                case Opcode.POW:
                case Opcode.IDIV:
                case Opcode.CONCAT:
                case Opcode.LE:
                case Opcode.LT:
                case Opcode.EQ:
                case Opcode.SET_TABLE:
                case Opcode.GET_TABLE:
                    return $"{op_str} R{i.A} R{i.B} R{i.C}";
                case Opcode.JMP:
                    return $"{op_str} {i.AxSigned}";
                case Opcode.TESTSET:
                case Opcode.SET_LIST:
                    return $"{op_str} R{i.A} R{i.B} {i.C}";
                case Opcode.SET_FIELD:
                    return $"{op_str} R{i.A} C{i.B} R{i.C}";
                case Opcode.GET_FIELD:
                    return $"{op_str} R{i.A} R{i.B} C{i.C}";
                case Opcode.CALL:
                    return $"{op_str} R{i.A} {i.B} {i.C}";

                default:
                    return $"{op_str} R{i.A} R{i.B} C{i.C}";
            }
        }

        private static string GetLineNumber(LuaFunction.Metadata meta, int instruction_index)
        {
            if (meta == null || instruction_index >= meta.Lines.Count)
                return "...";
            return meta.Lines[instruction_index].ToString().PadLeft(3);
        }

        private static string GetComment(LuaFunction f, int instruction_index)
        {
            var i = f.Instructions[instruction_index];

            switch (i.Opcode)
            {
                case Opcode.CLOSURE:
                    return $"; {f.NestedFunctions[i.Bx].Name}()";
                case Opcode.LOAD_CONST:
                    return $"; {f.Constants[i.Bx]}";
                //case Opcode.MOVE:
                //    return $"; TODO: variable names";
                case Opcode.RETURN:
                case Opcode.VARARG:
                    return $"; {(i.Bx == 0?"all":(i.Bx-1).ToString())} out";
                case Opcode.TEST:
                    return $"; expected {i.B > 0}";
                case Opcode.TESTSET:
                    return $"; expected {i.C > 0}";
                case Opcode.FORLOOP:
                case Opcode.FORPREP:
                    {
                        if (f.Meta != null)
                            return $"; to line {f.Meta.Lines[instruction_index + i.BxSigned + 1]}";
                        else
                            return "";
                    }
                case Opcode.JMP:
                    {
                        if (f.Meta != null)
                            return $"; to line {f.Meta.Lines[instruction_index + i.AxSigned + 1]}";
                        else
                            return "";
                    }
                case Opcode.SET_FIELD:
                    return $"; field {f.Constants[i.B]}";
                case Opcode.GET_FIELD:
                    return $"; field {f.Constants[i.C]}";
                case Opcode.CALL:
                    return $"; {(i.B == 0 ? "all" : (i.B - 1).ToString())} in, {(i.C == 0 ? "all" : (i.C - 1).ToString())} out";
                default:
                    return "";
            }
        }
    }
}
