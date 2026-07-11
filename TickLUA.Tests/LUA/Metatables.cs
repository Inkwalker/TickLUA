using TickLUA.VM.Objects;

namespace TickLUA_Tests.LUA
{
    internal class Metatables
    {
        #region setmetatable / getmetatable

        [Test]
        public void Setmetatable_ReturnsTable()
        {
            string source = @"
            local t = {}
            local mt = {}
            return setmetatable(t, mt) == t";

            var vm = Utils.Run(source, 100);
            Utils.AssertBoolResult(vm, true);
        }

        [Test]
        public void Getmetatable_Roundtrip()
        {
            string source = @"
            local t = {}
            local mt = {}
            setmetatable(t, mt)
            return getmetatable(t) == mt";

            var vm = Utils.Run(source, 100);
            Utils.AssertBoolResult(vm, true);
        }

        [Test]
        public void Getmetatable_NilWhenUnset()
        {
            string source = @"
            local t = {}
            return getmetatable(t)";

            var vm = Utils.Run(source, 100);
            Utils.AssertNilResult(vm);
        }

        [Test]
        public void Getmetatable_NilForNonTable()
        {
            string source = @"
            local a = getmetatable(5)
            local b = getmetatable('x')
            local c = getmetatable(true)
            return a, b, c";

            var vm = Utils.Run(source, 100);
            Utils.AssertNilResult(vm, 0);
            Utils.AssertNilResult(vm, 1);
            Utils.AssertNilResult(vm, 2);
        }

        [Test]
        public void Setmetatable_Nil_RemovesMetatable()
        {
            string source = @"
            local t = setmetatable({}, {})
            setmetatable(t, nil)
            return getmetatable(t)";

            var vm = Utils.Run(source, 100);
            Utils.AssertNilResult(vm);
        }

        [Test]
        public void Getmetatable_ProtectedField()
        {
            string source = @"
            local t = setmetatable({}, {__metatable = 'locked'})
            return getmetatable(t)";

            var vm = Utils.Run(source, 100);
            Utils.AssertStringResult(vm, "locked");
        }

        [Test]
        public void Setmetatable_Protected_Errors()
        {
            string source = @"
            local t = setmetatable({}, {__metatable = 'locked'})
            local ok, err = pcall(setmetatable, t, {})
            return ok, err";

            var vm = Utils.Run(source, 100);
            Utils.AssertBoolResult(vm, false, 0);
            Utils.AssertStringResult(vm, "cannot change a protected metatable", 1);
        }

        [Test]
        public void Setmetatable_NonTableSecondArg_Errors()
        {
            string source = @"
            local ok = pcall(setmetatable, {}, 5)
            return ok";

            var vm = Utils.Run(source, 100);
            Utils.AssertBoolResult(vm, false);
        }

        #endregion

        #region raw functions

        [Test]
        public void Rawget_ReadsDirectly()
        {
            string source = @"
            local t = {x = 7}
            local present = rawget(t, 'x')
            local missing = rawget(t, 'missing')
            return present, missing";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 7, 0);
            Utils.AssertNilResult(vm, 1);
        }

        [Test]
        public void Rawset_WritesDirectly()
        {
            string source = @"
            local t = {}
            local r = rawset(t, 'x', 3)
            return r == t, t.x";

            var vm = Utils.Run(source, 100);
            Utils.AssertBoolResult(vm, true, 0);
            Utils.AssertIntegerResult(vm, 3, 1);
        }

        [Test]
        public void Rawset_NilKey_Errors()
        {
            string source = @"
            local ok = pcall(rawset, {}, nil, 1)
            return ok";

            var vm = Utils.Run(source, 100);
            Utils.AssertBoolResult(vm, false);
        }

        [Test]
        public void Rawequal_Basics()
        {
            string source = @"
            local t = {}
            local same_table = rawequal(t, t)
            local diff_table = rawequal(t, {})
            local same_num = rawequal(1, 1)
            local same_str = rawequal('a', 'a')
            return same_table, diff_table, same_num, same_str";

            var vm = Utils.Run(source, 100);
            Utils.AssertBoolResult(vm, true, 0);
            Utils.AssertBoolResult(vm, false, 1);
            Utils.AssertBoolResult(vm, true, 2);
            Utils.AssertBoolResult(vm, true, 3);
        }

