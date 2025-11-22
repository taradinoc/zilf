/* Copyright 2010-2023 Tara McGrew
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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using Zilf.Common;
using Zilf.Diagnostics;
using Zilf.Emit;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.Language.Signatures;
using Zilf.ZModel;
using Zilf.ZModel.Values;

namespace Zilf.Compiler.Builtins
{
    [SuppressMessage("Style", "IDE0060", Justification = "ZBuiltins parameters are needed for validation, even if the values aren't used.")]
    [SuppressMessage("Redundancy", "RCS1163:Unused parameter.", Justification = "ZBuiltins parameters are needed for validation, even if the values aren't used.")]
    [SuppressMessage("Performance", "CA1801", Justification = "ZBuiltins parameters are needed for validation, even if the values aren't used.")]
    static class ZBuiltins
    {

        #region Generated Parser Infrastructure

        static bool TryCallGeneratedParser<TCall>(string name, TCall call, ZilObject[] args, out IOperand? result)
            where TCall : struct
        {
            // Use the appropriate typed dictionary based on call type - no casting needed!
            switch (call)
            {
                case ValueCall vc when GeneratedBuiltinParsers.ValueCallParsers.TryGetValue(name, out var valueEntry):
                    result = valueEntry.Parser(vc, args);
                    return true;

                case VoidCall vdc when GeneratedBuiltinParsers.VoidCallParsers.TryGetValue(name, out var voidEntry):
                    voidEntry.Parser(vdc, args);
                    result = null;
                    return true;

                case PredCall pc when GeneratedBuiltinParsers.PredCallParsers.TryGetValue(name, out var predEntry):
                    predEntry.Parser(pc, args);
                    result = null;
                    return true;

                case ValuePredCall vpc when GeneratedBuiltinParsers.ValuePredCallParsers.TryGetValue(name, out var valuePredEntry):
                    result = valuePredEntry.Parser(vpc, args);
                    return true;

                default:
                    result = null;
                    return false;
            }
        }

        #endregion

        #region Infrastructure

        public static IEnumerable<string> GetBuiltinNames()
        {
            // Return all builtin names from the generated parser tables
            var names = new HashSet<string>(StringComparer.Ordinal);
            names.UnionWith(GeneratedBuiltinParsers.VoidCallParsers.Keys);
            names.UnionWith(GeneratedBuiltinParsers.ValueCallParsers.Keys);
            names.UnionWith(GeneratedBuiltinParsers.PredCallParsers.Keys);
            names.UnionWith(GeneratedBuiltinParsers.ValuePredCallParsers.Keys);
            return names;
        }

        public static IEnumerable<ISignature> GetBuiltinSignatures(string name)
        {
            if (GeneratedBuiltinParsers.GeneratedBuiltinMetadata.TryGetValue(name, out var sigs) && sigs != null && sigs.Length > 0)
            {
                return sigs;
            }

            return Array.Empty<ISignature>();
        }

        public static bool IsBuiltinValueCall(string name, int zversion, int argCount)
        {
            // Check generated parsers using capability checking
            // Normalize versions 7 and 8 to version 5 (they have same operations as v5)
            int normalizedVersion = zversion > 6 ? 5 : zversion;
            return GeneratedBuiltinParsers.ValueCallParsers.TryGetValue(name, out var valueEntry) &&
                   valueEntry.SupportsCall(normalizedVersion, argCount);
        }

        public static bool IsBuiltinVoidCall(string name, int zversion, int argCount)
        {
            // Check generated parsers using capability checking
            // Normalize versions 7 and 8 to version 5 (they have same operations as v5)
            int normalizedVersion = zversion > 6 ? 5 : zversion;
            return GeneratedBuiltinParsers.VoidCallParsers.TryGetValue(name, out var voidEntry) &&
                   voidEntry.SupportsCall(normalizedVersion, argCount);
        }

        public static bool IsBuiltinPredCall(string name, int zversion, int argCount)
        {
            // Check generated parsers using capability checking
            // Normalize versions 7 and 8 to version 5 (they have same operations as v5)
            int normalizedVersion = zversion > 6 ? 5 : zversion;
            return GeneratedBuiltinParsers.PredCallParsers.TryGetValue(name, out var predEntry) &&
                   predEntry.SupportsCall(normalizedVersion, argCount);
        }

        public static bool IsBuiltinValuePredCall(string name, int zversion, int argCount)
        {
            // Check generated parsers using capability checking
            // Normalize versions 7 and 8 to version 5 (they have same operations as v5)
            int normalizedVersion = zversion > 6 ? 5 : zversion;
            return GeneratedBuiltinParsers.ValuePredCallParsers.TryGetValue(name, out var valuePredEntry) &&
                   valuePredEntry.SupportsCall(normalizedVersion, argCount);
        }

        public static bool IsBuiltinWithSideEffects(string name, int zversion, int argCount)
        {
            // Use the generated side effects method which is based on HasSideEffect = true
            // attribute analysis from all [Builtin] declarations
            // Note: Builtin name alone is sufficient - version and arg count don't affect side effects
            return GeneratedBuiltinParsers.HasSideEffects(name);
        }

        public static bool IsNearMatchBuiltin(string name, int zversion, int argCount, [NotNullWhen(true)] out CompilerError? error)
        {
            // Check if the builtin name exists in any generated parser dictionary
            bool hasVoidCall = GeneratedBuiltinParsers.VoidCallParsers.ContainsKey(name);
            bool hasValueCall = GeneratedBuiltinParsers.ValueCallParsers.ContainsKey(name);
            bool hasPredCall = GeneratedBuiltinParsers.PredCallParsers.ContainsKey(name);
            bool hasValuePredCall = GeneratedBuiltinParsers.ValuePredCallParsers.ContainsKey(name);

            if (hasVoidCall || hasValueCall || hasPredCall || hasValuePredCall)
            {
                // The builtin exists but doesn't support this version/argCount combination
                // Check if it would work with the current version but different arg count
                int normalizedVersion = zversion > 6 ? 5 : zversion;

                // Try different argument counts to see if any would work with this version
                for (int testArgCount = 0; testArgCount <= 10; testArgCount++) // reasonable upper bound
                {
                    if ((hasVoidCall && GeneratedBuiltinParsers.VoidCallParsers[name].SupportsCall(normalizedVersion, testArgCount)) ||
                        (hasValueCall && GeneratedBuiltinParsers.ValueCallParsers[name].SupportsCall(normalizedVersion, testArgCount)) ||
                        (hasPredCall && GeneratedBuiltinParsers.PredCallParsers[name].SupportsCall(normalizedVersion, testArgCount)) ||
                        (hasValuePredCall && GeneratedBuiltinParsers.ValuePredCallParsers[name].SupportsCall(normalizedVersion, testArgCount)))
                    {
                        // Found a working arg count for this version - this is a wrong argument count error
                        // Get the specific argument count ranges from the builtin metadata
                        var ranges = GetArgumentCountRanges(name, normalizedVersion);
                        if (ranges.Count > 0)
                        {
                            error = CompilerError.WrongArgCount(name, ranges);
                        }
                        else
                        {
                            // Fallback to generic message if we can't extract specific ranges
                            error = new CompilerError(CompilerMessages._0_Requires_1_Argument1s, name, new CountableString("a different number of", true));
                        }
                        return true;
                    }
                }

                // No working arg count found for this version - version not supported
                error = new CompilerError(CompilerMessages._0_Is_Not_Supported_In_This_Zmachine_Version, name);
                return true;
            }

            // not a near match
            error = null;
            return false;
        }

        private static List<ArgCountRange> GetArgumentCountRanges(string name, int normalizedVersion)
        {
            var ranges = new List<ArgCountRange>();

            // Check if the builtin exists in the metadata
            if (GeneratedBuiltinParsers.GeneratedBuiltinMetadata.TryGetValue(name, out var signatures))
            {
                foreach (var signature in signatures)
                {
                    // For Z-code builtins, check if this signature applies to the current version
                    if (signature is ZBuiltinSignature zSignature)
                    {
                        if (normalizedVersion >= zSignature.MinVersion && normalizedVersion <= zSignature.MaxVersion)
                        {
                            ranges.Add(new ArgCountRange(signature.MinArgs, signature.MaxArgs));
                        }
                    }
                }
            }

            return ranges;
        }

        static IOperand? CompileBuiltinCall<TCall>(string name, Compilation cc,
            IRoutineBuilder rb, ZilListoidBase form, TCall call)
            where TCall : struct
        {
            int zversion = cc.Context.ZEnvironment.ZVersion;
            var args = form.Skip(1).ToArray();

            // All builtin operations are now handled by generated parsers
            if (TryCallGeneratedParser(name, call, args, out var genResult))
            {
                return genResult;
            }

            // If we reach here, the builtin name is not recognized
            throw new ArgumentException($"Unknown builtin: {name}");
        }

        public static IOperand CompileValueCall(string name, Compilation cc, IRoutineBuilder rb, ZilForm form,
            IVariable? resultStorage)
        {
            var result = CompileBuiltinCall(name,
                cc,
                rb,
                form,
                new ValueCall(cc, rb, form, resultStorage ?? rb.Stack));
            Debug.Assert(result != null);
            return result;
        }

        public static void CompileVoidCall(string name, Compilation cc, IRoutineBuilder rb, ZilForm form)
        {
            CompileBuiltinCall(name, cc, rb, form, new VoidCall(cc, rb, form));
        }

        public static void CompilePredCall(string name, Compilation cc, IRoutineBuilder rb, ZilForm form, ILabel label, bool polarity)
        {
            CompileBuiltinCall(name, cc, rb, form, new PredCall(cc, rb, form, label, polarity));
        }

        public static void CompileValuePredCall(string name, Compilation cc, IRoutineBuilder rb, ZilForm form,
            IVariable? resultStorage, ILabel label, bool polarity)
        {
            CompileBuiltinCall(name, cc, rb, form,
                new ValuePredCall(cc, rb, form, resultStorage ?? rb.Stack, label, polarity));
        }

        #endregion

        #region Equality Opcodes

        // TODO: simplify this method
        // TODO: add a way to tag builtins as needing local variable access, so we can give better errors when they're used from GO?
        /// <summary>
        /// Returns true if the first argument is equal to any of the remaining arguments.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="arg1">The first argument to compare.</param>
        /// <param name="arg2">The second argument to compare.</param>
        /// <param name="restOfArgs">Optional additional arguments to compare.</param>
        /// <exception cref="CompilerError">Local variables are not allowed here.</exception>
        [Builtin("EQUAL?", "=?", "==?")]
        public static void VarargsEqualityOp(
            PredCall c, IOperand arg1, IOperand arg2,
              params IOperand[] restOfArgs)
        {
            if (arg1 is INumericOperand num1)
            {
                var value = num1.Value;
                var num2 = arg2 as INumericOperand;

                if (num2?.Value == value ||
                    restOfArgs.OfType<INumericOperand>().Any(arg => arg.Value == value))
                {
                    // we know it's equal, so pop any stack operands and branch accordingly
                    if (arg2 == c.rb.Stack)
                        c.rb.EmitPopStack();

                    foreach (var arg in restOfArgs)
                        if (arg == c.rb.Stack)
                            c.rb.EmitPopStack();

                    if (c.polarity)
                        c.rb.Branch(c.label);

                    return;
                }

                if (num2 != null &&
                    num2.Value != value &&
                    restOfArgs.All(arg => arg is INumericOperand numArg && numArg.Value != value))
                {
                    // we know it's not equal, and there are no stack operands, so branch accordingly
                    if (!c.polarity)
                        c.rb.Branch(c.label);

                    return;
                }

                // we can't simplify the branch, but we can still skip testing all the constants
                if (num2 != null || restOfArgs.Any(arg => arg is INumericOperand))
                {
                    var queue = new Queue<IOperand>(restOfArgs.Length);

                    if (num2 == null)
                        queue.Enqueue(arg2);

                    foreach (var arg in restOfArgs)
                        if (arg is not INumericOperand)
                            queue.Enqueue(arg);

                    arg2 = queue.Dequeue();
                    Debug.Assert(arg2 != null);
                    restOfArgs = queue.ToArray();
                }
            }

            if (restOfArgs.Length <= 2)
            {
                // TODO: there should really just be one BranchIfEqual with optional params
                switch (restOfArgs.Length)
                {
                    case 2:
                        Debug.Assert(restOfArgs[0] != null);
                        Debug.Assert(restOfArgs[1] != null);
                        c.rb.BranchIfEqual(arg1, arg2, restOfArgs[0], restOfArgs[1], c.label, c.polarity);
                        break;
                    case 1:
                        Debug.Assert(restOfArgs[0] != null);
                        c.rb.BranchIfEqual(arg1, arg2, restOfArgs[0], c.label, c.polarity);
                        break;
                    default:
                        c.rb.BranchIfEqual(arg1, arg2, c.label, c.polarity);
                        break;
                }
            }
            else
            {
                ZilAtom? tempAtom = null;
                if (arg1 == c.rb.Stack)
                {
                    tempAtom = ZilAtom.Parse("?TMP", c.cc.Context);
                    var tempLocal = c.cc.PushInnerLocal(c.rb, tempAtom, LocalBindingType.CompilerTemporary, c.form.SourceLine);
                    c.rb.EmitStore(tempLocal, arg1);
                    arg1 = tempLocal;
                }

                var queue = new Queue<IOperand>(1 + restOfArgs.Length);
                queue.Enqueue(arg2);
                foreach (var arg in restOfArgs)
                    queue.Enqueue(arg);

                if (c.polarity)
                {
                    while (queue.Count > 0)
                    {
                        switch (queue.Count)
                        {
                            default:
                                c.rb.BranchIfEqual(arg1, queue.Dequeue(), queue.Dequeue(), queue.Dequeue(), c.label, true);
                                break;
                            case 2:
                                c.rb.BranchIfEqual(arg1, queue.Dequeue(), queue.Dequeue(), c.label, true);
                                break;
                            case 1:
                                c.rb.BranchIfEqual(arg1, queue.Dequeue(), c.label, true);
                                break;
                        }
                    }
                }
                else
                {
                    var skip = c.rb.DefineLabel();

                    while (queue.Count > 0)
                    {
                        // the last test (count <= 3) has false polarity and branches to the target label
                        // the earlier ones (count > 3) have true polarity and just skip the rest of the tests
                        switch (queue.Count)
                        {
                            case 3:
                                c.rb.BranchIfEqual(arg1, queue.Dequeue(), queue.Dequeue(), queue.Dequeue(), c.label, false);
                                break;
                            case 2:
                                c.rb.BranchIfEqual(arg1, queue.Dequeue(), queue.Dequeue(), c.label, false);
                                break;
                            case 1:
                                c.rb.BranchIfEqual(arg1, queue.Dequeue(), c.label, false);
                                break;
                            default:
                                c.rb.BranchIfEqual(arg1, queue.Dequeue(), queue.Dequeue(), queue.Dequeue(), skip, true);
                                break;
                        }
                    }

                    c.rb.MarkLabel(skip);
                }
                // ReSharper restore AssignNullToNotNullAttribute

                if (tempAtom != null)
                    c.cc.PopInnerLocal(tempAtom);
            }
        }

        /// <summary>
        /// Returns true if the first argument is not equal to any of the remaining arguments.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="arg1">The first argument to compare.</param>
        /// <param name="arg2">The second argument to compare.</param>
        /// <param name="restOfArgs">Optional additional arguments to compare.</param>
        /// <exception cref="CompilerError">Local variables are not allowed here.</exception>
        [Builtin("N=?", "N==?")]
        public static void NegatedVarargsEqualityOp(
            PredCall c, IOperand arg1, IOperand arg2,
             params IOperand[] restOfArgs)
        {
            var innerCall = new PredCall(c.cc, c.rb, c.form, c.label, !c.polarity);
            VarargsEqualityOp(innerCall, arg1, arg2, restOfArgs);
        }

        #endregion

        #region Ternary Opcodes

        [Builtin("DCLEAR", Data = TernaryOp.ErasePicture, MinVersion = 6, HasSideEffect = true, Summary = "Erases a picture from the screen.")]
        [Builtin("DIROUT", Data = TernaryOp.DirectOutput, MinVersion = 6, HasSideEffect = true, Summary = "Directs output to the specified stream.")]
        [Builtin("DISPLAY", Data = TernaryOp.DrawPicture, MinVersion = 6, HasSideEffect = true, Summary = "Draws a picture on the screen.")]
        [Builtin("WINPOS", Data = TernaryOp.MoveWindow, MinVersion = 6, HasSideEffect = true, Summary = "Sets the position of a window.")]
        [Builtin("WINPUT", Data = TernaryOp.PutWindowProperty, MinVersion = 6, HasSideEffect = true, Summary = "Sets a property of a window.")]
        [Builtin("WINSIZE", Data = TernaryOp.WindowSize, MinVersion = 6, HasSideEffect = true, Summary = "Sets the size of a window.")]
        public static void TernaryVoidOp(
            VoidCall c, [Data] TernaryOp op,
             IOperand left, IOperand center, IOperand right)
        {
            c.rb.EmitTernary(op, left, center, right, null);
        }

        [Builtin("MARGIN", Data = TernaryOp.SetMargins, MinVersion = 6, HasSideEffect = true, Summary = "Sets a window's margins.")]
        [Builtin("WINATTR", Data = TernaryOp.WindowStyle, MinVersion = 6, HasSideEffect = true, Summary = "Sets a window's attributes.")]
        public static void TernaryOptionalVoidOp(
            VoidCall c, [Data] TernaryOp op,
             IOperand left, IOperand center, IOperand? right = null)
        {
            c.rb.EmitTernary(op, left, center, right ?? c.cc.Game.Zero, null);
        }

        [Builtin("PUT", "ZPUT", Data = TernaryOp.PutWord, HasSideEffect = true, Summary = "Stores a word value into a table.")]
        [Builtin("PUTB", Data = TernaryOp.PutByte, HasSideEffect = true, Summary = "Stores a byte value into a table.")]
        public static void TernaryTableVoidOp(
            VoidCall c, [Data] TernaryOp op,
            [Table] IOperand left, IOperand center, IOperand right)
        {
            c.rb.EmitTernary(op, left, center, right, null);
        }

        [Builtin("PUTP", Data = TernaryOp.PutProperty, HasSideEffect = true, Summary = "Stores a value into an object's property.")]
        public static void TernaryObjectVoidOp(
            VoidCall c, [Data] TernaryOp op,
            [Object] IOperand left, IOperand center, IOperand right)
        {
            c.rb.EmitTernary(op, left, center, right, null);
        }

        [Builtin("COPYT", Data = TernaryOp.CopyTable, HasSideEffect = true, MinVersion = 5, Summary = "Copies a table.")]
        public static void TernaryTableTableVoidOp(
            VoidCall c, [Data] TernaryOp op,
            [Table] IOperand left, [Table] IOperand center, IOperand right)
        {
            c.rb.EmitTernary(op, left, center, right, null);
        }

        #endregion

        #region Binary Opcodes

        [Builtin("MOD", Data = BinaryOp.Mod, Summary = "Computes the modulus (remainder) of two numbers.")]
        [Builtin("ASH", "ASHIFT", Data = BinaryOp.ArtShift, MinVersion = 5, Summary = "Performs an arithmetic (signed) shift on a number.")]
        [Builtin("LSH", "SHIFT", Data = BinaryOp.LogShift, MinVersion = 5, Summary = "Performs a logical (unsigned) shift on a number.")]
        [Builtin("WINGET", Data = BinaryOp.GetWindowProperty, MinVersion = 6, Summary = "Retrieves a property of a window.")]
        public static IOperand BinaryValueOp(
            ValueCall c, [Data] BinaryOp op, IOperand left, IOperand right)
        {
            if (left is INumericOperand nleft && right is INumericOperand nright)
            {
                switch (op)
                {
                    case BinaryOp.Mod:
                        return c.cc.Game.MakeOperand((short)(nleft.Value % nright.Value));
                    case BinaryOp.ArtShift:
                        if (nright.Value < 0)
                            return c.cc.Game.MakeOperand((short)(nleft.Value >> -nright.Value));
                        return c.cc.Game.MakeOperand((short)(nleft.Value << nright.Value));
                    case BinaryOp.LogShift:
                        if (nright.Value < 0)
                            return c.cc.Game.MakeOperand((short)((ushort)nleft.Value >> -nright.Value));
                        return c.cc.Game.MakeOperand((short)((ushort)nleft.Value << nright.Value));
                }
            }

            c.rb.EmitBinary(op, left, right, c.resultStorage);
            return c.resultStorage;
        }

        /// <summary>
        /// Computes the bitwise XOR of two numbers, where one operand must be the constant -1.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="left">The first number.</param>
        /// <param name="right">The second number.</param>
        /// <returns>The bitwise XOR of the two numbers, i.e., the bitwise NOT of whichever operand is not -1.</returns>
        /// <exception cref="CompilerError">The syntax is incorrect, or an error occurred while compiling a subexpression.</exception>
        [Builtin("XORB")]
        public static IOperand BinaryXorOp(ValueCall c, ZilObject left, ZilObject right)
        {
            ZilObject value;
            if (left is ZilFix lf && lf.Value == -1)
            {
                value = right;
            }
            else if (right is ZilFix rf && rf.Value == -1)
            {
                value = left;
            }
            else
            {
                return c.HandleMessage(CompilerMessages._0_One_Operand_Must_Be_1, "XORB");
            }

            var storage = c.cc.CompileAsOperand(c.rb, value, c.form.SourceLine, c.resultStorage);

            if (storage is INumericOperand num)
            {
                return c.cc.Game.MakeOperand((short)(~num.Value));
            }
            c.rb.EmitUnary(UnaryOp.Not, storage, c.resultStorage);
            return c.resultStorage;
        }

        [Builtin("ADD", "+", Data = BinaryOp.Add, Summary = "Computes the sum of two or more numbers.")]
        [Builtin("SUB", "-", Data = BinaryOp.Sub, Summary = "Computes the difference between two or more numbers.")]
        [Builtin("MUL", "*", Data = BinaryOp.Mul, Summary = "Computes the product of two or more numbers.")]
        [Builtin("DIV", "/", Data = BinaryOp.Div, Summary = "Computes the quotient of two or more numbers.")]
        [Builtin("BAND", "ANDB", Data = BinaryOp.And, Summary = "Computes the bitwise AND of two or more numbers.")]
        [Builtin("BOR", "ORB", Data = BinaryOp.Or, Summary = "Computes the bitwise OR of two or more numbers.")]
        public static IOperand ArithmeticOp(
            ValueCall c, [Data] BinaryOp op, params IOperand[] args)
        {
            GetArithmeticInfo(op, out var initialValue, out var operation, out var compileUnary);

            // can we evaluate the whole operation at compile time?
            if (args.Length > 0)
            {
                var folded = FoldConstantArithmetic(c.cc, initialValue, operation, args);
                if (folded != null)
                    return folded;
            }

            // nope, compile it
            switch (args.Length)
            {
                case 0:
                    return c.cc.Game.MakeOperand(initialValue);

                case 1:
                    return compileUnary(c, c.cc.Game.MakeOperand(initialValue), args[0]);

                case 2:
                    c.rb.EmitBinary(op, args[0], args[1], c.resultStorage);
                    return c.resultStorage;

                default:
                    c.rb.EmitBinary(op, args[0], args[1], c.rb.Stack);
                    for (int i = 2; i + 1 < args.Length; i++)
                    {
                        c.rb.EmitBinary(op, c.rb.Stack, args[i], c.rb.Stack);
                    }
                    c.rb.EmitBinary(op, c.rb.Stack, args[^1], c.resultStorage);
                    return c.resultStorage;
            }
        }

        static void GetArithmeticInfo(BinaryOp op, out short initialValue,
            out Func<short, short, short> operation,
            out Func<ValueCall, IOperand, IOperand, IOperand> compileUnary)
        {
            // a delegate implementing the actual arithmetic operation
            operation = op switch
            {
                BinaryOp.Add => (Func<short, short, short>)((a, b) => (short)(a + b)),
                BinaryOp.Sub => (a, b) => (short)(a - b),
                BinaryOp.Mul => (a, b) => (short)(a * b),
                BinaryOp.Div => (a, b) => (short)(a / b),
                BinaryOp.And => (a, b) => (short)(a & b),
                BinaryOp.Or => (a, b) => (short)(a | b),
                _ => throw new ArgumentOutOfRangeException(nameof(op), op, null)
            };

            // the initial value, which is returned as-is if there are no args,
            // or possibly combined with the single arg if there's only one
            initialValue = op switch
            {
                BinaryOp.Mul => 1,
                BinaryOp.Div => 1,
                BinaryOp.And => -1,
                _ => 0
            };

            // another delegate describing how to combine the initial value
            // with the single arg in that case
            // ReSharper disable once ConvertSwitchStatementToSwitchExpression
#pragma warning disable IDE0066 // Convert switch statement to expression
            switch (op)
#pragma warning restore IDE0066 // Convert switch statement to expression
            {
                case BinaryOp.Add:
                case BinaryOp.Mul:
                case BinaryOp.And:
                case BinaryOp.Or:
                    // <+ X>, <* X>, <BAND X>, and <BOR X> all return X
                    compileUnary = (c, init, arg) => arg;
                    break;

                case BinaryOp.Sub:
                    // <- X> negates X
                    compileUnary = (c, init, arg) =>
                    {
                        c.rb.EmitUnary(UnaryOp.Neg, arg, c.resultStorage);
                        return c.resultStorage;
                    };
                    break;

                default:
                    // </ X> divides 1 by X
                    // presumably it sounded like a good idea at the time
                    compileUnary = (c, init, arg) =>
                    {
                        c.rb.EmitBinary(op, init, arg, c.resultStorage);
                        return c.resultStorage;
                    };
                    break;
            }
        }

        static IOperand? FoldConstantArithmetic(Compilation cc, short init, Func<short, short, short> op,
            IOperand[] args)
        {
            // make sure all args are constants
            foreach (var arg in args)
                if (arg is not INumericOperand)
                    return null;

            if (args.Length == 1)
                return cc.Game.MakeOperand(op(init, (short)((INumericOperand)args[0]).Value));

            var value = (short)((INumericOperand)args[0]).Value;
            for (int i = 1; i < args.Length; i++)
                value = op(value, (short)((INumericOperand)args[i]).Value);

            return cc.Game.MakeOperand(value);
        }

        /// <summary>
        /// Computes the bitwise AND of two numbers.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="left">The first number.</param>
        /// <param name="right">The second number.</param>
        [Builtin("BAND", "ANDB")]
        public static void BinaryAndPredOp(PredCall c, IOperand left, IOperand right)
        {
            var nleft = left as INumericOperand;
            var nright = right as INumericOperand;

            // if both are constants, we can fully optimize
            if (nleft != null && nright != null)
            {
                var result = (short)(nleft.Value) & (short)(nright.Value);
                if ((result != 0) == c.polarity)
                    c.rb.Branch(c.label);

                return;
            }

            // if one is a constant power of two, we can use BTST
            if (nleft != null || nright != null)
            {
                IOperand variable;
                INumericOperand constant;

                if (nleft != null)
                {
                    constant = nleft;
                    variable = right;
                }
                else
                {
                    constant = nright!;
                    variable = left;
                }

                if (constant.Value == 0)
                {
                    // always false
                    if (!c.polarity)
                        c.rb.Branch(c.label);

                    return;
                }
                if ((constant.Value & (constant.Value - 1)) == 0)
                {
                    // power of two
                    c.rb.Branch(Condition.TestBits, variable, constant, c.label, c.polarity);
                    return;
                }
            }

            // otherwise use BAND and ZERO?
            c.rb.EmitBinary(BinaryOp.And, left, right, c.rb.Stack);
            c.rb.BranchIfZero(c.rb.Stack, c.label, !c.polarity);
        }

        /// <summary>
        /// Adds an offset to a table or constant pointer.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="left">The table or constant pointer.</param>
        /// <param name="right">The offset to add, in bytes. If omitted, defaults to 1.</param>
        /// <returns>A constant pointer with the offset added.</returns>
        [Builtin("REST", "ZREST")]
        public static IOperand RestOp(ValueCall c, IOperand left, IOperand? right = null)
        {
            // if left and right are constants, we can add them at assembly time
            return (left, right) switch
            {
                (IConstantOperand lconst, IConstantOperand rconst) => lconst.Add(rconst),
                (IConstantOperand lconst, null) => lconst.Add(c.cc.Game.One),
                _ => ArithmeticOp(c, BinaryOp.Add, left, right ?? c.cc.Game.One),
            };
        }

        /// <summary>
        /// Subtracts an offset from a table or constant pointer.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="left">The table or constant pointer.</param>
        /// <param name="right">The offset to subtract, in bytes. If omitted, defaults to 1.</param>
        /// <returns>A (non-constant) pointer with the offset subtracted.</returns>
        [Builtin("BACK", "ZBACK")]
        public static IOperand BackOp(ValueCall c, IOperand left, IOperand? right = null)
        {
            return ArithmeticOp(c, BinaryOp.Sub, left, right ?? c.cc.Game.One);
        }

        [Builtin("CURSET", Data = BinaryOp.SetCursor, MinVersion = 4, MaxVersion = 5, HasSideEffect = true, Summary = "Sets the cursor row and column.")]
        [Builtin("COLOR", Data = BinaryOp.SetColor, MinVersion = 5, HasSideEffect = true, Summary = "Sets the foreground and background colors.")]
        [Builtin("DIROUT", Data = BinaryOp.DirectOutput, HasSideEffect = true, Summary = "Directs output to the specified stream.")]
        [Builtin("THROW", Data = BinaryOp.Throw, MinVersion = 5, HasSideEffect = true, Summary = "Causes a value to be returned from an outer function call.")]
        [Builtin("SCROLL", Data = BinaryOp.ScrollWindow, MinVersion = 6, HasSideEffect = true, Summary = "Scrolls the contents of a window by a number of pixels.")]
        public static void BinaryVoidOp(
            VoidCall c, [Data] BinaryOp op, IOperand left, IOperand right)
        {
            c.rb.EmitBinary(op, left, right, null);
        }

        [Builtin("CURSET", MinVersion = 6, HasSideEffect = true, Summary = "Sets the cursor row and column in a window.")]
        public static void CursetVoidOp(VoidCall c, IOperand line, IOperand? column = null,
            IOperand? window = null)
        {
            if (window != null)
            {
                Debug.Assert(column != null);
                c.rb.EmitTernary(TernaryOp.SetCursor, line, column, window, null);
            }
            else
            {
                c.rb.EmitBinary(BinaryOp.SetCursor, line, column ?? c.cc.Game.Zero, null);
            }
        }

        [Builtin("GRTR?", "G?", Data = Condition.Greater, Summary = "Tests whether the first value is greater than the second.")]
        [Builtin("LESS?", "L?", Data = Condition.Less, Summary = "Tests whether the first value is less than the second.")]
        [Builtin("BTST", Data = Condition.TestBits, Summary = "Tests whether all of the bits set in the second value are also set in the first value.")]
        public static void BinaryPredOp(
            PredCall c, [Data] Condition cond, IOperand left, IOperand right)
        {
            if (left is INumericOperand nleft && right is INumericOperand nright)
            {
                var branch = cond switch
                {
                    Condition.Greater => (nleft.Value > nright.Value),
                    Condition.Less => (nleft.Value < nright.Value),
                    Condition.TestBits => ((nleft.Value & nright.Value) == nright.Value),
                    _ => throw UnhandledCaseException.FromEnum(cond)
                };

                if (branch == c.polarity)
                    c.rb.Branch(c.label);
            }
            else
            {
                c.rb.Branch(cond, left, right, c.label, c.polarity);
            }
        }

        /// <summary>
        /// Sets up a menu.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="menuId">The menu number, which must be greater than 2.</param>
        /// <param name="table">A table of ZSCII strings comprising the menu name and items.</param>
        [Builtin("MENU", MinVersion = 6, HasSideEffect = true)]
        public static void BinaryMenuOp(
            PredCall c, IOperand menuId, [Table] IOperand table)
        {
            c.rb.Branch(Condition.MakeMenu, menuId, table, c.label, c.polarity);
        }

        [Builtin("L=?", Data = Condition.Greater, Summary = "Tests whether the first value is less than or equal to the second.")]
        [Builtin("G=?", Data = Condition.Less, Summary = "Tests whether the first value is greater than or equal to the second.")]
        public static void NegatedBinaryPredOp(
            PredCall c, [Data] Condition cond, IOperand left, IOperand right)
        {
            BinaryPredOp(new PredCall(c.cc, c.rb, c.form, c.label, !c.polarity), cond, left, right);
        }

        [Builtin("DLESS?", Data = Condition.DecCheck, HasSideEffect = true, Summary = "Decrements the named variable and tests whether it is now less than the specified value.")]
        [Builtin("IGRTR?", Data = Condition.IncCheck, HasSideEffect = true, Summary = "Increments the named variable and tests whether it is now greater than the specified value.")]
        public static void BinaryVariablePredOp(
            PredCall c, [Data] Condition cond, [Variable(VariableScopeQuirks = VariableScopeQuirks.Both)] IVariable left, IOperand right)
        {
            c.cc.MarkVariableAsReadAndWritten(left);
            c.rb.Branch(cond, left, right, c.label, c.polarity);
        }

        [Builtin("PICINF", MinVersion = 6, HasSideEffect = true, Summary = "Tests whether the specified picture (or any picture) exists, and writes metadata about it into an array if so.")]
        public static void PicinfPredOp(PredCall c, IOperand left, [Table] IOperand right)
        {
            c.rb.Branch(Condition.PictureData, left, right, c.label, c.polarity);
        }

        [Builtin("DLESS?", Data = Condition.Less, HasSideEffect = true, Summary = "Decrements the named variable and tests whether it is now less than the specified value.")]
        [Builtin("IGRTR?", Data = Condition.Greater, HasSideEffect = true, Summary = "Increments the named variable and tests whether it is now greater than the specified value.")]
        public static void BinaryVariablePredOp(
            PredCall c, [Data] Condition cond, [Variable] SoftGlobal left, IOperand right)
        {
            Debug.Assert(c.cc.SoftGlobalsTable != null, "c.cc.SoftGlobalsTable != null");

            var offset = c.cc.Game.MakeOperand(left.Offset);

            // increment and leave the new value on the stack
            c.rb.EmitBinary(
                left.IsWord ? BinaryOp.GetWord : BinaryOp.GetByte,
                c.cc.SoftGlobalsTable,
                offset,
                c.rb.Stack);
            c.rb.EmitBinary(
                cond == Condition.Greater ? BinaryOp.Add : BinaryOp.Sub,
                c.rb.Stack,
                c.cc.Game.One,
                c.rb.Stack);
            c.rb.EmitUnary(
                UnaryOp.LoadIndirect,
                c.rb.Stack.Indirect,
                c.rb.Stack);
            c.rb.EmitTernary(
                left.IsWord ? TernaryOp.PutWord : TernaryOp.PutByte,
                c.cc.SoftGlobalsTable,
                offset,
                c.rb.Stack,
                null);

            // this works even if right == Stack, since the new value will be popped first
            c.rb.Branch(cond, c.rb.Stack, right, c.label, c.polarity);
        }

        [Builtin("GETP", Data = BinaryOp.GetProperty, Summary = "Returns the value of an object's property.")]
        [Builtin("NEXTP", Data = BinaryOp.GetNextProp, Summary = "Returns an object's next (or first) property number.")]
        public static IOperand BinaryObjectValueOp(
            ValueCall c, [Data] BinaryOp op, [Object] IOperand left, IOperand right)
        {
            c.rb.EmitBinary(op, left, right, c.resultStorage);
            return c.resultStorage;
        }

        [Builtin("FSET", Data = BinaryOp.SetFlag, HasSideEffect = true, Summary = "Sets an object's flag.")]
        [Builtin("FCLEAR", Data = BinaryOp.ClearFlag, HasSideEffect = true, Summary = "Clears an object's flag.")]
        public static void BinaryObjectVoidOp(
            VoidCall c, [Data] BinaryOp op, [Object] IOperand left, IOperand right)
        {
            c.rb.EmitBinary(op, left, right, null);
        }

        [Builtin("FSET?", Data = Condition.TestAttr, Summary = "Tests whether an object has a flag set.")]
        public static void BinaryObjectPredOp(
            PredCall c, [Data] Condition cond, [Object] IOperand left, IOperand right)
        {
            c.rb.Branch(cond, left, right, c.label, c.polarity);
        }

        [Builtin("IN?", Data = Condition.Inside, Summary = "Tests whether one object is directly contained by another object.")]
        public static void BinaryObjectObjectPredOp(
            PredCall c, [Data] Condition cond, [Object] IOperand left, [Object] IOperand right)
        {
            c.rb.Branch(cond, left, right, c.label, c.polarity);
        }

        [Builtin("MOVE", Data = BinaryOp.MoveObject, HasSideEffect = true, Summary = "Moves one object to become the first child of another object.")]
        public static void BinaryObjectObjectVoidOp(
            VoidCall c, [Data] BinaryOp op, [Object] IOperand left, [Object] IOperand right)
        {
            c.rb.EmitBinary(op, left, right, null);
        }

        [Builtin("GETPT", Data = BinaryOp.GetPropAddress, Summary = "Gets the address of the table containing an object's property.")]
        [return: Table]
        public static IOperand BinaryObjectToTableValueOp(
            ValueCall c, [Data] BinaryOp op, [Object] IOperand left, IOperand right)
        {
            c.rb.EmitBinary(op, left, right, c.resultStorage);
            return c.resultStorage;
        }

        [Builtin("GET", "NTH", "ZGET", Data = BinaryOp.GetWord, Summary = "Gets a word value from a table.")]
        [Builtin("GETB", Data = BinaryOp.GetByte, Summary = "Gets a byte value from a table.")]
        public static IOperand BinaryTableValueOp(
            ValueCall c, [Data] BinaryOp op, [Table] IOperand left, IOperand right)
        {
            c.rb.EmitBinary(op, left, right, c.resultStorage);
            return c.resultStorage;
        }

        #endregion

        #region Unary Opcodes

        [Builtin("BCOM", Data = UnaryOp.Not, Summary = "Computes the bitwise NOT of a number.")]
        [Builtin("RANDOM", "ZRANDOM", Data = UnaryOp.Random, HasSideEffect = true, Summary = "Computes a random number within a range, or reseeds the random number generator.")]
        [Builtin("FONT", Data = UnaryOp.SetFont, MinVersion = 5, HasSideEffect = true, Summary = "Selects a font for the current window.")]
        [Builtin("CHECKU", Data = UnaryOp.CheckUnicode, MinVersion = 5, Summary = "Determines whether the specified Unicode character can be printed and/or read as input.")]
        public static IOperand UnaryValueOp(
            ValueCall c, [Data] UnaryOp op, IOperand value)
        {
            if (op == UnaryOp.Not && value is INumericOperand num)
            {
                return c.cc.Game.MakeOperand((short)(~num.Value));
            }

            c.rb.EmitUnary(op, value, c.resultStorage);
            return c.resultStorage;
        }

        [Builtin("DIRIN", Data = UnaryOp.DirectInput, HasSideEffect = true, Summary = "Directs input from the specified stream.")]
        [Builtin("DIROUT", Data = UnaryOp.DirectOutput, HasSideEffect = true, Summary = "Directs output to the specified stream.")]
        [Builtin("BUFOUT", "ZBUFOUT", Data = UnaryOp.OutputBuffer, MinVersion = 4, HasSideEffect = true, Summary = "Enables or disables text buffering for the lower window.")]
        [Builtin("HLIGHT", Data = UnaryOp.OutputStyle, HasSideEffect = true, Summary = "Sets the output style (highlighting) for the current window.")]
        [Builtin("CLEAR", Data = UnaryOp.ClearWindow, MinVersion = 4, HasSideEffect = true, Summary = "Clears the specified window.")]
        [Builtin("SCREEN", Data = UnaryOp.SelectWindow, HasSideEffect = true, Summary = "Selects the specified window for output.")]
        [Builtin("SPLIT", Data = UnaryOp.SplitWindow, HasSideEffect = true, Summary = "Sets the height of the upper window.")]
        [Builtin("ERASE", Data = UnaryOp.EraseLine, MinVersion = 4, HasSideEffect = true, Summary = "Erases part or all of the current line in the current window.")]
        [Builtin("MOUSE-LIMIT", Data = UnaryOp.MouseWindow, MinVersion = 6, HasSideEffect = true, Summary = "Limits the mouse to stay within the specified window.")]
        public static void UnaryVoidOp(
            VoidCall c, [Data] UnaryOp op, IOperand value)
        {
            c.rb.EmitUnary(op, value, null);
        }

        /// <summary>
        /// Tests whether a value is zero.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="value">The value to check.</param>
        [Builtin("ZERO?", "0?")]
        public static void ZeroPredOp(PredCall c, IOperand value)
        {
            if (value is INumericOperand num)
            {
                if ((num.Value == 0) == c.polarity)
                    c.rb.Branch(c.label);
            }
            else
            {
                c.rb.BranchIfZero(value, c.label, c.polarity);
            }
        }

        /// <summary>
        /// Tests whether a value is one.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="value">The value to check.</param>
        [Builtin("1?")]
        public static void OnePredOp(PredCall c, IOperand value)
        {
            if (value is INumericOperand num)
            {
                if ((num.Value == 1) == c.polarity)
                    c.rb.Branch(c.label);
            }
            else
            {
                c.rb.BranchIfEqual(value, c.cc.Game.One, c.label, c.polarity);
            }
        }

        [Builtin("LOC", Data = UnaryOp.GetParent, Summary = "Returns an object's direct container.")]
        public static IOperand UnaryObjectValueOp(
            ValueCall c, [Data] UnaryOp op, [Object] IOperand obj)
        {
            c.rb.EmitUnary(op, obj, c.resultStorage);
            return c.resultStorage;
        }

        [Builtin("FIRST?", Data = false, Summary = "Gets the first child of an object.")]
        [Builtin("NEXT?", Data = true, Summary = "Gets the sibling of an object.")]
        public static void UnaryObjectValuePredOp(
            ValuePredCall c, [Data] bool sibling, [Object] IOperand obj)
        {
            if (sibling)
                c.rb.EmitGetSibling(obj, c.resultStorage, c.label, c.polarity);
            else
                c.rb.EmitGetChild(obj, c.resultStorage, c.label, c.polarity);
        }

        [Builtin("PTSIZE", Data = UnaryOp.GetPropSize, Summary = "Returns the size of an object's property.")]
        public static IOperand UnaryTableValueOp(
            ValueCall c, [Data] UnaryOp op, [Table] IOperand value)
        {
            c.rb.EmitUnary(op, value, c.resultStorage);
            return c.resultStorage;
        }

        [Builtin("REMOVE", "ZREMOVE", Data = UnaryOp.RemoveObject, HasSideEffect = true, Summary = "Removes an object from the object tree.")]
        public static void UnaryObjectVoidOp(
            VoidCall c, [Data] UnaryOp op, [Object] IOperand value)
        {
            c.rb.EmitUnary(op, value, null);
        }

        [Builtin("ASSIGNED?", Data = Condition.ArgProvided, MinVersion = 5, Summary = "Tests whether an argument was passed to the current routine.")]
        public static void UnaryVariablePredOp(
            PredCall c, [Data] Condition cond, [Variable] IVariable var)
        {
            c.cc.MarkVariableAsRead(var);
            c.rb.Branch(cond, var, null, c.label, c.polarity);
        }

        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "var")]
        [Builtin("ASSIGNED?", MinVersion = 5, Summary = "Tests whether an argument was passed to the current routine.")]
        public static void SoftGlobalAssignedOp(PredCall c, [Variable] SoftGlobal var)
        {
            // globals are never "assigned" in this sense
            if (!c.polarity)
                c.rb.Branch(c.label);
        }

        [Builtin("CURGET", Data = UnaryOp.GetCursor, MinVersion = 4, HasSideEffect = true, Summary = "Stores the cursor's row and column into a table.")]
        [Builtin("PICSET", Data = UnaryOp.PictureTable, MinVersion = 6, HasSideEffect = true, Summary = "Caches the pictures listed in a table.")]
        [Builtin("MOUSE-INFO", Data = UnaryOp.ReadMouse, MinVersion = 6, HasSideEffect = true, Summary = "Stores the mouse Y coordinate, X coordinate, button bits, and menu word into a table.")]
        [Builtin("PRINTF", Data = UnaryOp.PrintForm, MinVersion = 6, HasSideEffect = true, Summary = "Prints lines of text from a formatted table.")]
        public static void UnaryTableVoidOp(
            VoidCall c, [Data] UnaryOp op, [Table] IOperand value)
        {
            c.rb.EmitUnary(op, value, null);
        }

        #endregion

        #region Print Opcodes

        [Builtin("PRINT", "ZPRINT", Data = PrintOp.PackedAddr, HasSideEffect = true, Summary = "Prints a packed ZSCII string from memory, given its packed address.")]
        [Builtin("PRINTB", "ZPRINTB", Data = PrintOp.Address, HasSideEffect = true, Summary = "Prints a ZSCII string from memory, given its byte address.")]
        [Builtin("PRINTC", Data = PrintOp.Character, HasSideEffect = true, Summary = "Prints a ZSCII character.")]
        [Builtin("PRINTD", Data = PrintOp.Object, HasSideEffect = true, Summary = "Prints the DESC of an object.")]
        [Builtin("PRINTN", Data = PrintOp.Number, HasSideEffect = true, Summary = "Prints a number.")]
        [Builtin("PRINTU", Data = PrintOp.Unicode, HasSideEffect = true, Summary = "Prints a Unicode character.")]
        public static void UnaryPrintVoidOp(
            VoidCall c, [Data] PrintOp op, IOperand value)
        {
            c.rb.EmitPrint(op, value);
        }

        /// <summary>
        /// Prints a rectangle of text spreading right and down from the current cursor position.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="table">The table of ZSCII characters to print.</param>
        /// <param name="width">The width of the rectangle to print.</param>
        /// <param name="height">The height of the rectangle to print. If omitted, defaults to 1.</param>
        /// <param name="skip">The number of characters to skip between each row. If omitted, defaults to 0.</param>
        [Builtin("PRINTT", HasSideEffect = true)]
        public static void PrintTableOp(
            VoidCall c, [Table] IOperand table, IOperand width,
             IOperand? height = null, IOperand? skip = null)
        {
            c.rb.EmitPrintTable(table, width, height, skip);
        }

        [Builtin("PRINTI", Data = false, HasSideEffect = true, Summary = "Prints a packed ZSCII string embedded in the routine.")]
        [Builtin("PRINTR", Data = true, HasSideEffect = true, Summary = "Prints a packed ZSCII string embedded in the routine, followed by a CRLF, and then returns true.")]
        public static void UnaryPrintStringOp(
            VoidCall c, [Data] bool crlfRtrue, string text)
        {
            c.rb.EmitPrint(text, crlfRtrue);
        }

        /// <summary>
        /// Prints a carriage return and line feed.
        /// </summary>
        /// <param name="c"></param>
        [Builtin("CRLF", "ZCRLF", HasSideEffect = true)]
        public static void CrlfVoidOp(VoidCall c)
        {
            c.rb.EmitPrintNewLine();
        }

        #endregion

        #region Variable Opcodes/Builtins

        /// <summary>
        /// Sets the value of a local variable.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="dest">The name of the local variable to set, or an expression producing the number of the variable.</param>
        /// <param name="value">The new value to assign to the local variable.</param>
        /// <returns>The new value.</returns>
        /// <exception cref="CompilerError">The syntax is incorrect, or an error occurred while compiling a subexpression.</exception>
        [Builtin("SET", HasSideEffect = true)]
        public static IOperand SetValueOp(
            ValueCall c, [Variable(VariableScopeQuirks = VariableScopeQuirks.Local)]  IVariable dest, ZilObject value)
        {
            c.cc.MarkVariableAsWritten(dest);

            var storage = c.cc.CompileAsOperand(c.rb, value, c.form.SourceLine, dest);
            if (storage != dest)
                c.rb.EmitStore(dest, storage);
            return dest;
        }

        /// <summary>
        /// Sets the value of a local variable.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="dest">The name of the local variable to set, or an expression producing the number of the variable.</param>
        /// <param name="value">The new value to assign to the local variable.</param>
        /// <returns>The new value.</returns>
        /// <exception cref="CompilerError">The syntax is incorrect, or an error occurred while compiling a subexpression.</exception>
        [Builtin("SET", HasSideEffect = true)]
        public static IOperand SetValueOp(
            ValueCall c, [Variable(VariableScopeQuirks = VariableScopeQuirks.Local)] SoftGlobal dest, ZilObject value)
        {
            var storage = c.cc.CompileAsOperand(c.rb, value, c.form.SourceLine, c.rb.Stack);

            Debug.Assert(c.cc.SoftGlobalsTable != null, "c.cc.SoftGlobalsTable != null");

            if (storage == c.rb.Stack)
            {
                // duplicate the value
                c.rb.EmitUnary(UnaryOp.LoadIndirect, c.rb.Stack.Indirect, c.rb.Stack);
            }

            c.rb.EmitTernary(
                dest.IsWord ? TernaryOp.PutWord : TernaryOp.PutByte,
                c.cc.SoftGlobalsTable,
                c.cc.Game.MakeOperand(dest.Offset),
                storage,
                null);

            return storage;
        }

        /// <summary>
        /// Sets the value of a local variable.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="dest">The name of the local variable to set, or an expression producing the number of the variable.</param>
        /// <param name="value">The new value to assign to the local variable.</param>
        /// <returns>The new value.</returns>
        /// <exception cref="CompilerError">The syntax is incorrect, or an error occurred while compiling a subexpression.</exception>
        [Builtin("SET", HasSideEffect = true, Priority = 2)]
        public static IOperand SetValueOp(ValueCall c, IOperand dest, IOperand value)
        {
            // use a temporary variable
            // slightly tricky because dest and value might both be Stack,
            // and we need them in the other order for the StoreIndirect.
            // the temp variable will also go out of scope immediately.

            // TODO: recognize when the evaluation order doesn't matter and swap them to emit simpler code

            var tempAtom = ZilAtom.Parse("?TMP", c.cc.Context);
            c.cc.PushInnerLocal(c.rb, tempAtom, LocalBindingType.CompilerTemporary, c.form.SourceLine);
            try
            {
                var tempLocal = c.cc.Locals[tempAtom].LocalBuilder;
                // store value into temp
                c.rb.EmitStore(tempLocal, value);
                // store temp into dest
                c.rb.EmitBinary(BinaryOp.StoreIndirect, dest, tempLocal, null);
                // push temp back onto stack as result
                c.rb.EmitStore(c.rb.Stack, tempLocal);
                return c.rb.Stack;
            }
            finally
            {
                c.cc.PopInnerLocal(tempAtom);
            }
        }

        /// <summary>
        /// Sets the value of a global variable.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="dest">The name of the global variable to set, or an expression producing the number of the variable.</param>
        /// <param name="value">The new value to assign to the global variable.</param>
        /// <returns>The new value.</returns>
        /// <exception cref="CompilerError">The syntax is incorrect, or an error occurred while compiling a subexpression.</exception>
        [Builtin("SETG", HasSideEffect = true)]
        public static IOperand SetgValueOp(
            ValueCall c, [Variable(VariableScopeQuirks = VariableScopeQuirks.Global)] IVariable dest, ZilObject value)
        {
            return SetValueOp(c, dest, value);
        }

        /// <summary>
        /// Sets the value of a global variable.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="dest">The name of the global variable to set, or an expression producing the number of the variable.</param>
        /// <param name="value">The new value to assign to the global variable.</param>
        /// <returns>The new value.</returns>
        /// <exception cref="CompilerError">The syntax is incorrect, or an error occurred while compiling a subexpression.</exception>
        [Builtin("SETG", HasSideEffect = true)]
        public static IOperand SetgValueOp(
            ValueCall c, [Variable(VariableScopeQuirks = VariableScopeQuirks.Global)] SoftGlobal dest, ZilObject value)
        {
            return SetValueOp(c, dest, value);
        }

        [Builtin("SETG", HasSideEffect = true, Priority = 2)]
        public static IOperand SetgValueOp(ValueCall c, IOperand dest, IOperand value)
        {
            return SetValueOp(c, dest, value);
        }

        /// <exception cref="CompilerError">Local variables are not allowed here.</exception>
        [Builtin("SET")]
        public static void SetVoidOp(
            VoidCall c, [Variable(VariableScopeQuirks = VariableScopeQuirks.Local)] IOperand dest, ZilObject value)
        {
            // in void context, we don't need to return the newly set value, so we
            // can support <SET <fancy-expression> value>.

            if (dest is IIndirectOperand ind)
            {
                var destVar = ind.Variable;
                c.cc.MarkVariableAsWritten(destVar);
                var storage = c.cc.CompileAsOperand(c.rb, value, c.form.SourceLine, destVar);
                if (storage != destVar)
                    c.rb.EmitStore(destVar, storage);
            }
            else
            {
                using var operands = c.cc.CompileOperands(c.rb, c.form.SourceLine, value);

                if (dest == c.rb.Stack && operands[0] == c.rb.Stack)
                {
                    var tempAtom = ZilAtom.Parse("?TMP", c.cc.Context);
                    c.cc.PushInnerLocal(c.rb, tempAtom, LocalBindingType.CompilerTemporary, c.form.SourceLine);
                    try
                    {
                        var tempLocal = c.cc.Locals[tempAtom].LocalBuilder;
                        c.rb.EmitStore(tempLocal, operands[0]);
                        c.rb.EmitBinary(BinaryOp.StoreIndirect, dest, tempLocal, null);
                    }
                    finally
                    {
                        c.cc.PopInnerLocal(tempAtom);
                    }
                }
                else
                {
                    c.rb.EmitBinary(BinaryOp.StoreIndirect, dest, operands[0], null);
                }
            }
        }

        /// <summary>
        /// Sets the value of a local variable.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="dest">The name of the local variable to set, or an expression producing the number of the variable.</param>
        /// <param name="value">The new value to assign to the local variable.</param>
        /// <exception cref="CompilerError">The syntax is incorrect, or an error occurred while compiling a subexpression.</exception>
        [Builtin("SET")]
        public static void SetVoidOp(
            VoidCall c, [Variable(VariableScopeQuirks = VariableScopeQuirks.Local)] SoftGlobal dest, ZilObject value)
        {
            Debug.Assert(c.cc.SoftGlobalsTable != null);
            c.rb.EmitTernary(
                dest.IsWord ? TernaryOp.PutWord : TernaryOp.PutByte,
                c.cc.SoftGlobalsTable,
                c.cc.Game.MakeOperand(dest.Offset),
                c.cc.CompileAsOperand(c.rb, value, c.form.SourceLine),
                null);
        }

        /// <summary>
        /// Sets the value of a global variable.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="dest">The name of the global variable to set, or an expression producing the number of the variable.</param>
        /// <param name="value">The new value to assign to the global variable.</param>
        /// <exception cref="CompilerError">Local variables are not allowed here.</exception>
        [Builtin("SETG")]
        public static void SetgVoidOp(
            VoidCall c, [Variable(VariableScopeQuirks = VariableScopeQuirks.Global)] IOperand dest, ZilObject value)
        {
            SetVoidOp(c, dest, value);
        }

        /// <summary>
        /// Sets the value of a global variable.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="dest">The name of the global variable to set, or an expression producing the number of the variable.</param>
        /// <param name="value">The new value to assign to the global variable.</param>
        /// <exception cref="CompilerError">The syntax is incorrect, or an error occurred while compiling a subexpression.</exception>
        [Builtin("SETG")]
        public static void SetgVoidOp(
            VoidCall c, [Variable(VariableScopeQuirks = VariableScopeQuirks.Global)] SoftGlobal dest, ZilObject value)
        {
            SetVoidOp(c, dest, value);
        }

        /// <summary>
        /// Sets the value of a local variable.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="dest">The name of the local variable to set, or an expression producing the number of the variable.</param>
        /// <param name="value">The new value to assign to the local variable.</param>
        /// <exception cref="CompilerError">The syntax is incorrect, or an error occurred while compiling a subexpression.</exception>
        [Builtin("SET", HasSideEffect = true)]
        public static void SetPredOp(
            PredCall c, [Variable(VariableScopeQuirks = VariableScopeQuirks.Local)] IVariable dest, ZilObject value)
        {
            // see note in SetValueOp regarding dest being IVariable
            c.cc.MarkVariableAsWritten(dest);
            c.cc.CompileAsOperandWithBranch(c.rb, value, dest, c.label, c.polarity);
        }

        /// <summary>
        /// Sets the value of a global variable.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="dest">The name of the global variable to set, or an expression producing the number of the variable.</param>
        /// <param name="value">The new value to assign to the global variable.</param>
        /// <exception cref="CompilerError">The syntax is incorrect, or an error occurred while compiling a subexpression.</exception>
        [Builtin("SETG", HasSideEffect = true)]
        public static void SetgPredOp(
            PredCall c, [Variable(VariableScopeQuirks = VariableScopeQuirks.Global)] IVariable dest, ZilObject value)
        {
            SetPredOp(c, dest, value);
        }

        [Builtin("INC", Data = BinaryOp.Add, HasSideEffect = true, Summary = "Increments the value of a variable by 1.")]
        [Builtin("DEC", Data = BinaryOp.Sub, HasSideEffect = true, Summary = "Decrements the value of a variable by 1.")]
        public static IOperand IncValueOp(ValueCall c, [Data] BinaryOp op,
            [Variable(VariableScopeQuirks = VariableScopeQuirks.Both)] IVariable victim)
        {
            c.cc.MarkVariableAsReadAndWritten(victim);
            c.rb.EmitBinary(op, victim, c.cc.Game.One, victim);
            return victim;
        }

        [Builtin("INC", Data = BinaryOp.Add, HasSideEffect = true, Summary = "Increments the value of a variable by 1.")]
        [Builtin("DEC", Data = BinaryOp.Sub, HasSideEffect = true, Summary = "Decrements the value of a variable by 1.")]
        public static IOperand IncValueOp(ValueCall c, [Data] BinaryOp op,
            [Variable(VariableScopeQuirks = VariableScopeQuirks.Both)] SoftGlobal victim)
        {
            var offset = c.cc.Game.MakeOperand(victim.Offset);

            Debug.Assert(c.cc.SoftGlobalsTable != null, "c.cc.SoftGlobalsTable != null");

            c.rb.EmitBinary(
                victim.IsWord ? BinaryOp.GetWord : BinaryOp.GetByte,
                c.cc.SoftGlobalsTable,
                offset,
                c.rb.Stack);
            c.rb.EmitBinary(op, c.rb.Stack, c.cc.Game.One, c.rb.Stack);
            c.rb.EmitUnary(UnaryOp.LoadIndirect, c.rb.Stack.Indirect, c.rb.Stack);
            c.rb.EmitTernary(
                victim.IsWord ? TernaryOp.PutWord : TernaryOp.PutByte,
                c.cc.SoftGlobalsTable,
                offset,
                c.rb.Stack,
                null);
            return c.rb.Stack;
        }

        [Builtin("INC", Data = BinaryOp.Add, HasSideEffect = true, Summary = "Increments the value of a variable by 1.")]
        [Builtin("DEC", Data = BinaryOp.Sub, HasSideEffect = true, Summary = "Decrements the value of a variable by 1.")]
        public static void IncVoidOp(VoidCall c, [Data] BinaryOp op,
            [Variable(VariableScopeQuirks = VariableScopeQuirks.Both)] IVariable victim)
        {
            c.cc.MarkVariableAsReadAndWritten(victim);
            c.rb.EmitBinary(op, victim, c.cc.Game.One, victim);
        }

        [Builtin("INC", Data = BinaryOp.Add, HasSideEffect = true, Summary = "Increments the value of a variable by 1.")]
        [Builtin("DEC", Data = BinaryOp.Sub, HasSideEffect = true, Summary = "Decrements the value of a variable by 1.")]
        public static void IncVoidOp(VoidCall c, [Data] BinaryOp op,
            [Variable(VariableScopeQuirks = VariableScopeQuirks.Both)] SoftGlobal victim)
        {
            var offset = c.cc.Game.MakeOperand(victim.Offset);

            Debug.Assert(c.cc.SoftGlobalsTable != null, "c.cc.SoftGlobalsTable != null");

            c.rb.EmitBinary(
                victim.IsWord ? BinaryOp.GetWord : BinaryOp.GetByte,
                c.cc.SoftGlobalsTable,
                offset,
                c.rb.Stack);
            c.rb.EmitBinary(op, c.rb.Stack, c.cc.Game.One, c.rb.Stack);
            c.rb.EmitTernary(
                victim.IsWord ? TernaryOp.PutWord : TernaryOp.PutByte,
                c.cc.SoftGlobalsTable,
                offset,
                c.rb.Stack,
                null);
        }

        /// <summary>
        /// Pushes a value onto the evaluation stack.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="value">The value to push.</param>
        [Builtin("PUSH", HasSideEffect = true)]
        public static void PushVoidOp(VoidCall c, IOperand value)
        {
            c.rb.EmitStore(c.rb.Stack, value);
        }

        /// <summary>
        /// Pushes a value onto a user-defined stack.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="value">The value to push.</param>
        /// <param name="stack">The address of the user-defined stack to push onto.</param>
        [Builtin("XPUSH", MinVersion = 6, HasSideEffect = true)]
        public static void XpushPredOp(PredCall c, IOperand value, IOperand stack)
        {
            c.rb.EmitPushUserStack(value, stack, c.label, c.polarity);
        }

        /// <summary>
        /// Pops a value from the evaluation stack or a user-defined stack.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="stack">The address of the user-defined stack to pop from. If omitted, pops from the evaluation stack.</param>
        /// <returns>The popped value.</returns>
        [Builtin("POP", MinVersion = 6, HasSideEffect = true)]
        public static IOperand PopValueOp(ValueCall c, IOperand? stack = null)
        {
            if (stack == null)
                c.rb.EmitStore(c.resultStorage, c.rb.Stack);
            else
                c.rb.EmitUnary(UnaryOp.PopUserStack, stack, c.resultStorage);

            return c.resultStorage;
        }

        /// <summary>
        /// Discards values from the evaluation stack or a user-defined stack.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="count">The number of values to discard.</param>
        /// <param name="stack">The address of the user-defined stack to discard from. If omitted, discards from the evaluation stack.</param>
        [Builtin("FSTACK", MinVersion = 6, HasSideEffect = true)]
        public static void FstackVoidOp(VoidCall c, IOperand count, IOperand? stack = null)
        {
            if (stack == null)
                c.rb.EmitUnary(UnaryOp.FlushStack, count, null);
            else
                c.rb.EmitBinary(BinaryOp.FlushUserStack, count, stack, null);
        }

        /// <summary>
        /// Returns the value of a variable.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="var">The name of the variable to get, or an expression producing the number of the variable.</param>
        /// <returns>The value of the variable.</returns>
        [Builtin("VALUE", Priority = 1)]
        public static IOperand ValueOp_Variable(ValueCall c, [Variable] IVariable var)
        {
            if (var == c.rb.Stack)
            {
                c.rb.EmitUnary(UnaryOp.LoadIndirect, var.Indirect, c.resultStorage);
                return c.resultStorage;
            }

            c.cc.MarkVariableAsRead(var);
            return var;
        }

        /// <summary>
        /// Returns the value of a variable.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="value">The name of the variable to get, or an expression producing the number of the variable.</param>
        /// <returns>The value of the variable.</returns>
        [Builtin("VALUE", Priority = 2)]
        public static IOperand ValueOp_Operand(ValueCall c, [Variable] IOperand value)
        {
            c.rb.EmitUnary(UnaryOp.LoadIndirect, value, c.resultStorage);
            return c.resultStorage;
        }

        /// <summary>
        /// Returns the value of a global or constant.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="atom">The name of the global or constant to get.</param>
        /// <returns>The value of the global or constant.</returns>
        [Builtin("GVAL")]
        public static IOperand GvalOp(ValueCall c, ZilAtom atom)
        {
            // constant, global, object, or routine
            if (c.cc.Constants.TryGetValue(atom, out var operand))
                return operand;
            if (c.cc.Globals.TryGetValue(atom, out var global))
                return global;
            if (c.cc.Objects.TryGetValue(atom, out var objbld))
                return objbld;
            if (c.cc.Routines.TryGetValue(atom, out var routine))
                return routine;

            // soft global
            if (c.cc.SoftGlobals.TryGetValue(atom, out var softGlobal))
            {
                Debug.Assert(c.cc.SoftGlobalsTable != null, nameof(c.cc.SoftGlobalsTable) + " != null");

                c.rb.EmitBinary(
                    softGlobal.IsWord ? BinaryOp.GetWord : BinaryOp.GetByte,
                    c.cc.SoftGlobalsTable,
                    c.cc.Game.MakeOperand(softGlobal.Offset),
                    c.resultStorage);
                return c.resultStorage;
            }

            // quirks: local
            if (c.cc.Locals.TryGetValue(atom, out var lbr))
            {
                c.cc.Context.HandleError(new CompilerError(
                    c.form,
                    CompilerMessages.No_Such_0_Variable_1_Using_The_2_Instead,
                    "global",
                    atom,
                    "local"));
                Compilation.MarkVariableAsRead(lbr);
                return lbr.LocalBuilder;
            }

            // error
            c.cc.Context.HandleError(new CompilerError(
                c.form,
                CompilerMessages.Undefined_0_1,
                "global or constant",
                atom));
            return c.cc.Game.Zero;
        }

        /// <summary>
        /// Returns the value of a local variable.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="atom">The name of the local variable to get.</param>
        /// <returns>The value of the local variable.</returns>
        [Builtin("LVAL")]
        public static IOperand LvalOp(ValueCall c, ZilAtom atom)
        {
            // local
            if (c.cc.Locals.TryGetValue(atom, out var lbr))
            {
                Compilation.MarkVariableAsRead(lbr);
                return lbr.LocalBuilder;
            }

            // quirks: constant, global, object, or routine
            if (c.cc.Constants.TryGetValue(atom, out var operand))
            {
                c.cc.Context.HandleError(new CompilerError(
                    c.form,
                    CompilerMessages.No_Such_0_Variable_1_Using_The_2_Instead,
                    "local",
                    atom,
                    "constant"));
                return operand;
            }

            if (c.cc.Globals.TryGetValue(atom, out var global))
            {
                c.cc.Context.HandleError(new CompilerError(
                    c.form,
                    CompilerMessages.No_Such_0_Variable_1_Using_The_2_Instead,
                    "local",
                    atom,
                    "global"));
                return global;
            }

            if (c.cc.Objects.TryGetValue(atom, out var objbld))
            {
                c.cc.Context.HandleError(new CompilerError(
                    c.form,
                    CompilerMessages.No_Such_0_Variable_1_Using_The_2_Instead,
                    "local",
                    atom,
                    "object"));
                return objbld;
            }

            if (c.cc.Routines.TryGetValue(atom, out var routine))
            {
                c.cc.Context.HandleError(new CompilerError(
                    c.form,
                    CompilerMessages.No_Such_0_Variable_1_Using_The_2_Instead,
                    "local",
                    atom,
                    "routine"));
                return routine;
            }

            // error
            c.cc.Context.HandleError(new CompilerError(
                c.form,
                CompilerMessages.Undefined_0_1,
                "local",
                atom));
            return c.cc.Game.Zero;
        }

        #endregion

        #region Nullary Opcodes

        [Builtin("CATCH", Data = NullaryOp.Catch, MinVersion = 5, Summary = "Returns a catch token for use with THROW.")]
        [Builtin("ISAVE", Data = NullaryOp.SaveUndo, HasSideEffect = true, MinVersion = 5, Summary = "Saves the current game state for later restoration with IRESTORE.")]
        [Builtin("IRESTORE", Data = NullaryOp.RestoreUndo, HasSideEffect = true, MinVersion = 5, Summary = "Restores a game state previously saved with ISAVE.")]
        public static IOperand NullaryValueOp(ValueCall c, [Data] NullaryOp op)
        {
            c.rb.EmitNullary(op, c.resultStorage);
            return c.resultStorage;
        }

        [Builtin("ORIGINAL?", Data = Condition.Original, MinVersion = 5, Summary = "Asks the interpreter to test whether the story file is genuine. Typically hardcoded to return true.")]
        [Builtin("VERIFY", Data = Condition.Verify, Summary = "Tests whether the checksum of the story file is valid.")]
        public static void NullaryPredOp(PredCall c, [Data] Condition cond)
        {
            c.rb.Branch(cond, null, null, c.label, c.polarity);
        }

        [Builtin("USL", Data = NullaryOp.ShowStatus, MinVersion = 3, MaxVersion = 3, HasSideEffect = true, Summary = "Updates the status line.")]
        public static void NullaryVoidOp(VoidCall c, [Data] NullaryOp op)
        {
            c.rb.EmitNullary(op, null);
        }

        [Builtin("RTRUE", Data = 1, HasSideEffect = true, Summary = "Returns true (1) from the current routine.")]
        [Builtin("RFALSE", Data = 0, HasSideEffect = true, Summary = "Returns false (0) from the current routine.")]
        [Builtin("RFATAL", Data = 2, HasSideEffect = true, Summary = "Returns a fatal error code (2) from the current routine.")]
        [Builtin("RSTACK", Data = -1, HasSideEffect = true, Summary = "Returns a value popped from the stack from the current routine.")]
        public static void NullaryReturnOp(VoidCall c, [Data] int what)
        {
            var operand =
                (what == 1) ? c.cc.Game.One :
                (what == 0) ? c.cc.Game.Zero :
                (what == 2) ? (IOperand)c.cc.Game.MakeOperand(2) :
                c.rb.Stack;

            c.rb.Return(operand);
        }

        /// <summary>
        /// Resets all interpreter state and restarts the game from the beginning.
        /// </summary>
        /// <param name="c"></param>
        [Builtin("RESTART", HasSideEffect = true)]
        public static void RestartOp(VoidCall c)
        {
            c.rb.EmitRestart();
        }

        /// <summary>
        /// Quits the interpreter, ending the game.
        /// </summary>
        /// <param name="c"></param>
        [Builtin("QUIT", HasSideEffect = true)]
        public static void QuitOp(VoidCall c)
        {
            c.rb.EmitQuit();
        }

        #endregion

        #region Input Opcodes

        /// <summary>
        /// Reads a line of text from the user into the given text and parse buffers.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="text">The address of the buffer where the input text will be written.</param>
        /// <param name="parse">The address of the buffer where the parsed vocabulary words will be written.</param>
        [Builtin("READ", "ZREAD", MaxVersion = 3, HasSideEffect = true)]
        public static void ReadOp_V3(VoidCall c, IOperand text, IOperand parse)
        {
            c.rb.EmitRead(text, parse, null, null, null);
        }

        /// <summary>
        /// Reads a line of text from the user into the given text and parse buffers.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="text">The address of the buffer where the input text will be written.</param>
        /// <param name="parse">The address of the buffer where the parsed vocabulary words will be written.</param>
        /// <param name="time">The interval between calls to the interrupt routine, in tenths of a second. If omitted, no routine is called.</param>
        /// <param name="routine">The address of an interrupt routine to call while waiting for input. If omitted, no routine is called.</param>
        [Builtin("READ", "ZREAD", MinVersion = 4, MaxVersion = 4, HasSideEffect = true)]
        public static void ReadOp_V4(VoidCall c, IOperand text, IOperand parse,
            IOperand? time = null, [Routine] IOperand? routine = null)
        {
            c.rb.EmitRead(text, parse, time, routine, null);
        }

        /// <summary>
        /// Reads a line of text from the user into the given text and parse buffers.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="text">The address of the buffer where the input text will be written.</param>
        /// <param name="parse">The address of the buffer where the parsed vocabulary words will be written.</param>
        /// <param name="time">The interval between calls to the interrupt routine, in tenths of a second. If omitted, no routine is called.</param>
        /// <param name="routine">The address of an interrupt routine to call while waiting for input. If omitted, no routine is called.</param>
        [Builtin("READ", "ZREAD", MinVersion = 5, HasSideEffect = true)]
        public static IOperand ReadOp_V5(ValueCall c, IOperand text,
            IOperand? parse = null, IOperand? time = null,
            [Routine] IOperand? routine = null)
        {
            c.rb.EmitRead(text, parse, time, routine, c.resultStorage);
            return c.resultStorage;
        }

        /// <summary>
        /// Reads a character from the user.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="dummy">Must be 1.</param>
        /// <param name="interval">The interval between calls to the interrupt routine, in tenths of a second. If omitted, no routine is called.</param>
        /// <param name="routine">The address of an interrupt routine to call while waiting for input. If omitted, no routine is called.</param>
        [Builtin("INPUT", MinVersion = 4, HasSideEffect = true)]
        public static IOperand InputOp(ValueCall c, IOperand dummy,
            IOperand? interval = null, [Routine] IOperand? routine = null)
        {
            if (c.form.StartsWith(out ZilObject? _, out ZilFix? fix) && fix.Value != 1)
            {
                return c.HandleMessage(
                    CompilerMessages._0_Argument_1_2,
                    "INPUT",
                    1,
                    "argument must be '1'");
            }

            c.rb.EmitReadChar(interval, routine, c.resultStorage);

            if (dummy == c.rb.Stack)
                c.rb.EmitPopStack();

            return c.resultStorage;
        }

        #endregion

        #region Sound Opcodes

        /// <summary>
        /// Controls a sound effect.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="number">The number of the sound effect to control.</param>
        /// <param name="effect">The operation to perform: 1 to prepare, 2 to start playing, 3 to stop playing, and 4 to finish.</param>
        /// <param name="volume">The volume in the low byte and the number of repeats in the high byte.</param>
        [Builtin("SOUND", MaxVersion = 4, HasSideEffect = true)]
        public static void SoundOp_V3(VoidCall c, IOperand number,
            IOperand? effect = null, IOperand? volume = null)
        {
            c.rb.EmitPlaySound(number, effect, volume, null);
        }

        /// <summary>
        /// Controls a sound effect.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="number">The number of the sound effect to control.</param>
        /// <param name="effect">The operation to perform: 1 to prepare, 2 to start playing, 3 to stop playing, and 4 to finish.</param>
        /// <param name="volume">The volume in the low byte and the number of repeats in the high byte.</param>
        /// <param name="routine">The address of an interrupt routine to call when the sound is done playing. If omitted, no routine is called.</param>
        public static void SoundOp_V5(VoidCall c, IOperand number,
            IOperand? effect = null, IOperand? volume = null,
             [Routine] IOperand? routine = null)
        {
            c.rb.EmitPlaySound(number, effect, volume, routine);
        }

        #endregion

        #region Vocab Opcodes

        /// <summary>
        /// Encodes text from a ZSCII string into a packed string as if it were a vocabulary word.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="src">The address of the ZSCII string.</param>
        /// <param name="length">The number of ZSCII characters to encode.</param>
        /// <param name="srcOffset">The offset within the string at which to start encoding.</param>
        /// <param name="dest">The address where the encoded packed string will be stored.</param>
        [Builtin("ZWSTR", MinVersion = 5, HasSideEffect = true)]
        public static void EncodeTextOp(VoidCall c,
            [Table] IOperand src, IOperand length,
            IOperand srcOffset, [Table] IOperand dest)
        {
            c.rb.EmitEncodeText(src, length, srcOffset, dest);
        }

        /// <summary>
        /// Tokenizes input text into words.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="text">The address of the input text to tokenize, in the same format it would be stored by READ.</param>
        /// <param name="parse">The address of the buffer where the tokenized words will be stored.</param>
        /// <param name="dictionary">The address of a user-specified vocabulary table to use. If omitted, defaults to the regular game vocabulary table.</param>
        /// <param name="flag">If true, entries will not be written for unrecognized words. If omitted, defaults to false.</param>
        [Builtin("LEX", MinVersion = 5, HasSideEffect = true)]
        public static void LexOp(VoidCall c,
            [Table] IOperand text, [Table] IOperand parse,
            [Table] IOperand? dictionary = null, IOperand? flag = null)
        {
            c.rb.EmitTokenize(text, parse, dictionary, flag);
        }

        #endregion

        #region Save/Restore Opcodes

        /// <summary>
        /// Restores a saved game state.
        /// </summary>
        /// <param name="c"></param>
        /// <returns>False if the restore failed. Does not return if it succeeded.</returns>
        /// <exception cref="NotSupportedException">Wrong Z-machine version for this form of the opcode.</exception>
        [Builtin("RESTORE", "ZRESTORE", MaxVersion = 3, HasSideEffect = true)]
        public static void RestoreOp_V3(PredCall c)
        {
            if (c.rb.HasBranchSave)
            {
                c.rb.EmitRestore(c.label, c.polarity);
            }
            else
            {
                throw new NotSupportedException($"{nameof(RestoreOp_V3)} without {nameof(c.rb.HasBranchSave)}");
            }
        }

        /// <summary>
        /// Restores a saved game state.
        /// </summary>
        /// <param name="c"></param>
        /// <returns>Zero if the restore failed. Does not return if it succeeded.</returns>
        /// <exception cref="NotSupportedException">Wrong Z-machine version for this form of the opcode.</exception>
        [Builtin("RESTORE", "ZRESTORE", MinVersion = 4, HasSideEffect = true)]
        public static IOperand RestoreOp_V4(ValueCall c)
        {
            if (c.rb.HasStoreSave)
            {
                c.rb.EmitRestore(c.resultStorage);
                return c.resultStorage;
            }
            throw new NotSupportedException($"{nameof(RestoreOp_V4)} without {nameof(c.rb.HasStoreSave)}");
        }

        /// <summary>
        /// Restores a saved game state, or loads a file into memory.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="table">The address of the buffer where the file contents will be stored. If omitted, restores a saved game state.</param>
        /// <param name="bytes">The number of bytes to read from the file. If omitted, restores a saved game state.</param>
        /// <param name="name">The address of the buffer containing the file name prefixed by a length byte. If omitted, restores a saved game state.</param>
        /// <returns>The number of bytes loaded from the file, or zero if restoring a saved game state.</returns>
        /// <exception cref="NotSupportedException">Wrong Z-machine version for this form of the opcode.</exception>
        [Builtin("RESTORE", "ZRESTORE", MinVersion = 5, HasSideEffect = true)]
        public static IOperand RestoreOp_V5(ValueCall c, [Table] IOperand table,
            IOperand bytes, [Table] IOperand name)
        {
            if (c.rb.HasExtendedSave)
            {
                c.rb.EmitRestore(table, bytes, name, c.resultStorage);
                return c.resultStorage;
            }
            throw new NotSupportedException($"{nameof(RestoreOp_V5)} without {nameof(c.rb.HasExtendedSave)}");
        }

        /// <summary>
        /// Saves the game state.
        /// </summary>
        /// <param name="c"></param>
        /// <returns>False if the save failed, or true if it succeeded.</returns>
        /// <exception cref="NotSupportedException">Wrong Z-machine version for this form of the opcode.</exception>
        [Builtin("SAVE", "ZSAVE", MaxVersion = 3, HasSideEffect = true)]
        public static void SaveOp_V3(PredCall c)
        {
            if (c.rb.HasBranchSave)
            {
                c.rb.EmitSave(c.label, c.polarity);
            }
            else
            {
                throw new NotSupportedException($"{nameof(SaveOp_V3)} without {nameof(c.rb.HasBranchSave)}");
            }
        }

        /// <summary>
        /// Saves the game state.
        /// </summary>
        /// <param name="c"></param>
        /// <returns>Zero if the save failed, 1 if it succeeded, or 2 when the game state is being restored later.</returns>
        /// <exception cref="NotSupportedException">Wrong Z-machine version for this form of the opcode.</exception>
        [Builtin("SAVE", "ZSAVE", MinVersion = 4, HasSideEffect = true)]
        public static IOperand SaveOp_V4(ValueCall c)
        {
            if (c.rb.HasStoreSave)
            {
                c.rb.EmitSave(c.resultStorage);
                return c.resultStorage;
            }
            throw new NotSupportedException($"{nameof(SaveOp_V4)} without {nameof(c.rb.HasStoreSave)}");
        }

        /// <summary>
        /// Saves the game state, or saves a portion of memory to a file.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="table">The address of the buffer where the file contents are stored. If omitted, saves the game state.</param>
        /// <param name="bytes">The number of bytes to write to the file. If omitted, saves the game state.</param>
        /// <param name="name">The address of the buffer containing the file name prefixed by a length byte. If omitted, saves the game state.</param>
        /// <returns>Zero if the save failed, 1 if it succeeded, or 2 when the game state is being restored later.</returns>
        /// <exception cref="NotSupportedException">Wrong Z-machine version for this form of the opcode.</exception>
        [Builtin("SAVE", "ZSAVE", MinVersion = 5, HasSideEffect = true)]
        public static IOperand SaveOp_V5(ValueCall c, [Table] IOperand table,
            IOperand bytes, [Table] IOperand name)
        {
            if (c.rb.HasExtendedSave)
            {
                c.rb.EmitSave(table, bytes, name, c.resultStorage);
                return c.resultStorage;
            }
            throw new NotSupportedException($"{nameof(SaveOp_V5)} without {nameof(c.rb.HasExtendedSave)}");
        }

        #endregion

        #region Routine Opcodes/Builtins

        /// <summary>
        /// Causes the current routine or enclosing PROG/REPEAT block to return a value.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="expr">The value to return. If omitted, defaults to 1.</param>
        /// <param name="block">The enclosing PROG/REPEAT block to return from. If omitted, returns from the innermost PROG/REPEAT or the current routine.</param>
        /// <exception cref="CompilerError">The syntax is incorrect, or an error occurred while compiling a subexpression.</exception>
        [Builtin("RETURN", HasSideEffect = true)]
        public static void ReturnOp(VoidCall c, ZilObject? expr = null, Block? block = null)
        {
            var origBlock = block;

            block ??= c.cc.Blocks.First(b => (b.Flags & BlockFlags.ExplicitOnly) == 0);

            IOperand value;

            var quirkMode = c.cc.Context.ReturnQuirkMode;

            var preferRoutine = quirkMode switch
            {
                ReturnQuirkMode.ByVersion => (c.cc.Context.ZEnvironment.ZVersion >= 5),
                ReturnQuirkMode.PreferRoutine => true,
                _ => false
            };

            if (block.ReturnLabel == null || (expr != null && preferRoutine))
            {
                // return from routine
                value = expr != null
                    ? c.cc.CompileAsOperand(c.rb, expr, c.form.SourceLine)
                    : c.cc.Game.One;
                c.rb.Return(value);

                /* TODO: warn about iffy preferRoutine situations, if we can identify them somehow
                 * one possibility is if a local variable was set in this basic block and hasn't been
                 * read yet. it may be a flag that the author expects to read outside the loop. */
            }
            else
            {
                // return from enclosing PROG/REPEAT
                if ((block.Flags & BlockFlags.WantResult) != 0)
                {
                    var resultStorage = block.ResultStorage ?? c.rb.Stack;
                    if (expr == null)
                    {
                        c.rb.EmitStore(resultStorage, c.cc.Game.One);
                    }
                    else
                    {
                        value = c.cc.CompileAsOperand(
                            c.rb,
                            expr,
                            c.form.SourceLine,
                            resultStorage);
                        if (value != resultStorage)
                            c.rb.EmitStore(resultStorage, value);
                    }
                }
                else if (expr != null)
                {
                    value = c.cc.CompileAsOperand(c.rb, expr, c.form.SourceLine);
                    if (value == c.rb.Stack)
                        c.rb.EmitPopStack();

                    // warn that the value was ignored, unless this looks like a case where
                    // a dummy value was required in order to return from a named block
                    if (origBlock == null ||
                        (value != c.cc.Game.Zero && value != c.cc.Game.One))
                    {
                        c.HandleMessage(CompilerMessages.RETURN_Value_Ignored_PROGREPEAT_Block_Is_In_Void_Context);
                    }
                }

                block.Flags |= BlockFlags.Returned;
                c.rb.Branch(block.ReturnLabel);
            }
        }

        /// <summary>
        /// Causes the current routine or enclosing PROG/REPEAT block to repeat from the beginning, without reinitializing variables.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="block">The enclosing PROG/REPEAT block to repeat. If omitted, repeats the innermost PROG/REPEAT or the current routine.</param>
        [Builtin("AGAIN", HasSideEffect = true)]
        public static void AgainOp(VoidCall c, Block? block = null)
        {
            block ??= c.cc.Blocks.First(b => (b.Flags & BlockFlags.ExplicitOnly) == 0);

            if (block.AgainLabel != null)
            {
                c.rb.Branch(block.AgainLabel);
            }
            else
            {
                c.HandleMessage(CompilerMessages.AGAIN_Requires_A_PROGREPEAT_Block_Or_Routine);
            }
        }

        /// <summary>
        /// Calls a routine and returns its value.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="routine">The packed address of the routine to call.</param>
        /// <param name="args">The arguments to pass to the routine.</param>
        /// <returns>The value returned by the routine.</returns>
        [Builtin("APPLY", "CALL", "ZAPPLY", HasSideEffect = true)]
        public static IOperand CallValueOp(ValueCall c,
            [Routine] IOperand routine, params IOperand[] args)
        {
            if (args.Length > c.cc.Game.MaxCallArguments)
            {
                return c.HandleMessage(
                    CompilerMessages.Too_Many_Call_Arguments_Only_0_Allowed_In_V1,
                    c.cc.Game.MaxCallArguments, c.cc.Context.ZEnvironment.ZVersion);
            }

            c.rb.EmitCall(routine, args, c.resultStorage);
            return c.resultStorage;
        }

        /// <summary>
        /// Calls a routine without returning a value.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="routine">The packed address of the routine to call.</param>
        /// <param name="args">The arguments to pass to the routine.</param>
        [Builtin("APPLY", "CALL", MinVersion = 5, HasSideEffect = true)]
        public static void CallVoidOp(VoidCall c,
            [Routine] IOperand routine, params IOperand[] args)
        {
            if (args.Length > c.cc.Game.MaxCallArguments)
            {
                c.HandleMessage(
                    CompilerMessages.Too_Many_Call_Arguments_Only_0_Allowed_In_V1,
                    c.cc.Game.MaxCallArguments, c.cc.Context.ZEnvironment.ZVersion);
                return;
            }

            c.rb.EmitCall(routine, args, null);
        }

        #endregion

        #region Table Opcodes/Builtins

        /// <summary>
        /// Searches for a value in a table.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="value">The value to search for.</param>
        /// <param name="table">The address of the table to search in.</param>
        /// <param name="length">The length of the table in words.</param>
        /// <returns>The address of the word in the table, or zero if it wasn't found.</returns>
        [Builtin("INTBL?", MinVersion = 4, MaxVersion = 4)]
        [return: Table]
        public static void IntblValuePredOp_V4(ValuePredCall c,
            IOperand value, [Table] IOperand table, IOperand length)
        {
            c.rb.EmitScanTable(value, table, length, null, c.resultStorage, c.label, c.polarity);
        }

        /// <summary>
        /// Searches for a value in a table.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="value">The value to search for.</param>
        /// <param name="table">The address of the table to search in.</param>
        /// <param name="length">The length of the table in words.</param>
        /// <param name="form">The length of each field in the table (in bytes), with the high bit set for words and cleared for bytes. If omitted, defaults to $82 (words, 2-byte fields).</param>
        [Builtin("INTBL?", MinVersion = 5)]
        [return: Table]
        public static void IntblValuePredOp_V5(ValuePredCall c,
            IOperand value, [Table] IOperand table, IOperand length, IOperand? form = null)
        {
            c.rb.EmitScanTable(value, table, length, form, c.resultStorage, c.label, c.polarity);
        }

        static bool TryGetLowCoreField(string name, Context ctx, ISourceLine src, ZilObject fieldSpec, bool writing,
            out int offset, out LowCoreFlags flags, out int minVersion)
        {
            offset = 0;
            flags = LowCoreFlags.None;
            minVersion = 0;

            if (fieldSpec is ZilAtom atom)
            {
                var field = LowCoreField.Get(atom);
                if (field == null)
                {
                    ctx.HandleError(new CompilerError(src, CompilerMessages.Unrecognized_0_1, "header field", atom));
                    return false;
                }
                if (!ctx.ZEnvironment.VersionMatches(field.MinVersion, field.MaxVersion))
                {
                    ctx.HandleError(new CompilerError(src, CompilerMessages._0_Field_1_Is_Not_Supported_In_This_Zmachine_Version, name, atom));
                    return false;
                }
                if (writing && (field.Flags & LowCoreFlags.Writable) == 0)
                {
                    ctx.HandleError(new CompilerError(src, CompilerMessages._0_Field_1_Is_Not_Writable, name, atom));
                    return false;
                }

                offset = field.Offset;
                flags = field.Flags;
                minVersion = field.MinVersion;
                return true;
            }

            if (fieldSpec is ZilList list)
            {
                if (!list.HasLength(2))
                {
                    ctx.HandleError(new CompilerError(src, CompilerMessages._0_List_Must_Have_2_Elements, name));
                    return false;
                }

                atom = (list.First as ZilAtom)!;
                if (atom == null)
                {
                    ctx.HandleError(new CompilerError(src, CompilerMessages._0_First_List_Element_Must_Be_An_Atom, name));
                    return false;
                }

                Debug.Assert(list.Rest != null);
                if (list.Rest.First is not ZilFix { Value: 0 or 1 } fix)
                {
                    ctx.HandleError(new CompilerError(src, CompilerMessages._0_Second_List_Element_Must_Be_0_Or_1, name));
                    return false;
                }

                var field = LowCoreField.Get(atom);
                if (field == null)
                {
                    ctx.HandleError(new CompilerError(src, CompilerMessages.Unrecognized_0_1, "header field", atom));
                    return false;
                }
                if (!ctx.ZEnvironment.VersionMatches(field.MinVersion, field.MaxVersion))
                {
                    ctx.HandleError(new CompilerError(src, CompilerMessages._0_Field_1_Is_Not_Supported_In_This_Zmachine_Version, name, atom));
                    return false;
                }
                if ((field.Flags & LowCoreFlags.Byte) != 0)
                {
                    ctx.HandleError(new CompilerError(src, CompilerMessages._0_Field_1_Is_Not_A_Word_Field, name, atom));
                    return false;
                }
                if (writing && (field.Flags & LowCoreFlags.Writable) == 0)
                {
                    ctx.HandleError(new CompilerError(src, CompilerMessages._0_Field_1_Is_Not_Writable, name, atom));
                    return false;
                }

                offset = field.Offset * 2 + fix.Value;
                flags = field.Flags | LowCoreFlags.Byte;
                minVersion = field.MinVersion;
                return true;
            }

            ctx.HandleError(new CompilerError(src, CompilerMessages._0_Argument_1_2, name, 1, "argument must be an atom or list"));
            return false;
        }

        /// <summary>
        /// Reads a field from the low memory area (header and extension table).
        /// </summary>
        /// <param name="c"></param>
        /// <param name="fieldSpec">The name of a header field to read, or a list consisting of the header field name and either 0 or 1 to read the high or low byte.</param>
        /// <returns>The value of the header field.</returns>
        [Builtin("LOWCORE")]
        public static IOperand LowCoreReadOp(ValueCall c, ZilObject fieldSpec)
        {
            if (!TryGetLowCoreField("LOWCORE", c.cc.Context, c.form.SourceLine, fieldSpec, false, out var offset, out var flags, out _))
                return c.cc.Game.Zero;

            var binaryOp = ((flags & LowCoreFlags.Byte) != 0) ? BinaryOp.GetByte : BinaryOp.GetWord;
            if ((flags & LowCoreFlags.Extended) != 0)
            {
                var wordOffset = ((flags & LowCoreFlags.Byte) != 0) ? (offset + 1) / 2 : offset;
                c.cc.Context.ZEnvironment.EnsureMinimumHeaderExtension(wordOffset);
                c.rb.EmitBinary(BinaryOp.GetWord, c.cc.Game.Zero, c.cc.Game.MakeOperand(27 /* EXTAB */), c.rb.Stack);
                c.rb.EmitBinary(binaryOp, c.rb.Stack, c.cc.Game.MakeOperand(offset), c.resultStorage);
            }
            else
            {
                c.rb.EmitBinary(binaryOp, c.cc.Game.Zero, c.cc.Game.MakeOperand(offset), c.resultStorage);
            }

            return c.resultStorage;
        }

        /// <summary>
        /// Writes a value to a field in the low memory area (header and extension table).
        /// </summary>
        /// <param name="c"></param>
        /// <param name="fieldSpec">The name of a header field to write, or a list consisting of the header field name and either 0 or 1 to write the high or low byte.</param>
        /// <param name="newValue"></param>
        [Builtin("LOWCORE", HasSideEffect = true)]
        public static void LowCoreWriteOp(VoidCall c, ZilObject fieldSpec, IOperand newValue)
        {
            if (!TryGetLowCoreField("LOWCORE", c.cc.Context, c.form.SourceLine, fieldSpec, true, out var offset, out var flags, out _))
                return;

            var ternaryOp = ((flags & LowCoreFlags.Byte) != 0) ? TernaryOp.PutByte : TernaryOp.PutWord;
            if ((flags & LowCoreFlags.Extended) != 0)
            {
                var wordOffset = ((flags & LowCoreFlags.Byte) != 0) ? (offset + 1) / 2 : offset;
                c.cc.Context.ZEnvironment.EnsureMinimumHeaderExtension(wordOffset);
                c.rb.EmitBinary(BinaryOp.GetWord, c.cc.Game.Zero, c.cc.Game.MakeOperand(27 /* EXTAB */), c.rb.Stack);
                c.rb.EmitTernary(ternaryOp, c.rb.Stack, c.cc.Game.MakeOperand(offset), newValue, null);
            }
            else
            {
                c.rb.EmitTernary(ternaryOp, c.cc.Game.Zero, c.cc.Game.MakeOperand(offset), newValue, null);
            }
        }

        /// <summary>
        /// Iterates over a table in the low memory area (header and extension table), calling a handler routine for each byte.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="fieldSpec">The name of a header field to iterate over.</param>
        /// <param name="length">The number of bytes to iterate over.</param>
        /// <param name="handler">The handler routine to call for each byte.</param>
        /// <exception cref="CompilerError">Local variables are not allowed here.</exception>
        [Builtin("LOWCORE-TABLE", HasSideEffect = true)]
        public static void LowCoreTableOp(VoidCall c, ZilObject fieldSpec, int length, ZilAtom handler)
        {
            if (!TryGetLowCoreField("LOWCORE-TABLE", c.cc.Context, c.form.SourceLine, fieldSpec, false, out var offset, out var flags, out _))
                return;

            if ((flags & LowCoreFlags.Byte) == 0)
            {
                offset *= 2;
            }

            var tmpAtom = ZilAtom.Parse("?TMP", c.cc.Context);
            var lb = c.cc.PushInnerLocal(c.rb, tmpAtom, LocalBindingType.CompilerTemporary, c.form.SourceLine);
            try
            {
                c.rb.EmitStore(lb, c.cc.Game.MakeOperand(offset));

                var label = c.rb.DefineLabel();
                c.rb.MarkLabel(label);

                var form = (ZilForm)Program.Parse(c.cc.Context, c.form.SourceLine, "<{0} <GETB 0 .{1}>>", handler, tmpAtom).Single();
                c.cc.CompileForm(c.rb, form, false, null);

                c.rb.Branch(Condition.IncCheck, lb, c.cc.Game.MakeOperand(offset + length - 1), label, false);
            }
            finally
            {
                c.cc.PopInnerLocal(tmpAtom);
            }
        }

        [Builtin("ITABLE", Summary = "Defines a new table initialized by repeatedly evaluating an expression.")]
        [Builtin("TABLE", "PTABLE", "LTABLE", "PLTABLE", Summary = "Defines a new table.")]
        [return: Table]
        public static IOperand TableOp(ValueCall c, params ZilObject[] args)
        {
            /* We can't evaluate c.form directly, because it may contain wrapped values from macro
             * expansions; those values will have been unwrapped before passing them in as args.
             * But the table SUBRs have complicated argument syntax, so instead of reimplementing
             * it here, we'll just create a new FORM. */
            var nameAtom = c.form.First;
            Debug.Assert(nameAtom != null);
            var formWithExpandedArgs = new ZilForm([nameAtom, ..args]) { SourceLine = c.form.SourceLine };

            var table = (ZilTable)formWithExpandedArgs.Eval(c.cc.Context);
            var tableBuilder = c.cc.Game.DefineTable(table.Name, (table.Flags & TableFormat.Pure) != 0);
            c.cc.Tables.Add(table, tableBuilder);
            return tableBuilder;
        }

        #endregion

        #region Logical & Loop Builtins

        [Builtin("PROG", Data = StdAtom.PROG, Summary = "Evaluates a sequence of expressions inside a new block, returning the value of the last expression.")]
        [Builtin("REPEAT", Data = StdAtom.REPEAT, Summary = "Evaluates a sequence of expressions inside a new block, repeating until a RETURN or AGAIN is encountered.")]
        [Builtin("BIND", Data = StdAtom.BIND, Summary = "Evaluates a sequence of expressions without creating a new block, returning the value of the last expression.")]
        public static IOperand ProgValueOp(ValueCall c, [Data] StdAtom mode, params ZilObject[] args)
        {
            bool repeat = mode == StdAtom.REPEAT;
            bool catchy = mode != StdAtom.BIND;
            var (_, progBody) = c.form;
            return c.cc.CompilePROG(c.rb, progBody, c.form.SourceLine, true, c.resultStorage, mode.ToString(), repeat, catchy)!;
        }

        [Builtin("PROG", Data = StdAtom.PROG, Summary = "Evaluates a sequence of expressions inside a new block.")]
        [Builtin("REPEAT", Data = StdAtom.REPEAT, Summary = "Evaluates a sequence of expressions inside a new block, repeating until a RETURN or AGAIN is encountered.")]
        [Builtin("BIND", Data = StdAtom.BIND, Summary = "Evaluates a sequence of expressions without creating a new block.")]
        public static void ProgVoidOp(VoidCall c, [Data] StdAtom mode, params ZilObject[] args)
        {
            bool repeat = mode == StdAtom.REPEAT;
            bool catchy = mode != StdAtom.BIND;
            var (_, progBody) = c.form;
            c.cc.CompilePROG(c.rb, progBody, c.form.SourceLine, false, null, mode.ToString(), repeat, catchy);
        }

        [Builtin("NOT", Data = false, Summary = "Returns true if the condition is false, and false otherwise.")]
        [Builtin("F?", Data = false, Summary = "Returns true if the condition is false, and false otherwise.")]
        [Builtin("T?", Data = true, Summary = "Returns true if the condition is true, and false otherwise.")]
        public static IOperand TrueFalseValueOp(ValueCall c, [Data] bool polarity, ZilObject condition)
        {
            var label1 = c.rb.DefineLabel();
            var label2 = c.rb.DefineLabel();
            c.cc.CompileCondition(c.rb, condition, c.form.SourceLine, label1, !polarity);
            c.rb.EmitStore(c.resultStorage, c.cc.Game.One);
            c.rb.Branch(label2);
            c.rb.MarkLabel(label1);
            c.rb.EmitStore(c.resultStorage, c.cc.Game.Zero);
            c.rb.MarkLabel(label2);
            return c.resultStorage;
        }

        [Builtin("NOT", Data = false, Summary = "Returns true if the condition is false, and false otherwise.")]
        [Builtin("F?", Data = false, Summary = "Returns true if the condition is false, and false otherwise.")]
        [Builtin("T?", Data = true, Summary = "Returns true if the condition is true, and false otherwise.")]
        public static void TrueFalsePredOp(PredCall c, [Data] bool polarity, ZilObject condition)
        {
            c.cc.CompileCondition(c.rb, condition, c.form.SourceLine, c.label, polarity == c.polarity);
        }

        [Builtin("OR", Data = false, Summary = "Evaluates a sequence of expressions until one is true, returning the true value, or returning zero if all are false.")]
        [Builtin("AND", Data = true, Summary = "Evaluates a sequence of expressions until one is false, returning zero, or returning the last true value if none are false.")]
        public static IOperand AndOrValueOp(ValueCall c, [Data] bool and, params ZilObject[] args)
        {
            Debug.Assert(c.form.Rest != null);
            return c.cc.CompileBoolean(c.rb, c.form.Rest, c.form.SourceLine, and, true, c.resultStorage)!;
        }

        [Builtin("OR", Data = false, Summary = "Evaluates a sequence of expressions until one is true.")]
        [Builtin("AND", Data = true, Summary = "Evaluates a sequence of expressions until one is false.")]
        public static void AndOrVoidOp(VoidCall c, [Data] bool and, params ZilObject[] args)
        {
            Debug.Assert(c.form.Rest != null);
            c.cc.CompileBoolean(c.rb, c.form.Rest, c.form.SourceLine, and, false, null);
        }

        [Builtin("OR", Data = false, Summary = "Evaluates a sequence of expressions until one is true, returning true, or returning false if all are false.")]
        [Builtin("AND", Data = true, Summary = "Evaluates a sequence of expressions until one is false, returning false, or returning true if all are true.")]
        public static void AndOrPredOp(PredCall c, [Data] bool and, params ZilObject[] args)
        {
            c.cc.CompileBoolean(c.rb, args, c.form.SourceLine, and, c.label, c.polarity);
        }

        [Builtin("DO", Summary = "Evaluates a sequence of expressions in a loop, using a counter variable.")]
        public static IOperand DoLoopValueOp(ValueCall c, params ZilObject[] body)
        {
            Debug.Assert(c.form.Rest != null);
            return c.cc.CompileDO(c.rb, c.form.Rest, c.form.SourceLine, true, c.resultStorage);
        }

        [Builtin("DO", Summary = "Evaluates a sequence of expressions in a loop, using a counter variable.")]
        public static void DoLoopVoidOp(VoidCall c, params ZilObject[] body)
        {
            Debug.Assert(c.form.Rest != null);
            c.cc.CompileDO(c.rb, c.form.Rest, c.form.SourceLine, false, null);
        }

        [Builtin("MAP-CONTENTS", Summary = "Evaluates a sequence of expressions repeatedly for each object in a given location, returning the value of the last expression.")]
        public static IOperand MapContentsValueOp(ValueCall c, params ZilObject[] body)
        {
            Debug.Assert(c.form.Rest != null);
            return c.cc.CompileMAP_CONTENTS(c.rb, c.form.Rest, c.form.SourceLine, true, c.resultStorage);
        }

        [Builtin("MAP-CONTENTS", Summary = "Evaluates a sequence of expressions repeatedly for each object in a given location.")]
        public static void MapContentsVoidOp(VoidCall c, params ZilObject[] body)
        {
            Debug.Assert(c.form.Rest != null);
            c.cc.CompileMAP_CONTENTS(c.rb, c.form.Rest, c.form.SourceLine, false, null);
        }

        [Builtin("MAP-DIRECTIONS", Summary = "Evaluates a sequence of expressions repeatedly for each direction exiting a given location, returning the value of the last expression.")]
        public static IOperand MapDirectionsValueOp(ValueCall c, params ZilObject[] body)
        {
            Debug.Assert(c.form.Rest != null);
            return c.cc.CompileMAP_DIRECTIONS(c.rb, c.form.Rest, c.form.SourceLine, true, c.resultStorage);
        }

        [Builtin("MAP-DIRECTIONS", Summary = "Evaluates a sequence of expressions repeatedly for each direction exiting a given location.")]
        public static void MapDirectionsVoidOp(VoidCall c, params ZilObject[] body)
        {
            Debug.Assert(c.form.Rest != null);
            c.cc.CompileMAP_DIRECTIONS(c.rb, c.form.Rest, c.form.SourceLine, false, null);
        }

        [Builtin("COND", Summary = "Evaluates a series of conditional clauses, returning the value of the last expression in the first true clause.")]
        public static IOperand CondValueOp(ValueCall c, params ZilObject[] clauses)
        {
            Debug.Assert(c.form.Rest != null);
            return c.cc.CompileCOND(c.rb, c.form.Rest, c.form.SourceLine, true, c.resultStorage)!;
        }

        [Builtin("COND", Summary = "Evaluates a series of conditional clauses.")]
        public static void CondVoidOp(VoidCall c, params ZilObject[] clauses)
        {
            Debug.Assert(c.form.Rest != null);
            c.cc.CompileCOND(c.rb, c.form.Rest, c.form.SourceLine, false, null);
        }

        [Builtin("VERSION?", Summary = "Evaluates the conditional clause corresponding to the current Z-machine version, returning the value of the last expression in the clause.")]
        public static IOperand IfVersionValueOp(ValueCall c, params ZilObject[] clauses)
        {
            Debug.Assert(c.form.Rest != null);
            return c.cc.CompileVERSION_P(c.rb, c.form.Rest, c.form.SourceLine, true, c.resultStorage)!;
        }

        [Builtin("VERSION?", Summary = "Evaluates the conditional clause corresponding to the current Z-machine version.")]
        public static void IfVersionVoidOp(VoidCall c, params ZilObject[] clauses)
        {
            Debug.Assert(c.form.Rest != null);
            c.cc.CompileVERSION_P(c.rb, c.form.Rest, c.form.SourceLine, false, null);
        }

        [Builtin("IFFLAG", Summary = "Evaluates a series of conditional clauses based on the values of compilation flags, returning the value of the last expression in the first applicable clause.")]
        public static IOperand IfFlagValueOp(ValueCall c, params ZilObject[] clauses)
        {
            Debug.Assert(c.form.Rest != null);
            return c.cc.CompileIFFLAG(c.rb, c.form.Rest, c.form.SourceLine, true, c.resultStorage)!;
        }

        [Builtin("IFFLAG", Summary = "Evaluates a series of conditional clauses based on the values of compilation flags.")]
        public static void IfFlagVoidOp(VoidCall c, params ZilObject[] clauses)
        {
            Debug.Assert(c.form.Rest != null);
            c.cc.CompileIFFLAG(c.rb, c.form.Rest, c.form.SourceLine, false, null);
        }

        #endregion

        [Builtin("CHTYPE", Summary = "Does nothing.")]
        public static IOperand ChtypeValueOp(ValueCall c, IOperand value, ZilAtom type)
        {
            // TODO: check type?
            return value;
        }

        [Builtin("TELL", Summary = "Outputs text to the player, or calls printing routines, based on a sequence of tokens.")]
        public static void TellVoidOp(VoidCall c, params ZilObject[] args)
        {
            c.cc.CompileTell(c.rb, c.form.SourceLine, args);
        }
    }
}
