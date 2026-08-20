# Routine optimizer development guide

This guide describes the production routine IR and the rules for changing its optimizer.

## Where the pipeline lives

The shared implementation is in `src/Zilf.Emit/Intermediate`:

- `RoutineIr.cs` defines values, instructions, effects, memory regions, basic blocks, terminators, lowering payloads,
  verification, and per-routine call-effect summaries.
- `IrRoutineBuilder.cs` implements `IRoutineBuilder`. It records Zap and Glulx operations, tracks promoted values and
  virtual stack results, builds the CFG, invokes optimization, and replays surviving operations into the target builder.
- `RoutineIrOptimizer.cs` contains the optimization and physical-home preparation passes.

`Zap.GameBuilder` wraps its target routine builder in `IrRoutineBuilder`. `Glulx.GameBuilder` uses
`GlulxIrRoutineBuilder` or `Glulx16IrRoutineBuilder`; these subclasses also record backend capability operations.
Cornerstone still emits directly and does not use this IR. Zap and Glulx retain their peephole optimizers after IR
lowering for target-specific instruction selection and encoding idioms.

`IRoutineBuilder` is the boundary between the compiler and emission layer. Keep that public interface source-compatible
unless a task explicitly requires an API change.

## Current value and control-flow model

`IrValue` is an immutable identity. It can represent a numeric constant, an immutable external operand, a mutable
external operand, or an instruction result. `PhysicalHome` records where a value can be materialized, but it is not
proof that the home still contains the value at every use. Any optimization that reuses a physical home must separately
prove availability and account for intervening writes.

Routine locals and compiler temporaries are promoted while their values are known and directly addressable. Globals,
indirect variables, story memory, and the evaluation stack are not ordinary promotable locals. Dirty promoted locals
are materialized before an operation that requires their physical state. A local accessed indirectly forces a
conservative flush because the indirect access aliases its physical home.

The current representation uses explicit SSA for eligible routine locals and compiler temporaries. After recording,
`RoutineIr.PromoteLocalsToSsa` computes reaching local definitions, inserts predecessor-aligned `Phi` instructions at
joins and loop headers, and rewrites local snapshots to their SSA definitions. Globals, indirect variables, story
memory, and the evaluation stack remain outside this promotion. Phi results retain the promoted local as their
physical home; the recorder's edge materializations establish those homes before control transfers, so phi
instructions do not emit target operations during lowering.

CFG rewrites must call `RebuildPredecessors`, which also removes obsolete phi inputs. A phi's operands are ordered to
match its block's predecessor list. Optimizations may use this correspondence, but must not treat a phi like an
ordinary instruction whose operands are all evaluated in the phi's block.

Every block must have one terminator: `Jump`, `Branch`, or `Return`. `RoutineIr.Verify` checks basic structural
invariants and rebuilds predecessor lists. It does not prove dominance, effect correctness, stack balance, or physical
home availability; those remain responsibilities of recording and optimization code.

## Optimization pipeline

`RoutineIrOptimizer.Optimize` currently runs these stages:

1. Verify the recorded IR.
2. Promote eligible locals to explicit SSA and insert phi nodes.
3. Protect copies whose source homes will be clobbered.
4. Coalesce and remove materializations, and forward copy destinations.
5. Run sparse conditional constant propagation (SCCP), including phi values.
6. Simplify branches and remove unreachable blocks.
7. Discover reducible natural loops and hoist conservative loop-invariant computations into existing preheaders.
8. Run dominator-based global value numbering (GVN), including supported memory reads and region-versioned state.
9. Iterate constant/copy folding, CFG simplification, dead-instruction removal, and unreachable-block removal to a
   fixed point.
10. Verify the resulting IR.

Lowering then resolves values to physical operands and replays `IrLoweringOperation` delegates. Zap also performs a
size-aware choice between a constant and an equivalent available local. Unresolved symbolic constants, such as object
numbers and vocabulary addresses, must not be assumed to fit in a small-constant encoding merely because their final
Z-machine value is not known yet.

`DisableIrOptimization` skips the optimizer stages but still records, verifies, lowers, and runs target cleanup. This is
the primary differential-debugging switch; it is not a request to bypass the IR.

## Numeric semantics

Always use the routine's `IrNumericSemantics` when evaluating an operation:

- Zap and Glulx16 use Z-machine 16-bit word semantics.
- Native Glulx uses 32-bit semantics.

Constant folding normalizes operands and results accordingly. It must decline unsafe or undefined cases, including
division by zero, signed division overflow, and invalid shift counts. Add tests that demonstrate different 16-bit and
32-bit results whenever an optimization touches arithmetic, comparison, shifts, or overflow.

Do not infer the final encoded size or numeric value of a symbolic target operand from a temporary placeholder. Keep
symbol identity for lowering unless transforming it is necessary, and put target encoding decisions behind a backend
policy or capability.

## Effects, memory, and calls

An instruction is removable only when it is pure. `IrEffect` distinguishes pure work, memory reads and writes, calls,
I/O, nondeterminism, stack operations, control effects, and fully opaque operations. Effectful operations stay ordered,
but not every effect must invalidate every available value:

