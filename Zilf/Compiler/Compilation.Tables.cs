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

using System.Diagnostics;
using JetBrains.Annotations;
using Zilf.Diagnostics;
using Zilf.Emit;
using Zilf.Emit.Zap;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.ZModel.Values;

namespace Zilf.Compiler
{
    partial class Compilation
    {
        void BuildHeaderExtensionTable()
        {
            var size = Context.ZEnvironment.HeaderExtensionWords;
            if (size > 0)
            {
                if (Game.Options is GameOptions.V5Plus v5Options)
                {
                    var extab = Game.DefineTable("EXTAB", false);
                    extab.AddShort((short)size);
                    for (int i = 0; i < size; i++)
                        extab.AddShort(Game.Zero);

                    v5Options.HeaderExtensionTable = extab;
                }
                else
                {
                    throw new CompilerError(CompilerMessages.Header_Extensions_Not_Supported_For_This_Target);
                }
            }
        }

        struct TableElementOperand
        {
            [NotNull]
            public readonly IOperand Operand;
            public readonly bool? IsWord;

            public TableElementOperand([NotNull] IOperand operand, bool? isWord)
            {
                Operand = operand;
                IsWord = isWord;
            }
        }

        void BuildTable([NotNull] ZilTable zt, [NotNull] ITableBuilder tb)
        {

            if ((zt.Flags & TableFlags.Lexv) != 0)
            {
                IOperand[] values = new IOperand[zt.ElementCount];
                zt.CopyTo(values, (zo, isWord) => CompileConstant(zo), Game.Zero, Context);

                tb.AddByte((byte)(zt.ElementCount / 3));
                tb.AddByte(0);

                for (int i = 0; i < values.Length; i++)
                    if (i % 3 == 0)
                        tb.AddShort(values[i]);
                    else
                        tb.AddByte(values[i]);
            }
            else
            {
                var values = new TableElementOperand?[zt.ElementCount];

                TableElementOperand? ConvertElement(ZilObject zo, bool isWord)
                {
                    // it's usually a constant value
                    var constVal = CompileConstant(zo);
                    if (constVal != null)
                        return new TableElementOperand(constVal, isWord);

                    // but we'll also allow a global name if the global contains a table
                    if (zo is ZilAtom atom && Globals.TryGetValue(atom, out var global) && global.DefaultValue is ITableBuilder)
                    {
                        return new TableElementOperand(global.DefaultValue, isWord);
                    }

                    return null;
                }

                var defaultFiller = new TableElementOperand(Game.Zero, null);
                zt.CopyTo(values, ConvertElement, defaultFiller, Context);

                for (int i = 0; i < values.Length; i++)
                {
                    if (values[i] == null)
                    {
                        var rawElements = new ZilObject[zt.ElementCount];
                        zt.CopyTo(rawElements, (zo, isWord) => zo, null, Context);
                        Context.HandleError(new CompilerError(
                            zt.SourceLine,
                            CompilerMessages.Nonconstant_Initializer_For_0_1_2,
                            "table element",
                            i,
                            rawElements[i]));
                        values[i] = defaultFiller;
                    }
                }

                bool defaultWord = (zt.Flags & TableFlags.Byte) == 0;

                for (int i = 0; i < values.Length; i++)
                {
                    Debug.Assert(values[i] != null);

                    if (values[i].Value.IsWord ?? defaultWord)
                    {
                        tb.AddShort(values[i].Value.Operand);
                    }
                    else
                    {
                        tb.AddByte(values[i].Value.Operand);
                    }
                }
            }
        }
    }
}
