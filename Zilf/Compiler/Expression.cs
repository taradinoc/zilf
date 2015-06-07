/* Copyright 2010, 2015 Jesse McGrew
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
using System.Linq;
using Zilf.Compiler.Builtins;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;

namespace Zilf.Compiler
{
    internal static class Expression
    {
        public static bool HasSideEffects(CompileCtx cc, ZilObject expr)
        {
            ZilForm form = expr as ZilForm;

            // only forms can have side effects
            if (form == null)
                return false;

            // malformed forms are errors anyway
            ZilAtom head = form.First as ZilAtom;
            if (head == null)
                return false;

            // some instructions always have side effects
            var zversion = cc.Context.ZEnvironment.ZVersion;
            var argCount = form.Rest.Count();
            if (ZBuiltins.IsBuiltinWithSideEffects(head.Text, zversion, argCount))
                return true;

            // routines are presumed to have side effects
            if (cc.Routines.ContainsKey(head))
                return true;

            // other instructions could still have side effects if their arguments do
            foreach (ZilObject obj in form.Rest)
                if (HasSideEffects(cc, obj))
                    return true;

            return false;
        }


    }
}