- I/O such as printing does not write story memory and should not invalidate memory CSE.
- A write invalidates reads only in overlapping `IrMemoryRegion` values.
- A call uses its callee's `IrRoutineEffectSummary` when the callee is known and complete. Unknown or incomplete calls
  conservatively write all memory regions.
- `Opaque` means the optimizer has no useful semantic information and invalidates all memory-dependent availability.

Current memory regions are globals, tables, properties, object tree, and attributes. Region classification is an alias
model, not documentation: an incorrect narrow region can miscompile a game. Use `All` until a narrower classification
is proven.

Within those regions, the recorder assigns exact identities to directly addressed globals. Global identities use the
canonical `IGlobalBuilder` supplied by the game builder, so they remain stable across routine builders. Dynamic or
indirect addresses retain region-wide dependencies. Routine summaries record exact writes, but callers currently
consume their transitive effects at region granularity: the call graph and its summaries are still being completed while
routines are recorded. Incomplete calls, opaque operations, and unclassified writes remain region-wide barriers.

GVN computes a stable version for each memory region at every instruction. Writes and calls produce new versions for
the regions they may change, while joins use a stable merge version when predecessor versions differ. Memory-dependent
expressions include these versions in their value-numbering keys. This avoids repeated path searches and makes loop
back-edge invalidation explicit. Dependencies inherited through mutable external values are included. The versions
remain region-versioned for GVN; exact identities currently refine LICM for writes visible in the same routine.
Extending GVN's version keys and closed-call-graph summaries to these identities remains future work.

A call does not inherently overwrite the caller's routine locals. However, values captured from globals or memory must
remain snapshots when required. Never move a local materialization from before a call to after it if the local exists to
preserve a pre-call value.

## Loop-invariant code motion

The optimizer recognizes natural loops from dominance back edges and processes nested loops from inner to outer. When
a loop lacks a dedicated preheader, the optimizer creates one if every external edge can be represented faithfully by
the current lowering metadata. The recorder inserts the new labeled block immediately before the header in lowering
order. Header phi inputs from external predecessors are merged in the preheader. Unsupported targeted conditional
edges are left unchanged rather than retargeted unsafely.

Eligible instructions must be value-numberable, have loop-invariant operands, and be free of required-home constraints.
A stable non-stack physical home is reused when available. An unescaped virtual-stack result with an `EmitTo` lowering
path is promoted to a compiler temporary when the backend can allocate one; Zap allocation still respects the 15-local
limit. Before allocating, LICM checks the preheader for an equivalent pure expression with an available non-stack home;
such candidates are left for GVN, avoiding an unused temporary and equal-cost copy. Memory reads are hoisted only when
the loop does not write an overlapping region; this check includes state
inherited transitively from mutable globals. The instruction's block must dominate every latch and exit source,
preventing a conditional read from becoming unconditional. Calls, writes, phi nodes, materializations, escaping stack
results, opaque or ordered operations, and potentially trapping arithmetic are not hoisted. Strength reduction and
identity-based memory aliasing remain future work.

The induction-variable pass recognizes single-latch basic induction phis whose update adds or subtracts a constant. If
two promoted locals have equivalent initial values and normalized steps, uses of the redundant induction variable are
rewritten to the canonical one, its update and phi are removed, and obsolete edge materializations for its physical
home are discarded. Different initial values, steps, multiple latches, required homes, and derived induction variables
are currently left unchanged.

## Adding or de-opaquifying an IR operation

When an `IRoutineBuilder` operation gains structured semantics, review every item below. Missing one can either block
the intended optimization or make it unsound.

1. **IR vocabulary:** Add or reuse an `IrOpcode` in `RoutineIr.cs`. Decide its operand order, whether it defines a
   result, and whether the result is a value, predicate, address, or target-specific token.
2. **Recording:** Update the relevant `IrRoutineBuilder` entry point and its `TryMap`, `TryMapMemoryRead`, or
   `TryClassify` helper. Use `GetValue` for operands and `SetProducedValue` for a local or virtual-stack result. Preserve
   combined store-and-branch semantics when the target operation combines them.
3. **Effects and aliasing:** Assign the narrowest proven `IrEffect`. For memory access, update `GetReadRegions` or the
   appropriate `GetWriteRegions` mapping. If it is a call, attach a call summary. If semantics are incomplete, keep the
   operation effectful; do not mark it pure just to make DCE or GVN see it.
4. **Lowering:** Supply an `IrLoweringOperation` replay delegate. Result-producing operations should normally supply
   `EmitTo` so destination coalescing can select a different physical home. Set `ResultHome`, `IsStackResult`,
   `StackEscapes`, `RequiredHome`, and `IsMaterialization` consistently.
5. **Optimizer evaluation:** If the operation is foldable, add its exact semantics to `TryEvaluate` and, when useful,
   `TryEvaluateKnownFacts`. Include refusal cases as well as successful folds.