        [Test]
        public void Rawlen_TableAndString()
        {
            string source = @"
            local table_len = rawlen({10, 20, 30})
            local string_len = rawlen('abcd')
            return table_len, string_len";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 3, 0);
            Utils.AssertIntegerResult(vm, 4, 1);
        }

        [Test]
        public void Rawlen_NonMeasurable_Errors()
        {
            string source = @"
            local ok = pcall(rawlen, 5)
            return ok";

            var vm = Utils.Run(source, 100);
            Utils.AssertBoolResult(vm, false);
        }

        #endregion

        #region __index

        [Test]
        public void Index_TableFallback()
        {
            string source = @"
            local defaults = {color = 'red'}
            local t = setmetatable({}, {__index = defaults})
            return t.color";

            var vm = Utils.Run(source, 100);
            Utils.AssertStringResult(vm, "red");
        }

        [Test]
        public void Index_RawHitWins()
        {
            string source = @"
            local defaults = {color = 'red'}
            local t = setmetatable({color = 'blue'}, {__index = defaults})
            return t.color";

            var vm = Utils.Run(source, 100);
            Utils.AssertStringResult(vm, "blue");
        }

        [Test]
        public void Index_MissEverywhere_IsNil()
        {
            string source = @"
            local t = setmetatable({}, {__index = {}})
            return t.missing";

            var vm = Utils.Run(source, 100);
            Utils.AssertNilResult(vm);
        }

        [Test]
        public void Index_ChainedTables()
        {
            string source = @"
            local grandparent = {x = 42}
            local parent = setmetatable({}, {__index = grandparent})
            local child = setmetatable({}, {__index = parent})
            return child.x";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 42);
        }

        [Test]
        public void Index_Function()
        {
            string source = @"
            local t = setmetatable({}, {__index = function(tbl, key)
                return key .. '!'
            end})
            return t.hello";

            var vm = Utils.Run(source, 1000);
            Utils.AssertStringResult(vm, "hello!");
        }

        [Test]
        public void Index_Function_ReceivesTableAndKey()
        {
            string source = @"
            local t
            t = setmetatable({}, {__index = function(tbl, key)
                if tbl == t and key == 'probe' then return 1 else return 0 end
            end})
            return t.probe";

            var vm = Utils.Run(source, 1000);
            Utils.AssertIntegerResult(vm, 1);
        }

        [Test]
        public void Index_Function_Closure_CapturesUpvalue()
        {
            string source = @"
            local count = 0
            local t = setmetatable({}, {__index = function(tbl, key)
                count = count + 1
                return count
            end})
            local a = t.first
            local b = t.second
            return a, b";

            var vm = Utils.Run(source, 1000);
            Utils.AssertIntegerResult(vm, 1, 0);
            Utils.AssertIntegerResult(vm, 2, 1);
        }

        [Test]
        public void Index_BracketAccess()
        {
            string source = @"
            local t = setmetatable({}, {__index = {[5] = 'five'}})
            return t[5]";

            var vm = Utils.Run(source, 100);
            Utils.AssertStringResult(vm, "five");
        }

        [Test]
        public void Index_LoopCap_Errors()
        {
            string source = @"
            local a = {}
            local b = {}
            setmetatable(a, {__index = b})
            setmetatable(b, {__index = a})
            local ok = pcall(function() return a.missing end)
            return ok";

            var vm = Utils.Run(source, 2000);
            Utils.AssertBoolResult(vm, false);
        }

        [Test]
        public void Index_NativeHandler()
        {
            string source = @"
            local native = ...
            local t = setmetatable({}, {__index = native})
            return t.anything";

            var native = new NativeFunctionObject("const_index",
                args => new LuaObject[] { new NumberObject(99) });

            var vm = Utils.Run(source, 100, native);
            Utils.AssertIntegerResult(vm, 99);
        }

