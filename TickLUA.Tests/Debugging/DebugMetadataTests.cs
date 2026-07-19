using System.Linq;
using TickLUA.Compilers.LUA;

namespace TickLUA_Tests.Debugging
{
    internal class DebugMetadataTests
    {
        private static LuaFunction.Metadata.LocalVarInfo FindLocal(LuaFunction func, string name)
        {
            var local = func.Meta.Locals.LastOrDefault(l => l.Name == name);
            Assert.NotNull(local, $"No local '{name}' in function {func.Name}");
            return local;
        }

        [Test]
        public void PlainLocals_AreRecorded()
        {
            var func = LuaCompiler.Compile(@"
                local a = 1
                local b = 2
                return a + b");

            var a = FindLocal(func, "a");
            var b = FindLocal(func, "b");

            Assert.That(a.Register, Is.Not.EqualTo(b.Register));
            Assert.That(a.StartPC, Is.LessThan(b.StartPC));
            // Both live until the chunk's block closes, after the final RETURN.
            Assert.That(a.EndPC, Is.EqualTo(func.InstructionCount));
            Assert.That(b.EndPC, Is.EqualTo(func.InstructionCount));
        }

        [Test]
        public void NoNamedLocals_ProducesNoEntries()
        {
            var func = LuaCompiler.Compile("return 1 + 2");

            Assert.That(func.Meta.Locals, Is.Empty);
            Assert.IsTrue(func.HasDebugInfo);
        }

        [Test]
        public void BlockLocal_ClosesAtBlockEnd()
        {
            var func = LuaCompiler.Compile(@"
                local a = 1
                do
                    local b = 2
                    a = b
                end
                return a");

            var a = FindLocal(func, "a");
            var b = FindLocal(func, "b");

            // b's lifetime is strictly inside a's.
            Assert.That(b.StartPC, Is.GreaterThan(a.StartPC));
            Assert.That(b.EndPC, Is.LessThan(a.EndPC));
        }

        [Test]
        public void Shadowing_InnerEntryComesLater()
        {
            var func = LuaCompiler.Compile(@"
                local x = 1
                do
                    local x = 2
                end
                return x");

            var entries = func.Meta.Locals.Where(l => l.Name == "x").ToList();
            Assert.That(entries.Count, Is.EqualTo(2));

            var outer = entries[0];
            var inner = entries[1];
            // Inner is declared later and dies earlier: nested range.
            Assert.That(inner.StartPC, Is.GreaterThan(outer.StartPC));
            Assert.That(inner.EndPC, Is.LessThan(outer.EndPC));
        }

        [Test]
        public void Redeclaration_InSameBlock_GetsSeparateEntry()
        {
            var func = LuaCompiler.Compile(@"
                local x = 1
                local x = 2
                return x");

            var entries = func.Meta.Locals.Where(l => l.Name == "x").ToList();
            Assert.That(entries.Count, Is.EqualTo(2));
            Assert.That(entries[0].Register, Is.Not.EqualTo(entries[1].Register));
        }

        [Test]
        public void Parameters_AreVisibleFromFunctionStart()
        {
            var func = LuaCompiler.Compile(@"
                local function f(p, q)
                    return p + q
                end
                return f(1, 2)");

            var nested = func.NestedFunctions[0];
            var p = FindLocal(nested, "p");
            var q = FindLocal(nested, "q");

            Assert.That(p.StartPC, Is.EqualTo(0));
            Assert.That(q.StartPC, Is.EqualTo(0));
            Assert.That(p.Register, Is.EqualTo(0));
            Assert.That(q.Register, Is.EqualTo(1));
            Assert.That(p.EndPC, Is.EqualTo(nested.InstructionCount));
        }

        [Test]
        public void NumericForLoop_ControlVariable_IsRecorded()
        {
            var func = LuaCompiler.Compile(@"
                local sum = 0
                for i = 1, 3 do
                    sum = sum + i
                end
                return sum");

            var i = FindLocal(func, "i");
            var sum = FindLocal(func, "sum");

            Assert.That(i.StartPC, Is.GreaterThan(sum.StartPC));
            Assert.That(i.EndPC, Is.LessThan(sum.EndPC));
        }

        [Test]
        public void ForInLoop_Variables_AreRecorded()
        {
            var func = LuaCompiler.Compile(@"
                local t = { 1, 2 }
                for k, v in pairs(t) do
                end
                return t");

            var k = FindLocal(func, "k");
            var v = FindLocal(func, "v");

            Assert.That(v.Register, Is.EqualTo(k.Register + 1));
            Assert.That(k.EndPC, Is.EqualTo(v.EndPC));
        }

        [Test]
        public void EscapingLocal_IsStillRecorded()
        {
            var func = LuaCompiler.Compile(@"
                local captured = 10
                local function get()
                    return captured
                end
                return get()");

            var captured = FindLocal(func, "captured");
            Assert.That(captured.EndPC, Is.GreaterThan(captured.StartPC));
        }

        [Test]
        public void NestedFunctions_CarryTheirOwnLocals()
        {
            var func = LuaCompiler.Compile(@"
                local outer_var = 1
                local function f()
                    local inner_var = 2
                    return inner_var
                end
                return f()");

            FindLocal(func, "outer_var");
            var nested = func.NestedFunctions[0];
            FindLocal(nested, "inner_var");

            Assert.IsFalse(func.Meta.Locals.Any(l => l.Name == "inner_var"));
            Assert.IsFalse(nested.Meta.Locals.Any(l => l.Name == "outer_var"));
        }

        [Test]
        public void AllEntries_HaveClosedRanges()
        {
            var func = LuaCompiler.Compile(@"
                local a = 1
                do local b = 2 end
                for i = 1, 2 do local c = i end
                local function f(p) local q = p end
                return a");

            void AssertClosed(LuaFunction f)
            {
                foreach (var local in f.Meta.Locals)
                {
                    Assert.That(local.StartPC, Is.GreaterThanOrEqualTo(0), local.Name);
                    Assert.That(local.EndPC, Is.GreaterThan(local.StartPC), local.Name);
                    Assert.That(local.EndPC, Is.LessThanOrEqualTo(f.InstructionCount), local.Name);
                }
                foreach (var nested in f.NestedFunctions)
                    AssertClosed(nested);
            }

            AssertClosed(func);
        }
    }
}
