# TickLUA

Ticking Lua, a sandboxed Lua VM in pure C#, built for running player-generated scripts inside video games.<br> 
Its main feature is full controll over script execution speed and memory usage.<br> 
The VM can perform one bytecode instruction at a time with `Tick()`, so a script can never freeze the game loop. 
Combined with call-stack and memory limits, this makes the VM safe to hand to players without threads, watchdogs, or process isolation.

## Creating a VM and execution limits

```csharp
var vm = new TickVM(new TickVMOptions
{
    MaxCallStackDepth = 200,              // frames per coroutine; overflow raises a Lua error
    MaxMemoryBytes    = 1 * 1024 * 1024,  // approximate cap on script-reachable memory
});

vm.Globals["log"] = new NativeFunctionObject("log", ...);  // set up globals

vm.Load(bytecode);                        // load the main chunk

while (!vm.IsFinished)
    vm.Tick();                            // one instruction per call

LuaObject[] results = vm.ExecutionResult; // main chunk's return values
```

A VM can have only one chunk loaded.

Extra `Load` arguments (`vm.Load(bytecode, arg1, arg2)`) become the chunk's `...` varargs and the global `arg` table.<br>
`vm.Load` hands back a `LuaCall` handle that can be used to monitor execution progress of the main chunk, resume it after `coroutine.yield` and get results.<br>

Both limits raise catchable Lua errors (`pcall`) when exceeded.<br>
Unhandled script errors surface as `RuntimeException` from `Tick()`, and also fault the handle.

## Compiling source code

```csharp
using TickLUA.Compilers.LUA;

LuaFunction bytecode = LuaCompiler.Compile(sourceText, "my_script");
```

`Compile` throws `CompilationException` on syntax errors.

## Bytecode serialization

Bytecode can be serialized and saved for later use:

```csharp
byte[] blob = BytecodeSerializer.Serialize(bytecode);                       // keeps debug info
byte[] slim = BytecodeSerializer.Serialize(bytecode, stripDebugInfo: true); // smaller, not debuggable

LuaFunction loaded = BytecodeSerializer.Deserialize(blob);
```

Deserialization throws `BytecodeFormatException` on corrupt or version-mismatched data. Stream overloads exist for both directions.

## Ticker

`Ticker` is a convenience driver over `Tick()` for the common host patterns: a per-frame instruction budget, line stepping, or run-to-completion.

```csharp
var ticker = new Ticker(vm);

TickerResult r = ticker.Tick(500);            // advance up to 500 instructions
r = ticker.TickLine();                        // run until the source line changes
r = ticker.TickToEnd(maxTicks: 100_000);      // run until nothing is left to run
```

Each method returns `Finished`, `Advanced`, or `LimitReached`. On `LimitReached` it can be called again next frame. A typical game integration gives each script N ticks per frame.

It also drives a single script function end to end, resuming it with no arguments whenever it yields:

```csharp
LuaCall call = ticker.RunFunction("global_func");
if (call.Status == LuaCallStatus.Completed)
    var results = call.Result;
```

## Global values

`vm.Globals` is the script's `_ENV`. Read and write it from the host at any time:

```csharp
vm.Globals["difficulty"] = new NumberObject(2);
vm.Globals["player_name"] = new StringObject("Ash");

var score = vm.Globals["score"] as NumberObject;
```

Lua values are `LuaObject` subclasses: `NumberObject`, `StringObject`, `BooleanObject`, `TableObject`, `NilObject.Nil`.

## Registering native functions

Wrap a C# delegate in a `NativeFunctionObject` and put it in globals. `NativeArgs` provides Lua-style argument checking (`CheckNumber`, `CheckString`, `OptInteger`, ...) that produces proper "bad argument #1 to 'name'" errors:

```csharp
vm.Globals["log"] = new NativeFunctionObject("log", args =>
{
    Console.WriteLine(args.CheckString(1));
    return null; // no results
});
```

Natives run synchronously inside one tick and must not re-enter the VM (no `Tick()` from inside a native).

## Calling named script functions

After the main chunk has run (and defined its global functions), the host can invoke script entry points by global name:

```csharp
var call = vm.StartFunction("on_update", new NumberObject(deltaTime)); // throws if missing
vm.TryStartFunction("on_init");                                       // false if missing — for optional hooks

while (!vm.IsFinished)
    vm.Tick(); // the started call runs on subsequent ticks

if (call.Status == LuaCallStatus.Completed)
    Console.WriteLine(call.Result[0]); // what on_update returned
```

