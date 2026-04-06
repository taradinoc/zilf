/* Copyright 2010-2025 Tara McGrew
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
using System.Linq;
using System.Text;

namespace Zilf.Emit.Glulx
{
    class RoutineBuilder : ConstantOperandBase, IRoutineBuilder, INonzeroConstantOperand,
        IProvideLowCoreEmulation, IProvideNoValuePredEmit, IProvideGlkFromStackEmit
    {
        const string INDENT = "\t";

        protected readonly GameBuilder gameBuilder;
        readonly string name;
        readonly bool entryPoint;

        readonly List<LocalBuilder> requiredParams = [];
        readonly List<LocalBuilder> optionalParams = [];
        readonly List<LocalBuilder> locals = [];
        readonly PeepholeBuffer<GlulxCode> peep;
        int nextLabelNum;
        bool varargsRequired;

        // Routines to trace for debugging
        static readonly HashSet<string> TracedRoutines = [
            // "V-LOOK",
            // "DESCRIBE-ROOM",
            // "WORD?",
            // "PARSER",
            // "SCOPE-CRAWL",
        ];

        public RoutineBuilder(GameBuilder gameBuilder, string name, bool entryPoint)
        {
            this.gameBuilder = gameBuilder;
            this.name = name;
            this.entryPoint = entryPoint;

            var shouldTrace = TracedRoutines.Contains(name.Replace("__", "-").Replace("_Q_", "?"));
            peep = new PeepholeBuffer<GlulxCode>
            {
                Combiner = new PeepholeCombiner(LocalExists),
                LabelFactory = DefineLabel,
                TracingEnabled = shouldTrace,
                TracingName = shouldTrace ? name : null
            };
            RoutineStart = DefineLabel();
        }

        static readonly StackOperand STACK = new();

        public string Name => name;

        public override string ToString() => name;

        public bool CleanStack => true;

        public ILabel RTrue => Label.RTRUE;

        public ILabel RFalse => Label.RFALSE;

        public IVariable Stack => STACK;

        public bool UsesStackBasedCalls => false;

        public ILabel RoutineStart { get; }

        public bool HasArgCount => true;

        public bool HasBranchSave => false;

        public bool HasStoreSave => false;

        public bool HasExtendedSave => true;

        public bool HasUndo => true;

        bool LocalExists(string localName)
        {
            return requiredParams.Concat(optionalParams).Concat(locals).Any(lb => lb.Name == localName);
        }

        LocalBuilder UseTempVariable(int num)
        {
            var name = $"__temp{num}";
            var local = locals.FirstOrDefault(l => l.Name == name);
            if (local != null)
                return local;

            int index = requiredParams.Count + optionalParams.Count + locals.Count;
            local = new LocalBuilder(name, index);
            locals.Add(local);
            return local;
        }

        public ILocalBuilder DefineRequiredParameter(string paramName)
        {
            paramName = GameBuilder.SanitizeSymbol(paramName);

            if (entryPoint)
                throw new InvalidOperationException("Entry point may not have required parameters");
            if (LocalExists(paramName))
                throw new ArgumentException("Local variable already exists: " + paramName, nameof(paramName));

            int index = requiredParams.Count + optionalParams.Count + locals.Count;
            var local = new LocalBuilder(paramName, index);
            requiredParams.Add(local);
            return local;
        }

        public ILocalBuilder DefineOptionalParameter(string paramName)
        {
            paramName = GameBuilder.SanitizeSymbol(paramName);

            if (LocalExists(paramName))
                throw new ArgumentException("Local variable already exists: " + paramName, nameof(paramName));

            int index = requiredParams.Count + optionalParams.Count + locals.Count;
            var local = new LocalBuilder(paramName, index);
            optionalParams.Add(local);
            return local;
        }

        public ILocalBuilder DefineLocal(string localName)
        {
            localName = GameBuilder.SanitizeSymbol(localName);

            if (LocalExists(localName))
                throw new ArgumentException("Local variable already exists: " + localName, nameof(localName));

            int index = requiredParams.Count + optionalParams.Count + locals.Count;
            var local = new LocalBuilder(localName, index);
            locals.Add(local);
            return local;
        }

        public ILabel DefineLabel()
        {
            return new Label($"L{nextLabelNum++}");
        }

        public void MarkLabel(ILabel label)
        {
            peep.MarkLabel(label);
        }

        protected void AddLine(string instruction, string? opcode, ILabel? target, PeepholeLineType type)
        {
            GlulxCode gc;
            gc.Text = instruction;
            gc.Opcode = opcode;
            gc.OriginalType = type;

            peep.AddLine(gc, target, type);
        }

        protected void Emit(string instruction, string? opcode = null)
        {
            AddLine(instruction, opcode, null, PeepholeLineType.Plain);
        }

        /// <summary>
        /// Maps branch opcodes to their inverted forms.
        /// In Glulx, branch polarity is encoded in the opcode itself.
        /// </summary>
        static readonly Dictionary<string, string> InvertedBranchOpcodes = new()
        {
            ["jz"] = "jnz",
            ["jnz"] = "jz",
            ["jeq"] = "jne",
            ["jne"] = "jeq",
            ["jlt"] = "jge",
            ["jge"] = "jlt",
            ["jgt"] = "jle",
            ["jle"] = "jgt",
            ["jltu"] = "jgeu",
            ["jgeu"] = "jltu",
            ["jgtu"] = "jleu",
            ["jleu"] = "jgtu",
        };

        /// <summary>
        /// Gets the instruction text with the opcode inverted if needed.
        /// The peephole optimizer may invert branch polarity (changing BranchPositive to BranchNegative),
        /// but in Glulx the polarity is encoded in the opcode itself, so we need to change the opcode.
        /// We compare the original type (stored when the instruction was created) with the current type
        /// to detect if the peephole optimizer has inverted the branch.
        /// </summary>
        static string GetPossiblyInvertedText(GlulxCode code, PeepholeLineType currentType)
        {
            // If the current type matches the original type, no inversion needed
            if (currentType == code.OriginalType)
            {
                return code.Text;
            }

            // Type was inverted by peephole optimizer - we need to invert the opcode
            if (code.Opcode == null || !InvertedBranchOpcodes.TryGetValue(code.Opcode, out var invertedOpcode))
            {
                return code.Text;
            }

            // Replace the opcode at the start of the instruction text
            if (code.Text.StartsWith(code.Opcode))
            {
                return invertedOpcode + code.Text.Substring(code.Opcode.Length);
            }

            // Fallback: just return original text (shouldn't happen)
            return code.Text;
        }

        protected static string FormatLoad(IOperand operand)
        {
            return operand is StackOperand ? "pop" : operand.ToString()!;
        }

        protected static string FormatStore(IOperand? operand)
        {
            return operand == null ? "drop" : operand is StackOperand ? "push" : operand.ToString()!;
        }


        public void Branch(ILabel label)
        {
            if (label == Label.RTRUE)
            {
                AddLine("return 1", "return", Label.RTRUE, PeepholeLineType.BranchAlways);
            }
            else if (label == Label.RFALSE)
            {
                AddLine("return 0", "return", Label.RFALSE, PeepholeLineType.BranchAlways);
            }
            else
            {
                // Don't embed the label in the text - let Finish() append it from the target
                AddLine("jump", "jump", label, PeepholeLineType.BranchAlways);
            }
        }

        public virtual void Branch(Condition cond, IOperand? left, IOperand? right, ILabel label, bool polarity)
        {
            string cmp;
            // For Glulx, always use BranchPositive because the opcode itself encodes the branch direction.
            // The peephole optimizer uses BranchPositive/BranchNegative to track polarity for optimizations
            // like "conditional branch over unconditional". When the optimizer inverts the type, we detect
            // the mismatch between OriginalType and current type and invert the opcode accordingly.
            const PeepholeLineType branchType = PeepholeLineType.BranchPositive;

            switch (cond)
            {
                case Condition.IncCheck:
                    Emit($"add {FormatLoad(left!)} 1 -> {FormatStore(left!)}", "add");
                    cmp = polarity ? "jgt" : "jle";
                    AddLine($"{cmp} {FormatLoad(left!)} {FormatLoad(right!)}", cmp, label, branchType);
                    return;

                case Condition.DecCheck:
                    Emit($"sub {FormatLoad(left!)} 1 -> {FormatStore(left!)}", "sub");
                    cmp = polarity ? "jlt" : "jge";
                    AddLine($"{cmp} {FormatLoad(left!)} {FormatLoad(right!)}", cmp, label, branchType);
                    return;

                case Condition.TestBits when right is StackOperand:
                    Emit($"copy {FormatLoad(right!)} -> {UseTempVariable(1)}", "copy");
                    Emit($"bitand {FormatLoad(left!)} __temp1 -> push", "bitand");
                    cmp = polarity ? "jeq" : "jne";
                    AddLine($"{cmp} pop __temp1", cmp, label, branchType);
                    return;

                case Condition.TestBits:
                    Emit($"bitand {FormatLoad(left!)} {FormatLoad(right!)} -> push", "bitand");
                    cmp = polarity ? "jeq" : "jne";
                    AddLine($"{cmp} pop {FormatLoad(right!)}", cmp, label, branchType);
                    return;

                case Condition.TestAttr:
                    Emit($"callfii {gameBuilder.RuntimeLib.Use(nameof(RuntimeLib.test_flag))} {FormatLoad(left!)} {FormatLoad(right!)} -> push", "callfii");
                    cmp = polarity ? "jnz" : "jz";
                    AddLine($"{cmp} pop", cmp, label, branchType);
                    return;

                case Condition.Inside:
                    Emit($"callfii {gameBuilder.RuntimeLib.Use(nameof(RuntimeLib.test_parent))} {FormatLoad(left!)} {FormatLoad(right!)} -> push", "callfii");
                    cmp = polarity ? "jnz" : "jz";
                    AddLine($"{cmp} pop", cmp, label, branchType);
                    return;

                case Condition.Verify:
                    Emit($"verify -> push");
                    cmp = polarity ? "jz" : "jnz";      // verify returns 0 for success
                    AddLine($"{cmp} pop", cmp, label, branchType);
                    return;

                case Condition.ArgProvided:
                    varargsRequired = true;
                    cmp = polarity ? "jge" : "jlt";
                    AddLine($"{cmp} _va_count {FormatLoad(left!)}", cmp, label, branchType);
                    return;
            }

            string opcode = cond switch
            {
                Condition.Less => polarity ? "jlt" : "jge",
                Condition.Greater => polarity ? "jgt" : "jle",
                _ => throw new NotImplementedException($"Condition {cond} not implemented")
            };

            AddLine($"{opcode} {FormatLoad(left!)} {FormatLoad(right!)}", opcode, label, branchType);
        }

        public void BranchIfEqual(IOperand value, IOperand option1, ILabel label, bool polarity)
        {
            string opcode = polarity ? "jeq" : "jne";
            AddLine($"{opcode} {FormatLoad(value)} {FormatLoad(option1)}", opcode, label, PeepholeLineType.BranchPositive);
        }

        public void BranchIfEqual(IOperand value, IOperand option1, IOperand option2, ILabel label, bool polarity)
        {
            string valueStr;
            if (value is StackOperand)
            {
                Emit($"copy {FormatLoad(value)} -> {UseTempVariable(1)}", "copy");
                valueStr = "__temp1";
            }
            else
            {
                valueStr = FormatLoad(value);
            }

            if (polarity)
            {
                AddLine($"jeq {valueStr} {FormatLoad(option1)}", "jeq", label, PeepholeLineType.BranchPositive);
                AddLine($"jeq {valueStr} {FormatLoad(option2)}", "jeq", label, PeepholeLineType.BranchPositive);
            }
            else
            {
                var tempLabel = DefineLabel();
                AddLine($"jeq {valueStr} {FormatLoad(option1)}", "jeq", tempLabel, PeepholeLineType.BranchPositive);
                AddLine($"jne {valueStr} {FormatLoad(option2)}", "jne", label, PeepholeLineType.BranchPositive);
                MarkLabel(tempLabel);
            }
        }

        public void BranchIfEqual(IOperand value, IOperand option1, IOperand option2, IOperand option3, ILabel label, bool polarity)
        {
            string valueStr;
            if (value is StackOperand)
            {
                Emit($"copy {FormatLoad(value)} -> {UseTempVariable(1)}", "copy");
                valueStr = "__temp1";
            }
            else
            {
                valueStr = FormatLoad(value);
            }

            if (polarity)
            {
                AddLine($"jeq {valueStr} {FormatLoad(option1)}", "jeq", label, PeepholeLineType.BranchPositive);
                AddLine($"jeq {valueStr} {FormatLoad(option2)}", "jeq", label, PeepholeLineType.BranchPositive);
                AddLine($"jeq {valueStr} {FormatLoad(option3)}", "jeq", label, PeepholeLineType.BranchPositive);
            }
            else
            {
                var tempLabel = DefineLabel();
                AddLine($"jeq {valueStr} {FormatLoad(option1)}", "jeq", tempLabel, PeepholeLineType.BranchPositive);
                AddLine($"jeq {valueStr} {FormatLoad(option2)}", "jeq", tempLabel, PeepholeLineType.BranchPositive);
                AddLine($"jne {valueStr} {FormatLoad(option3)}", "jne", label, PeepholeLineType.BranchPositive);
                MarkLabel(tempLabel);
            }
        }

        public void BranchIfZero(IOperand operand, ILabel label, bool polarity)
        {
            string opcode = polarity ? "jz" : "jnz";
            // Always BranchPositive - opcode encodes the direction
            AddLine($"{opcode} {FormatLoad(operand)}", opcode, label, PeepholeLineType.BranchPositive);
        }

        public void Return(IOperand result)
        {
            if (result == GameBuilder.ONE)
                AddLine("return 1", "return", Label.RTRUE, PeepholeLineType.BranchAlways);
            else if (result == GameBuilder.ZERO)
                AddLine("return 0", "return", Label.RFALSE, PeepholeLineType.BranchAlways);
            else
                AddLine($"return {FormatLoad(result)}", "return", null, PeepholeLineType.Terminator);
        }

        public void EmitStore(IVariable dest, IOperand src)
        {
            if (dest == STACK)
                Emit($"push {FormatLoad(src)}", "push");
            else
                Emit($"copy {FormatLoad(src)} -> {FormatStore(dest)}", "copy");
        }

        public virtual void EmitBinary(BinaryOp op, IOperand left, IOperand right, IVariable? result)
        {
            switch (op)
            {
                case BinaryOp.MoveObject:
                    Emit($"callfii {gameBuilder.RuntimeLib.Use(nameof(RuntimeLib.move_object))} {FormatLoad(left)} {FormatLoad(right)}", "callfii");
                    return;

                case BinaryOp.GetProperty:
                    Emit($"callfii {gameBuilder.RuntimeLib.Use(nameof(RuntimeLib.get_property))} {FormatLoad(left)} {FormatLoad(right)} -> {FormatStore(result!)}", "callfii");
                    return;

                case BinaryOp.GetPropAddress:
                    Emit($"callfii {gameBuilder.RuntimeLib.Use(nameof(RuntimeLib.get_property_address))} {FormatLoad(left)} {FormatLoad(right)} -> {FormatStore(result!)}", "callfii");
                    return;

                case BinaryOp.GetNextProp:
                    Emit($"callfii {gameBuilder.RuntimeLib.Use(nameof(RuntimeLib.get_next_property))} {FormatLoad(left)} {FormatLoad(right)} -> {FormatStore(result!)}", "callfii");
                    return;

                case BinaryOp.DirectOutput:
                    Emit($"callfii {gameBuilder.RuntimeLib.Use(nameof(RuntimeLib.direct_output))} {FormatLoad(left)} {FormatLoad(right)}", "callfii");
                    return;

                case BinaryOp.SetFlag:
                    Emit($"callfiii {gameBuilder.RuntimeLib.Use(nameof(RuntimeLib.set_flag))} {FormatLoad(left)} {FormatLoad(right)} 1", "callfiii");
                    return;

                case BinaryOp.ClearFlag:
                    Emit($"callfiii {gameBuilder.RuntimeLib.Use(nameof(RuntimeLib.set_flag))} {FormatLoad(left)} {FormatLoad(right)} 0", "callfiii");
                    return;

                case BinaryOp.SetCursor:
                    Emit($"callfii {gameBuilder.RuntimeLib.Use(nameof(RuntimeLib.move_cursor))} {FormatLoad(left)} {FormatLoad(right)}", "callfii");
                    return;

                case BinaryOp.ArtShift:
                    Emit($"callfii {gameBuilder.RuntimeLib.Use(nameof(RuntimeLib.art_shift))} {FormatLoad(left)} {FormatLoad(right)}", "callfii");
                    return;

                case BinaryOp.LogShift:
                    Emit($"callfii {gameBuilder.RuntimeLib.Use(nameof(RuntimeLib.log_shift))} {FormatLoad(left)} {FormatLoad(right)}", "callfii");
                    return;

                case BinaryOp.SetColor:
                case BinaryOp.SetTrueColor:
                    throw new NotSupportedException("Colors not supported for Glulx");
            }

            string opcode = op switch
            {
                BinaryOp.Add => "add",
                BinaryOp.Sub => "sub",
                BinaryOp.Mul => "mul",
                BinaryOp.Div => "div",
                BinaryOp.Mod => "mod",
                BinaryOp.And => "bitand",
                BinaryOp.Or => "bitor",
                BinaryOp.GetByte => "aloadb",
                BinaryOp.GetWord => "aload",
                BinaryOp.Throw => "throw",
                _ => throw new NotImplementedException($"Binary op {op} not implemented")
            };

            if (result == null)
            {
                Emit($"{opcode} {FormatLoad(left)} {FormatLoad(right)}", opcode);
            }
            else
            {
                Emit($"{opcode} {FormatLoad(left)} {FormatLoad(right)} -> {FormatStore(result)}", opcode);
            }
        }

        public virtual void EmitUnary(UnaryOp op, IOperand value, IVariable? result)
        {
            switch (op)
            {
                case UnaryOp.GetPropSize:
                    Emit($"callfi {gameBuilder.RuntimeLib.Use(nameof(RuntimeLib.get_property_size))} {FormatLoad(value)} -> {FormatStore(result)}", "callfi");
                    return;

                case UnaryOp.DirectOutput:
                    Emit($"callfi {gameBuilder.RuntimeLib.Use(nameof(RuntimeLib.direct_output))} {FormatLoad(value)}", "callfi");
                    return;

                case UnaryOp.DirectInput:
                    Emit($"callfi {gameBuilder.RuntimeLib.Use(nameof(RuntimeLib.direct_input))} {FormatLoad(value)}", "callfi");
                    return;

                case UnaryOp.GetParent:
                    Emit($"callfi {gameBuilder.RuntimeLib.Use(nameof(RuntimeLib.get_parent))} {FormatLoad(value)} -> {FormatStore(result)}", "callfi");
                    return;

                case UnaryOp.RemoveObject:
                    Emit($"callfi {gameBuilder.RuntimeLib.Use(nameof(RuntimeLib.remove_object))} {FormatLoad(value)}", "callfi");
                    return;

                case UnaryOp.LoadIndirect:
                    // this only works for global variables, where we have an address
                    if (value is IConstantOperand)
                        Emit($"copy [{FormatLoad(value)}] -> {FormatStore(result)}", "copy");
                    else
                        Emit($"aload {FormatLoad(value)} 0 -> {FormatStore(result)}", "aload");
                    return;

                case UnaryOp.OutputStyle:
                    Emit($"callfi {gameBuilder.RuntimeLib.Use(nameof(RuntimeLib.output_style))} {FormatLoad(value)}", "callfi");
                    return;

                case UnaryOp.SplitWindow:
                    Emit($"callfi {gameBuilder.RuntimeLib.Use(nameof(RuntimeLib.split_window))} {FormatLoad(value)}", "callfi");
                    return;

                case UnaryOp.SelectWindow:
                    Emit($"callfi {gameBuilder.RuntimeLib.Use(nameof(RuntimeLib.select_window))} {FormatLoad(value)}", "callfi");
                    return;

                case UnaryOp.ClearWindow:
                    Emit($"callfi {gameBuilder.RuntimeLib.Use(nameof(RuntimeLib.clear_window))} {FormatLoad(value)}", "callfi");
                    return;

                case UnaryOp.GetCursor:
                    Emit($"callfi {gameBuilder.RuntimeLib.Use(nameof(RuntimeLib.get_cursor))} {FormatLoad(value)}", "callfi");
                    return;

                case UnaryOp.EraseLine:
                    Emit($"callfi {gameBuilder.RuntimeLib.Use(nameof(RuntimeLib.erase_line))} {FormatLoad(value)}", "callfi");
                    return;

                case UnaryOp.Random:
                    Emit($"callfi {gameBuilder.RuntimeLib.Use(nameof(RuntimeLib.random_number))} {FormatLoad(value)} -> {FormatStore(result)}", "callfi");
                    return;
            }

            string opcode = op switch
            {
                UnaryOp.Neg => "neg",
                UnaryOp.Not => "bitnot",
                _ => throw new NotImplementedException($"Unary op {op} not implemented")
            };

            if (result == null)
            {
                Emit($"{opcode} {FormatLoad(value)}", opcode);
            }
            else
            {
                Emit($"{opcode} {FormatLoad(value)} -> {FormatStore(result)}", opcode);
            }
        }

        public virtual void EmitTernary(TernaryOp op, IOperand left, IOperand center, IOperand right, IVariable? result)
        {
            switch (op)
            {
                case TernaryOp.PutProperty:
                    Emit($"callfiii {gameBuilder.RuntimeLib.Use(nameof(RuntimeLib.put_property))} {FormatLoad(left)} {FormatLoad(center)} {FormatLoad(right)}", "callfiii");
                    return;

                case TernaryOp.CopyTable:
                    Emit($"callfiii {gameBuilder.RuntimeLib.Use(nameof(RuntimeLib.copy_table))} {FormatLoad(left)} {FormatLoad(center)} {FormatLoad(right)}", "callfiii");
                    return;

                case TernaryOp.PutByte:
                    if (gameBuilder.HasTracedTables)
                    {
                        // Use trace function instead of direct astoreb
                        Emit($"callfiii {gameBuilder.RuntimeLib.Use(nameof(RuntimeLib.trace_byte_write))} {FormatLoad(left)} {FormatLoad(center)} {FormatLoad(right)}", "callfiii");
                        return;
                    }
                    break;

                case TernaryOp.PutWord:
                    if (gameBuilder.HasTracedTables)
                    {
                        // Use trace function instead of direct astore
                        Emit($"callfiii {gameBuilder.RuntimeLib.Use(nameof(RuntimeLib.trace_word_write))} {FormatLoad(left)} {FormatLoad(center)} {FormatLoad(right)}", "callfiii");
                        return;
                    }
                    break;
            }

            string opcode = op switch
            {
                TernaryOp.PutByte => "astoreb",
                TernaryOp.PutWord => "astore",
                _ => throw new NotImplementedException($"Ternary op {op} not implemented")
            };

            if (result == null)
            {
                Emit($"{opcode} {FormatLoad(left)} {FormatLoad(center)} {FormatLoad(right)}", opcode);
            }
            else
            {
                Emit($"{opcode} {FormatLoad(left)} {FormatLoad(center)} {FormatLoad(right)} -> {FormatStore(result)}", opcode);
            }
        }

        public virtual void EmitNullary(NullaryOp op, IVariable? result)
        {
            switch (op)
            {
                case NullaryOp.ShowStatus:
                    throw new NotSupportedException("Glulx status line is drawn in software");

                case NullaryOp.SaveUndo:
                    Emit("saveundo -> push", "saveundo");
                    Emit($"callfi {gameBuilder.RuntimeLib.Use(nameof(RuntimeLib.translate_save_result))} pop -> {FormatStore(result)}", "callfi");
                    return;

                case NullaryOp.RestoreUndo:
                    Emit("restoreundo -> push", "restoreundo");
                    Emit($"callfi {gameBuilder.RuntimeLib.Use(nameof(RuntimeLib.translate_save_result))} pop -> {FormatStore(result)}", "callfi");
                    return;

                case NullaryOp.Catch:
                    var catchLabel = DefineLabel();
                    AddLine($"catch -> {UseTempVariable(1)}", "catch", catchLabel, PeepholeLineType.BranchNeutral);
                    // if we get here, __temp1 contains the thrown value
                    Emit("return __temp1", "return");
                    MarkLabel(catchLabel);
                    // if we get here, __temp1 contains the catch token
                    Emit($"copy __temp1 -> {FormatStore(result)}", "copy");
                    return;
            }

            string opcode = op switch
            {
                NullaryOp.SaveUndo => "saveundo",
                NullaryOp.RestoreUndo => "restoreundo",
                _ => throw new NotImplementedException($"Nullary op {op} not implemented")
            };

            if (result == null)
            {
                Emit($"{opcode}", opcode);
            }
            else
            {
                Emit($"{opcode} -> {FormatStore(result)}", opcode);
            }
        }

        protected virtual string FormatDirectCall(IOperand routine) => FormatLoad(routine);

        public void EmitCall(IOperand routine, IOperand[] args, IVariable? result)
        {
            string dest = result == null ? "drop" : FormatStore(result);

            if (routine is not IConstantOperand)
            {
                // for Z-machine compatibility, we need to check for zero
                switch (args.Length)
                {
                    case 0:
                        Emit($"callfi {gameBuilder.RuntimeLib.Use(nameof(RuntimeLib.check_call))} {FormatLoad(routine)} -> {dest}", "callfi");
                        return;
                    case 1:
                        Emit($"callfii {gameBuilder.RuntimeLib.Use(nameof(RuntimeLib.check_call))} {FormatLoad(routine)} {FormatLoad(args[0])} -> {dest}", "callfii");
                        return;
                    case 2:
                        Emit($"callfiii {gameBuilder.RuntimeLib.Use(nameof(RuntimeLib.check_call))} {FormatLoad(routine)} {FormatLoad(args[0])} {FormatLoad(args[1])} -> {dest}", "callfiii");
                        return;
                }
            }
            else
            {
                switch (args.Length)
                {
                    case 0:
                        Emit($"callf {FormatDirectCall(routine)} -> {dest}", "callf");
                        return;
                    case 1:
                        Emit($"callfi {FormatDirectCall(routine)} {FormatLoad(args[0])} -> {dest}", "callfi");
                        return;
                    case 2:
                        Emit($"callfii {FormatDirectCall(routine)} {FormatLoad(args[0])} {FormatLoad(args[1])} -> {dest}", "callfii");
                        return;
                    case 3:
                        Emit($"callfiii {FormatDirectCall(routine)} {FormatLoad(args[0])} {FormatLoad(args[1])} {FormatLoad(args[2])} -> {dest}", "callfiii");
                        return;
                }
            }

            // Push arguments in reverse order
            if (args.Contains(Stack))
            {
                // We could do some fancy stack juggling here, but instead, we use temp variables for every stack argument
                int n = 1;

                for (int i = 0; i < args.Length - 1; i++)
                {
                    if (args[i] == Stack)
                    {
                        Emit($"pull {UseTempVariable(n++)}", "pull");
                    }
                }

                Emit($"push {FormatLoad(args[^1])}", "push");

                for (int i = args.Length - 2; i >= 0; i--)
                {
                    if (args[i] == Stack)
                    {
                        Emit($"push __temp{--n}", "push");
                    }
                    else
                    {
                        Emit($"push {FormatLoad(args[i])}", "push");
                    }
                }
            }
            else
            {
                // Easy mode
                for (int i = args.Length - 1; i >= 0; i--)
                    Emit($"push {FormatLoad(args[i])}", "push");
            }

            if (routine is not IConstantOperand)
            {
                Emit($"push {FormatLoad(routine)}", "push");
                Emit($"call {gameBuilder.RuntimeLib.Use(nameof(RuntimeLib.check_call))} {args.Length + 1} -> {dest}", "call");
            }
            else
            {
                Emit($"call {FormatDirectCall(routine)} {args.Length} -> {dest}", "call");
            }
        }

        public void EmitQuit()
        {
            AddLine("quit", "quit", null, PeepholeLineType.Terminator);
        }

        public void EmitRestart()
        {
            AddLine("restart", "restart", null, PeepholeLineType.Terminator);
        }

        public void EmitSave(IVariable result)
        {
            Emit($"callf {gameBuilder.RuntimeLib.Use(nameof(RuntimeLib.save_game))} -> {FormatStore(result)}", "callf");
        }

        public void EmitSave(ILabel label, bool polarity)
        {
            throw new NotSupportedException("Branch save not supported for Glulx");
        }

        public void EmitSave(IOperand table, IOperand size, IOperand name, IOperand? prompt, IVariable result)
        {
            throw new NotImplementedException("Extended save not supported for Glulx");
        }

        public void EmitRestore(IVariable result)
        {
            Emit($"callf {gameBuilder.RuntimeLib.Use(nameof(RuntimeLib.restore_game))} -> {FormatStore(result)}", "callf");
        }

        public void EmitRestore(ILabel label, bool polarity)
        {
            throw new NotSupportedException("Branch restore not supported for Glulx");
        }

        public void EmitRestore(IOperand table, IOperand size, IOperand name, IOperand? prompt, IVariable result)
        {
            throw new NotImplementedException("Extended restore not supported for Glulx");
        }

        public void EmitPopStack()
        {
            Emit("pull -> drop", "pull");
        }

        public virtual void EmitPrint(string text, bool crlfRtrue)
        {
            var strOperand = gameBuilder.MakeOperand(text);
            if (crlfRtrue)
            {
                // we can get away with calling streamstr directly here, because _rt_streamchar will move to the next line
                Emit($"streamstr {FormatLoad(strOperand)}", "streamstr");
                Emit($"callfi {gameBuilder.RuntimeLib.Use(nameof(RuntimeLib.streamchar))} 10", "callfi");
                AddLine("return 1", "return", Label.RTRUE, PeepholeLineType.BranchAlways);
            }
            else
            {
                Emit($"callfi {gameBuilder.RuntimeLib.Use(nameof(RuntimeLib.streamstr))} {FormatLoad(strOperand)}", "callfi");
            }
        }

        public virtual void EmitPrint(PrintOp op, IOperand value)
        {
            switch (op)
            {
                case PrintOp.Character:
                    // all the printing operations have to go through RTL to update the status window cursor position
                    Emit($"callfi {gameBuilder.RuntimeLib.Use(nameof(RuntimeLib.streamchar))} {FormatLoad(value)}", "callfi");
                    break;
                case PrintOp.Number:
                    Emit($"callfi {gameBuilder.RuntimeLib.Use(nameof(RuntimeLib.streamnum))} {FormatLoad(value)}", "callfi");
                    break;
                case PrintOp.PackedAddr:
                    // this really means "Glulx string"
                    Emit($"callfi {gameBuilder.RuntimeLib.Use(nameof(RuntimeLib.streamstr))} {FormatLoad(value)}", "callfi");
                    break;
                case PrintOp.Address:
                    // this really means "vocab word"
                    Emit($"callfi {gameBuilder.RuntimeLib.Use(nameof(RuntimeLib.print_vocab_word))} {FormatLoad(value)}", "callfi");
                    break;
                case PrintOp.Object:
                    Emit($"callfi {gameBuilder.RuntimeLib.Use(nameof(RuntimeLib.print_object))} {FormatLoad(value)}", "callfi");
                    break;
                default:
                    throw new NotImplementedException($"Print op {op} not implemented");
            }
        }

        public void EmitPrintNewLine()
        {
            Emit($"callfi {gameBuilder.RuntimeLib.Use(nameof(RuntimeLib.streamchar))} 10", "callfi");
        }

        public void EmitPrintTable(IOperand table, IOperand width, IOperand? height, IOperand? skip)
        {
            throw new NotImplementedException("Print table not implemented yet");
        }

        public void EmitRead(IOperand chrbuf, IOperand? lexbuf, IOperand? interval, IOperand? routine, IVariable? result)
        {
            // TODO: support timed input
            string lexbufStr = lexbuf == null ? "0" : FormatLoad(lexbuf);
            Emit($"callfii {gameBuilder.RuntimeLib.Use(nameof(RuntimeLib.read_line))} {FormatLoad(chrbuf)} {lexbufStr} -> {FormatStore(result)}", "callfii");
        }

        public void EmitReadChar(IOperand? interval, IOperand? routine, IVariable result)
        {
            // TODO: support timed input
            Emit($"callf {gameBuilder.RuntimeLib.Use(nameof(RuntimeLib.read_char))} -> {FormatStore(result)}", "callfii");
        }

        public void EmitTokenize(IOperand text, IOperand parse, IOperand? dictionary, IOperand? flag)
        {
            throw new NotImplementedException("Tokenize not implemented yet");
        }

        public void EmitEncodeText(IOperand src, IOperand length, IOperand srcOffset, IOperand dest)
        {
            throw new NotImplementedException("EncodeText not implemented yet");
        }

        public void EmitScanTable(IOperand value, IOperand table, IOperand length, IOperand? form, IVariable result, ILabel label, bool polarity)
        {
            // no ValuePred in Glulx
            throw new NotSupportedException();
        }

        public virtual void EmitScanTable(IOperand value, IOperand table, IOperand length, IOperand? form, IVariable result)
        {
            // Use Glulx's binarysearch or linearsearch opcode
            form ??= gameBuilder.MakeOperand(0x84);     // word key, 4 byte structs

            int n = 1;

            if (value == Stack)
            {
                Emit($"pull {UseTempVariable(n++)}", "pull");
            }

            if (table == Stack)
            {
                Emit($"pull {UseTempVariable(n++)}", "pull");
            }

            if (length == Stack)
            {
                Emit($"pull {UseTempVariable(n++)}", "pull");
            }

            Emit($"push {FormatLoad(form)}", "push");

            if (length == Stack)
            {
                Emit($"push __temp{--n}", "push");
            }
            else
            {
                Emit($"push {FormatLoad(length)}", "push");
            }

            if (table == Stack)
            {
                Emit($"push __temp{--n}", "push");
            }
            else
            {
                Emit($"push {FormatLoad(table)}", "push");
            }

            if (value == Stack)
            {
                Emit($"push __temp{--n}", "push");
            }
            else
            {
                Emit($"push {FormatLoad(value)}", "push");
            }

            Emit($"call {gameBuilder.RuntimeLib.Use(nameof(RuntimeLib.scan_table))} 4 -> {FormatStore(result)}", "call");
        }

        public void EmitGetChild(IOperand value, IVariable result, ILabel label, bool polarity)
        {
            // no ValuePred in Glulx
            throw new NotSupportedException();
        }

        public void EmitGetChild(IOperand value, IVariable result)
        {
            Emit($"callfi {gameBuilder.RuntimeLib.Use(nameof(RuntimeLib.get_child))} {FormatLoad(value)} -> {FormatStore(result)}", "callfi");
        }

        public void EmitGetSibling(IOperand value, IVariable result, ILabel label, bool polarity)
        {
            // no ValuePred in Glulx
            throw new NotSupportedException();
        }

        public void EmitGetSibling(IOperand value, IVariable result)
        {
            Emit($"callfi {gameBuilder.RuntimeLib.Use(nameof(RuntimeLib.get_sibling))} {FormatLoad(value)} -> {FormatStore(result)}", "callfi");
        }

        public void EmitPlaySound(IOperand number, IOperand? effect, IOperand? volume, IOperand? routine)
        {
            throw new NotImplementedException("Sound not implemented yet");
        }

        public void EmitPushUserStack(IOperand value, IOperand stack, ILabel label, bool polarity)
        {
            throw new NotImplementedException("User stack not implemented yet");
        }

        public virtual bool TryEmitLowCoreRead(string field, IVariable resultStorage)
        {
            switch (field)
            {
                case "SCRH":    // screen width
                    Emit($"callf {gameBuilder.RuntimeLib.Use(nameof(RuntimeLib.get_screen_width))} -> {FormatStore(resultStorage)}", "callf");
                    return true;
                case "SCRV":    // screen height
                    Emit($"callf {gameBuilder.RuntimeLib.Use(nameof(RuntimeLib.get_screen_height))} -> {FormatStore(resultStorage)}", "callf");
                    return true;
                case "RELEASEID":
                case "ZORKID":
                    Emit($"aloads metadata_releaseid 0 -> {FormatStore(resultStorage)}", "aloads");
                    return true;
                case "MEMSIZE":
                    Emit($"getmemsize -> {FormatStore(resultStorage)}", "getmemsize");
                    return true;
                case "FLAGS":
                    Emit($"callf {gameBuilder.RuntimeLib.Use(nameof(RuntimeLib.get_lowcore_flags))} -> {FormatStore(resultStorage)}", "callf");
                    return true;
                case "ZVERSION":
                    Emit($"copy ((ZMACHINE_VERSION << 8) | ZVERSION_FLAGS) -> {FormatStore(resultStorage)}", "copy");
                    return true;
            }

            return false;
        }

        public bool TryEmitLowCoreWrite(string field, IOperand value)
        {
            switch (field)
            {
                case "FLAGS":
                    Emit($"callfi {gameBuilder.RuntimeLib.Use(nameof(RuntimeLib.set_lowcore_flags))} {FormatLoad(value)}", "callfi");
                    return true;
            }

            return false;
        }

        public virtual bool TryEmitLowCoreGetTable(string field, IVariable resultStorage)
        {
            switch (field)
            {
                case "SERIAL":
                    Emit($"copy metadata_serial -> {FormatStore(resultStorage)}", "copy");
                    return true;
            }

            return false;
        }

        protected virtual void WriteOptionalPreamble(StringBuilder sb)
        {
            // nada
        }

        public void Finish()
        {
            var sb = new StringBuilder();

            WriteOptionalPreamble(sb);

            // Function header
            if (entryPoint)
            {
                sb.AppendLine("entry_point:");
            }
            sb.AppendLine($"{name}:");

            if (optionalParams.Any(p => p.DefaultValue != null))
                varargsRequired = true;

            if (varargsRequired)
            {
                sb.AppendLine(INDENT + "func_va");
                sb.AppendLine(INDENT + "local _va_count");
                sb.AppendLine(INDENT + "pull -> _va_count");

                int argIndex = 1;
                foreach (var arg in requiredParams)
                {
                    sb.AppendLine(INDENT + $"local {arg}");
                    sb.AppendLine(INDENT + $"pull -> {arg}");
                    argIndex++;
                }

                foreach (var arg in optionalParams)
                {
                    sb.AppendLine(INDENT + $"local {arg}");
                    if (arg.DefaultValue != null)
                        sb.AppendLine(INDENT + $"copy {FormatLoad(arg.DefaultValue)} -> {arg}");
                }
                foreach (var arg in optionalParams)
                {
                    sb.AppendLine(INDENT + $"jlt _va_count {argIndex} -> ._va_done");
                    sb.AppendLine(INDENT + $"pull -> {arg}");
                    argIndex++;
                }
                sb.AppendLine("._va_done:");

                foreach (var local in locals)
                {
                    sb.AppendLine(INDENT + $"local {local}");
                }
            }
            else
            {
                sb.AppendLine(INDENT + "function");
                foreach (var local in requiredParams.Concat(optionalParams).Concat(locals))
                {
                    sb.AppendLine(INDENT + $"local {local}");
                }
            }

            // write preamble
            var preamble = new PeepholeBuffer<GlulxCode>();
            preamble.MarkLabel(RoutineStart);

            // initialize local variables
            foreach (var local in locals)
            {
                if (local.DefaultValue != null)
                {
                    preamble.AddLine(
                        new GlulxCode { Text = $"copy {FormatLoad(local.DefaultValue)} -> {FormatStore(local)}", Opcode = "copy" },
                        null,
                        PeepholeLineType.Plain);
                }
            }

            if (entryPoint)
            {
                preamble.AddLine(
                    new GlulxCode { Text = $"callf {gameBuilder.RuntimeLib.Use(nameof(RuntimeLib.initialize_glk))}", Opcode = "callf" },
                    null,
                    PeepholeLineType.Plain);

                WriteOptionalGlkSetup(preamble);
            }

            peep.InsertBufferFirst(preamble);

            // write routine body using the peephole optimizer
            peep.Finish((label, code, target, type) =>
            {
                // Handle special cases for return to RTRUE/RFALSE
                switch (type)
                {
                    case PeepholeLineType.BranchAlways when target == Label.RTRUE:
                        sb.Append(label != null ? $"{label}:" : "");
                        sb.AppendLine(INDENT + "return 1");
                        return;
                    case PeepholeLineType.BranchAlways when target == Label.RFALSE:
                        sb.Append(label != null ? $"{label}:" : "");
                        sb.AppendLine(INDENT + "return 0");
                        return;
                }

                if (label != null)
                {
                    sb.Append(label);
                    sb.Append(':');
                }
                sb.Append(INDENT);

                // For branch instructions, check if polarity was inverted by peephole optimizer
                // and substitute the inverted opcode if needed
                var text = (type == PeepholeLineType.BranchPositive || type == PeepholeLineType.BranchNegative)
                    ? GetPossiblyInvertedText(code, type)
                    : code.Text;
                sb.Append(text);

                // Append branch target for all branch types
                switch (type)
                {
                    case PeepholeLineType.BranchAlways when code.Opcode == "jump":
                        sb.Append(' ');
                        // Use special Glulx branch targets for rtrue/rfalse
                        if (target == Label.RTRUE)
                            sb.Append("rtrue");
                        else if (target == Label.RFALSE)
                            sb.Append("rfalse");
                        else
                            sb.Append(target);
                        break;
                    case PeepholeLineType.BranchPositive:
                    case PeepholeLineType.BranchNegative:
                    case PeepholeLineType.BranchNeutral:
                        sb.Append(" -> ");
                        // Use special Glulx branch targets for rtrue/rfalse
                        if (target == Label.RTRUE)
                            sb.Append("rtrue");
                        else if (target == Label.RFALSE)
                            sb.Append("rfalse");
                        else
                            sb.Append(target);
                        break;
                }

                sb.AppendLine();
            });

            gameBuilder.WriteRoutineOutput(sb.ToString(), entryPoint);
        }

        protected virtual void WriteOptionalGlkSetup(PeepholeBuffer<GlulxCode> peep)
        {
            // nada
        }

        public virtual void EmitGlkFromStack(IOperand operation, int argCount, IVariable? resultStorage)
        {
            Emit($"glk {FormatLoad(operation)} {argCount} -> {FormatStore(resultStorage)}", "glk");
        }
    }
}
