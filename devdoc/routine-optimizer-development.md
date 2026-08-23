# Routine optimizer development guide

This guide describes the production routine IR and the rules for changing its optimizer.

## Where the pipeline lives

The shared implementation is in `src/Zilf.Emit/Intermediate`:

- `RoutineIr.cs` defines values, instructions, effects, memory regions, basic blocks, terminators, lowering payloads,
  verification, and per-routine call-effect summaries.
- `IrRoutineBuilder*.cs` implements `IRoutineBuilder` as a partial class. The files separate the builder adapter,
  recording, operation classification, control flow, lifecycle/finalization, and lowering responsibilities.
- `RoutineIrOptimizer*.cs` contains the optimizer coordinator and partial-class files for value preparation, loops,
  CFG and memory analysis, GVN, physical-home preparation, and final simplification.
- `IrRoutineCoordinator.cs` provides the shared Zap/Glulx deferred-finalization and optimizer-statistics lifecycle.

`Zap.GameBuilder` wraps its target routine builder in `IrRoutineBuilder`. `Glulx.GameBuilder` uses
`GlulxIrRoutineBuilder` or `Glulx16IrRoutineBuilder`; these subclasses also record backend capability operations.
Cornerstone still emits directly and does not use this IR. Zap and Glulx retain their peephole optimizers after IR
lowering for target-specific instruction selection and encoding idioms.

For Zap and Glulx, routine `Finish` seals and verifies the IR. The game builder retains sealed routines until its own
`Finish`, then optimizes and lowers them in routine-finish order, matching the historical emission order. This gives
every optimizer invocation a closed set of
completed call summaries. Directly constructed `IrRoutineBuilder` instances still finalize immediately for isolated
backend and unit-test use.

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

Combined increment/decrement branches are recorded as local read-modify-write definitions as well as control-flow
operations. The updated local therefore participates in SSA and loop phis even though lowering still emits one target
`IGRTR?` or `DLESS?` instruction. Treating the branch as control-only can make a loop-carried local appear undefined
and must not be reintroduced: it allowed LICM to hoist a load indexed by the old local value.

The current representation uses explicit SSA for eligible routine locals and compiler temporaries. After recording,
`RoutineIr.PromoteLocalsToSsa` computes reaching local definitions, inserts predecessor-aligned `Phi` instructions at
joins and loop headers, and rewrites local snapshots to their SSA definitions. Globals, indirect variables, story
memory, and the evaluation stack remain outside this promotion. Phi results retain the promoted local as their
physical home; the recorder's edge materializations establish those homes before control transfers, so phi
instructions do not emit target operations during lowering.

After the main optimization fixed point, conservative phi-home coalescing can select a shared incoming local and
remove the corresponding edge materializations. It currently requires plain jump predecessors, every incoming value
to have the same local home, and every use to occur before that home is clobbered in the join block. The Zap cost policy
accepts the rewrite only when it removes more copies than it adds. Critical-edge splitting and parallel-copy cycles are
not handled by this pass.

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
the primary differential-debugging switch; it is not a request to bypass the IR. `DisableRoutineIr` bypasses recording
and emits directly to the target builder; the CLI uses it for `-O0` and `-O1`. Target peephole optimization remains
enabled at every CLI optimization level. `-Oz` runs the IR optimizer and accepts only calls whose target model predicts
a strict encoded-size reduction.

Routine inlining remains a compiler-level transformation before IR construction. Zap exposes an optional target cost
model which prices the normal call and the argument-specialized transplanted body in estimated encoded bytes and
dynamic instructions. `-O2` accepts only non-growing sites, while `-O3` permits bounded growth when dynamic instruction
cost falls. The resulting body then passes through the normal routine IR pipeline, allowing SCCP, GVN, and DCE to
consume constant arguments. Unsupported expressions, unresolved target costs, recursion, and local pressure reject the
site conservatively. Calls accepted by the earlier source-shape policy remain accepted under `-O2` and `-O3` for
compatibility; target costing governs every `-Oz` candidate and the broader candidate set. Backends without an inlining
cost model continue to disable inlining under `-Oz`.

Zap's encoded-size estimate distinguishes call forms, operand bytes, operand-type bytes, result
stores, and branch data. Unknown symbolic constants conservatively use the large-constant cost. Instruction
sequences whose final form depends on lowering remain deliberately pessimistic; pricing callee removal and the
lowered sequence together remains future work.

