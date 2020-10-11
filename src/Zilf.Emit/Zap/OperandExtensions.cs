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

using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Zapf.Parsing.Expressions;

namespace Zilf.Emit.Zap
{
    static class OperandExtensions
    {
        public static IOperand StripIndirect(this IOperand operand)
        {
            return operand is IIndirectOperand indirect ? indirect.Variable : operand;
        }

        public static AsmExpr ToAsmExpr(this IOperand operand)
        {
            return operand switch
            {
                NumericOperand num => (AsmExpr)new NumericLiteral(num.Value),
                IndirectOperand indirect => new QuoteExpr(indirect.Variable.ToAsmExpr()),
                SumOperand sum => new AdditionExpr(sum.Left.ToAsmExpr(), sum.Right.ToAsmExpr()),
                _ => new SymbolExpr(operand.ToString())
            };
        }

        public static bool IsStack(this AsmExpr asmExpr) => asmExpr is SymbolExpr sym && sym.Text == "STACK";

        [ContractAnnotation("=> false, inner: null; => true, inner: notnull")]
        public static bool IsQuote(this AsmExpr asmExpr, [NotNullWhen(true)] out AsmExpr? inner)
        {
            if (asmExpr is QuoteExpr quote)
            {
                inner = quote.Inner;
                return true;
            }

            inner = null;
            return false;
        }
    }
}
