using System;
using TickLUA.Compilers.LUA;
using TickLUA.VM.Debugging;
using TickLUA.VM.Objects;
using TickLUA.VM.Serialization;

namespace TickLUA_Tests.Debugging
{
    internal class DebugSessionTests
    {
        private static (TickVM vm, DebugSession dbg) Create(string source, params LuaObject[] args)
        {
            var func = LuaCompiler.Compile(source);
            var vm = new TickVM(func, args);
            return (vm, new DebugSession(vm));
        }

        private static void StepToLine(DebugSession dbg, int line, int maxSteps = 100)
        {
            for (int i = 0; i < maxSteps; i++)
            {
                if (dbg.CurrentLine == line) return;
                Assert.That(dbg.StepLine(), Is.EqualTo(StepResult.Stepped), "VM finished before reaching the line");
            }
            Assert.Fail($"Did not reach line {line} within {maxSteps} steps (stuck at {dbg.CurrentLine})");
        }

        private static float Number(DebugFrame frame, string name)
        {
            Assert.IsTrue(frame.Locals.ContainsKey(name), $"No local '{name}' in frame {frame.FunctionName}");
            Assert.IsInstanceOf<NumberObject>(frame.Locals[name], $"Local '{name}'");
            return ((NumberObject)frame.Locals[name]).Value;
        }

        #region Stepping

        [Test]
        public void StepLine_WalksLineSequence()
        {
            var (vm, dbg) = Create("local a = 1\nlocal b = 2\nlocal c = a + b\nreturn c");

            Assert.That(dbg.CurrentLine, Is.EqualTo(1));
            Assert.That(dbg.StepLine(), Is.EqualTo(StepResult.Stepped));
            Assert.That(dbg.CurrentLine, Is.EqualTo(2));
            Assert.That(dbg.StepLine(), Is.EqualTo(StepResult.Stepped));
            Assert.That(dbg.CurrentLine, Is.EqualTo(3));
            Assert.That(dbg.StepLine(), Is.EqualTo(StepResult.Stepped));
            Assert.That(dbg.CurrentLine, Is.EqualTo(4));
            Assert.That(dbg.StepLine(), Is.EqualTo(StepResult.Finished));

            Utils.AssertIntegerResult(vm, 3);
        }

        [Test]
        public void StepInstruction_IsSingleTick()
        {
            var (vm, dbg) = Create("local a = 1\nreturn a");

            Assert.That(dbg.StepInstruction(), Is.EqualTo(StepResult.Stepped));
            // One instruction of line 1 executed; a multi-instruction chunk is
            // not finished after a single tick.
            Assert.IsFalse(dbg.IsFinished);

            int guard = 0;
            while (dbg.StepInstruction() == StepResult.Stepped)
                Assert.That(++guard, Is.LessThan(100));

            Utils.AssertIntegerResult(vm, 1);
        }

        [Test]
        public void StepLine_EntersLuaCall()
        {
            var (vm, dbg) = Create(
                "local function f()\n" +
                "    return 42\n" +
                "end\n" +
                "local x = f()\n" +
                "return x");

            StepToLine(dbg, 4);
            Assert.That(dbg.StepLine(), Is.EqualTo(StepResult.Stepped));

            Assert.That(dbg.CurrentFunctionName, Is.EqualTo("main.f"));
            Assert.That(dbg.CurrentLine, Is.EqualTo(2));
        }

        [Test]
        public void StepOver_SkipsLuaCall()
        {
            var (vm, dbg) = Create(
                "local function f()\n" +
                "    return 42\n" +
                "end\n" +
                "local x = f()\n" +
                "return x");

            StepToLine(dbg, 4);
            Assert.That(dbg.StepOver(), Is.EqualTo(StepResult.Stepped));

            Assert.That(dbg.CurrentFunctionName, Is.EqualTo("main"));
            Assert.That(dbg.CurrentLine, Is.EqualTo(5));
        }

        [Test]
        public void StepOver_SkipsNativeCall()
        {
            var add = new NativeFunctionObject("add", args =>
                new LuaObject[] { new NumberObject(args.CheckNumber(0) + args.CheckNumber(1)) });

            var (vm, dbg) = Create("local add = ...\nlocal x = add(1, 2)\nreturn x", add);

            StepToLine(dbg, 2);
            Assert.That(dbg.StepOver(), Is.EqualTo(StepResult.Stepped));
            Assert.That(dbg.CurrentLine, Is.EqualTo(3));
        }

