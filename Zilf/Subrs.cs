/* Copyright 2010, 2012 Jesse McGrew
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

namespace Zilf
{
    static class Subrs
    {
        public delegate ZilObject SubrDelegate(Context ctx, ZilObject[] args);

        [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
        public class SubrAttribute : Attribute
        {
            private readonly string name;

            public SubrAttribute()
            {
            }

            public SubrAttribute(string name)
            {
                this.name = name;
            }

            public string Name
            {
                get { return name; }
            }
        }

        [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
        public class FSubrAttribute : SubrAttribute
        {
            public FSubrAttribute()
            {
            }

            public FSubrAttribute(string name)
                : base(name)
            {
            }
        }

        #region Meta

        [Subr("INSERT-FILE")]
        [Subr("FLOAD")]
        public static ZilObject INSERT_FILE(Context ctx, ZilObject[] args)
        {
            // we ignore arguments after the first
            if (args.Length == 0)
                throw new InterpreterError(null, "INSERT-FILE", 1, 0);

            if (args[0].GetTypeAtom(ctx).StdAtom != StdAtom.STRING)
                throw new InterpreterError("INSERT-FILE: first arg must be a string");

            string file = args[0].ToStringContext(ctx, true);

            try
            {
                string oldFile = ctx.CurrentFile;
                string newFile = ctx.FindIncludeFile(file);
                var oldFlags = ctx.CurrentFileFlags;

                ctx.CurrentFile = newFile;
                ctx.CurrentFileFlags = FileFlags.None;
                try
                {
                    using (var stream = ctx.OpenFile(newFile, false))
                    {
                        Antlr.Runtime.ICharStream inputStream =
                            new Antlr.Runtime.ANTLRInputStream(stream);
                        Program.Evaluate(ctx, inputStream);
                    }
                    return new ZilString("DONE");
                }
                finally
                {
                    ctx.CurrentFile = oldFile;
                    ctx.CurrentFileFlags = oldFlags;
                }
            }
            catch (System.IO.FileNotFoundException ex)
            {
                throw new InterpreterError("INSERT-FILE: file not found: " + file, ex);
            }
            catch (System.IO.IOException ex)
            {
                throw new InterpreterError("INSERT-FILE: error loading file: " +
                    ex.Message, ex);
            }
        }

        [Subr("FILE-FLAGS")]
        public static ZilObject FILE_FLAGS(Context ctx, ZilObject[] args)
        {
            var newFlags = FileFlags.None;

            foreach (var arg in args)
            {
                var atom = arg as ZilAtom;
                if (atom == null)
                    throw new InterpreterError("FILE-FLAGS: all args must be atoms");

                switch (atom.StdAtom)
                {
                    case StdAtom.CLEAN_STACK_P:
                        newFlags |= FileFlags.CleanStack;
                        break;

                    default:
                        throw new InterpreterError("FILE-FLAGS: unrecognized flag: " + atom);
                }
            }

            ctx.CurrentFileFlags = newFlags;
            return ctx.TRUE;
        }

        [Subr("ROUTINE-FLAGS")]
        public static ZilObject ROUTINE_FLAGS(Context ctx, ZilObject[] args)
        {
            var newFlags = RoutineFlags.None;

            foreach (var arg in args)
            {
                var atom = arg as ZilAtom;
                if (atom == null)
                    throw new InterpreterError("ROUTINE-FLAGS: all args must be atoms");

                switch (atom.StdAtom)
                {
                    case StdAtom.CLEAN_STACK_P:
                        newFlags |= RoutineFlags.CleanStack;
                        break;

                    default:
                        throw new InterpreterError("ROUTINE-FLAGS: unrecognized flag: " + atom);
                }
            }

            ctx.NextRoutineFlags = newFlags;
            return ctx.TRUE;
        }

        [Subr("TIME")]
        public static ZilObject TIME(Context ctx, ZilObject[] args)
        {
            // TODO: measure actual CPU time
            return new ZilFix(1);
        }

        [Subr("QUIT")]
        public static ZilObject QUIT(Context ctx, ZilObject[] args)
        {
            if (args.Length == 0)
                Environment.Exit(0);
            else if (args[0] is ZilFix)
                Environment.Exit(((ZilFix)args[0]).Value);
            else
                Environment.Exit(4);

            return ctx.TRUE;
        }

        [Subr("GC-MON")]
        [Subr("GC")]
        [Subr("BLOAT")]
        [Subr("ZSTR-ON")]
        [Subr("ZSTR-OFF")]
        [Subr("ENDLOAD")]
        [Subr("PUT-PURE-HERE")]
        public static ZilObject SubrIgnored(Context ctx, ZilObject[] args)
        {
            // nada
            return ctx.FALSE;
        }

        #endregion

        #region Output

        [Subr]
        public static ZilObject PRINC(Context ctx, ZilObject[] args)
        {
            if (args.Length != 1)
                throw new InterpreterError(null, "PRINC", 1, 1);

            Console.Write(args[0].ToStringContext(ctx, true));
            return args[0];
        }

        [Subr]
        public static ZilObject CRLF(Context ctx, ZilObject[] args)
        {
            Console.WriteLine();
            return ctx.TRUE;
        }

        [Subr]
        public static ZilObject IMAGE(Context ctx, ZilObject[] args)
        {
            if (args.Length != 1)
                throw new InterpreterError(null, "IMAGE", 1, 1);

            ZilFix ch = args[0] as ZilFix;
            if (ch == null)
                throw new InterpreterError("IMAGE: arg must be a FIX");

            Console.Write((char)ch.Value);
            return ch;
        }

        #endregion

        #region Atoms and Atom Values

        [Subr]
        public static ZilObject SPNAME(Context ctx, ZilObject[] args)
        {
            if (args.Length != 1)
                throw new InterpreterError(null, "SPNAME", 1, 1);

            ZilAtom atom = args[0] as ZilAtom;
            if (atom == null)
                throw new InterpreterError("SPNAME: arg must be an atom");

            return new ZilString(atom.Text);
        }

        [Subr]
        public static ZilObject PARSE(Context ctx, ZilObject[] args)
        {
            // in MDL, this parses an arbitrary expression, but parsing atoms is probably enough for ZIL

            if (args.Length != 1)
                throw new InterpreterError(null, "PARSE", 1, 1);

            if (args[0].GetTypeAtom(ctx).StdAtom != StdAtom.STRING)
                throw new InterpreterError("PARSE: arg must be a string");

            return ZilAtom.Parse(args[0].ToStringContext(ctx, true), ctx);
        }

        [Subr]
        public static ZilObject SETG(Context ctx, ZilObject[] args)
        {
            if (args.Length != 2)
                throw new InterpreterError(null, "SETG", 2, 2);

            if (!(args[0] is ZilAtom))
                throw new InterpreterError("SETG: first arg must be an atom");

            if (args[1] == null)
                throw new ArgumentNullException();

            ctx.SetGlobalVal((ZilAtom)args[0], args[1]);
            return args[1];
        }

        [Subr]
        public static ZilObject SET(Context ctx, ZilObject[] args)
        {
            if (args.Length != 2)
                throw new InterpreterError(null, "SET", 2, 2);

            if (!(args[0] is ZilAtom))
                throw new InterpreterError("SET: first arg must be an atom");

            if (args[1] == null)
                throw new ArgumentNullException();

            ctx.SetLocalVal((ZilAtom)args[0], args[1]);
            return args[1];
        }

        [Subr]
        public static ZilObject GVAL(Context ctx, ZilObject[] args)
        {
            if (args.Length != 1)
                throw new InterpreterError(null, "GVAL", 1, 1);

            if (!(args[0] is ZilAtom))
                throw new InterpreterError("GVAL: arg must be an atom");

            ZilObject result = ctx.GetGlobalVal((ZilAtom)args[0]);
            if (result == null)
                throw new InterpreterError("atom has no global value: " +
                    args[0].ToStringContext(ctx, false));

            return result;
        }

        [Subr("GASSIGNED?")]
        public static ZilObject GASSIGNED_P(Context ctx, ZilObject[] args)
        {
            if (args.Length != 1)
                throw new InterpreterError(null, "GASSIGNED?", 1, 1);

            ZilAtom atom = args[0] as ZilAtom;
            if (atom == null)
                throw new InterpreterError("GASSIGNED?: arg must be an atom");

            return ctx.GetGlobalVal(atom) != null ? ctx.TRUE : ctx.FALSE;
        }

        [Subr]
        public static ZilObject LVAL(Context ctx, ZilObject[] args)
        {
            if (args.Length != 1)
                throw new InterpreterError(null, "LVAL", 1, 1);

            if (!(args[0] is ZilAtom))
                throw new InterpreterError("LVAL: arg must be an atom");

            ZilObject result = ctx.GetLocalVal((ZilAtom)args[0]);
            if (result == null)
                throw new InterpreterError("atom has no local value: " +
                    args[0].ToStringContext(ctx, false));

            return result;
        }

        [Subr("ASSIGNED?")]
        public static ZilObject ASSIGNED_P(Context ctx, ZilObject[] args)
        {
            if (args.Length != 1)
                throw new InterpreterError(null, "ASSIGNED?", 1, 1);

            ZilAtom atom = args[0] as ZilAtom;
            if (atom == null)
                throw new InterpreterError("ASSIGNED?: arg must be an atom");

            return ctx.GetLocalVal(atom) != null ? ctx.TRUE : ctx.FALSE;
        }

        [Subr]
        public static ZilObject GETPROP(Context ctx, ZilObject[] args)
        {
            if (args.Length < 2 || args.Length > 3)
                throw new InterpreterError(null, "GETPROP", 2, 3);

            var result = ctx.GetProp(args[0], args[1]);

            if (result != null)
            {
                return result;
            }
            else if (args.Length > 2)
            {
                return args[2].Eval(ctx);
            }
            else
            {
                return ctx.FALSE;
            }
        }

        [Subr]
        public static ZilObject PUTPROP(Context ctx, ZilObject[] args)
        {
            if (args.Length < 2 || args.Length > 3)
                throw new InterpreterError(null, "PUTPROP", 2, 3);

            if (args.Length == 2)
            {
                // clear, and return previous value or <>
                var result = ctx.GetProp(args[0], args[1]);
                ctx.PutProp(args[0], args[1], null);
                return result ?? ctx.FALSE;
            }
            else
            {
                // set, and return first arg
                ctx.PutProp(args[0], args[1], args[2]);
                return args[0];
            }
        }

        #endregion

        #region Functions/Macros

        [FSubr]
        public static ZilObject DEFINE(Context ctx, ZilObject[] args)
        {
            if (args.Length < 3)
                throw new InterpreterError(null, "DEFINE", 3, 0);

            ZilAtom atom = args[0].Eval(ctx) as ZilAtom;
            if (atom == null)
                throw new InterpreterError("DEFINE: first arg must evaluate to an atom");
            if (!ctx.AllowRedefine && ctx.GetGlobalVal(atom) != null)
                throw new InterpreterError("DEFINE: already defined: " + atom.ToStringContext(ctx, false));

            if (args[1].GetTypeAtom(ctx).StdAtom != StdAtom.LIST)
                throw new InterpreterError("DEFINE: second arg must be a list");

            ZilFunction func = new ZilFunction(atom,
                (IEnumerable<ZilObject>)args[1],
                args.Skip(2));
            ctx.SetGlobalVal(atom, func);
            return atom;
        }

        [FSubr]
        public static ZilObject DEFMAC(Context ctx, ZilObject[] args)
        {
            if (args.Length < 3)
                throw new InterpreterError(null, "DEFMAC", 3, 0);

            ZilAtom atom = args[0].Eval(ctx) as ZilAtom;
            if (atom == null)
                throw new InterpreterError("DEFMAC: first arg must be an atom");
            if (!ctx.AllowRedefine && ctx.GetGlobalVal(atom) != null)
                throw new InterpreterError("DEFMAC: already defined: " + atom.ToStringContext(ctx, false));

            if (args[1].GetTypeAtom(ctx).StdAtom != StdAtom.LIST)
                throw new InterpreterError("DEFMAC: second arg must be a list");

            ZilFunction func = new ZilFunction(atom,
                (IEnumerable<ZilObject>)args[1],
                args.Skip(2));
            ZilEvalMacro macro = new ZilEvalMacro(func);
            ctx.SetGlobalVal(atom, macro);
            return atom;
        }

        [FSubr]
        public static ZilObject QUOTE(Context ctx, ZilObject[] args)
        {
            if (args.Length != 1)
                throw new InterpreterError(null, "QUOTE", 1, 1);

            return args[0];
        }

        [Subr]
        public static ZilObject EVAL(Context ctx, ZilObject[] args)
        {
            if (args.Length != 1)
                throw new InterpreterError(null, "EVAL", 1, 1);

            return args[0].Eval(ctx);
        }

        [Subr]
        public static ZilObject EXPAND(Context ctx, ZilObject[] args)
        {
            if (args.Length != 1)
                throw new InterpreterError(null, "EXPAND", 1, 1);

            return args[0].Expand(ctx);
        }

        [Subr]
        public static ZilObject APPLY(Context ctx, ZilObject[] args)
        {
            if (args.Length == 0)
                throw new InterpreterError(null, "APPLY", 1, 0);

            IApplicable ap = args[0] as IApplicable;
            if (ap == null)
                throw new InterpreterError("APPLY: first arg must be an applicable type");

            ZilObject[] newArgs = new ZilObject[args.Length - 1];
            Array.Copy(args, 1, newArgs, 0, args.Length - 1);
            return ap.ApplyNoEval(ctx, newArgs);
        }

        #endregion

        #region Types and Constructors

        [Subr]
        public static ZilObject TYPE(Context ctx, ZilObject[] args)
        {
            if (args.Length != 1)
                throw new InterpreterError(null, "TYPE", 1, 1);

            return args[0].GetTypeAtom(ctx);
        }

        [Subr("TYPE?")]
        public static ZilObject TYPE_P(Context ctx, ZilObject[] args)
        {
            if (args.Length < 2)
                throw new InterpreterError(null, "TYPE?", 2, 0);

            ZilAtom type = args[0].GetTypeAtom(ctx);
            for (int i = 1; i < args.Length; i++)
                if (args[i] == type)
                    return type;

            return ctx.FALSE;
        }

        [Subr]
        public static ZilObject CHTYPE(Context ctx, ZilObject[] args)
        {
            if (args.Length != 2)
                throw new InterpreterError(null, "CHTYPE", 2, 2);

            ZilAtom atom = args[1] as ZilAtom;
            if (atom == null)
                throw new InterpreterError("CHTYPE: second arg must be an atom");

            return ctx.ChangeType(args[0], atom);
        }

        [Subr("MAKE-GVAL")]
        public static ZilObject MAKE_GVAL(Context ctx, ZilObject[] args)
        {
            if (args.Length != 1)
                throw new InterpreterError(null, "MAKE-GVAL", 1, 1);

            return new ZilForm(new ZilObject[] { ctx.GetStdAtom(StdAtom.GVAL), args[0] });
        }

        [Subr("APPLICABLE?")]
        public static ZilObject APPLICABLE_P(Context ctx, ZilObject[] args)
        {
            if (args.Length != 1)
                throw new InterpreterError(null, "APPLICABLE?", 1, 1);

            return (args[0] is IApplicable) ? ctx.TRUE : ctx.FALSE;
        }

        [Subr("STRUCTURED?")]
        public static ZilObject STRUCTURED_P(Context ctx, ZilObject[] args)
        {
            if (args.Length != 1)
                throw new InterpreterError(null, "STRUCTURED?", 1, 1);

            return (args[0] is IStructure) ? ctx.TRUE : ctx.FALSE;
        }

        [Subr]
        public static ZilObject FORM(Context ctx, ZilObject[] args)
        {
            if (args.Length == 0)
                throw new InterpreterError(null, "FORM", 1, 0);

            if (ctx.CallingForm == null)
                return new ZilForm(args);
            else
                return new ZilForm(ctx.CallingForm.SourceFile, ctx.CallingForm.SourceLine, args);
        }

        [Subr]
        public static ZilObject LIST(Context ctx, ZilObject[] args)
        {
            return new ZilList(args);
        }

        [Subr]
        public static ZilObject VECTOR(Context ctx, ZilObject[] args)
        {
            return new ZilVector(args);
        }

        [Subr]
        public static ZilObject BYTE(Context ctx, ZilObject[] args)
        {
            if (args.Length != 1)
                throw new InterpreterError(null, "BYTE", 1, 1);

            return ctx.ChangeType(args[0], ctx.GetStdAtom(StdAtom.BYTE));
        }

        [Subr]
        public static ZilObject CONS(Context ctx, ZilObject[] args)
        {
            if (args.Length != 2)
                throw new InterpreterError(null, "CONS", 2, 2);

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
            if (args.Length < 2)
                throw new InterpreterError(null, "FUNCTION", 2, 0);
            if (args[0].GetTypeAtom(ctx).StdAtom != StdAtom.LIST)
                throw new InterpreterError("FUNCTION: first arg must be a list");

            return new ZilFunction(null,
                (IEnumerable<ZilObject>)args[0],
                args.Skip(1));
        }

        [Subr]
        public static ZilObject STRING(Context ctx, ZilObject[] args)
        {
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
        public static ZilObject ASCII(Context ctx, ZilObject[] args)
        {
            if (args.Length != 1)
                throw new InterpreterError(null, "ASCII", 1, 1);

            ZilChar ch = args[0] as ZilChar;
            if (ch != null)
                return new ZilFix(ch.Char);

            ZilFix fix = args[0] as ZilFix;
            if (fix != null)
                return new ZilChar((char)fix.Value);

            throw new InterpreterError("ASCII: arg must be a character or FIX");
        }

        #endregion

        #region Structures

        [Subr("EMPTY?")]
        public static ZilObject EMPTY_P(Context ctx, ZilObject[] args)
        {
            if (args.Length != 1)
                throw new InterpreterError(null, "EMPTY?", 1, 1);

            IStructure st = args[0] as IStructure;
            if (st == null)
                throw new InterpreterError("EMPTY?: arg must be a structure");

            return st.IsEmpty() ? ctx.TRUE : ctx.FALSE;
        }

        /*[Subr]
        public static ZilObject FIRST(Context ctx, ZilObject[] args)
        {
            if (args.Length != 1)
                throw new InterpreterError(null, "FIRST", 1, 1);

            IStructure st = args[0] as IStructure;
            if (st == null)
                throw new InterpreterError("FIRST: arg must be a structure");

            return st.GetFirst();
        }*/

        [Subr]
        public static ZilObject REST(Context ctx, ZilObject[] args)
        {
            if (args.Length < 1 || args.Length > 2)
                throw new InterpreterError(null, "REST", 1, 2);

            IStructure st = args[0] as IStructure;
            if (st == null)
                throw new InterpreterError("REST: first arg must be a structure");

            int skip = 1;
            if (args.Length == 2)
            {
                ZilFix fix = args[1] as ZilFix;
                if (fix == null)
                    throw new InterpreterError("REST: second arg must be a FIX");
                skip = fix.Value;
            }

            return (ZilObject)st.GetRest(skip);
        }

        [Subr]
        public static ZilObject NTH(Context ctx, ZilObject[] args)
        {
            if (args.Length != 2)
                throw new InterpreterError(null, "NTH", 2, 2);

            IStructure st = args[0] as IStructure;
            if (st == null)
                throw new InterpreterError("NTH: first arg must be a structure");

            ZilFix idx = args[1] as ZilFix;
            if (idx == null)
                throw new InterpreterError("NTH: second arg must be a FIX");

            ZilObject result = st[idx.Value - 1];
            if (result == null)
                throw new InterpreterError("reading past end of structure");

            return result;
        }

        [Subr]
        public static ZilObject PUT(Context ctx, ZilObject[] args)
        {
            if (args.Length != 3)
                throw new InterpreterError(null, "PUT", 3, 3);

            IStructure st = args[0] as IStructure;
            if (st == null)
                throw new InterpreterError("PUT: first arg must be a structure");

            ZilFix idx = args[1] as ZilFix;
            if (idx == null)
                throw new InterpreterError("PUT: second arg must be a FIX");

            st[idx.Value - 1] = args[2];
            return args[2];
        }

        [Subr]
        public static ZilObject LENGTH(Context ctx, ZilObject[] args)
        {
            if (args.Length != 1)
                throw new InterpreterError(null, "LENGTH", 1, 1);

            IStructure st = args[0] as IStructure;
            if (st == null)
                throw new InterpreterError("LENGTH: arg must be a structure");

            return new ZilFix(st.GetLength());
        }

        [Subr("LENGTH?")]
        public static ZilObject LENGTH_P(Context ctx, ZilObject[] args)
        {
            if (args.Length != 2)
                throw new InterpreterError(null, "LENGTH?", 2, 2);

            IStructure st = args[0] as IStructure;
            if (st == null)
                throw new InterpreterError("LENGTH?: first arg must be a structure");

            ZilFix limit = args[1] as ZilFix;
            if (limit == null)
                throw new InterpreterError("LENGTH?: second arg must be a FIX");

            int? length = st.GetLength(limit.Value);
            if (length == null)
                return ctx.FALSE;
            else
                return new ZilFix(length.Value);
        }

        [Subr]
        public static ZilObject PUTREST(Context ctx, ZilObject[] args)
        {
            if (args.Length != 2)
                throw new InterpreterError(null, "PUTREST", 2, 2);

            ZilList list = args[0] as ZilList;
            if (list == null || list.GetTypeAtom(ctx).StdAtom != StdAtom.LIST)
                throw new InterpreterError("PUTREST: first arg must be a list");

            ZilList newRest = args[1] as ZilList;
            if (newRest == null)
                throw new InterpreterError("PUTREST: second arg must be a list");

            // well, not *exactly* a list...
            if (newRest.GetTypeAtom(ctx).StdAtom == StdAtom.LIST)
                list.Rest = newRest;
            else
                list.Rest = new ZilList(newRest);

            return list;
        }

        #endregion

        #region Arithmetic

        private static ZilObject PerformArithmetic(int init, string name, Func<int, int, int> op,
            ZilObject[] args)
        {
            const string STypeError = "every arg must be a FIX";

            switch (args.Length)
            {
                case 0:
                    return new ZilFix(init);

                case 1:
                    if (!(args[0] is ZilFix))
                        throw new InterpreterError(name + ": " + STypeError);
                    else
                        return new ZilFix(op(init, ((ZilFix)args[0]).Value));

                default:
                    if (!(args[0] is ZilFix))
                        throw new InterpreterError(name + ": " + STypeError);

                    int result = ((ZilFix)args[0]).Value;

                    for (int i = 1; i < args.Length; i++)
                    {
                        if (!(args[i] is ZilFix))
                            throw new InterpreterError(name + ": " + STypeError);

                        result = op(result, ((ZilFix)args[i]).Value);
                    }

                    return new ZilFix(result);
            }
        }

        [Subr("+")]
        public static ZilObject Plus(Context ctx, ZilObject[] args)
        {
            return PerformArithmetic(0, "+", (x, y) => x + y, args);
        }

        [Subr("-")]
        public static ZilObject Minus(Context ctx, ZilObject[] args)
        {
            return PerformArithmetic(0, "-", (x, y) => x - y, args);
        }

        [Subr("*")]
        public static ZilObject Times(Context ctx, ZilObject[] args)
        {
            return PerformArithmetic(1, "*", (x, y) => x * y, args);
        }

        [Subr("/")]
        public static ZilObject Divide(Context ctx, ZilObject[] args)
        {
            try
            {
                return PerformArithmetic(1, "/", (x, y) => x / y, args);
            }
            catch (DivideByZeroException)
            {
                throw new InterpreterError("division by zero");
            }
        }

        [Subr]
        public static ZilObject LSH(Context ctx, ZilObject[] args)
        {
            // "Logical shift", not left shift.
            // Positive shifts left, negative shifts right.
            
            if (args.Length != 2)
                throw new InterpreterError(null, "LSH", 2, 2);

            var a = args[0] as ZilFix;
            var b = args[1] as ZilFix;

            if (a == null || b == null)
                throw new InterpreterError("LSH: every arg must be a FIX");

            int result;

            if (b.Value >= 0)
            {
                int count = b.Value % 256;
                result = count >= 32 ? 0 : a.Value << count;
            }
            else
            {
                int count = -b.Value % 256;
                result = count >= 32 ? 0 : (int)((uint)a.Value >> count);
            }

            return new ZilFix(result);
        }

        [Subr]
        public static ZilObject ORB(Context ctx, ZilObject[] args)
        {
            return PerformArithmetic(0, "ORB", (x, y) => x | y, args);
        }

        [Subr]
        public static ZilObject ANDB(Context ctx, ZilObject[] args)
        {
            return PerformArithmetic(-1, "ANDB", (x, y) => x & y, args);
        }

        #endregion

        #region Conditions

        [FSubr]
        public static ZilObject COND(Context ctx, ZilObject[] args)
        {
            if (args.Length < 1)
                throw new InterpreterError(null, "COND", 1, 0);

            ZilObject result = null;

            foreach (ZilObject zo in args)
            {
                if (zo.GetTypeAtom(ctx).StdAtom == StdAtom.LIST)
                {
                    ZilList zl = (ZilList)zo;
                    result = zl.First.Eval(ctx);

                    if (result.IsTrue)
                    {
                        foreach (ZilObject inner in zl.Skip(1))
                            result = inner.Eval(ctx);

                        return result;
                    }
                }
                else
                    throw new InterpreterError("COND: every arg must be a list");
            }

            return result;
        }

        [FSubr]
        public static ZilObject OR(Context ctx, ZilObject[] args)
        {
            ZilObject result = ctx.FALSE;

            foreach (ZilObject arg in args)
            {
                result = arg.Eval(ctx);
                if (result.IsTrue)
                    return result;
            }

            return result;
        }

        [FSubr]
        public static ZilObject AND(Context ctx, ZilObject[] args)
        {
            ZilObject result = ctx.TRUE;

            foreach (ZilObject arg in args)
            {
                result = arg.Eval(ctx);
                if (!result.IsTrue)
                    return result;
            }

            return result;
        }

        [Subr("OR?")]
        public static ZilObject OR_P(Context ctx, ZilObject[] args)
        {
            ZilObject result = ctx.FALSE;

            foreach (ZilObject arg in args)
            {
                result = arg;
                if (result.IsTrue)
                    return result;
            }

            return result;
        }

        [Subr("AND?")]
        public static ZilObject AND_P(Context ctx, ZilObject[] args)
        {
            ZilObject result = ctx.TRUE;

            foreach (ZilObject arg in args)
            {
                result = arg;
                if (!result.IsTrue)
                    return result;
            }

            return result;
        }

        [Subr]
        public static ZilObject NOT(Context ctx, ZilObject[] args)
        {
            if (args.Length != 1)
                throw new InterpreterError(null, "NOT", 1, 1);

            return args[0].IsTrue ? ctx.FALSE : ctx.TRUE;
        }

        [Subr("=?")]
        public static ZilObject Eq_P(Context ctx, ZilObject[] args)
        {
            if (args.Length != 2)
                throw new InterpreterError(null, "=?", 2, 2);

            return args[0].Equals(args[1]) ? ctx.TRUE : ctx.FALSE;
        }

        [Subr("N=?")]
        public static ZilObject NEq_P(Context ctx, ZilObject[] args)
        {
            if (args.Length != 2)
                throw new InterpreterError(null, "N=?", 2, 2);

            return args[0].Equals(args[1]) ? ctx.FALSE : ctx.TRUE;
        }

        [Subr("==?")]
        public static ZilObject Eeq_P(Context ctx, ZilObject[] args)
        {
            if (args.Length != 2)
                throw new InterpreterError(null, "==?", 2, 2);

            bool equal;
            if (args[0] is IStructure)
                equal = (args[0] == args[1]);
            else
                equal = args[0].Equals(args[1]);

            return equal ? ctx.TRUE : ctx.FALSE;
        }

        [Subr("N==?")]
        public static ZilObject NEeq_P(Context ctx, ZilObject[] args)
        {
            if (args.Length != 2)
                throw new InterpreterError(null, "N==?", 2, 2);

            bool equal;
            if (args[0] is IStructure)
                equal = (args[0] == args[1]);
            else
                equal = args[0].Equals(args[1]);

            return equal ? ctx.FALSE : ctx.TRUE;
        }

        private static ZilObject PerformComparison(Context ctx, string name, Func<int, int, bool> op, ZilObject[] args)
        {
            const string STypeError = "every arg must be a FIX";

            if (args.Length != 2)
                throw new InterpreterError(null, name, 2, 2);

            if (!(args[0] is ZilFix && args[1] is ZilFix))
                throw new InterpreterError(name + ": " + STypeError);

            int value1 = ((ZilFix)args[0]).Value;
            int value2 = ((ZilFix)args[1]).Value;

            return op(value1, value2) ? ctx.TRUE : ctx.FALSE;
        }

        [Subr("L?")]
        public static ZilObject L_P(Context ctx, ZilObject[] args)
        {
            return PerformComparison(ctx, "L?", (a, b) => a < b, args);
        }

        [Subr("L=?")]
        public static ZilObject LEq_P(Context ctx, ZilObject[] args)
        {
            return PerformComparison(ctx, "L=?", (a, b) => a <= b, args);
        }

        [Subr("G?")]
        public static ZilObject G_P(Context ctx, ZilObject[] args)
        {
            return PerformComparison(ctx, "G?", (a, b) => a > b, args);
        }

        [Subr("G=?")]
        public static ZilObject GEq_P(Context ctx, ZilObject[] args)
        {
            return PerformComparison(ctx, "G=?", (a, b) => a >= b, args);
        }

        #endregion

        #region Mapping

        [Subr]
        public static ZilObject MAPF(Context ctx, ZilObject[] args)
        {
            return PerformMap(ctx, args, true);
        }

        [Subr]
        public static ZilObject MAPR(Context ctx, ZilObject[] args)
        {
            return PerformMap(ctx, args, false);
        }

        private class MapRetException : ControlException
        {
            private readonly ZilObject[] values;

            public MapRetException(ZilObject[] values)
                : base("MAPRET")
            {
                this.values = values;
            }

            protected MapRetException(string name, ZilObject[] values)
                : base(name)
            {
                this.values = values;
            }

            public ZilObject[] Values
            {
                get { return values; }
            }
        }

        private class MapStopException : MapRetException
        {
            public MapStopException(ZilObject[] values)
                : base("MAPSTOP", values)
            {
            }
        }

        private class MapLeaveException : ControlException
        {
            private readonly ZilObject value;

            public MapLeaveException(ZilObject value)
                : base("MAPLEAVE")
            {
                this.value = value;
            }

            public ZilObject Value
            {
                get { return value; }
            }
        }

        private static ZilObject PerformMap(Context ctx, ZilObject[] args, bool first)
        {
            string name = first ? "MAPF" : "MAPR";

            if (args.Length < 2)
                throw new InterpreterError(name + ": expected at least 2 args");

            IApplicable finalf = args[0] as IApplicable;

            if (finalf == null && args[0].IsTrue)
                throw new InterpreterError(name + ": first arg must be FALSE or an applicable object");

            IApplicable loopf = args[1] as IApplicable;

            if (loopf == null)
                throw new InterpreterError(name + ": second arg must be an applicable object");

            int numStructs = args.Length - 2;
            IStructure[] structs = new IStructure[numStructs];
            ZilObject[] loopArgs = new ZilObject[numStructs];

            for (int i = 0; i < numStructs; i++)
            {
                structs[i] = args[i + 2] as IStructure;
                if (structs[i] == null)
                    throw new InterpreterError(name + ": args after first two must be structures");
            }

            List<ZilObject> results = new List<ZilObject>();

            while (true)
            {
                // prepare loop args
                int i;
                for (i = 0; i < numStructs; i++)
                {
                    IStructure st = structs[i];
                    if (st == null || st.IsEmpty())
                        break;

                    if (first)
                        loopArgs[i] = st.GetFirst();
                    else
                        loopArgs[i] = (ZilObject)st;

                    structs[i] = st.GetRest(1);
                }

                if (i < numStructs)
                    break;

                // apply loop function
                try
                {
                    results.Add(loopf.ApplyNoEval(ctx, loopArgs));
                }
                catch (MapStopException ex)
                {
                    // add values to results and exit loop
                    results.AddRange(ex.Values);
                    break;
                }
                catch (MapRetException ex)
                {
                    // add values to results and continue
                    results.AddRange(ex.Values);
                }
                catch (MapLeaveException ex)
                {
                    // discard results, skip finalf, and return this value from the map
                    return ex.Value;
                }
            }

            // apply final function
            if (finalf != null)
                return finalf.ApplyNoEval(ctx, results.ToArray());

            if (results.Count > 0)
                return results[results.Count - 1];

            return ctx.FALSE;
        }

        [Subr]
        public static ZilObject MAPRET(Context ctx, ZilObject[] args)
        {
            throw new MapRetException(args);
        }

        [Subr]
        public static ZilObject MAPSTOP(Context ctx, ZilObject[] args)
        {
            throw new MapStopException(args);
        }

        [Subr]
        public static ZilObject MAPLEAVE(Context ctx, ZilObject[] args)
        {
            if (args.Length > 1)
                throw new InterpreterError(null, "MAPLEAVE", 0, 1);

            throw new MapLeaveException(args.Length == 0 ? ctx.TRUE : args[0]);
        }

        #endregion

        #region Looping

        [FSubr]
        public static ZilObject PROG(Context ctx, ZilObject[] args)
        {
            return PerformProg(ctx, args, "PROG", false, true);
        }

        [FSubr]
        public static ZilObject REPEAT(Context ctx, ZilObject[] args)
        {
            return PerformProg(ctx, args, "REPEAT", true, true);
        }

        [FSubr]
        public static ZilObject BIND(Context ctx, ZilObject[] args)
        {
            return PerformProg(ctx, args, "BIND", false, false);
        }

        private static ZilObject PerformProg(Context ctx, ZilObject[] args, string name, bool repeat, bool catchy)
        {
            if (args.Length < 2)
                throw new InterpreterError(null, name, 2, 0);

            // bind atoms
            ZilList bindings = args[0] as ZilList;
            if (bindings == null || bindings.GetTypeAtom(ctx).StdAtom != StdAtom.LIST)
                throw new InterpreterError(name + ": first arg must be a list of zero or more atom bindings");

            Queue<ZilAtom> boundAtoms = new Queue<ZilAtom>();

            try
            {
                foreach (ZilObject b in bindings)
                {
                    switch (b.GetTypeAtom(ctx).StdAtom)
                    {
                        case StdAtom.ATOM:
                            ctx.PushLocalVal((ZilAtom)b, null);
                            boundAtoms.Enqueue((ZilAtom)b);
                            break;

                        case StdAtom.LIST:
                            ZilList list = (ZilList)b;
                            if (list.First == null || list.Rest == null ||
                                list.Rest.First == null || (list.Rest.Rest != null && list.Rest.Rest.First != null))
                                throw new InterpreterError(name + ": binding with value must be a 2-element list");
                            ZilAtom atom = list.First as ZilAtom;
                            ZilObject value = list.Rest.First;
                            if (atom == null || value == null)
                                throw new InterpreterError(name + ": invalid atom binding");
                            ctx.PushLocalVal(atom, value.Eval(ctx));
                            boundAtoms.Enqueue(atom);
                            break;

                        default:
                            throw new InterpreterError(name + ": elements of binding list must be atoms or lists");
                    }
                }

                // evaluate body
                ZilObject result = null;
                if (catchy)
                {
                    bool again;
                    do
                    {
                        again = false;
                        for (int i = 1; i < args.Length; i++)
                        {
                            try
                            {
                                result = args[i].Eval(ctx);
                            }
                            catch (ReturnException ex)
                            {
                                return ex.Value;
                            }
                            catch (AgainException)
                            {
                                again = true;
                            }
                        }
                    } while (repeat || again);
                }
                else
                {
                    do
                    {
                        for (int i = 1; i < args.Length; i++)
                            result = args[i].Eval(ctx);
                    } while (repeat);
                }

                return result;
            }
            finally
            {
                while (boundAtoms.Count > 0)
                    ctx.PopLocalVal(boundAtoms.Dequeue());
            }
        }

        [Subr]
        public static ZilObject RETURN(Context ctx, ZilObject[] args)
        {
            if (args.Length == 0)
                throw new ReturnException(ctx.TRUE);
            else if (args.Length == 1)
                throw new ReturnException(args[0]);
            else
                throw new InterpreterError(null, "RETURN", 0, 1);
        }

        [Subr]
        public static ZilObject AGAIN(Context ctx, ZilObject[] args)
        {
            if (args.Length == 0)
                throw new AgainException();
            else
                throw new InterpreterError(null, "AGAIN", 0, 0);
        }

        #endregion

        #region Z-Code

        [FSubr]
        public static ZilObject ROUTINE(Context ctx, ZilObject[] args)
        {
            if (args.Length < 3)
                throw new InterpreterError(null, "ROUTINE", 3, 0);

            ZilAtom atom = args[0].Eval(ctx) as ZilAtom;
            if (atom == null)
                throw new InterpreterError("ROUTINE: first arg must be an atom");

            if (ctx.GetZVal(atom) != null)
            {
                if (ctx.AllowRedefine)
                    ctx.Redefine(atom);
                else
                    throw new InterpreterError("ROUTINE: already defined: " + atom.ToStringContext(ctx, false));
            }

            if (args[1].GetTypeAtom(ctx).StdAtom != StdAtom.LIST)
                throw new InterpreterError("ROUTINE: second arg must be a list");

            var flags = CombineFlags(ctx.CurrentFileFlags, ctx.NextRoutineFlags);
            ctx.NextRoutineFlags = RoutineFlags.None;

            ZilRoutine rtn = new ZilRoutine(
                atom,
                (IEnumerable<ZilObject>)args[1],
                args.Skip(2),
                flags);
            ctx.SetZVal(atom, rtn);
            ctx.ZEnvironment.Routines.Add(rtn);
            return atom;
        }

        private static RoutineFlags CombineFlags(FileFlags fileFlags, RoutineFlags routineFlags)
        {
            var result = routineFlags;

            if ((fileFlags & FileFlags.CleanStack) != 0)
                result |= RoutineFlags.CleanStack;

            return result;
        }

        [Subr]
        public static ZilObject CONSTANT(Context ctx, ZilObject[] args)
        {
            if (args.Length != 2)
                throw new InterpreterError(null, "CONSTANT", 2, 2);

            ZilAtom atom = args[0] as ZilAtom;
            if (atom == null)
                throw new InterpreterError(null, "CONSTANT: first arg must be an atom");

            if (ctx.GetZVal(atom) != null)
            {
                if (ctx.AllowRedefine)
                    ctx.Redefine(atom);
                else
                    throw new InterpreterError("CONSTANT: already defined: " + atom.ToStringContext(ctx, false));
            }

            return ctx.AddZConstant(atom, args[1]);
        }

        [Subr]
        public static ZilObject GLOBAL(Context ctx, ZilObject[] args)
        {
            if (args.Length != 2)
                throw new InterpreterError(null, "GLOBAL", 2, 2);

            ZilAtom atom = args[0] as ZilAtom;
            if (atom == null)
            {
                var adecl = args[0] as ZilAdecl;
                if (adecl != null)
                    atom = adecl.First as ZilAtom;

                if (atom == null)
                    throw new InterpreterError(null, "GLOBAL: first arg must be an atom (or ADECL'd atom)");
            }

            var oldVal = ctx.GetZVal(atom);
            if (oldVal != null)
            {
                if (ctx.AllowRedefine)
                {
                    if (oldVal is ZilGlobal)
                    {
                        var defaultValue = ((ZilGlobal)oldVal).Value;
                        if (defaultValue is ZilTable)
                        {
                            // prevent errors about duplicate symbol T?GLOBAL-NAME
                            // TODO: undefine the table if it hasn't been referenced anywhere yet
                            ((ZilTable)defaultValue).Name = null;
                        }
                    }

                    ctx.Redefine(atom);
                }
                else
                    throw new InterpreterError("GLOBAL: already defined: " + atom.ToStringContext(ctx, false));
            }

            if (args[1] is ZilTable)
                ((ZilTable)args[1]).Name = "T?" + atom.ToStringContext(ctx, false);

            ZilGlobal g = new ZilGlobal(atom, args[1]);
            ctx.SetZVal(atom, g);
            ctx.ZEnvironment.Globals.Add(g);
            return g;
        }

        [Subr]
        public static ZilObject OBJECT(Context ctx, ZilObject[] args)
        {
            return PerformObject(ctx, args, false);
        }

        [Subr]
        public static ZilObject ROOM(Context ctx, ZilObject[] args)
        {
            return PerformObject(ctx, args, true);
        }

        private static ZilObject PerformObject(Context ctx, ZilObject[] args, bool isRoom)
        {
            string name = isRoom ? "ROOM" : "OBJECT";

            if (args.Length < 1)
                throw new InterpreterError(null, name, 1, 0);

            ZilAtom atom = args[0] as ZilAtom;
            if (atom == null)
                throw new InterpreterError(name + ": first arg must be an atom");

            if (ctx.GetZVal(atom) != null)
            {
                if (ctx.AllowRedefine)
                    ctx.Redefine(atom);
                else
                    throw new InterpreterError(name + ": already defined: " + atom.ToStringContext(ctx, false));
            }

            ZilList[] props = new ZilList[args.Length - 1];
            for (int i = 0; i < props.Length; i++)
            {
                props[i] = args[i + 1] as ZilList;
                if (props[i] == null || props[i].GetTypeAtom(ctx).StdAtom != StdAtom.LIST)
                    throw new InterpreterError(name + ": all property definitions must be lists");
            }

            ZilModelObject zmo = new ZilModelObject(atom, props, isRoom);
            ctx.SetZVal(atom, zmo);
            ctx.ZEnvironment.Objects.Add(zmo);
            return zmo;
        }

        [Subr]
        public static ZilObject ITABLE(Context ctx, ZilObject[] args)
        {
            // Syntax:
            //    <ITABLE [specifier] count [(flags...)] [init...]>
            // 'count' is a number of repetitions.
            // 'specifier' controls the length marker. BYTE specifier
            // makes the length marker a byte (but the table is still a
            // word table unless changed with a flag).
            // 'init' is a sequence of values to be repeated 'count' times.
            // values are compiled as words unless BYTE/LEXV flag is specified.

            if (args.Length < 1)
                throw new InterpreterError(null, "ITABLE", 1, 0);

            int i = 0;
            TableFlags flags = 0;

            // optional specifier
            if (args[i] is ZilAtom)
            {
                switch (((ZilAtom)args[i]).StdAtom)
                {
                    case StdAtom.NONE:
                        // no change
                        break;
                    case StdAtom.BYTE:
                        flags = TableFlags.ByteLength;
                        break;
                    case StdAtom.WORD:
                        flags = TableFlags.WordLength;
                        break;
                    default:
                        throw new InterpreterError("ITABLE: specifier must be NONE, BYTE, or WORD");
                }

                i++;
            }

            // element count
            ZilFix elemCount;
            if (i >= args.Length || (elemCount = args[i] as ZilFix) == null)
                throw new InterpreterError("ITABLE: missing element count");
            if (elemCount.Value < 1)
                throw new InterpreterError(null, "ITABLE: invalid table size");
            i++;

            // optional flags
            if (i < args.Length && args[i] is ZilList)
            {
                bool gotLength = false;

                foreach (ZilObject obj in (ZilList)args[i])
                {
                    ZilAtom flag = obj as ZilAtom;
                    if (flag == null)
                        throw new InterpreterError("ITABLE: flags must be atoms");

                    switch (flag.StdAtom)
                    {
                        case StdAtom.BYTE:
                            flags |= TableFlags.Byte;
                            break;
                        case StdAtom.WORD:
                            flags &= ~TableFlags.Byte;
                            break;
                        case StdAtom.LENGTH:
                            gotLength = true;
                            break;
                        case StdAtom.LEXV:
                            flags |= TableFlags.Lexv;
                            break;
                        default:
                            throw new InterpreterError("ITABLE: unrecognized flag: " + flag);
                    }
                }

                if (gotLength)
                {
                    if ((flags & TableFlags.Byte) != 0)
                        flags |= TableFlags.ByteLength;
                    else
                        flags |= TableFlags.WordLength;
                }

                i++;
            }

            ZilObject[] initializer;
            if (i >= args.Length)
            {
                initializer = null;
            }
            else
            {
                initializer = new ZilObject[args.Length - i];
                Array.Copy(args, i, initializer, 0, initializer.Length);
            }

            ZilTable tab = new ZilTable(ctx.CallingForm.SourceFile, ctx.CallingForm.SourceLine,
                elemCount.Value, initializer, flags);
            ctx.ZEnvironment.Tables.Add(tab);
            return tab;
        }

        private static ZilTable PerformTable(Context ctx, ZilObject[] args,
            bool pure, bool wantLength)
        {
            // syntax:
            //    <[P][L]TABLE [(flags...)] values...>

            string name = pure ?
                (wantLength ? "PLTABLE" : "PTABLE") :
                (wantLength ? "LTABLE" : "TABLE");

            const int T_WORDS = 0;
            const int T_BYTES = 1;
            const int T_STRING = 2;
            int type = T_WORDS;

            int i = 0;
            if (args.Length > 0)
            {
                if (args[0].GetTypeAtom(ctx).StdAtom == StdAtom.LIST)
                {
                    i++;

                    foreach (ZilObject obj in (ZilList)args[0])
                    {
                        ZilAtom flag = obj as ZilAtom;
                        if (flag == null)
                            throw new InterpreterError(name + ": flags must be atoms");

                        switch (flag.StdAtom)
                        {
                            case StdAtom.LENGTH:
                                wantLength = true;
                                break;
                            case StdAtom.PURE:
                                pure = true;
                                break;
                            case StdAtom.BYTE:
                                type = T_BYTES;
                                break;
                            case StdAtom.STRING:
                                type = T_STRING;
                                break;
                            case StdAtom.KERNEL:
                                // no idea what this one does
                                break;
                            default:
                                throw new InterpreterError(name + ": unrecognized flag: " + flag);
                        }
                    }
                }
            }

            TableFlags flags = 0;
            if (pure)
                flags |= TableFlags.Pure;
            if (type == T_BYTES || type == T_STRING)
                flags |= TableFlags.Byte;
            if (wantLength)
            {
                if (type == T_BYTES || type == T_STRING)
                    flags |= TableFlags.ByteLength;
                else
                    flags |= TableFlags.WordLength;
            }

            List<ZilObject> values = new List<ZilObject>(args.Length - i);
            while (i < args.Length)
            {
                ZilObject val = args[i];
                if (type == T_STRING && val.GetTypeAtom(ctx).StdAtom == StdAtom.STRING)
                {
                    string str = val.ToStringContext(ctx, true);
                    foreach (char c in str)
                        values.Add(new ZilFix(c));
                }
                else
                    values.Add(val);

                i++;
            }

            ZilTable tab = new ZilTable(ctx.CallingForm.SourceFile, ctx.CallingForm.SourceLine,
                1, values.ToArray(), flags);
            ctx.ZEnvironment.Tables.Add(tab);
            return tab;
        }

        [Subr]
        public static ZilObject TABLE(Context ctx, ZilObject[] args)
        {
            return PerformTable(ctx, args, false, false);
        }

        [Subr]
        public static ZilObject LTABLE(Context ctx, ZilObject[] args)
        {
            return PerformTable(ctx, args, false, true);
        }

        [Subr]
        public static ZilObject PTABLE(Context ctx, ZilObject[] args)
        {
            return PerformTable(ctx, args, true, false);
        }

        [Subr]
        public static ZilObject PLTABLE(Context ctx, ZilObject[] args)
        {
            return PerformTable(ctx, args, true, true);
        }

        [Subr]
        public static ZilObject VERSION(Context ctx, ZilObject[] args)
        {
            if (args.Length < 1 || args.Length > 2)
                throw new InterpreterError(null, "VERSION", 1, 2);

            int newVersion = ParseZVersion("VERSION", args[0]);

            ctx.ZEnvironment.ZVersion = newVersion;
            ctx.SetGlobalVal(ctx.GetStdAtom(StdAtom.PLUS_MODE), newVersion > 3 ? ctx.TRUE : ctx.FALSE);

            if (args.Length > 1)
            {
                var atom = args[1] as ZilAtom;
                if (atom == null || atom.StdAtom != StdAtom.TIME)
                    throw new InterpreterError("VERSION: second arg must be the atom TIME");

                if (ctx.ZEnvironment.ZVersion != 3)
                    throw new InterpreterError("VERSION: TIME is only meaningful in version 3");

                ctx.ZEnvironment.TimeStatusLine = true;
            }

            return new ZilFix(newVersion);
        }

        private static int ParseZVersion(string name, ZilObject expr)
        {
            int newVersion;
            if (expr is ZilAtom)
            {
                switch (((ZilAtom)expr).StdAtom)
                {
                    case StdAtom.ZIP:
                        newVersion = 3;
                        break;
                    case StdAtom.EZIP:
                        newVersion = 4;
                        break;
                    case StdAtom.XZIP:
                        newVersion = 5;
                        break;
                    case StdAtom.YZIP:
                        newVersion = 6;
                        break;
                    default:
                        throw new InterpreterError(name + ": unrecognized atom (must be ZIP, EZIP, XZIP, YZIP)");
                }
            }
            else if (expr is ZilFix)
            {
                newVersion = ((ZilFix)expr).Value;
                if (newVersion < 3 || newVersion > 8)
                    throw new InterpreterError(name + ": version number out of range (must be 3-6)");
            }
            else
                throw new InterpreterError(name + ": arg must be an atom or a FIX");
            return newVersion;
        }

        [Subr("CHECK-VERSION?")]
        public static ZilObject CHECK_VERSION_P(Context ctx, ZilObject[] args)
        {
            if (args.Length != 1)
                throw new InterpreterError(null, "CHECK-VERSION?", 1, 1);

            int version = ParseZVersion("CHECK-VERSION?", args[0]);
            return ctx.ZEnvironment.ZVersion == version ? ctx.TRUE : ctx.FALSE;
        }

        [FSubr("VERSION?")]
        public static ZilObject VERSION_P(Context ctx, ZilObject[] args)
        {
            if (args.Length < 1)
                throw new InterpreterError(null, "VERSION?", 1, 0);

            ZilAtom tAtom = ctx.GetStdAtom(StdAtom.T);
            ZilAtom elseAtom = ctx.GetStdAtom(StdAtom.ELSE);

            foreach (ZilObject clause in args)
            {
                ZilList list = clause as ZilList;
                if (list == null || list.GetTypeAtom(ctx).StdAtom != StdAtom.LIST)
                    throw new InterpreterError("VERSION?: args must be lists");

                if (list.First == tAtom || list.First == elseAtom ||
                    ParseZVersion("VERSION?", list.First) == ctx.ZEnvironment.ZVersion)
                {
                    ZilObject result = list.First;
                    foreach (ZilObject expr in list.Rest)
                        result = expr.Eval(ctx);
                    return result;
                }
            }

            return ctx.FALSE;
        }

        [Subr]
        public static ZilObject DIRECTIONS(Context ctx, ZilObject[] args)
        {
            if (args.Length == 0)
                throw new InterpreterError(null, "DIRECTIONS", 1, 0);

            if (!args.All(zo => zo is ZilAtom))
                throw new InterpreterError("DIRECTIONS: all args must be atoms");

            ctx.ZEnvironment.Directions.Clear();
            foreach (ZilAtom arg in args)
            {
                ctx.ZEnvironment.Directions.Add(arg);
                ctx.ZEnvironment.GetVocabDirection(arg, ctx.CallingForm as ISourceLine);
                ctx.ZEnvironment.LowDirection = arg;
            }

            return ctx.TRUE;
        }

        [Subr]
        public static ZilObject BUZZ(Context ctx, ZilObject[] args)
        {
            if (args.Length == 0)
                throw new InterpreterError(null, "BUZZ", 1, 0);

            if (!args.All(zo => zo is ZilAtom))
                throw new InterpreterError("BUZZ: all args must be atoms");

            foreach (ZilAtom arg in args)
                ctx.ZEnvironment.Buzzwords.Add(new KeyValuePair<ZilAtom, ISourceLine>(arg, ctx.CallingForm));

            return ctx.TRUE;
        }

        [Subr]
        public static ZilObject VOC(Context ctx, ZilObject[] args)
        {
            if (args.Length < 1 || args.Length > 2)
                throw new InterpreterError(null, "VOC", 1, 2);

            var text = args[0] as ZilString;
            if (text == null)
                throw new InterpreterError("VOC: first arg must be a string");

            var atom = ZilAtom.Parse(text.Text, ctx);
            var word = ctx.ZEnvironment.GetVocab(atom);

            if (args.Length > 1 && !(args[1] is ZilFalse))
            {
                var type = args[1] as ZilAtom;
                if (type == null)
                    throw new InterpreterError("VOC: second arg must be FALSE or an atom");

                switch (type.StdAtom)
                {
                    case StdAtom.ADJ:
                    case StdAtom.ADJECTIVE:
                        word = ctx.ZEnvironment.GetVocabAdjective(atom, ctx.CallingForm);
                        break;

                    case StdAtom.NOUN:
                    case StdAtom.OBJECT:
                        word = ctx.ZEnvironment.GetVocabNoun(atom, ctx.CallingForm);
                        break;

                    case StdAtom.BUZZ:
                        word = ctx.ZEnvironment.GetVocabBuzzword(atom, ctx.CallingForm);
                        break;

                    default:
                        throw new InterpreterError("VOC: unrecognized part of speech: " + type);
                }
            }

            return new ZilForm(ctx.CallingForm.SourceFile, ctx.CallingForm.SourceLine,
                new ZilObject[] { ctx.GetStdAtom(StdAtom.GVAL), ZilAtom.Parse("W?" + atom, ctx) });
        }

        [Subr]
        public static ZilObject SYNTAX(Context ctx, ZilObject[] args)
        {
            if (args.Length < 3)
                throw new InterpreterError(null, "SYNTAX", 3, 0);

            Syntax syntax = Syntax.Parse(ctx.CallingForm, args, ctx);
            ctx.ZEnvironment.Syntaxes.Add(syntax);

            if (syntax.Synonyms.Count > 0)
            {
                var synonymArgs = Enumerable.Repeat(syntax.Verb.Atom, 1).Concat(syntax.Synonyms).ToArray();
                PerformSynonym(ctx, synonymArgs, "SYNTAX (verb synonyms)", typeof(VerbSynonym));
            }

            return syntax.Verb.Atom;
        }

        private static ZilObject PerformSynonym(Context ctx, ZilObject[] args,
            string name, Type synonymType)
        {
            if (args.Length < 1)
                throw new InterpreterError(null, name, 1, 0);

            const string STypeError = ": args must be atoms";

            ZilAtom atom = args[0] as ZilAtom;
            if (atom == null)
                throw new InterpreterError(name + STypeError);

            Word oldWord;
            if (ctx.ZEnvironment.Vocabulary.TryGetValue(atom, out oldWord) == false)
            {
                oldWord = new Word(atom);
                ctx.ZEnvironment.Vocabulary.Add(atom, oldWord);
            }

            object[] ctorArgs = new object[2];
            ctorArgs[0] = oldWord;

            for (int i = 1; i < args.Length; i++)
            {
                atom = args[i] as ZilAtom;
                if (atom == null)
                    throw new InterpreterError(name + STypeError);

                Word newWord;
                if (ctx.ZEnvironment.Vocabulary.TryGetValue(atom, out newWord) == false)
                {
                    newWord = new Word(atom);
                    ctx.ZEnvironment.Vocabulary.Add(atom, newWord);
                }

                ctorArgs[1] = newWord;
                ctx.ZEnvironment.Synonyms.Add((Synonym)Activator.CreateInstance(
                    synonymType, ctorArgs));
            }

            return atom;
        }

        [Subr]
        public static ZilObject SYNONYM(Context ctx, ZilObject[] args)
        {
            return PerformSynonym(ctx, args, "SYNONYM", typeof(Synonym));
        }

        [Subr("VERB-SYNONYM")]
        public static ZilObject VERB_SYNONYM(Context ctx, ZilObject[] args)
        {
            return PerformSynonym(ctx, args, "VERB-SYNONYM", typeof(VerbSynonym));
        }

        [Subr("PREP-SYNONYM")]
        public static ZilObject PREP_SYNONYM(Context ctx, ZilObject[] args)
        {
            return PerformSynonym(ctx, args, "PREP-SYNONYM", typeof(PrepSynonym));
        }

        [Subr("ADJ-SYNONYM")]
        public static ZilObject ADJ_SYNONYM(Context ctx, ZilObject[] args)
        {
            return PerformSynonym(ctx, args, "ADJ-SYNONYM", typeof(AdjSynonym));
        }

        [Subr("DIR-SYNONYM")]
        public static ZilObject DIR_SYNONYM(Context ctx, ZilObject[] args)
        {
            return PerformSynonym(ctx, args, "DIR-SYNONYM", typeof(DirSynonym));
        }

        [Subr("FREQUENT-WORDS?")]
        public static ZilObject FREQUENT_WORDS_P(Context ctx, ZilObject[] args)
        {
            // nada - we always generate frequent words
            return ctx.TRUE;
        }

        [Subr]
        public static ZilObject ID(Context ctx, ZilObject[] args)
        {
            if (args.Length != 1)
                throw new InterpreterError(null, "ID", 1, 1);

            return args[0];
        }

        [Subr]
        public static ZilObject PROPDEF(Context ctx, ZilObject[] args)
        {
            if (args.Length < 2 || args.Length > 3)
                throw new InterpreterError(null, "PROPDEF", 2, 3);

            ZilAtom atom = args[0] as ZilAtom;
            if (atom == null)
                throw new InterpreterError("PROPDEF: first arg must be an atom");

            if (ctx.ZEnvironment.PropertyDefaults.ContainsKey(atom))
                Errors.TerpWarning(ctx, null,
                    "overriding default value for property '{0}'",
                    atom);

            ctx.ZEnvironment.PropertyDefaults[atom] = args[1];

            //XXX complex property patterns
            if (args.Length == 3)
                throw new NotImplementedException();

            return atom;
        }

        [Subr]
        public static ZilObject SNAME(Context ctx, ZilObject[] args)
        {
            if (args.Length != 1)
                throw new InterpreterError(null, "SNAME", 1, 1);

            return args[0];
        }

        [FSubr]
        public static ZilObject GDECL(Context ctx, ZilObject[] args)
        {
            // ignore global declarations
            return ctx.FALSE;
        }

        [Subr("ORDER-OBJECTS?")]
        public static ZilObject ORDER_OBJECTS_P(Context ctx, ZilObject[] args)
        {
            ZilAtom atom;

            if (args.Length < 1)
                throw new InterpreterError(null, "ORDER-OBJECTS?", 1, 0);

            if (args[0] is ZilAtom)
            {
                switch (((ZilAtom)args[0]).StdAtom)
                {
                    case StdAtom.DEFINED:
                        ctx.ZEnvironment.ObjectOrdering = ObjectOrdering.Defined;
                        return args[0];
                    case StdAtom.ROOMS_FIRST:
                        ctx.ZEnvironment.ObjectOrdering = ObjectOrdering.RoomsFirst;
                        return args[0];
                    case StdAtom.ROOMS_LAST:
                        ctx.ZEnvironment.ObjectOrdering = ObjectOrdering.RoomsLast;
                        return args[0];

                    case StdAtom.FIRST:
                        for (int i = 1; i < args.Length; i++)
                        {
                            atom = args[i] as ZilAtom;
                            if (atom != null)
                                ctx.ZEnvironment.FirstObjects.Add(atom);
                        }
                        return args[0];
                    case StdAtom.LAST:
                        for (int i = 1; i < args.Length; i++)
                        {
                            atom = args[i] as ZilAtom;
                            if (atom != null)
                                ctx.ZEnvironment.LastObjects.Add(atom);
                        }
                        return args[0];
                }
            }

            throw new InterpreterError("ORDER-OBJECTS?: first arg must be DEFINED, ROOMS-FIRST, ROOMS-LAST, FIRST, or LAST");
        }

        [Subr("ORDER-TREE?")]
        public static ZilObject ORDER_TREE_P(Context ctx, ZilObject[] args)
        {
            if (args.Length != 1)
                throw new InterpreterError(null, "ORDER-TREE?", 1, 1);

            if (args[0] is ZilAtom)
            {
                switch (((ZilAtom)args[0]).StdAtom)
                {
                    case StdAtom.REVERSE_DEFINED:
                        ctx.ZEnvironment.TreeOrdering = TreeOrdering.ReverseDefined;
                        return args[0];
                }
            }

            throw new InterpreterError("ORDER-TREE?: first arg must be REVERSE-DEFINED");
        }

        [Subr("ORDER-FLAGS?")]
        public static ZilObject ORDER_FLAGS_P(Context ctx, ZilObject[] args)
        {
            if (args.Length < 2)
                throw new InterpreterError(null, "ORDER-FLAGS?", 2, 0);

            var atom = args[0] as ZilAtom;
            if (atom == null || atom.StdAtom != StdAtom.LAST)
                throw new InterpreterError("ORDER-FLAGS?: first arg must be LAST");

            for (int i = 1; i < args.Length; i++)
            {
                atom = args[i] as ZilAtom;
                if (atom == null)
                    throw new InterpreterError("ORDER-FLAGS?: all args must be atoms");

                ctx.ZEnvironment.FlagsOrderedLast.Add(atom);
            }

            return args[0];
        }

        [FSubr("TELL-TOKENS")]
        public static ZilObject TELL_TOKENS(Context ctx, ZilObject[] args)
        {
            ctx.ZEnvironment.TellPatterns.Clear();
            return ADD_TELL_TOKENS(ctx, args);
        }

        [FSubr("ADD-TELL-TOKENS")]
        public static ZilObject ADD_TELL_TOKENS(Context ctx, ZilObject[] args)
        {
            ctx.ZEnvironment.TellPatterns.AddRange(TellPattern.Parse(args, ctx));
            return ctx.TRUE;
        }

        [Subr]
        public static ZilObject ZSTART(Context ctx, ZilObject[] args)
        {
            if (args.Length != 1)
                throw new InterpreterError(null, "ZSTART", 1, 1);

            var atom = args[0] as ZilAtom;
            if (atom == null)
                throw new InterpreterError("ZSTART: arg must be an atom");

            ctx.ZEnvironment.EntryRoutineName = atom;
            return args[0];
        }

        [Subr("BIT-SYNONYM")]
        public static ZilObject BIT_SYNONYM(Context ctx, ZilObject[] args)
        {
            if (args.Length < 2)
                throw new InterpreterError(null, "BIT-SYNONYM", 2, 0);

            if (!args.All(a => a is ZilAtom))
                throw new InterpreterError("BIT-SYNONYM: all args must be atoms");

            var first = (ZilAtom)args[0];
            ZilAtom original;

            if (ctx.ZEnvironment.TryGetBitSynonym(first, out original))
                first = original;

            foreach (var synonym in args.Skip(1).Cast<ZilAtom>())
            {
                if (ctx.GetZVal(synonym) != null)
                    throw new InterpreterError("BIT-SYNONYM: symbol is already defined: " + synonym);

                ctx.ZEnvironment.AddBitSynonym(synonym, first);
            }

            return first;
        }

        #endregion
    }
}
