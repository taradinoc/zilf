/* Copyright 2010-2018 Jesse McGrew
 * 
 * This file is part of ZILF.
 * 
 * ZILF is free software: you can redistribute it and/or modify it
 * under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * ZILF is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with ZILF.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Zilf.Diagnostics;
using Zilf.Emit;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;
using JetBrains.Annotations;
using Zilf.Common;

namespace Zilf.Compiler
{
    partial class Compilation
    {
        /// <exception cref="CompilerError">The syntax is incorrect, or an error occurred while compiling a subexpression.</exception>
        [ContractAnnotation("wantResult: false => null")]
        [ContractAnnotation("wantResult: true => notnull")]
        internal IOperand CompilePROG([NotNull] IRoutineBuilder rb, [NotNull] ZilListoidBase args,
            [NotNull] ISourceLine src,
            bool wantResult, [CanBeNull] IVariable resultStorage, [NotNull] string name, bool repeat, bool catchy)
        {
            if (!args.IsCons(out var first, out var rest))
            {
                throw new CompilerError(CompilerMessages._0_Argument_1_2, name, 1, "argument must be an activation atom or binding list");
            }

            if (first is ZilAtom activationAtom)
            {
                args = rest;
            }
            else
            {
                activationAtom = null;
            }

            if (!args.IsCons(out var bindings, out var body) || !(bindings is ZilList bindingList))
            {
                throw new CompilerError(CompilerMessages._0_Missing_Binding_List, name);
            }

            if (!wantResult)
            {
                /* Recognize the "deferred return" pattern:
                 * 
                 *   <PROG (... var ...)
                 *       ...
                 *       <SET var expr>
                 *       ...
                 *       <LVAL var>>
                 *       
                 * In this pattern, the PROG (or BIND) binds an atom and references it exactly twice:
                 * with SET anywhere in the body, and with LVAL as the last expression in the PROG.
                 * 
                 * In void context, since the variable value is discarded anyway, we can eliminate the
                 * store and the binding, transforming it to this:
                 * 
                 *   <PROG (...)
                 *       ...
                 *       expr
                 *       ...>
                 */

                TransformProgArgsIfImplementingDeferredReturn(ref bindingList, ref body);
            }

            // add new locals, if any
            var innerLocals = new Queue<ZilAtom>();

            void AddLocal(ZilAtom atom)
            {
                innerLocals.Enqueue(atom);
                PushInnerLocal(rb, atom);
            }

            void AddLocalWithDefault(ZilAtom atom, ZilObject value)
            {
                innerLocals.Enqueue(atom);
                var lb = PushInnerLocal(rb, atom);
                var loc = CompileAsOperand(rb, value, src, lb);
                if (loc != lb)
                    rb.EmitStore(lb, loc);
            }

            foreach (var obj in bindingList)
            {
                switch (obj)
                {
                    case ZilAtom atom:
                        AddLocal(atom);
                        break;

                    case ZilAdecl adecl when adecl.First is ZilAtom atom:
                        AddLocal(atom);
                        break;

                    case ZilList list when !list.HasLength(2):
                        throw new CompilerError(CompilerMessages._0_Expected_1_Element1s_In_Binding_List, name, 2);

                    case ZilList list when list.Matches(out ZilAtom atom, out ZilObject value):
                        AddLocalWithDefault(atom, value);
                        break;

                    case ZilList list when list.Matches(out ZilAdecl adecl, out ZilObject value) && adecl.First is ZilAtom atom:
                        AddLocalWithDefault(atom, value);
                        break;

                    case ZilAdecl _:
                    case ZilList _:
                        throw new CompilerError(CompilerMessages.Invalid_Atom_Binding);
                        
                    default:
                        throw new CompilerError(CompilerMessages.Elements_Of_Binding_List_Must_Be_Atoms_Or_Lists);
                }
            }

            if (wantResult && resultStorage == null)
                resultStorage = rb.Stack;

            var block = new Block
            {
                Name = activationAtom,
                AgainLabel = rb.DefineLabel(),
                ReturnLabel = rb.DefineLabel(),
                ResultStorage = resultStorage
            };

            if (wantResult)
                block.Flags |= BlockFlags.WantResult;
            if (!catchy)
                block.Flags |= BlockFlags.ExplicitOnly;

            rb.MarkLabel(block.AgainLabel);
            Blocks.Push(block);

            try
            {
                // generate code for prog body
                IOperand result;

                if (wantResult)
                {
                    var clauseResult = CompileClauseBody(rb, body, !repeat, resultStorage);

                    /* The resultStorage we pass to CompileClauseBody (like any other method) is just
                     * a hint, so clauseResult might be different if the result is easily accessible.
                     * For example:
                     * 
                     *     <SET R <PROG () <SET X 123>>>  ;"clauseResult is X"
                     * 
                     *     SET 'X,123       ;inner SET
                     *     SET 'R,X         ;outer SET
                     * 
                     * But we can't always use it as-is, because there might be RETURNs inside that store
                     * a value and branch to the end of the PROG.
                     * 
                     * RETURN always stores the value in our desired resultStorage, so we need to move
                     * the clause body result there too:
                     * 
                     *     <SET R <PROG () <COND (.F <RETURN 123>)> 456>>
                     * 
                     *     ZERO? F /L1
                     *     SET 'R,123       ;RETURN
                     *     JUMP L2
                     * L1: SET 'R,456       ;move clauseResult to resultStorage
                     * L2: ...
                     */

                    if (repeat)
                    {
                        result = resultStorage;
                    }
                    else if (clauseResult != resultStorage && (block.Flags & BlockFlags.Returned) != 0)
                    {
                        rb.EmitStore(resultStorage, clauseResult);
                        result = resultStorage;
                    }
                    else
                    {
                        result = clauseResult;
                    }
                }
                else
                {
                    CompileClauseBody(rb, body, false, null);
                    result = null;
                }

                if (repeat)
                    rb.Branch(block.AgainLabel);

                if ((block.Flags & BlockFlags.Returned) != 0)
                    rb.MarkLabel(block.ReturnLabel);

                return result;
            }
            finally
            {
                while (innerLocals.Count > 0)
                    PopInnerLocal(innerLocals.Dequeue());

                Blocks.Pop();
            }
        }

        [SuppressMessage("ReSharper", "ImplicitlyCapturedClosure")]
        static void TransformProgArgsIfImplementingDeferredReturn([NotNull] ref ZilList bindingList, [NotNull] ref ZilListoidBase body)
        {
            // ends with <LVAL atom>?
            if (!(body.EnumerateNonRecursive().LastOrDefault() is ZilForm lastExpr) || !lastExpr.IsLVAL(out var atom))
                return;

            // atom is bound in the prog?
            if (!GetUninitializedAtomsFromBindingList(bindingList).Contains(atom))
                return;

            // atom is set earlier in the prog?
            var setExpr = body.OfType<ZilForm>()
                .FirstOrDefault(
                    form => form.HasLength(3) &&
                            (form.First as ZilAtom)?.StdAtom == StdAtom.SET &&
                            form.Rest?.First == atom);

            if (setExpr == null)
                return;

            // atom is not referenced anywhere else?
            if (!body.All(zo =>
                ReferenceEquals(zo, setExpr) || ReferenceEquals(zo, lastExpr) || !RecursivelyContains(zo, atom)))
                return;

            // we got a winner!
            bindingList = new ZilList(
                bindingList.Where(zo => GetUninitializedAtomFromBindingListItem(zo) != atom));

            body = new ZilList(
                body
                    .Where(zo => !ReferenceEquals(zo, lastExpr))
                    .Select(zo => ReferenceEquals(zo, setExpr) ? ((IStructure)zo)[2] : zo));
        }

        [NotNull]
        static IEnumerable<ZilAtom> GetUninitializedAtomsFromBindingList([NotNull] ZilListBase bindingList)
        {
            return bindingList.EnumerateNonRecursive()
                .Select(GetUninitializedAtomFromBindingListItem)
                .Where(a => a != null);
        }

        [CanBeNull]
        static ZilAtom GetUninitializedAtomFromBindingListItem([NotNull] ZilObject zo)
        {
            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (zo.StdTypeAtom)
            {
                case StdAtom.ATOM:
                    return (ZilAtom)zo;

                case StdAtom.ADECL:
                    return ((ZilAdecl)zo).First as ZilAtom;

                default:
                    return null;
            }
        }

        static bool RecursivelyContains([NotNull] ZilObject haystack, [NotNull] ZilObject needle)
        {
            if (haystack == needle)
                return true;

            return haystack is IEnumerable<ZilObject> sequence &&
                sequence.Any(zo => RecursivelyContains(zo, needle));
        }

        private interface IBoundedLoopBuilder
        {
            /// <summary>
            /// The name of the loop statement, for use in diagnostics.
            /// </summary>
            [NotNull]
            string Name { get; }

            /// <summary>
            /// Validates the loop syntax and constructs an instance of <see cref="IBoundedLoop"/>.
            /// </summary>
            /// <param name="blc"></param>
            /// <returns></returns>
            /// <exception cref="CompilerError">The loop syntax was incorrect.</exception>
            [NotNull]
            IBoundedLoop MakeLoop(BoundedLoopContext blc);
        }

        private struct BoundedLoopContext
        {
            public readonly Compilation cc;
            public readonly ZilList spec;
            public readonly ISourceLine src;

            public BoundedLoopContext([NotNull] Compilation cc, [NotNull] ZilList spec, [NotNull] ISourceLine src)
            {
                this.cc = cc;
                this.spec = spec;
                this.src = src;
            }
        }

        private interface IBoundedLoop : IDisposable
        {
            /// <summary>
            /// Emit code to be executed before the again label.
            /// </summary>
            /// <param name="rb"></param>
            /// <param name="block"></param>
            /// <param name="exhaustedLabel"></param>
            /// <remarks>
            /// This code should initialize the counter.
            /// </remarks>
            void BeforeBlock([NotNull] IRoutineBuilder rb, [NotNull] Block block, [NotNull] ILabel exhaustedLabel);

            /// <summary>
            /// Emit code to be executed before the loop body on each iteration.
            /// </summary>
            /// <remarks>
            /// For a pre-check loop, this code should branch to the exhausted label to
            /// terminate the loop, or (optionally) advance the counter and branch to the
            /// again label to skip the iteration.
            /// </remarks>
            void BeforeBody();

            /// <summary>
            /// Emit code to be executed after the loop body on each iteration.
            /// </summary>
            /// <remarks>
            /// <para>This code should advance the counter.</para>
            /// <para>For a pre-check loop, it should unconditionally branch
            /// to the again label.</para>
            /// <para>For a post-check loop, it should conditionally branch to
            /// the again label to continue to the next iteration, or fall through
            /// to the exhausted label to terminate the loop.</para>
            /// </remarks>
            void AfterBody();
        }

        private abstract class BoundedLoop : IBoundedLoop
        {
            readonly BoundedLoopContext blc;

            protected BoundedLoop(BoundedLoopContext blc)
            {
                this.blc = blc;
            }

#pragma warning disable IDE1006 // Naming Styles
            // ReSharper disable InconsistentNaming
            [ProvidesContext]
            protected Compilation cc => blc.cc;
            [ProvidesContext]
            protected ISourceLine src => blc.src;
            // ReSharper restore InconsistentNaming
#pragma warning restore IDE1006 // Naming Styles

            IRoutineBuilder rb;
            ILabel againLabel, exhaustedLabel;

            public abstract void Dispose();
            protected abstract void EmitBeforeBlock([NotNull] IRoutineBuilder rb, [NotNull] ILabel exhaustedLabel);
            protected abstract void EmitBeforeBody([NotNull] IRoutineBuilder rb, [NotNull] ILabel againLabel, [NotNull] ILabel exhaustedLabel);
            protected abstract void EmitAfterBody([NotNull] IRoutineBuilder rb, [NotNull] ILabel againLabel);

            public void BeforeBlock(IRoutineBuilder rb, Block block, ILabel exhaustedLabel)
            {
                this.rb = rb;
                this.againLabel = block.AgainLabel;
                this.exhaustedLabel = exhaustedLabel;

                EmitBeforeBlock(rb, exhaustedLabel);
            }

            public void BeforeBody()
            {
                EmitBeforeBody(rb, againLabel, exhaustedLabel);
            }

            public void AfterBody()
            {
                EmitAfterBody(rb, againLabel);
            }
        }

        [CanBeNull]
        [ContractAnnotation("wantResult: true, resultStorage: notnull => notnull")]
        [ContractAnnotation("wantResult: false, resultStorage: null => canbenull")]
        private IOperand CompileBoundedLoop(
            [NotNull] IRoutineBuilder rb, [NotNull] IBoundedLoopBuilder builder,
            [NotNull] ZilListoidBase args, [NotNull] ISourceLine src,
            bool wantResult, [CanBeNull] IVariable resultStorage)
        {
            // extract loop spec ("binding list", although we don't care about the bindings here)
            // TODO: allow activation atoms in bounded loops?
            if (!args.IsCons(out var first, out var rest) || !(first is ZilList spec))
            {
                throw new CompilerError(CompilerMessages.Expected_Binding_List_At_Start_Of_0, builder.Name);
            }

            // instantiate the loop and let it check binding syntax
            var blc = new BoundedLoopContext(this, spec, src);

            using (var loop = builder.MakeLoop(blc))
            {
                // look for the optional end statements
                ZilListoidBase body;
                if (rest.StartsWith(out ZilList endStmts))
                {
                    (_, body) = rest;
                }
                else
                {
                    body = rest;
                }

                // create block
                resultStorage = wantResult ? (resultStorage ?? rb.Stack) : null;

                var block = new Block
                {
                    AgainLabel = rb.DefineLabel(),
                    ResultStorage = resultStorage,
                    ReturnLabel = rb.DefineLabel(),
                    Flags = wantResult ? BlockFlags.WantResult : 0
                };

                Blocks.Push(block);
                try
                {
                    var exhaustedLabel = rb.DefineLabel();

                    // let the loop initialize counters, etc.
                    loop.BeforeBlock(rb, block, exhaustedLabel);

                    // mark the top of the block ("again" label) and let the loop add prechecks, etc.
                    rb.MarkLabel(block.AgainLabel);
                    loop.BeforeBody();

                    // compile the body
                    CompileClauseBody(rb, body, false, null);

                    // let the loop add postchecks, etc., and mark the end of the block ("exhausted" label)
                    loop.AfterBody();

                    rb.MarkLabel(exhaustedLabel);

                    // compile the end statements if present, and provide a return value if requested
                    if (endStmts != null)
                        CompileClauseBody(rb, endStmts, false, null);

                    if (wantResult)
                        rb.EmitStore(resultStorage, Game.One);

                    // if <RETURN> was used inside the loop, mark the return label
                    if ((block.Flags & BlockFlags.Returned) != 0)
                        rb.MarkLabel(block.ReturnLabel);
                }
                finally
                {
                    Blocks.Pop();
                }

                return wantResult ? resultStorage : null;
            }
        }

        /// <exception cref="CompilerError">The syntax is incorrect, or an error occurred while compiling a subexpression.</exception>
        [ContractAnnotation("wantResult: false => null")]
        [ContractAnnotation("wantResult: true => notnull")]
        internal IOperand CompileDO([NotNull] IRoutineBuilder rb, [NotNull] ZilListoidBase args, [NotNull] ISourceLine src, bool wantResult,
            [CanBeNull] IVariable resultStorage)
        {
            return CompileBoundedLoop(rb, DoLoop.Builder, args, src, wantResult, resultStorage);
        }

        private class DoLoop : IBoundedLoopBuilder
        {
            public static readonly IBoundedLoopBuilder Builder = new DoLoop();

            private DoLoop()
            {
            }

            public string Name => "DO";

            public IBoundedLoop MakeLoop(BoundedLoopContext blc)
            {
                if (!blc.spec.HasLength(3, 4))
                {
                    throw new CompilerError(
                        blc.src,
                        CompilerMessages._0_Expected_1_Element1s_In_Binding_List,
                        Name,
                        new CountableString("3 or 4", true));
                }

                if (!blc.spec.Matches(out ZilAtom atom, out ZilObject start, out ZilObject end, out ZilObject inc) &&
                    !blc.spec.Matches(out atom, out start, out end))
                {
                    throw new CompilerError(
                        blc.src,
                        CompilerMessages._0_1_Element_In_Binding_List_Must_Be_2,
                        Name,
                        "first",
                        "an atom");
                }

                return new Loop(blc, atom, start, end, inc);
            }

            private class Loop : BoundedLoop
            {
                readonly ZilAtom atom;
                readonly ZilObject start, end, inc;
                bool precheck;
                ILocalBuilder counter;

                public Loop(BoundedLoopContext blc, ZilAtom atom, ZilObject start, ZilObject end, ZilObject inc)
                    : base(blc)
                {
                    this.atom = atom;
                    this.start = start;
                    this.end = end;
                    this.inc = inc;
                }

                public override void Dispose()
                {
                    if (counter != null)
                        cc.PopInnerLocal(atom);
                }

                protected override void EmitBeforeBlock(IRoutineBuilder rb, ILabel exhaustedLabel)
                {
                    // initialize counter
                    this.counter = cc.PushInnerLocal(rb, atom);
                    var operand = cc.CompileAsOperand(rb, start, src, counter);
                    if (operand != counter)
                        rb.EmitStore(counter, operand);
                }

                protected override void EmitBeforeBody(IRoutineBuilder rb, ILabel againLabel, ILabel exhaustedLabel)
                {
                    // test and branch before the body, if end is a (non-[GL]VAL) FORM
                    if (end.IsNonVariableForm())
                    {
                        cc.CompileCondition(rb, end, end.SourceLine, exhaustedLabel, true);
                        precheck = true;
                    }
                    else
                    {
                        precheck = false;
                    }
                }

                protected override void EmitAfterBody(IRoutineBuilder rb, ILabel againLabel)
                {
                    // increment
                    bool down;
                    if (inc != null)
                    {
                        int incValue;

                        if (inc is ZilFix fix && (incValue = fix.Value) < 0)
                        {
                            rb.EmitBinary(BinaryOp.Sub, counter, cc.Game.MakeOperand(-incValue), counter);
                            down = true;
                        }
                        else if (inc.IsNonVariableForm())
                        {
                            var operand = cc.CompileAsOperand(rb, inc, src, counter);
                            if (operand != counter)
                                rb.EmitStore(counter, operand);
                            down = false;
                        }
                        else
                        {
                            var operand = cc.CompileAsOperand(rb, inc, src);
                            rb.EmitBinary(BinaryOp.Add, counter, operand, counter);
                            down = false;
                        }
                    }
                    else
                    {
                        down = start is ZilFix fix1 && end is ZilFix fix2 && fix2.Value < fix1.Value;
                        rb.EmitBinary(down ? BinaryOp.Sub : BinaryOp.Add, counter, cc.Game.One, counter);
                    }

                    // test and branch after the body, if end is GVAL/LVAL or a constant
                    if (!precheck)
                    {
                        var operand = cc.CompileAsOperand(rb, end, src);
                        rb.Branch(down ? Condition.Less : Condition.Greater, counter, operand, againLabel, false);
                    }
                    else
                    {
                        rb.Branch(againLabel);
                    }
                }
            }
        }

        /// <exception cref="CompilerError">The syntax is incorrect, or an error occurred while compiling a subexpression.</exception>
        [ContractAnnotation("wantResult: false => null")]
        [ContractAnnotation("wantResult: true => notnull")]
        internal IOperand CompileMAP_CONTENTS([NotNull] IRoutineBuilder rb, [NotNull] ZilListoidBase args, [NotNull] ISourceLine src, bool wantResult,
            [CanBeNull] IVariable resultStorage)
        {
            return CompileBoundedLoop(rb, MapContentsLoop.Builder, args, src, wantResult, resultStorage);
        }

        private class MapContentsLoop : IBoundedLoopBuilder
        {
            public static readonly IBoundedLoopBuilder Builder = new MapContentsLoop();

            private MapContentsLoop()
            {
            }

            public string Name => "MAP-CONTENTS";

            public IBoundedLoop MakeLoop(BoundedLoopContext blc)
            {
                if (blc.spec.Matches(out ZilAtom atom, out ZilAtom nextAtom, out ZilObject container))
                    return new TwoVarLoop(blc, atom, nextAtom, container);

                if (blc.spec.Matches(out atom, out container))
                    return new OneVarLoop(blc, atom, container);

                // throw an appropriate error
                var matched = blc.spec.Matches(out ZilObject atomObj, out ZilObject nextAtomObj, out container) ||
                              blc.spec.Matches(out atomObj, out container);

                if (!matched)
                {
                    throw new CompilerError(
                        CompilerMessages._0_Expected_1_Element1s_In_Binding_List,
                        "MAP-CONTENTS",
                        new CountableString("2 or 3", true));
                }

                if (!(atomObj is ZilAtom))
                    throw new CompilerError(CompilerMessages._0_1_Element_In_Binding_List_Must_Be_2, "MAP-CONTENTS", "first", "an atom");

                if (nextAtomObj != null && !(nextAtomObj is ZilAtom))
                    throw new CompilerError(CompilerMessages._0_1_Element_In_Binding_List_Must_Be_2, "MAP-CONTENTS", "middle", "an atom");

                // shouldn't get here
                throw new UnreachableCodeException();
            }

            abstract class LoopBase : BoundedLoop
            {
                [NotNull]
                readonly ZilAtom atom;

                [NotNull]
                readonly ZilObject container;

                protected ILocalBuilder counter;

                protected LoopBase(BoundedLoopContext blc, [NotNull] ZilAtom atom, [NotNull] ZilObject container)
                    : base(blc)
                {
                    this.atom = atom;
                    this.container = container;
                }

                public override void Dispose()
                {
                    if (counter != null)
                        cc.PopInnerLocal(atom);
                }

                protected override void EmitBeforeBlock(IRoutineBuilder rb, ILabel exhaustedLabel)
                {
                    // initialize counter
                    counter = cc.PushInnerLocal(rb, atom);
                    var operand = cc.CompileAsOperand(rb, container, src);
                    rb.EmitGetChild(operand, counter, exhaustedLabel, false);
                }
            }

            private class OneVarLoop : LoopBase
            {
                public OneVarLoop(BoundedLoopContext blc, [NotNull] ZilAtom atom, [NotNull] ZilObject container)
                    : base(blc, atom, container)
                {
                }

                protected override void EmitBeforeBody(IRoutineBuilder rb, ILabel againLabel, ILabel exhaustedLabel)
                {
                    // nada
                }

                protected override void EmitAfterBody(IRoutineBuilder rb, ILabel againLabel)
                {
                    // next object
                    rb.EmitGetSibling(counter, counter, againLabel, true);
                }
            }

            private class TwoVarLoop : LoopBase
            {
                [NotNull]
                readonly ZilAtom nextAtom;

                ILocalBuilder next;

                public TwoVarLoop(BoundedLoopContext blc, [NotNull] ZilAtom atom, [NotNull] ZilAtom nextAtom, [NotNull] ZilObject container)
                    : base(blc, atom, container)
                {
                    this.nextAtom = nextAtom;
                }

                public override void Dispose()
                {
                    if (next != null)
                        cc.PopInnerLocal(nextAtom);

                    base.Dispose();
                }

                protected override void EmitBeforeBody(IRoutineBuilder rb, ILabel againLabel, ILabel exhaustedLabel)
                {
                    // initialize next
                    next = cc.PushInnerLocal(rb, nextAtom);
                    var tempLabel = rb.DefineLabel();
                    rb.EmitGetSibling(counter, next, tempLabel, true);
                    rb.MarkLabel(tempLabel);
                }

                protected override void EmitAfterBody(IRoutineBuilder rb, ILabel againLabel)
                {
                    rb.EmitStore(counter, next);
                    rb.BranchIfZero(counter, againLabel, false);
                }
            }
        }

        /// <exception cref="CompilerError">The syntax is incorrect, or an error occurred while compiling a subexpression.</exception>
        [ContractAnnotation("wantResult: false => null")]
        [ContractAnnotation("wantResult: true => notnull")]
        internal IOperand CompileMAP_DIRECTIONS([NotNull] IRoutineBuilder rb, [NotNull] ZilListoidBase args, [NotNull] ISourceLine src, bool wantResult,
            [CanBeNull] IVariable resultStorage)
        {
            return CompileBoundedLoop(rb, MapDirectionsLoop.Builder, args, src, wantResult, resultStorage);
        }

        private class MapDirectionsLoop : IBoundedLoopBuilder
        {
            public static readonly IBoundedLoopBuilder Builder = new MapDirectionsLoop();

            private MapDirectionsLoop()
            {
            }

            public string Name => "MAP-DIRECTIONS";

            public IBoundedLoop MakeLoop(BoundedLoopContext blc)
            {
                if (blc.spec.Matches(out ZilAtom dirAtom, out ZilAtom ptAtom, out ZilObject room) &&
                    (room.IsLVAL(out _) || room.IsGVAL(out _)))
                {
                    return new Loop(blc, dirAtom, ptAtom, room);
                }

                // throw an appropriate error
                var matched = blc.spec.Matches(out ZilObject dirObj, out ZilObject ptObj, out room);

                if (!matched)
                    throw new CompilerError(CompilerMessages._0_Expected_1_Element1s_In_Binding_List, Name, 3);

                if (!(dirObj is ZilAtom))
                    throw new CompilerError(
                        CompilerMessages._0_1_Element_In_Binding_List_Must_Be_2, Name, "first", "an atom");

                if (!(ptObj is ZilAtom))
                    throw new CompilerError(
                        CompilerMessages._0_1_Element_In_Binding_List_Must_Be_2, Name, "middle", "an atom");

                if (!room.IsLVAL(out _) && !room.IsGVAL(out _))
                    throw new CompilerError(CompilerMessages._0_1_Element_In_Binding_List_Must_Be_2, Name, "last",
                        "an LVAL or GVAL");

                // shouldn't get here
                throw new UnreachableCodeException();
            }

            private class Loop : BoundedLoop
            {
                readonly ZilAtom dirAtom, ptAtom;
                readonly ZilObject room;
                ILocalBuilder counter;

                public Loop(BoundedLoopContext blc, [NotNull] ZilAtom dirAtom, [NotNull] ZilAtom ptAtom,
                    [NotNull] ZilObject room)
                    : base(blc)
                {
                    this.dirAtom = dirAtom;
                    this.ptAtom = ptAtom;
                    this.room = room;
                }

                public override void Dispose()
                {
                    if (counter != null)
                        cc.PopInnerLocal(dirAtom);
                }

                protected override void EmitBeforeBlock(IRoutineBuilder rb, ILabel exhaustedLabel)
                {
                    // initialize counter
                    counter = cc.PushInnerLocal(rb, dirAtom);
                    rb.EmitStore(counter, cc.Game.MakeOperand(cc.Game.MaxProperties + 1));
                }

                protected override void EmitBeforeBody(IRoutineBuilder rb, ILabel againLabel, ILabel exhaustedLabel)
                {
                    // exit loop if no more directions
                    rb.Branch(Condition.DecCheck, counter,
                        cc.Constants[cc.Context.GetStdAtom(StdAtom.LOW_DIRECTION)], exhaustedLabel, true);

                    // get next prop table
                    var propTable = cc.PushInnerLocal(rb, ptAtom);
                    var roomOperand = cc.CompileAsOperand(rb, room, src);
                    rb.EmitBinary(BinaryOp.GetPropAddress, roomOperand, counter, propTable);
                    rb.BranchIfZero(propTable, againLabel, true);
                }

                protected override void EmitAfterBody(IRoutineBuilder rb, ILabel againLabel)
                {
                    rb.Branch(againLabel);
                }
            }
        }
    }
}