6. **GVN and memory dependencies:** If it is safe to common, add it to `IsValueNumberable`; update `IsMemoryRead`,
   commutativity, and the optimizer's memory-region dependency mapping as applicable. Memory reads require both
   expression equality and proof that no relevant write occurs on any path.
7. **Copy/DCE behavior:** Check whether required physical homes prevent replacement, whether a folded operation must be
   rewritten as a target copy, and whether `IsRemovable` remains correct.
8. **Backend extensions:** Update `GlulxIrRoutineBuilder` or another wrapper for capability-interface methods that do not
   pass through the base `IRoutineBuilder` API. Keep unsupported target extensions explicitly effectful.
9. **Tests:** Add recording/lowering tests and optimizer tests in `RoutineIrTests.cs`. Cover target semantics, effects,
   calls or writes between repeated expressions, stack results, physical-home clobbers, CFG joins, and both optimized
   and disabled modes where relevant. Verify emitted Zap or Glulx instructions, not only the IR shape.

Prefer an accurately classified `TargetOperation` over inventing a portable opcode when no optimization consumes its
semantics. Moving an operation from `Opaque` to `InputOutput`, `Control`, `Stack`, or a region-specific memory effect can
still unlock value availability across it.

## Physical homes and stack results

An IR equality does not guarantee that a reusable machine operand exists. GVN may remove an expression only when its
earlier result still has a valid home, or when promoting a virtual stack result to a local is profitable and safe.
Relevant mechanisms include liveness from `FindLiveHomesAfter`, home reservations, reusable compiler temporaries, and
`CanReusePhysicalHome`.

The evaluation stack is not a freely reusable register. Reusing or deleting a stack-producing instruction can change
stack depth even when values are equal. Respect `IsStackResult` and `StackEscapes`, and test balanced behavior across
calls, branches, and ordered operations. Avoid increasing the Z-machine's 15-local limit; temporary scavenging must use
dead homes or decline the optimization.

## How to evaluate an optimization

Compiler tests establish safety, but real generated code establishes usefulness. For Advent, Rascal, Zork, or another
representative project:

1. Compile once with normal optimization and once with `DisableIrOptimization`.
2. Diff the generated `.zap` or Glulx assembly. Ignore label renumbering and inspect changed routines instruction by
   instruction.
3. Classify each change: removed computation, folded branch, reused load, better destination, encoding improvement, or
   incidental layout change. A moved `SET` is not automatically an optimization and may expose a snapshot bug.
4. Assemble both outputs and compare story sizes, but do not use size alone as evidence.
5. Run the stories or the available project tests. Pay special attention to calls, indirect locals, memory mutation,
   stack balance, optional parameters, and debug builds.

Debug builds append stable routine-IR statistics to the generated assembly. These include input and output instruction
and opaque-operation counts, applications by optimization, GVN physical-availability rejections, and opaque recording
fallbacks grouped by operation. Use them to determine whether the source truly lacks candidates, recording left the
operation opaque, an effect invalidated availability, or physical-home profitability rejected the rewrite.

Run the fast solution suite after changes:

```powershell
dotnet test Zilf.sln -c Debug --filter "TestCategory!=Slow"
```

Run the complete solution suite, including slow real-world projects, before finishing substantial optimizer work.
Always run tests through `Zilf.sln`.

## Promising future work

These are directions, not assumptions that the prerequisites already exist:

- **De-opaquify remaining operations.** First classify operations that are ordered but do not mutate story memory, then
  structure result-producing reads and predicates. This often unlocks existing GVN without adding a new pass.
- **More aggressive phi lowering.** The current edge materializations avoid critical-edge and parallel-copy hazards
  without consuming extra Z locals. A future pass could remove more of those stores by splitting critical edges and
  resolving parallel-copy cycles with stack-backed scratch storage when profitable.
- **More precise versioned memory.** Add exact global and constant-base identities to GVN's version keys, extend proven
  identities to objects and properties, and add store-to-load forwarding where address and value are available.
- **A target cost model.** Compare immediate size, variable operands, instruction form, required copies, stack traffic,
  and local pressure. An algebraic or CSE rewrite should be rejected when it merely replaces an instruction with an
  equal-cost copy or forces a worse encoding.
- **Algebraic simplification and reassociation.** Extend identities cautiously under fixed-width semantics. Reassociation
  can expose constants and common subexpressions but can also change overflow behavior if modeled incorrectly.
- **More loop optimization.** Extend basic induction recognition to derived induction variables and strength reduction
  after a target cost model is available, and teach branch lowering to retarget every conditional edge form.
- **Stronger interprocedural summaries.** More precise read/write and purity summaries can preserve values across known
  calls. Recursive and indirect calls require conservative fixed-point handling.
- **Global and memory value promotion.** Defer this until aliasing, calls, save/restore behavior, and observable physical
  state are modeled well enough to prove correctness.
The usual priority is to improve semantic modeling before adding a more aggressive rewrite. Existing SCCP, GVN, CFG
cleanup, and DCE become more effective as fewer operations are opaque and as memory and physical availability become
more precise.