        [Test]
        public void Rawget_BypassesIndexMetamethod()
        {
            string source = @"
            local t = setmetatable({}, {__index = {x = 1}})
            local via_meta = t.x
            local raw = rawget(t, 'x')
            return via_meta, raw";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 1, 0);
            Utils.AssertNilResult(vm, 1);
        }

        #endregion

        #region __newindex

        [Test]
        public void Newindex_TableRedirect()
        {
            string source = @"
            local store = {}
            local t = setmetatable({}, {__newindex = store})
            t.x = 5
            local in_t = rawget(t, 'x')
            return store.x, in_t";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 5, 0);
            Utils.AssertNilResult(vm, 1);
        }

        [Test]
        public void Newindex_ExistingKey_RawSet()
        {
            string source = @"
            local store = {}
            local t = setmetatable({x = 1}, {__newindex = store})
            t.x = 2
            local redirected = store.x
            return t.x, redirected";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 2, 0);
            Utils.AssertNilResult(vm, 1);
        }

        [Test]
        public void Newindex_Function()
        {
            string source = @"
            local log = {}
            local t = setmetatable({}, {__newindex = function(tbl, key, value)
                rawset(log, key, value * 2)
            end})
            t.x = 21
            local in_t = rawget(t, 'x')
            return log.x, in_t";

            var vm = Utils.Run(source, 1000);
            Utils.AssertIntegerResult(vm, 42, 0);
            Utils.AssertNilResult(vm, 1);
        }

        [Test]
        public void Rawset_BypassesNewindexMetamethod()
        {
            string source = @"
            local store = {}
            local t = setmetatable({}, {__newindex = store})
            rawset(t, 'x', 7)
            local redirected = store.x
            return t.x, redirected";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 7, 0);
            Utils.AssertNilResult(vm, 1);
        }

        [Test]
        public void Newindex_NilKey_Errors()
        {
            string source = @"
            local t = {}
            local ok = pcall(function() t[nil] = 1 end)
            return ok";

            var vm = Utils.Run(source, 1000);
            Utils.AssertBoolResult(vm, false);
        }

        #endregion

        #region __call

        [Test]
        public void Call_ClosureHandler()
        {
            string source = @"
            local t = setmetatable({}, {__call = function(self, a, b)
                return a + b
            end})
            return t(3, 4)";

            var vm = Utils.Run(source, 1000);
            Utils.AssertIntegerResult(vm, 7);
        }

        [Test]
        public void Call_SelfIsPrepended()
        {
            string source = @"
            local t = setmetatable({factor = 10}, {__call = function(self, x)
                return self.factor * x
            end})
            return t(5)";

            var vm = Utils.Run(source, 1000);
            Utils.AssertIntegerResult(vm, 50);
        }

        [Test]
        public void Call_NoArgs()
        {
            string source = @"
            local t = setmetatable({}, {__call = function(self)
                return 1
            end})
            return t()";

            var vm = Utils.Run(source, 1000);
            Utils.AssertIntegerResult(vm, 1);
        }

        [Test]
        public void Call_NestedCallableTable()
        {
            string source = @"
            local inner = setmetatable({}, {__call = function(self, outer, x)
                return x + 1
            end})
            local outer = setmetatable({}, {__call = inner})
            return outer(41)";

            var vm = Utils.Run(source, 1000);
            Utils.AssertIntegerResult(vm, 42);
        }

        [Test]
        public void Call_VariableArgs()
        {
            string source = @"
            local function pair() return 2, 3 end
            local t = setmetatable({}, {__call = function(self, a, b)
                return a + b
            end})
            return t(pair())";

            var vm = Utils.Run(source, 1000);
            Utils.AssertIntegerResult(vm, 5);
        }