At each inlining level, the compiler can omit a routine when one caller contains all its direct calls and every call is
inlined. The routine must not escape as a value or be retained by data, syntax, entry-point, or `KEEP` references. Its
compilation is deferred until its caller has been processed, and it is omitted only when no call remains. Under `-Oz`,
this removal also counts toward the estimated size reduction for a routine with one direct call. The credit prices the
candidate body and return but excludes uncertain routine-header and alignment savings. Candidate-to-candidate chains
do not receive removal credit yet, which keeps deferred compilation and nested transplantation conservative. Candidate
discovery counts syntactic calls in every expanded routine, including routines scheduled later during compilation.
Limiting this scan to the initial reachable set can incorrectly omit a callee whose definition remains.

This cost-guided source transplantation does not make routine IR relocatable. Late-IR inlining would first require
structured, cloneable lowering data for labels, returns, physical homes, and stack state instead of backend delegates
captured from the original routine.

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

Within those regions, the recorder assigns exact identities to directly addressed globals and allocated tables. Global
identities use the canonical `IGlobalBuilder` supplied by the game builder. Table identities use internal allocation
metadata preserved through constant address addition; they never depend on rendered operand text. Constant table
indices produce byte ranges, while dynamic indices retain allocation identity with an unknown range. Different table
allocations do not alias, and ranges in one allocation alias only when they overlap or either range is unknown.
Properties, attributes, and object-tree state remain region-wide.

Routine summaries record exact and unknown reads and writes plus non-memory effects. Formal parameters used as table
roots or constant-member objects retain symbolic identities in the summary. Direct-call edges bind those identities to
the caller's constant allocation, object, or enclosing formal parameter; unresolved arguments degrade only the
affected region to an unknown access. Once all sealed routines are available, the binding-aware fixed point computes
transitive effects over the closed direct-call graph, including recursion. Optimizer call handling consumes the bound
exact effects; indirect, external, incomplete, and opaque calls remain region-wide barriers. The IR recorder itself
still invalidates cached mutable global operands at region granularity while recording, because this happens before
closed-world finalization.

A global initialized with an allocated table address can provide table points-to identity when the closed summaries
prove that no routine can write that global. This is alias evidence only: lowering still reads the global normally.

Table points-to identities propagate through SSA copies and constant address addition or subtraction. Phi nodes retain
an allocation when every incoming pointer names that allocation; they retain an exact base offset only when every
incoming offset agrees. A differing or unknown offset never becomes zero implicitly. Exact stores can forward their
stored SSA value to a later matching load when all paths agree, no overlapping write intervenes, and the value still
has a stable non-stack local home. Constants remain forwardable without a physical home.

Stable constant-object and formal-parameter identities also propagate through copies and agreeing phi nodes. This lets
property, attribute, and object-tree reads recover exact identities after SSA rewriting instead of relying only on the
original backend operand. Values may likewise carry a bounded set of known routine targets. A call through such a value
uses the merged closed summaries of those targets; a merge containing an unknown target remains conservative.

GVN computes stable versions for region-wide unknown-write epochs and every exact location used by a routine. Exact
writes update aliasing locations; unknown writes update the region epoch and all exact locations in that region. Joins
use stable merge versions when predecessor versions differ. Read keys contain both their exact-location versions and
the enclosing unknown-write epochs. LICM uses the same alias test for loop-local and summarized call writes.

A call does not inherently overwrite the caller's routine locals. However, values captured from globals or memory must
remain snapshots when required. Never move a local materialization from before a call to after it if the local exists to
preserve a pre-call value.

Complete calls whose closed summaries contain no writes, I/O, nondeterminism, stack effects, or opaque effects are
value-numberable. Internal control flow does not make a call effectful. Their result keys include the routine target,
arguments, unknown-read region versions, and exact read-identity versions. GVN may therefore reuse a result across
dominated control flow, but an overlapping summarized or direct write invalidates it. These dependencies belong to the
call expression, not to values subsequently derived from its immutable result; propagating them into derived values can
spuriously block arithmetic CSE after unrelated calls. Emitted labels are structural control operations rather than I/O;
classifying labels as I/O contaminates every routine summary that contains a label.

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

