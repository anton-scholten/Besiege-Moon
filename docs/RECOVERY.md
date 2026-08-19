# How the source was recovered

The C# for this mod was lost; only the shipped `MoonAssembly.dll` (20,992 bytes,
built 2018) survived. `Moon/MoonScripts/` was reconstructed from that assembly
and then checked against it. This is the record of how, and of how much the
result can be trusted.

## The tooling

No .NET toolchain is installed on this machine and none was added. Everything
came out of the game's own `Besiege_Data/Managed`:

- **Reading the assembly**: `Mono.Cecil.dll`, which Besiege ships. A small
  dumper walks the metadata — types, base types, fields with their flags,
  method signatures, custom attributes, locals, exception handlers — and prints
  every method body as an instruction list with branch targets resolved to the
  *ordinal* of the target instruction rather than a byte offset, so two builds
  with different encodings can still be compared line by line.
- **Running the dumper, and rebuilding the mod**: Besiege's own `mcs.dll`,
  driven offline through the game's `libmono.so`. That is what `tools/build.sh`
  does, and the same host runs the dumper against an assembly.

Worth knowing if this is ever done again:

- Cecil's `Instruction.Operand` for `ldstr` is already the decoded string, and
  for a branch it is the target `Instruction` object — not a token and not a
  displacement. Building an ordinal map over `Body.Instructions` first, and
  printing operands through it, is what makes the output diffable.
- The dumper has to be compiled to the 2.0 profile to run under the game's Mono.
  `tools/build.sh`'s compiler host does that anyway, so the dumper is built the
  same way as the mod.

## What the assembly gave up

Everything structural survives compilation and was read directly rather than
guessed: the seven types and their base types, every field with its type and its
`public`/`private`/`static` flags, every method signature and its accessibility,
the `[XmlRoot]` arguments naming the two block-module elements, the `Text` and
`GS_mapping` auto-properties (backing fields plus `MethodSemantics` rows pairing
the accessors), the compiler-generated iterator behind `DeleteThis`, and all
four assembly references.

What does **not** survive is what you would expect: local variable names,
parameter names of private methods where the `Param` table was not emitted,
comments, and the file layout. Local names in the reconstruction are chosen for
readability — `distance` in `Moon.FixedUpdate` was `V_9`.

## The original was not built with Besiege's compiler

This matters for how the check below is read. The 2018 assembly is a **Debug**
build produced by Microsoft's C# compiler: it carries `[Debuggable]`, every
method is padded with `nop`, and the iterator state machine is named
`<DeleteThis>d__4` in the csc style. The rebuild is a Release build from
Besiege's `mcs`, which names the same class `<DeleteThis>c__Iterator0` and
implements it differently.

So a byte-for-byte comparison was never available. What is available is a
comparison of what each method *does*, which is the thing worth checking.

## How the reconstruction was checked

Both assemblies were dumped and compared method by method on their semantic
content: which members are called, which fields are read and written, and which
constants and strings appear — ignoring locals, branch encodings, stack
shuffling and conversions, since those are exactly what the two compilers are
free to disagree about. The systematic disagreements are:

| in the original (csc, Debug) | in the rebuild (mcs, Release) |
| --- | --- |
| `nop` between every statement | absent |
| every condition spilled: `stloc.N` / `ldloc.N` / `brfalse` | `brfalse` straight off the stack |
| `!x` as `ldc.i4.0` / `ceq`; `x != k` as `ceq` / `ldc.i4.0` / `ceq` | the fused `brtrue`, `bne.un`, `bge.un`, `ble.un` |
| every `return` routed through one shared exit `ret` | `ret` in place |
| object initialisers built with `dup` | built through a temp local |
| `call Int32::ToString()` | `constrained.` + `callvirt Object::ToString()` |
| iterator `<M>d__N`, `<>1__state`, `<>2__current`, `<>4__this` | `<M>c__Iterator0`, `$PC`, `$current`, `$this` |

Result: **49 methods compared, and every hand-written one matched.** The only
four that did not are inside the compiler-generated iterator itself — mcs adds a
`$disposing` flag where csc encodes the same thing in the state number, and the
two spell the state-machine constructor differently. No call in a hand-written
method goes to a different member, no constant differs, no field access is
missing on either side.

The comparison found one real transcription error before it passed, which is the
argument for doing it: the moon's `MeshCollider` physic material was read as
`bounceCombine = Maximum, frictionCombine = Multiply` where the assembly says
`Minimum` and `Maximum`. Nothing else in the reconstruction was wrong.

That comparison is worth keeping if the sources are ever touched again in a way
that is meant to be behaviour-preserving; the scripts that produced it are not
in the repo, but the method is a page of Python over the dumper's output.

## Reading the comparison the other way

Two things the check does **not** prove, and neither is a defect in the method:

- It says the reconstruction matches the 2018 assembly. It says nothing about
  whether the 2018 assembly was *correct* — and it was not. See the fixes in
  [AGENTS.md](../AGENTS.md#what-was-wrong-with-it), all of which were made after
  the check passed, and each of which now shows up in it as a difference in
  exactly the method it was made in.
- `float` constants in the dump are printed as their decimal expansions
  (`0.05000000074505806`). Those are exactly `0.05f` and friends; the source uses
  the short form and recompiles to the same bits.