        [Test]
        public void Call_NativeHandler()
        {
            string source = @"
            local native = ...
            local t = setmetatable({}, {__call = native})
            return t(20)";

            // Handler receives (self, 20).
            var native = new NativeFunctionObject("call_handler",
                args => new LuaObject[] { new NumberObject(args.CheckInteger(1) + 1) });

            var vm = Utils.Run(source, 100, native);
            Utils.AssertIntegerResult(vm, 21);
        }

        [Test]
        public void Call_NonCallable_Errors()
        {
            string source = @"
            local t = {}
            local ok = pcall(function() return t() end)
            return ok";

            var vm = Utils.Run(source, 1000);
            Utils.AssertBoolResult(vm, false);
        }

        [Test]
        public void Pcall_CallableTable()
        {
            string source = @"
            local t = setmetatable({}, {__call = function(self, x)
                return x * 2
            end})
            local ok, result = pcall(t, 21)
            return ok, result";

            var vm = Utils.Run(source, 1000);
            Utils.AssertBoolResult(vm, true, 0);
            Utils.AssertIntegerResult(vm, 42, 1);
        }

        [Test]
        public void Pcall_NonCallable_ReturnsFalse()
        {
            string source = @"
            local ok, err = pcall({}, 1)
            return ok";

            var vm = Utils.Run(source, 1000);
            Utils.AssertBoolResult(vm, false);
        }

        [Test]
        public void Call_CallableTableAsIterator()
        {
            string source = @"
            local iter = setmetatable({i = 0}, {__call = function(self, state, control)
                self.i = self.i + 1
                if self.i <= 3 then return self.i end
            end})
            local sum = 0
            for v in iter do
                sum = sum + v
            end
            return sum";

            var vm = Utils.Run(source, 2000);
            Utils.AssertIntegerResult(vm, 6);
        }

        #endregion

        #region arithmetic metamethods

        [Test]
        public void Add_VectorStyle()
        {
            string source = @"
            local mt = {__add = function(a, b)
                return {x = a.x + b.x, y = a.y + b.y}
            end}
            local v1 = setmetatable({x = 1, y = 2}, mt)
            local v2 = setmetatable({x = 10, y = 20}, mt)
            local sum = v1 + v2
            return sum.x, sum.y";

            var vm = Utils.Run(source, 1000);
            Utils.AssertIntegerResult(vm, 11, 0);
            Utils.AssertIntegerResult(vm, 22, 1);
        }

        [Test]
        public void Add_RightOperandFallback()
        {
            string source = @"
            local t = setmetatable({}, {__add = function(a, b)
                if a == 1 then return 100 end
                return 0
            end})
            return 1 + t";

            var vm = Utils.Run(source, 1000);
            Utils.AssertIntegerResult(vm, 100);
        }

        [Test]
        public void Sub_Mul_Div()
        {
            string source = @"
            local mt = {
                __sub = function(a, b) return a.v - b.v end,
                __mul = function(a, b) return a.v * b.v end,
                __div = function(a, b) return a.v / b.v end,
            }
            local a = setmetatable({v = 20}, mt)
            local b = setmetatable({v = 4}, mt)
            local sub = a - b
            local mul = a * b
            local div = a / b
            return sub, mul, div";

            var vm = Utils.Run(source, 2000);
            Utils.AssertIntegerResult(vm, 16, 0);
            Utils.AssertIntegerResult(vm, 80, 1);
            Utils.AssertIntegerResult(vm, 5, 2);
        }

        [Test]
        public void Mod_Pow_Idiv()
        {
            string source = @"
            local mt = {
                __mod = function(a, b) return a.v % b.v end,
                __pow = function(a, b) return a.v ^ b.v end,
                __idiv = function(a, b) return a.v // b.v end,
            }
            local a = setmetatable({v = 9}, mt)
            local b = setmetatable({v = 2}, mt)
            local mod = a % b
            local pow = a ^ b
            local idiv = a // b
            return mod, pow, idiv";

            var vm = Utils.Run(source, 2000);
            Utils.AssertIntegerResult(vm, 1, 0);
            Utils.AssertFloatResult(vm, 81, 1);
            Utils.AssertIntegerResult(vm, 4, 2);
        }

