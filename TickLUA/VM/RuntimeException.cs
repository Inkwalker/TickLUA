using System;
using System.Collections.Generic;
using System.Text;
using TickLUA.VM.Objects;

namespace TickLUA.VM
{
    public class RuntimeException : Exception
    {
        /// <summary>
        /// A single Lua frame in a captured traceback: the function's name and the
        /// source line it was executing (1-based; 0 when no line info is available).
        /// </summary>
        public readonly struct TracebackFrame
        {
            public string FunctionName { get; }
            public int Line { get; }

            public TracebackFrame(string functionName, int line)
            {
                FunctionName = functionName;
                Line = line;
            }
        }

        private LuaObject errorValue;
        private IReadOnlyList<TracebackFrame> luaTraceback;

        /// <summary>
        /// The Lua error value carried by this error (any Lua value, per error(v)).
        /// For message-only errors this is a StringObject of <see cref="Exception.Message"/>.
        /// </summary>
        public LuaObject ErrorValue => errorValue ?? (errorValue = new StringObject(Message));

        /// <summary>
        /// The Lua functions on the call stack when the error went unhandled, innermost
        /// first, each with its source line. Null until the error escapes every pcall
        /// boundary (protected calls don't build a traceback).
        /// </summary>
        public IReadOnlyList<TracebackFrame> LuaTraceback => luaTraceback;

        public RuntimeException(string message) : base(message) { }

        public RuntimeException(LuaObject value) : base(DescribeValue(value))
        {
            errorValue = value ?? NilObject.Nil;
        }

        /// <summary>
        /// Records the Lua call stack, once, at the point the error becomes unhandled.
        /// Later calls are ignored so the innermost capture wins.
        /// </summary>
        internal void CaptureTraceback(IReadOnlyList<TracebackFrame> frames)
        {
            if (luaTraceback == null)
                luaTraceback = frames;
        }

        /// <summary>
        /// A human-readable traceback of the captured Lua frames, or null when none
        /// was captured (the error was caught by pcall).
        /// </summary>
        public string FormatTraceback()
        {
            if (luaTraceback == null || luaTraceback.Count == 0)
                return null;

            var sb = new StringBuilder();
            sb.Append("stack traceback:");
            foreach (var frame in luaTraceback)
            {
                sb.Append("\n\t");
                if (frame.Line > 0)
                    sb.Append("line ").Append(frame.Line).Append(": ");
                if (frame.FunctionName == "main")
                    sb.Append("in main chunk");
                else
                    sb.Append("in function '").Append(frame.FunctionName ?? "?").Append('\'');
            }
            return sb.ToString();
        }

        public override string ToString()
        {
            var traceback = FormatTraceback();
            return traceback == null ? base.ToString() : base.ToString() + "\n" + traceback;
        }

        private static string DescribeValue(LuaObject value)
        {
            if (value == null || value is NilObject)
                return "nil";
            if (value is StringObject str)
                return str.Value;
            return value.ToString();
        }
    }
}
