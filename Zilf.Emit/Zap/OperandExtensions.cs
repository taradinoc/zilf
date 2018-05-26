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

using JetBrains.Annotations;
using Zapf.Parsing.Expressions;

namespace Zilf.Emit.Zap
{
    static class OperandExtensions
    {
        [NotNull]
        public static IOperand StripIndirect([NotNull] this IOperand operand)
        {
            return operand is IIndirectOperand indirect ? indirect.Variable : operand;
        }

        [NotNull]
        public static AsmExpr ToAsmExpr([NotNull] this IOperand operand)
        {
            switch (operand)
            {
                case NumericOperand num:
                    return new NumericLiteral(num.Value);

                case IndirectOperand indirect:
                    return new QuoteExpr(indirect.Variable.ToAsmExpr());

                case SumOperand sum:
                    return new AdditionExpr(sum.Left.ToAsmExpr(), sum.Right.ToAsmExpr());

                default:
                    return new SymbolExpr(operand.ToString());
            }
        }

        public static bool IsStack([NotNull] this AsmExpr asmExpr)
        {
            return asmExpr is SymbolExpr sym && sym.Text == "STACK";
        }

        [ContractAnnotation("=> false, inner: null; => true, inner: notnull")]
        public static bool IsQuote([NotNull] this AsmExpr asmExpr, out AsmExpr inner)
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