        [Test]
        public void Unm()
        {
            string source = @"
            local t = setmetatable({v = 5}, {__unm = function(a)
                return -a.v
            end})
            return -t";

            var vm = Utils.Run(source, 1000);
            Utils.AssertIntegerResult(vm, -5);
        }

        [Test]
        public void Concat_LeftAndRight()
        {
            string source = @"
            local mt = {__concat = function(a, b)
                local left = type_tag(a)
                local right = type_tag(b)
                return left .. '|' .. right
            end}
            function type_tag(v)
                if getmetatable(v) ~= nil then return 'obj' end
                return v
            end
            local t = setmetatable({}, mt)
            local from_left = t .. 'x'
            local from_right = 'y' .. t
            return from_left, from_right";

            var vm = Utils.Run(source, 3000);
            Utils.AssertStringResult(vm, "obj|x", 0);
            Utils.AssertStringResult(vm, "y|obj", 1);
        }

        [Test]
        public void Len_Metamethod_WinsOverRaw()
        {
            string source = @"
            local t = setmetatable({1, 2, 3}, {__len = function(self)
                return 99
            end})
            return #t";

            var vm = Utils.Run(source, 1000);
            Utils.AssertIntegerResult(vm, 99);
        }

        [Test]
        public void Len_RawWhenNoMetamethod()
        {
            string source = @"
            local t = setmetatable({1, 2, 3}, {})
            return #t";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 3);
        }

        [Test]
        public void Arith_NoMetamethod_ErrorCaughtByPcall()
        {
            string source = @"
            local ok, err = pcall(function() return {} + 1 end)
            return ok";

            var vm = Utils.Run(source, 1000);
            Utils.AssertBoolResult(vm, false);
        }

        [Test]
        public void Concat_NoMetamethod_ErrorCaughtByPcall()
        {
            string source = @"
            local ok = pcall(function() return {} .. 'x' end)
            return ok";

            var vm = Utils.Run(source, 1000);
            Utils.AssertBoolResult(vm, false);
        }

        #endregion

        #region comparison metamethods

        [Test]
        public void Eq_Metamethod()
        {
            string source = @"
            local mt = {__eq = function(a, b) return a.v == b.v end}
            local a = setmetatable({v = 1}, mt)
            local b = setmetatable({v = 1}, mt)
            local c = setmetatable({v = 2}, mt)
            local ab = a == b
            local ac = a == c
            return ab, ac";

            var vm = Utils.Run(source, 2000);
            Utils.AssertBoolResult(vm, true, 0);
            Utils.AssertBoolResult(vm, false, 1);
        }

        [Test]
        public void Eq_SameReference_SkipsMetamethod()
        {
            string source = @"
            local calls = 0
            local mt = {__eq = function(a, b) calls = calls + 1 return false end}
            local a = setmetatable({}, mt)
            local same = a == a
            return same, calls";

            var vm = Utils.Run(source, 2000);
            Utils.AssertBoolResult(vm, true, 0);
            Utils.AssertIntegerResult(vm, 0, 1);
        }

        [Test]
        public void Eq_NotEquals_Inverted()
        {
            string source = @"
            local mt = {__eq = function(a, b) return a.v == b.v end}
            local a = setmetatable({v = 1}, mt)
            local b = setmetatable({v = 1}, mt)
            return a ~= b";

            var vm = Utils.Run(source, 2000);
            Utils.AssertBoolResult(vm, false);
        }

        [Test]
        public void Eq_TableVsNumber_NoMetamethodCall()
        {
            string source = @"
            local mt = {__eq = function(a, b) return true end}
            local a = setmetatable({}, mt)
            return a == 5";

            var vm = Utils.Run(source, 2000);
            Utils.AssertBoolResult(vm, false);
        }

        [Test]
        public void Eq_ResultCoercedToBoolean()
        {
            string source = @"
            local mt = {__eq = function(a, b) return 1 end}
            local a = setmetatable({}, mt)
            local b = setmetatable({}, mt)
            return a == b";

            var vm = Utils.Run(source, 2000);
            Utils.AssertBoolResult(vm, true);
        }