Memory rejection statistics are also grouped by optimization, candidate opcode, memory region, and a bounded barrier
kind (`exact write`, `unknown write`, `summarized call`, `incomplete call`, or `opaque operation`). Profitability
rejections distinguish GVN and LICM. These aggregate categories intentionally omit routine and operand names so debug
assembly remains deterministic and reasonably compact. When evaluating a change, regenerate `sample/rascal` and
`sample/zork1` with optimization enabled and disabled, then compare these counters before inspecting the affected
routines and assembling both outputs.

Run the fast solution suite after changes:

```powershell
dotnet test Zilf.sln -c Debug --filter "TestCategory!=Slow"
```

Run the complete solution suite, including slow real-world projects, before finishing substantial optimizer work.
Always run tests through `Zilf.sln`.

## Promising future work

An August 2026 Debug `-O2` measurement after adding exact object-read provenance found 653 exact memory locations in
Zork1 (73 recovered object-member reads) and 1,079 in Rascal (228 recovered object-member reads). A subsequent
closed-world points-to catalog propagated routine targets from scalar object properties, exact table entries, small
homogeneous routine tables, immutable routine-pointer globals, local copies, and agreeing phis. It resolved 2 indirect
calls in Zork1 and 20 in Rascal.

Neither change altered emitted instructions: after removing the statistics block and path-dependent `.INSERT` lines,
both games' `-O2` ZAP was identical to the immediately preceding `-O2` revision. GVN still eliminated only 3 of 730
candidates in Zork1 and 76 of 2,331 in Rascal. This shows that aggregate candidate and barrier counts do not establish
usefulness; always compare generated routines against the previous revision at the same optimization level.

A follow-up demand-driven change made complete write-free calls value-numberable and stopped treating emitted labels as
I/O. In Rascal this identified 591 read-only calls and reduced the assembled `-O2` story from 137,352 to 136,956 bytes
(-396). For example, `VALID-INTERIOR-TILE?` now calls `TILE-AT` once instead of three times, while `FLOOR?` calls it
twice instead of six times. Zork1 identified 32 read-only calls but remained 84,208 bytes with no instruction change in
the spot-checked `FIGHT-STRENGTH` sites because their argument lists differ. This is useful evidence that call-result
CSE should precede more elaborate expression summaries. The next work should follow this order:

1. **More precise writes and escaping memory identities.** Exact object-member reads are recovered after SSA, but writes
   through copied object values and additional derived or escaping table pointers still degrade whole regions. Preserve
   those identities and extend store-to-load forwarding where the stored value remains physically available.
2. **Small expression summaries and specialization.** Call-result CSE handles identical targets and arguments. Add a
   bounded representation for simple return expressions only when it exposes a measured common subexpression. The
   duplicated base calculation in Zork1's `FIGHT-STRENGTH` and `FIGHT-STRENGTH,0` calls is the current acceptance case.
   Keep recursion, control-dependent returns, trapping operations, and increased local pressure conservative.
3. **Complete cost-aware phi lowering.** Split redirectable critical edges, schedule parallel copies, and resolve cycles
   with balanced stack scratch storage. Do this only after every combined conditional edge has cloneable retargeting
   metadata and the target policy can price added jumps and stack traffic.
4. **Stack-aware result reuse and the target cost model.** The adjacent repeated `GET SPEC,1` in Rascal remains because
   the second stack result escapes as a call argument. Support rewriting such operands without changing stack order,
   and price instruction forms, required copies, stack traffic, and local pressure as one
   rewrite. Reject algebraic, PRE, and CSE changes that replace work with an equal-cost copy or worse encoding.
5. **Algebraic and loop transformations.** Add fixed-width-safe reassociation, derived induction variables, and strength
   reduction only after the cost model can reject neutral or larger target sequences.
6. **De-opaquify remaining operations selectively.** The remaining opaque operations in these games are mostly reads,
   save/restore, throw, and indirect access. Structure an operation only when its ordering and alias semantics are known;
   the low counts make broad de-opaquification less valuable than call and memory precision.
7. **Global and memory value promotion.** Defer this until aliases, calls, save/restore behavior, and observable physical
   state are modeled well enough to prove correctness.

Continue to improve semantic modeling before adding aggressive rewrites. Existing SCCP, GVN, CFG cleanup, PRE, LICM,
and DCE benefit automatically when fewer calls and memory accesses become region-wide barriers.
