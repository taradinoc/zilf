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
