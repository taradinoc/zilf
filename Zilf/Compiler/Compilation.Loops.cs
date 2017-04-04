/* Copyright 2010-2017 Jesse McGrew
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
using System.Diagnostics.Contracts;
using System.Linq;
using Zilf.Diagnostics;
using Zilf.Emit;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;

namespace Zilf.Compiler
{
    partial class Compilation
    {
        IOperand CompilePROG(IRoutineBuilder rb, ZilList args,
#pragma warning disable RECS0154 // Parameter is never used
            ISourceLine src,
            bool wantResult, IVariable resultStorage, string name, bool repeat, bool catchy)
#pragma warning restore RECS0154 // Parameter is never used
        {
            Contract.Requires(rb != null);
            Contract.Requires(src != null);

            // NOTE: resultStorage is unused here, because PROG's result could come from
            // a RETURN statement (and REPEAT's result can *only* come from RETURN).
            // thus we have to return the result on the stack, because RETURN doesn't have
            // the context needed to put its result in the right place.
            // TODO: make resultStorage a field in the Block so RETURN has that context.

            if (args == null || args.First == null)
            {
                throw new CompilerError(CompilerMessages._0_Argument_1_2, name, 1, "argument must be an activation atom or binding list");
            }

            var activationAtom = args.First as ZilAtom;
            if (activationAtom != null)
            {
                args = args.Rest;
            }

            if (args == null || args.First == null ||
                args.First.StdTypeAtom != StdAtom.LIST)
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

                args = TransformProgArgsIfImplementingDeferredReturn(args);
            }

            // add new locals, if any
            var innerLocals = new Queue<ZilAtom>();
            foreach (ZilObject obj in (ZilList)args.First)
            {
                ZilAtom atom;

                switch (obj.StdTypeAtom)
                {
                    case StdAtom.ATOM:
                        atom = (ZilAtom)obj;
                        innerLocals.Enqueue(atom);
                        PushInnerLocal(rb, atom);
                        break;

                    case StdAtom.ADECL:
                        atom = ((ZilAdecl)obj).First as ZilAtom;
                        if (atom == null)
                            throw new CompilerError(CompilerMessages.Invalid_Atom_Binding);
                        innerLocals.Enqueue(atom);
                        PushInnerLocal(rb, atom);
                        break;

                    case StdAtom.LIST:
                        var list = (ZilList)obj;
                        if (list.First == null || list.Rest == null ||
                            list.Rest.First == null || (list.Rest.Rest != null && list.Rest.Rest.First != null))
                        {
                            throw new CompilerError(CompilerMessages._0_Expected_1_Element1s_In_Binding_List, name, 2);
                        }
                        atom = list.First as ZilAtom;
                        if (atom == null)
                        {
                            if (list.First is ZilAdecl adecl)
                                atom = adecl.First as ZilAtom;
                        }
                        ZilObject value = list.Rest.First;
                        if (atom == null)
                            throw new CompilerError(CompilerMessages.Invalid_Atom_Binding);
                        innerLocals.Enqueue(atom);
                        var lb = PushInnerLocal(rb, atom);
                        var loc = CompileAsOperand(rb, value, src, lb);
                        if (loc != lb)
                            rb.EmitStore(lb, loc);
                        break;

                    default:
                        throw new CompilerError(CompilerMessages.Elements_Of_Binding_List_Must_Be_Atoms_Or_Lists);
                }
            }

            var block = new Block
            {
                Name = activationAtom,
                AgainLabel = rb.DefineLabel(),
                ReturnLabel = rb.DefineLabel()
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
                args = args.Rest;
                var clauseResult = CompileClauseBody(rb, args, wantResult, rb.Stack);

                if (repeat)
                    rb.Branch(block.AgainLabel);

                if ((block.Flags & BlockFlags.Returned) != 0)
                    rb.MarkLabel(block.ReturnLabel);

                return wantResult ? clauseResult : null;
            }
            finally
            {
                while (innerLocals.Count > 0)
                    PopInnerLocal(innerLocals.Dequeue());

                Blocks.Pop();
            }
        }

        static ZilList TransformProgArgsIfImplementingDeferredReturn(ZilList args)
        {
            if (args.First is ZilList bindingList && bindingList.StdTypeAtom == StdAtom.LIST)
            {
                // ends with <LVAL atom>?
                if (args.EnumerateNonRecursive().LastOrDefault() is ZilForm lastExpr &&
                    lastExpr.IsLVAL(out var atom))
                {
                    // atom is bound in the prog?
                    if (GetUninitializedAtomsFromBindingList(bindingList).Contains(atom))
                    {
                        // atom is set earlier in the prog?
                        var setExpr = args.Rest.OfType<ZilForm>().FirstOrDefault(form =>
                                ((IStructure)form).GetLength(3) == 3 &&
                                ((form.First as ZilAtom)?.StdAtom == StdAtom.SET) &&
                                form.Rest.First == atom);

                        if (setExpr != null)
                        {
                            // atom is not referenced anywhere else?
                            if (args.Rest.All(zo => zo == setExpr || zo == lastExpr || !RecursivelyContains(zo, atom)))
                            {
                                // we got a winner!
                                var newBindingList = new ZilList(
                                    bindingList.Where(
                                        zo => GetUninitializedAtomFromBindingListItem(zo) != atom));

                                var newBody = new ZilList(
                                    args.Rest
                                    .Where(zo => zo != lastExpr)
                                    .Select(zo => zo == setExpr ? ((IStructure)zo)[2] : zo));

                                return new ZilList(newBindingList, newBody);
                            }
                        }
                    }
                }
            }

            return args;
        }

        static IEnumerable<ZilAtom> GetUninitializedAtomsFromBindingList(ZilList bindingList)
        {
            Contract.Assert(bindingList != null);
            Contract.Ensures(Contract.Result<IEnumerable<ZilAtom>>() != null);

            return bindingList.EnumerateNonRecursive()
                .Select(GetUninitializedAtomFromBindingListItem)
                .Where(a => a != null);
        }

        static ZilAtom GetUninitializedAtomFromBindingListItem(ZilObject zo)
        {
            Contract.Assert(zo != null);

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

        static bool RecursivelyContains(ZilObject haystack, ZilObject needle)
        {
            if (Equals(haystack, needle))
                return true;

            if (haystack is IEnumerable<ZilObject> sequence)
            {
                foreach (var zo in sequence)
                {
                    if (RecursivelyContains(zo, needle))
                        return true;
                }
            }

            return false;
        }

        [SuppressMessage("Microsoft.Contracts", "TestAlwaysEvaluatingToAConstant", Justification = "block.Flags can be changed by other methods")]
        IOperand CompileDO(IRoutineBuilder rb, ZilList args, ISourceLine src, bool wantResult,
#pragma warning disable RECS0154 // Parameter is never used
            IVariable resultStorage)
#pragma warning restore RECS0154 // Parameter is never used
        {
            Contract.Requires(rb != null);
            Contract.Requires(args != null);
            Contract.Requires(src != null);
            Contract.Ensures(Contract.Result<IOperand>() != null || !wantResult);

            // resultStorage is unused here for the same reason as in CompilePROG.

            // parse binding list
            if (!(args.First is ZilList spec) || spec.StdTypeAtom != StdAtom.LIST)
            {
                throw new CompilerError(CompilerMessages.Expected_Binding_List_At_Start_Of_0, "DO");
            }

            var specLength = ((IStructure)spec).GetLength(4);
            if (specLength < 3 || specLength == null)
            {
                throw new CompilerError(
                    CompilerMessages._0_Expected_1_Element1s_In_Binding_List,
                    "DO",
                    new CountableString("3 or 4", true));
            }

            if (!(spec.First is ZilAtom atom))
            {
                throw new CompilerError(CompilerMessages._0_1_Element_In_Binding_List_Must_Be_2, "DO", "first", "an atom");
            }

            var start = spec.Rest.First;
            var end = spec.Rest.Rest.First;

            // look for an end block
            var body = args.Rest;
            if (body.First is ZilList endStmts && endStmts.StdTypeAtom == StdAtom.LIST)
            {
                body = body.Rest;
            }
            else
            {
                endStmts = null;
            }

            // create block
            var block = new Block
            {
                AgainLabel = rb.DefineLabel(),
                ReturnLabel = rb.DefineLabel(),
                Flags = wantResult ? BlockFlags.WantResult : 0
            };

            Blocks.Push(block);

            var exhaustedLabel = rb.DefineLabel();

            // initialize counter
            var counter = PushInnerLocal(rb, atom);
            var operand = CompileAsOperand(rb, start, src, counter);
            if (operand != counter)
                rb.EmitStore(counter, operand);

            rb.MarkLabel(block.AgainLabel);

            // test and branch before the body, if end is a (non-[GL]VAL) FORM
            bool testFirst;
            if (end.IsNonVariableForm())
            {
                CompileCondition(rb, end, end.SourceLine, exhaustedLabel, true);
                testFirst = true;
            }
            else
            {
                testFirst = false;
            }

            // body
            while (body != null && !body.IsEmpty)
            {
                // ignore the results of all statements
                CompileStmt(rb, body.First, false);
                body = body.Rest;
            }

            // increment
            bool down;
            if (specLength == 4)
            {
                var inc = spec.Rest.Rest.Rest.First;
                int incValue;

                if (inc is ZilFix fix && (incValue = fix.Value) < 0)
                {
                    rb.EmitBinary(BinaryOp.Sub, counter, Game.MakeOperand(-incValue), counter);
                    down = true;
                }
                else if (inc.IsNonVariableForm())
                {
                    operand = CompileAsOperand(rb, inc, src, counter);
                    if (operand != counter)
                        rb.EmitStore(counter, operand);
                    down = false;
                }
                else
                {
                    operand = CompileAsOperand(rb, inc, src);
                    rb.EmitBinary(BinaryOp.Add, counter, operand, counter);
                    down = false;
                }
            }
            else
            {
                down = (start is ZilFix fix1 && end is ZilFix fix2 && fix2.Value < fix1.Value);
                rb.EmitBinary(down ? BinaryOp.Sub : BinaryOp.Add, counter, Game.One, counter);
            }

            // test and branch after the body, if end is GVAL/LVAL or a constant
            if (!testFirst)
            {
                operand = CompileAsOperand(rb, end, src);
                rb.Branch(down ? Condition.Less : Condition.Greater, counter, operand, block.AgainLabel, false);
            }
            else
            {
                rb.Branch(block.AgainLabel);
            }

            // exhausted label, end statements, provide a return value if we need one
            rb.MarkLabel(exhaustedLabel);

            while (endStmts != null && !endStmts.IsEmpty)
            {
                CompileStmt(rb, endStmts.First, false);
                endStmts = endStmts.Rest;
            }

            if (wantResult)
                rb.EmitStore(rb.Stack, Game.One);

            // clean up block and counter
            if ((block.Flags & BlockFlags.Returned) != 0)   // Code Contracts message is suppressed on this line (see attribute)
                rb.MarkLabel(block.ReturnLabel);

            PopInnerLocal(atom);

            Blocks.Pop();

            return wantResult ? rb.Stack : null;
        }

        IOperand CompileMAP_CONTENTS(IRoutineBuilder rb, ZilList args, ISourceLine src, bool wantResult,
#pragma warning disable RECS0154 // Parameter is never used
            IVariable resultStorage)
#pragma warning restore RECS0154 // Parameter is never used
        {
            Contract.Requires(rb != null);
            Contract.Requires(args != null);
            Contract.Requires(src != null);
            Contract.Ensures(Contract.Result<IOperand>() != null || !wantResult);

            // parse binding list
            if (!(args.First is ZilList spec) || spec.StdTypeAtom != StdAtom.LIST)
            {
                throw new CompilerError(CompilerMessages.Expected_Binding_List_At_Start_Of_0, "MAP-CONTENTS");
            }

            var specLength = ((IStructure)spec).GetLength(3);
            if (specLength < 2 || specLength == null)
            {
                throw new CompilerError(
                    CompilerMessages._0_Expected_1_Element1s_In_Binding_List,
                    "MAP-CONTENTS",
                    new CountableString("2 or 3", true));
            }

            if (!(spec.First is ZilAtom atom))
            {
                throw new CompilerError(CompilerMessages._0_1_Element_In_Binding_List_Must_Be_2, "MAP-CONTENTS", "first", "an atom");
            }

            ZilAtom nextAtom;
            ZilObject container;
            if (specLength == 3)
            {
                nextAtom = spec.Rest.First as ZilAtom;
                if (nextAtom == null)
                {
                    throw new CompilerError(CompilerMessages._0_1_Element_In_Binding_List_Must_Be_2, "MAP-CONTENTS", "middle", "an atom");
                }

                container = spec.Rest.Rest.First;
            }
            else
            {
                nextAtom = null;
                container = spec.Rest.First;
            }
            Contract.Assume(container != null);

            // look for an end block
            var body = args.Rest;
            if (body.First is ZilList endStmts && body.First.StdTypeAtom == StdAtom.LIST)
            {
                body = body.Rest;
            }
            else
            {
                endStmts = null;
            }

            // create block
            var block = new Block
            {
                AgainLabel = rb.DefineLabel(),
                ReturnLabel = rb.DefineLabel(),
                Flags = wantResult ? BlockFlags.WantResult : 0
            };

            Blocks.Push(block);

            var exhaustedLabel = rb.DefineLabel();

            // initialize counter
            var counter = PushInnerLocal(rb, atom);
            var operand = CompileAsOperand(rb, container, src);
            rb.EmitGetChild(operand, counter, exhaustedLabel, false);

            rb.MarkLabel(block.AgainLabel);

            // loop over the objects using one or two variables
            if (nextAtom != null)
            {
                // initialize next
                var next = PushInnerLocal(rb, nextAtom);
                var tempLabel = rb.DefineLabel();
                rb.EmitGetSibling(counter, next, tempLabel, true);
                rb.MarkLabel(tempLabel);

                // body
                while (body != null && !body.IsEmpty)
                {
                    // ignore the results of all statements
                    CompileStmt(rb, body.First, false);
                    body = body.Rest;
                }

                // next object
                rb.EmitStore(counter, next);
                rb.BranchIfZero(counter, block.AgainLabel, false);

                // clean up next
                PopInnerLocal(nextAtom);
            }
            else
            {
                // body
                while (body != null && !body.IsEmpty)
                {
                    // ignore the results of all statements
                    CompileStmt(rb, body.First, false);
                    body = body.Rest;
                }

                // next object
                rb.EmitGetSibling(counter, counter, block.AgainLabel, true);
            }

            // exhausted label, end statements, provide a return value if we need one
            rb.MarkLabel(exhaustedLabel);

            while (endStmts != null && !endStmts.IsEmpty)
            {
                CompileStmt(rb, endStmts.First, false);
                endStmts = endStmts.Rest;
            }

            if (wantResult)
                rb.EmitStore(rb.Stack, Game.One);

            // clean up block and counter
            rb.MarkLabel(block.ReturnLabel);

            PopInnerLocal(atom);

            Blocks.Pop();

            return wantResult ? rb.Stack : null;
        }

        IOperand CompileMAP_DIRECTIONS(IRoutineBuilder rb, ZilList args, ISourceLine src, bool wantResult,
#pragma warning disable RECS0154 // Parameter is never used
            IVariable resultStorage)
#pragma warning restore RECS0154 // Parameter is never used
        {
            Contract.Requires(rb != null);
            Contract.Requires(args != null);
            Contract.Requires(src != null);
            Contract.Ensures(Contract.Result<IOperand>() != null || !wantResult);

            // parse binding list
            if (!(args.First is ZilList spec) || spec.StdTypeAtom != StdAtom.LIST)
            {
                throw new CompilerError(CompilerMessages.Expected_Binding_List_At_Start_Of_0, "MAP-DIRECTIONS");
            }

            var specLength = ((IStructure)spec).GetLength(3);
            if (specLength != 3)
            {
                throw new CompilerError(CompilerMessages._0_Expected_1_Element1s_In_Binding_List, "MAP-DIRECTIONS", 3);
            }

            if (!(spec.First is ZilAtom dirAtom))
            {
                throw new CompilerError(CompilerMessages._0_1_Element_In_Binding_List_Must_Be_2, "MAP-DIRECTIONS", "first", "an atom");
            }

            if (!(spec.Rest?.First is ZilAtom ptAtom))
            {
                throw new CompilerError(CompilerMessages._0_1_Element_In_Binding_List_Must_Be_2, "MAP-DIRECTIONS", "middle", "an atom");
            }

            var room = spec.Rest.Rest.First;
            if (!room.IsLVAL(out _) && !room.IsGVAL(out _))
            {
                throw new CompilerError(CompilerMessages._0_1_Element_In_Binding_List_Must_Be_2, "MAP-DIRECTIONS", "last", "an LVAL or GVAL");
            }

            // look for an end block
            var body = args.Rest;
            if (body.First is ZilList endStmts && endStmts.StdTypeAtom == StdAtom.LIST)
            {
                body = body.Rest;
            }
            else
            {
                endStmts = null;
            }

            // create block
            var block = new Block
            {
                AgainLabel = rb.DefineLabel(),
                ReturnLabel = rb.DefineLabel(),
                Flags = wantResult ? BlockFlags.WantResult : 0
            };

            Blocks.Push(block);

            var exhaustedLabel = rb.DefineLabel();

            // initialize counter
            var counter = PushInnerLocal(rb, dirAtom);
            rb.EmitStore(counter, Game.MakeOperand(Game.MaxProperties + 1));

            rb.MarkLabel(block.AgainLabel);

            rb.Branch(Condition.DecCheck, counter,
                Constants[Context.GetStdAtom(StdAtom.LOW_DIRECTION)], exhaustedLabel, true);

            var propTable = PushInnerLocal(rb, ptAtom);
            var roomOperand = CompileAsOperand(rb, room, src);
            rb.EmitBinary(BinaryOp.GetPropAddress, roomOperand, counter, propTable);
            rb.BranchIfZero(propTable, block.AgainLabel, true);

            // body
            while (body != null && !body.IsEmpty)
            {
                // ignore the results of all statements
                CompileStmt(rb, body.First, false);
                body = body.Rest;
            }

            // loop
            rb.Branch(block.AgainLabel);

            // end statements
            while (endStmts != null && !endStmts.IsEmpty)
            {
                CompileStmt(rb, endStmts.First, false);
                endStmts = endStmts.Rest;
            }

            // exhausted label, provide a return value if we need one
            rb.MarkLabel(exhaustedLabel);
            if (wantResult)
                rb.EmitStore(rb.Stack, Game.One);

            // clean up block and variables
            rb.MarkLabel(block.ReturnLabel);

            PopInnerLocal(ptAtom);
            PopInnerLocal(dirAtom);

            Blocks.Pop();

            return wantResult ? rb.Stack : null;
        }
    }
}