        [Test]
        public void StepOver_SkipsCoroutineResume()
        {
            var (vm, dbg) = Create(
                "local co = coroutine.create(function()\n" +
                "    coroutine.yield(1)\n" +
                "end)\n" +
                "coroutine.resume(co)\n" +
                "coroutine.resume(co)\n" +
                "return 7");

            StepToLine(dbg, 4);
            Assert.That(dbg.StepOver(), Is.EqualTo(StepResult.Stepped));

            Assert.That(dbg.CurrentFunctionName, Is.EqualTo("main"));
            Assert.That(dbg.CurrentLine, Is.EqualTo(5));
        }

        [Test]
        public void StepOut_ReturnsToCaller()
        {
            var (vm, dbg) = Create(
                "local function f()\n" +
                "    local y = 1\n" +
                "    return y\n" +
                "end\n" +
                "local x = f()\n" +
                "return x");

            StepToLine(dbg, 5);
            Assert.That(dbg.StepLine(), Is.EqualTo(StepResult.Stepped));
            Assert.That(dbg.CurrentFunctionName, Is.EqualTo("main.f"));

            Assert.That(dbg.StepOut(), Is.EqualTo(StepResult.Stepped));
            Assert.That(dbg.CurrentFunctionName, Is.EqualTo("main"));
        }

        [Test]
        public void StepLine_OneLineLoop_StopsEachIteration()
        {
            var (vm, dbg) = Create(
                "local i = 0\n" +
                "while i < 3 do i = i + 1 end\n" +
                "return i");

            StepToLine(dbg, 2);

            // A whole-loop-on-one-line iteration still yields a stop (the
            // backward jump), instead of silently spinning to the next line.
            Assert.That(dbg.StepLine(), Is.EqualTo(StepResult.Stepped));
            Assert.That(dbg.CurrentLine, Is.EqualTo(2));

            int guard = 0;
            while (dbg.CurrentLine == 2)
            {
                Assert.That(dbg.StepLine(), Is.EqualTo(StepResult.Stepped));
                Assert.That(++guard, Is.LessThan(50));
            }
            Assert.That(dbg.CurrentLine, Is.EqualTo(3));
        }

        [Test]
        public void StepOver_ThroughPcallError()
        {
            var (vm, dbg) = Create(
                "local ok, err = pcall(function()\n" +
                "    error('boom')\n" +
                "end)\n" +
                "return ok, err");

            int guard = 0;
            while (dbg.StepOver() != StepResult.Finished)
                Assert.That(++guard, Is.LessThan(100));

            Utils.AssertBoolResult(vm, false, 0);
        }

        #endregion

        #region Breakpoints

        [Test]
        public void Breakpoint_PausesBeforeLineExecutes()
        {
            var (vm, dbg) = Create("local flag = 0\nflag = 1\nflag = 2\nreturn flag");
            dbg.AddBreakpoint(3);

            Assert.That(dbg.Continue(), Is.EqualTo(StepResult.BreakpointHit));
            Assert.That(dbg.CurrentLine, Is.EqualTo(3));

            // Line 3 has not executed: flag still holds line 2's value.
            var frame = dbg.GetCallStack()[0];
            Assert.That(Number(frame, "flag"), Is.EqualTo(1));

            Assert.That(dbg.Continue(), Is.EqualTo(StepResult.Finished));
            Utils.AssertIntegerResult(vm, 2);
        }

        [Test]
        public void Breakpoint_RefiresEachLoopIteration()
        {
            var (vm, dbg) = Create(
                "local count = 0\n" +
                "for i = 1, 3 do\n" +
                "    count = count + 1\n" +
                "end\n" +
                "return count");
            dbg.AddBreakpoint(3);

            for (int expected = 0; expected < 3; expected++)
            {
                Assert.That(dbg.Continue(), Is.EqualTo(StepResult.BreakpointHit));
                Assert.That(Number(dbg.GetCallStack()[0], "count"), Is.EqualTo(expected));
            }

            Assert.That(dbg.Continue(), Is.EqualTo(StepResult.Finished));
            Utils.AssertIntegerResult(vm, 3);
        }

        [Test]
        public void Breakpoint_FunctionNameFilter()
        {
            string source =
                "local function f()\n" +
                "    local z = 1\n" +
                "    return z\n" +
                "end\n" +
                "return f()";

            var (_, dbg) = Create(source);
            dbg.AddBreakpoint("main.f", 2);
            Assert.That(dbg.Continue(), Is.EqualTo(StepResult.BreakpointHit));
            Assert.That(dbg.CurrentFunctionName, Is.EqualTo("main.f"));

            // Same line, wrong function: never fires.
            var (_, dbg2) = Create(source);
            dbg2.AddBreakpoint("main", 2);
            Assert.That(dbg2.Continue(), Is.EqualTo(StepResult.Finished));
        }