The call runs as a fresh coroutine under the same tick/limit regime. `StartFunction` hands back a `LuaCall` handle that can be used to check corutine status after each `Tick()`:

| `Status` | Meaning |
| --- | --- |
| `Running` | still executing, or queued to resume next tick |
| `Paused` | the body yielded; `Result` holds the yielded values |
| `Completed` | the body returned; `Result` holds its return values |
| `Faulted` | an uncaught error killed it; `Error` holds it (and it still propagates out of `Tick()`) |
| `Cancelled` | the host called `Cancel()` |

A yield pauses the call. It must be resumed manually:

```csharp
var call = vm.StartFunction("producer");
while (!call.IsFinished)
{
    vm.Tick();
    if (call.Status == LuaCallStatus.Paused)
        call.Resume(...); // feed the next coroutine.yield()
}
```

`Cancel()` stops the coroutine and releases memory.


## Module loader

`require`/`dofile` ask the host for source through a delegate. The VM never touches the filesystem:

```csharp
vm.ModuleReader = path => path == "utils" ? File.ReadAllText("scripts/utils.lua") : null;
```

Returning `null` makes the require fail with a Lua error; leaving `ModuleReader` unset makes every require fail. Loaded modules are cached in `vm.LoadedModules` (`package.loaded`), which you can also preload to expose modules directly.

## Custom Lua types

Custom types can be created by subclassing `LuaObject` and implementing one of the interfaces:

- `IIndexable` — raw `obj[key]` get/set storage
- `IMetatable` — metamethod support (`__index`, `__call`, operators, ...)
- `IHasLen` — the `#` operator

`MetatableBuilder` builds metatables fluently from native delegates:

```csharp
class Vec2 : LuaObject, IMetatable
{
    public float X, Y;

    private static readonly TableObject SharedMetatable = MetatableBuilder
        .Index(args => /* field lookup */ ...)
        .Add(args => /* vector addition */ ...)
        .Protect("Vec2");

    public TableObject Metatable => SharedMetatable;

    public override StringObject ToStringObject() => new StringObject($"({X}, {Y})");
    public override long ShallowMemoryCost() => 32;
}

vm.Globals["pos"] = new Vec2 { X = 1, Y = 2 };
```

Custom types are protected. Calling `setmetatable` will raise "cannot change a protected metatable". `Protect` sets the `__metatable` field, which closes `getmetatable` forcing it to return a string vaule instead of a table reference.<br>
`ShallowMemoryCost()` is counted towards the memory limits.

## Debugging

`DebugSession` layers stepping, breakpoints, and inspection over `Tick()`. It requires bytecode that was not serialized with `stripDebugInfo`:

```csharp
var dbg = new DebugSession(vm);

dbg.AddBreakpoint(12);                  // any function, line 12
dbg.AddBreakpoint("update", 30);        // specific function

StepResult r = dbg.Continue();          // run to breakpoint / end / budget
r = dbg.StepLine();                     // also: StepInstruction, StepOver, StepOut

int line = dbg.CurrentLine;
var stack = dbg.GetCallStack();         // frames with names, lines, and local variables
```

Like the Ticker, every multi-tick method takes a tick budget and returns `BudgetExhausted` instead of blocking.

## Quirks

Ways TickLUA deliberately diverges from reference Lua.

### `coroutine.isyieldable()` always returns `true`

TickLUA runs every chunk as a call on its own coroutine, the main chunk included (see [`Load`](#creating-a-vm-and-execution-limits)) so yielding is always illegal:

```lua
print(coroutine.isyieldable())  -- true, even at the top level
coroutine.yield(1)              -- legal: pauses the main chunk for the host
```

`coroutine.running()` is unaffected: its second return still reports whether you are in the main chunk.

### Number are 32-bit floats

For simplicity `NumberObject` wraps a single-precision `float`. There is no integer subtype.

### C-style operators

The lexer takes `!=` as a synonym for `~=`:

```lua
if a ~= b then end
if a != b then end   -- valid
```

Compound assignment operators are also supported:

```lua
a += 5      a -= 5
a *= 2      a /= 2      a //= 2
a %= 3      a ^= 2
```