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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Zilf.Interpreter.Values;
using Zilf.Language;

namespace Zilf.Interpreter
{
    static partial class Subrs
    {
        [Subr]
        public static ZilObject TYPE(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length != 1)
                throw new InterpreterError("TYPE", 1, 1);

            return args[0].GetTypeAtom(ctx);
        }

        [Subr("TYPE?")]
        public static ZilObject TYPE_P(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length < 2)
                throw new InterpreterError("TYPE?", 2, 0);

            ZilAtom type = args[0].GetTypeAtom(ctx);
            for (int i = 1; i < args.Length; i++)
                if (args[i] == type)
                    return type;

            return ctx.FALSE;
        }

        [Subr]
        public static ZilObject CHTYPE(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length != 2)
                throw new InterpreterError("CHTYPE", 2, 2);

            ZilAtom atom = args[1] as ZilAtom;
            if (atom == null)
                throw new InterpreterError("CHTYPE: second arg must be an atom");

            return ctx.ChangeType(args[0], atom);
        }

        [Subr]
        public static ZilObject NEWTYPE(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length < 2 || args.Length > 3)
                throw new InterpreterError("NEWTYPE", 2, 3);

            var name = args[0] as ZilAtom;
            if (name == null)
                throw new InterpreterError("NEWTYPE: first arg must be an atom");

            if (ctx.IsRegisteredType(name))
                throw new InterpreterError("NEWTYPE: already registered: " + name.ToStringContext(ctx, false));

            var primtypeAtom = args[1] as ZilAtom;
            if (primtypeAtom == null)
                throw new InterpreterError("NEWTYPE: second arg must be an atom");

            PrimType primtype;
            switch (primtypeAtom.StdAtom)
            {
                case StdAtom.ATOM:
                    primtype = PrimType.ATOM;
                    break;
                case StdAtom.FIX:
                    primtype = PrimType.FIX;
                    break;
                case StdAtom.STRING:
                    primtype = PrimType.STRING;
                    break;
                case StdAtom.LIST:
                    primtype = PrimType.LIST;
                    break;
                case StdAtom.TABLE:
                    primtype = PrimType.TABLE;
                    break;
                case StdAtom.VECTOR:
                    primtype = PrimType.VECTOR;
                    break;

                default:
                    if (ctx.IsRegisteredType(primtypeAtom))
                        primtype = ctx.GetTypePrim(primtypeAtom);
                    else
                        throw new InterpreterError("NEWTYPE: unrecognized primtype: " + primtypeAtom.ToStringContext(ctx, false));
                    break;
            }

            ctx.RegisterType(name, primtype);
            return name;
        }

        [Subr("MAKE-GVAL")]
        public static ZilObject MAKE_GVAL(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length != 1)
                throw new InterpreterError("MAKE-GVAL", 1, 1);

            return new ZilForm(new ZilObject[] { ctx.GetStdAtom(StdAtom.GVAL), args[0] }) { SourceLine = SourceLines.MakeGval };
        }

        [Subr("APPLICABLE?")]
        public static ZilObject APPLICABLE_P(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length != 1)
                throw new InterpreterError("APPLICABLE?", 1, 1);

            return (args[0] is IApplicable) ? ctx.TRUE : ctx.FALSE;
        }

        [Subr("STRUCTURED?")]
        public static ZilObject STRUCTURED_P(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length != 1)
                throw new InterpreterError("STRUCTURED?", 1, 1);

            return (args[0] is IStructure) ? ctx.TRUE : ctx.FALSE;
        }

        [Subr]
        public static ZilObject FORM(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length == 0)
                throw new InterpreterError("FORM", 1, 0);

            var result = new ZilForm(args);
            if (ctx.CallingForm != null)
                result.SourceLine = ctx.CallingForm.SourceLine;
            return result;
        }

        [Subr]
        public static ZilObject LIST(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            return new ZilList(args);
        }

        [Subr]
        public static ZilObject VECTOR(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            return new ZilVector(args);
        }
        
        [Subr]
        public static ZilObject ILIST(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length < 1 || args.Length > 2)
                throw new InterpreterError("ILIST", 1, 2);

            var count = args[0] as ZilFix;
            if (count == null || count.Value < 0)
                throw new InterpreterError("ILIST: first arg must be a non-negative FIX");

            var contents = new List<ZilObject>(count.Value);
            for (int i = 0; i < count.Value; i++)
            {
                if (args.Length >= 2)
                    contents.Add(args[1].Eval(ctx));
                else
                    contents.Add(ctx.FALSE);
            }

            return new ZilList(contents);
        }

        [Subr]
        public static ZilObject IVECTOR(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length < 1 || args.Length > 2)
                throw new InterpreterError("IVECTOR", 1, 2);

            var count = args[0] as ZilFix;
            if (count == null || count.Value < 0)
                throw new InterpreterError("IVECTOR: first arg must be a non-negative FIX");

            var contents = new List<ZilObject>(count.Value);
            for (int i = 0; i < count.Value; i++)
            {
                if (args.Length >= 2)
                    contents.Add(args[1].Eval(ctx));
                else
                    contents.Add(ctx.FALSE);
            }

            return new ZilVector(contents.ToArray());
        }

        [Subr]
        public static ZilObject BYTE(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length != 1)
                throw new InterpreterError("BYTE", 1, 1);

            return ctx.ChangeType(args[0], ctx.GetStdAtom(StdAtom.BYTE));
        }

        [Subr]
        public static ZilObject CONS(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length != 2)
                throw new InterpreterError("CONS", 2, 2);

            ZilList list = args[1] as ZilList;
            if (list == null)
                throw new InterpreterError("CONS: second arg must be a list");

            if (list.GetTypeAtom(ctx).StdAtom != StdAtom.LIST)
                list = new ZilList(list);

            return new ZilList(args[0], list);
        }

        [FSubr]
        public static ZilObject FUNCTION(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length < 2)
                throw new InterpreterError("FUNCTION", 2, 0);

            ZilAtom activationAtom = args[0] as ZilAtom;
            ZilList argList;
            IEnumerable<ZilObject> body;

            if (activationAtom == null)
            {
                argList = args[0] as ZilList;
                if (argList == null || argList.GetTypeAtom(ctx).StdAtom != StdAtom.LIST)
                    throw new InterpreterError("FUNCTION: first arg must be a list");
                body = args.Skip(1);
            }
            else
            {
                argList = args[1] as ZilList;
                if (argList == null || argList.GetTypeAtom(ctx).StdAtom != StdAtom.LIST)
                    throw new InterpreterError("FUNCTION: second arg must be a list");
                body = args.Skip(2);
            }

            return new ZilFunction(null, activationAtom, argList, body);
        }

        [Subr]
        public static ZilObject STRING(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            StringBuilder sb = new StringBuilder();

            foreach (ZilObject arg in args)
            {
                switch (arg.GetTypeAtom(ctx).StdAtom)
                {
                    case StdAtom.STRING:
                    case StdAtom.CHARACTER:
                        sb.Append(arg.ToStringContext(ctx, true));
                        break;

                    default:
                        throw new InterpreterError("STRING: all args must be strings or characters");
                }
            }

            return new ZilString(sb.ToString());
        }

        [Subr]
        public static ZilObject ISTRING(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length < 1 || args.Length > 2)
                throw new InterpreterError("STRING", 1, 2);

            var count = args[0] as ZilFix;
            if (count == null || count.Value < 0)
                throw new InterpreterError("ISTRING: first arg must be a non-negative FIX");

            var contents = new List<char>(count.Value);
            for (int i = 0; i < count.Value; i++)
            {
                if (args.Length >= 2)
                {
                    var ch = args[1].Eval(ctx) as ZilChar;
                    if (ch == null)
                        throw new InterpreterError("ISTRING: iterated values must be CHARACTERs");
                    contents.Add(ch.Char);
                }
                else
                    contents.Add('\0');
            }

            return new ZilString(new string(contents.ToArray()));
        }

        [Subr]
        public static ZilObject ASCII(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length != 1)
                throw new InterpreterError("ASCII", 1, 1);

            ZilChar ch = args[0] as ZilChar;
            if (ch != null)
                return new ZilFix(ch.Char);

            ZilFix fix = args[0] as ZilFix;
            if (fix != null)
                return new ZilChar((char)fix.Value);

            throw new InterpreterError("ASCII: arg must be a character or FIX");
        }

    }
}