        [Test]
        public void Lt_Le_Metamethods()
        {
            string source = @"
            local mt = {
                __lt = function(a, b) return a.v < b.v end,
                __le = function(a, b) return a.v <= b.v end,
            }
            local small = setmetatable({v = 1}, mt)
            local big = setmetatable({v = 2}, mt)
            local lt = small < big
            local le_eq = small <= setmetatable({v = 1}, mt)
            local lt_false = big < small
            return lt, le_eq, lt_false";

            var vm = Utils.Run(source, 3000);
            Utils.AssertBoolResult(vm, true, 0);
            Utils.AssertBoolResult(vm, true, 1);
            Utils.AssertBoolResult(vm, false, 2);
        }

        [Test]
        public void Gt_Ge_UseSwappedOperands()
        {
            string source = @"
            local mt = {
                __lt = function(a, b) return a.v < b.v end,
                __le = function(a, b) return a.v <= b.v end,
            }
            local small = setmetatable({v = 1}, mt)
            local big = setmetatable({v = 2}, mt)
            local gt = big > small
            local ge = big >= big
            return gt, ge";

            var vm = Utils.Run(source, 3000);
            Utils.AssertBoolResult(vm, true, 0);
            Utils.AssertBoolResult(vm, true, 1);
        }

        [Test]
        public void Lt_InCondition()
        {
            string source = @"
            local mt = {__lt = function(a, b) return a.v < b.v end}
            local a = setmetatable({v = 1}, mt)
            local b = setmetatable({v = 2}, mt)
            if a < b then return 'yes' else return 'no' end";

            var vm = Utils.Run(source, 2000);
            Utils.AssertStringResult(vm, "yes");
        }

        [Test]
        public void Compare_NoMetamethod_ErrorCaughtByPcall()
        {
            string source = @"
            local ok = pcall(function() return {} < {} end)
            return ok";

            var vm = Utils.Run(source, 1000);
            Utils.AssertBoolResult(vm, false);
        }

        [Test]
        public void Compare_ErrorInsideMetamethod_CaughtByPcall()
        {
            string source = @"
            local mt = {__lt = function(a, b) error('boom') end}
            local a = setmetatable({}, mt)
            local b = setmetatable({}, mt)
            local ok, err = pcall(function() return a < b end)
            return ok, err";

            var vm = Utils.Run(source, 2000);
            Utils.AssertBoolResult(vm, false, 0);
            Utils.AssertStringResult(vm, "boom", 1);
        }

        #endregion

        #region OOP pattern

        [Test]
        public void Oop_ClassPattern_MethodCall()
        {
            string source = @"
            local Account = {}
            Account.__index = Account

            function Account.new(balance)
                return setmetatable({balance = balance}, Account)
            end

            function Account:deposit(amount)
                self.balance = self.balance + amount
            end

            function Account:getBalance()
                return self.balance
            end

            local acc = Account.new(100)
            acc:deposit(50)
            acc:deposit(25)
            return acc:getBalance()";

            var vm = Utils.Run(source, 2000);
            Utils.AssertIntegerResult(vm, 175);
        }

        [Test]
        public void Oop_Inheritance()
        {
            string source = @"
            local Animal = {}
            Animal.__index = Animal

            function Animal:speak()
                return self.sound
            end

            local Dog = setmetatable({}, {__index = Animal})
            Dog.__index = Dog

            local rex = setmetatable({sound = 'woof'}, Dog)
            return rex:speak()";

            var vm = Utils.Run(source, 2000);
            Utils.AssertStringResult(vm, "woof");
        }

        [Test]
        public void Index_OnGlobalsTable()
        {
            string source = @"
            local fallback = {answer = 42}
            setmetatable(_ENV, {__index = fallback})
            return answer";

            var vm = Utils.Run(source, 1000);
            Utils.AssertIntegerResult(vm, 42);
        }

        #endregion
    }
}