        [Test]
        public void Breakpoint_RemoveWorks()
        {
            var (vm, dbg) = Create("local a = 1\na = 2\nreturn a");
            dbg.AddBreakpoint(2);
            dbg.RemoveBreakpoint(2);

            Assert.That(dbg.Continue(), Is.EqualTo(StepResult.Finished));
        }

        [Test]
        public void Continue_BudgetExhausted_IsResumable()
        {
            var (vm, dbg) = Create(
                "local i = 0\n" +
                "while true do\n" +
                "    i = i + 1\n" +
                "end");

            Assert.That(dbg.Continue(maxTicks: 20), Is.EqualTo(StepResult.BudgetExhausted));
            float first = Number(dbg.GetCallStack()[0], "i");

            Assert.That(dbg.Continue(maxTicks: 20), Is.EqualTo(StepResult.BudgetExhausted));
            float second = Number(dbg.GetCallStack()[0], "i");

            Assert.That(second, Is.GreaterThan(first));
        }

        #endregion

        #region Inspection

        [Test]
        public void Inspection_BlockLocal_AppearsAndDisappears()
        {
            var (vm, dbg) = Create(
                "local a = 1\n" +
                "do\n" +
                "    local b = 2\n" +
                "    a = b\n" +
                "end\n" +
                "return a");

            dbg.AddBreakpoint(4);
            Assert.That(dbg.Continue(), Is.EqualTo(StepResult.BreakpointHit));
            var inside = dbg.GetCallStack()[0];
            Assert.That(Number(inside, "a"), Is.EqualTo(1));
            Assert.That(Number(inside, "b"), Is.EqualTo(2));

            dbg.AddBreakpoint(6);
            Assert.That(dbg.Continue(), Is.EqualTo(StepResult.BreakpointHit));
            var outside = dbg.GetCallStack()[0];
            Assert.That(Number(outside, "a"), Is.EqualTo(2));
            Assert.IsFalse(outside.Locals.ContainsKey("b"), "block local leaked out of its scope");
        }

        [Test]
        public void Inspection_Shadowing_InnerValueWins()
        {
            var (vm, dbg) = Create(
                "local x = 1\n" +
                "do\n" +
                "    local x = 2\n" +
                "    x = x + 0\n" +
                "end\n" +
                "return x");

            dbg.AddBreakpoint(4);
            Assert.That(dbg.Continue(), Is.EqualTo(StepResult.BreakpointHit));
            Assert.That(Number(dbg.GetCallStack()[0], "x"), Is.EqualTo(2));
        }

        [Test]
        public void Inspection_ParametersUpvaluesAndCallerFrames()
        {
            // Note: "return add(5)" would compile to a TAILCALL, which
            // replaces the caller frame — only one frame would be visible.
            var (vm, dbg) = Create(
                "local base = 10\n" +
                "local function add(n)\n" +
                "    return base + n\n" +
                "end\n" +
                "local r = add(5)\n" +
                "return r");

            dbg.AddBreakpoint(3);
            Assert.That(dbg.Continue(), Is.EqualTo(StepResult.BreakpointHit));

            var stack = dbg.GetCallStack();
            Assert.That(stack.Count, Is.EqualTo(2));

            var inner = stack[0];
            Assert.That(inner.FunctionName, Is.EqualTo("main.add"));
            Assert.That(inner.Line, Is.EqualTo(3));
            Assert.That(Number(inner, "n"), Is.EqualTo(5));
            Assert.IsInstanceOf<NumberObject>(inner.Upvalues["base"]);
            Assert.That(((NumberObject)inner.Upvalues["base"]).Value, Is.EqualTo(10));

            var caller = stack[1];
            Assert.That(caller.FunctionName, Is.EqualTo("main"));
            Assert.That(caller.Line, Is.EqualTo(5));
            Assert.That(Number(caller, "base"), Is.EqualTo(10));
            Assert.IsTrue(caller.Locals.ContainsKey("add"));
        }

