using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TickLUA.VM.Objects;

namespace TickLUA_Tests.Instructions
{
    public class Functions
    {
        [Test]
        public void CLOSURE()
        {
            var outer = new LuaFunction(2);

            outer.Instructions.Add(Instruction.LOAD_INT(0, 42));
            outer.Instructions.Add(Instruction.CLOSURE(1, 0));
            outer.Instructions.Add(Instruction.RETURN(1, 1));

            var inner = new LuaFunction(0);
            inner.Instructions.Add(Instruction.NOP());
            inner.Upvalues.Add(new LuaFunction.UpvalueDef(true, 0));

            outer.NestedFunctions.Add(inner);

            var vm = Utils.Run(outer, 3);

            Assert.NotNull(vm.ExecutionResult);
            Assert.IsTrue(vm.ExecutionResult.Length > 0);
            Assert.IsInstanceOf<ClosureObject>(vm.ExecutionResult[0]);

            var closure = (ClosureObject)vm.ExecutionResult[0];

            Assert.That(closure.Upvalues[0].Value, Is.EqualTo(new NumberObject(42)));
        }
    }
}