        [Test]
        public void Inspection_SnapshotIsPointInTime()
        {
            var (vm, dbg) = Create("local x = 1\nx = 2\nreturn x");

            dbg.AddBreakpoint(2);
            Assert.That(dbg.Continue(), Is.EqualTo(StepResult.BreakpointHit));
            var before = dbg.GetCallStack()[0];
            Assert.That(Number(before, "x"), Is.EqualTo(1));

            Assert.That(dbg.StepLine(), Is.EqualTo(StepResult.Stepped));

            // The old snapshot still reports the value captured at its time.
            Assert.That(Number(before, "x"), Is.EqualTo(1));
            Assert.That(Number(dbg.GetCallStack()[0], "x"), Is.EqualTo(2));
        }

        [Test]
        public void Inspection_TableValues_AreDrillable()
        {
            var (vm, dbg) = Create(
                "local t = { answer = 42 }\n" +
                "t.other = 1\n" +
                "return t");

            dbg.AddBreakpoint(2);
            Assert.That(dbg.Continue(), Is.EqualTo(StepResult.BreakpointHit));

            var t = dbg.GetCallStack()[0].Locals["t"];
            Assert.IsInstanceOf<TableObject>(t);
            Assert.That(((TableObject)t)["answer"], Is.EqualTo(new NumberObject(42)));
        }

        #endregion

        #region VM limits

        [Test]
        public void Limits_ConfiguredValues_AreReported()
        {
            var func = LuaCompiler.Compile(
                "local function f()\n" +
                "    local t = { 1, 2, 3 }\n" +
                "    return t\n" +
                "end\n" +
                "local r = f()\n" +
                "return r");
            var vm = new TickVM(func, new TickVMOptions
            {
                MaxMemoryBytes = 64 * 1024,
                MaxCallStackDepth = 32,
            });
            var dbg = new DebugSession(vm);

            Assert.That(dbg.MaxMemoryBytes, Is.EqualTo(64 * 1024));
            Assert.That(dbg.MaxCallStackDepth, Is.EqualTo(32));
            Assert.That(dbg.CallStackDepth, Is.EqualTo(1));

            dbg.AddBreakpoint(3);
            Assert.That(dbg.Continue(), Is.EqualTo(StepResult.BreakpointHit));

            // Paused inside f: main + f on the stack, and the ledger has
            // charged the frames and the table t.
            Assert.That(dbg.CallStackDepth, Is.EqualTo(2));
            Assert.That(dbg.CallStackDepth, Is.EqualTo(dbg.GetCallStack().Count));
            Assert.That(dbg.MemoryBytes, Is.GreaterThan(0));
            Assert.That(dbg.MemoryBytes, Is.LessThanOrEqualTo(dbg.MaxMemoryBytes.Value));

            Assert.That(dbg.Continue(), Is.EqualTo(StepResult.Finished));
        }

        [Test]
        public void Limits_Unlimited_ReportNull()
        {
            var (vm, dbg) = Create("local a = { 1, 2 }\nreturn a");

            Assert.IsNull(dbg.MaxMemoryBytes);
            Assert.IsNull(dbg.MaxCallStackDepth);
            // No limit configured means no memory accounting runs.
            Assert.That(dbg.MemoryBytes, Is.EqualTo(0));
            Assert.That(dbg.CallStackDepth, Is.EqualTo(1));
        }

        #endregion

        #region Debug info gating

        [Test]
        public void StrippedBytecode_SessionCreationThrows()
        {
            var compiled = LuaCompiler.Compile("local a = 1\nreturn a");
            var stripped = BytecodeSerializer.Deserialize(BytecodeSerializer.Serialize(compiled, stripDebugInfo: true));

            Assert.IsFalse(stripped.HasDebugInfo);
            var vm = new TickVM(stripped);
            Assert.Throws<InvalidOperationException>(() => new DebugSession(vm));
        }

        [Test]
        public void DeserializedChunk_IsDebuggable()
        {
            var compiled = LuaCompiler.Compile("local a = 1\nlocal b = a + 1\nreturn b");
            var restored = BytecodeSerializer.Deserialize(BytecodeSerializer.Serialize(compiled));

            var vm = new TickVM(restored);
            var dbg = new DebugSession(vm);

            dbg.AddBreakpoint(2);
            Assert.That(dbg.Continue(), Is.EqualTo(StepResult.BreakpointHit));
            Assert.That(Number(dbg.GetCallStack()[0], "a"), Is.EqualTo(1));

            Assert.That(dbg.Continue(), Is.EqualTo(StepResult.Finished));
            Utils.AssertIntegerResult(vm, 2);
        }

        #endregion
    }
}
