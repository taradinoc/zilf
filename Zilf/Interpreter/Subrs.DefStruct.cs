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
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using Zilf.Interpreter.Values;
using Zilf.Language;

namespace Zilf.Interpreter
{
    static partial class Subrs
    {
        [FSubr("SET-DEFSTRUCT-FILE-DEFAULTS")]
        public static ZilObject SET_DEFSTRUCT_FILE_DEFAULTS(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            var defaults = new ZilList(args);
            ctx.PutProp(ctx.GetStdAtom(StdAtom.DEFSTRUCT), ctx.GetStdAtom(StdAtom.DEFAULT), defaults);
            return defaults;
        }

        private struct DefStructDefaults
        {
            public ZilAtom NthFunc, PutFunc;
            public int StartOffset;
            public bool SuppressType, SuppressDefaultCtor;
            public ZilList CustomCtorSpec;
            public ZilList InitArgs;
        }

        private struct DefStructField
        {
            public ZilAtom Name;
            public ZilAtom NthFunc, PutFunc;
            public int Offset;
            public ZilObject Decl;
            public ZilObject Default;
        }

        [FSubr]
        public static ZilObject DEFSTRUCT(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length < 3)
                throw new InterpreterError("DEFSTRUCT", 3, 0);

            // new type name
            var name = args[0] as ZilAtom;
            if (name == null)
                throw new InterpreterError("DEFSTRUCT: first arg must be an atom");

            if (ctx.IsRegisteredType(name))
                throw new InterpreterError("DEFSTRUCT: type is already registered: " + name);

            // base type, and optional default field settings
            ZilAtom baseType;
            var defaults = new DefStructDefaults
            {
                NthFunc = ctx.GetStdAtom(StdAtom.NTH),
                PutFunc = ctx.GetStdAtom(StdAtom.PUT),
                StartOffset = 1,
            };

            var fileDefaultList = ctx.GetProp(ctx.GetStdAtom(StdAtom.DEFSTRUCT), ctx.GetStdAtom(StdAtom.DEFAULT)) as ZilList;
            if (fileDefaultList != null && fileDefaultList.GetTypeAtom(ctx).StdAtom == StdAtom.LIST)
                ParseDefStructDefaults(ctx, fileDefaultList, ref defaults);

            switch (args[1].GetTypeAtom(ctx).StdAtom)
            {
                case StdAtom.ATOM:
                    baseType = (ZilAtom)args[1];
                    break;

                case StdAtom.LIST:
                    var defaultList = (ZilList)args[1];
                    baseType = defaultList.First as ZilAtom;
                    if (baseType == null)
                        throw new InterpreterError("DEFSTRUCT: first element of defaults list must be a type atom");
                    ParseDefStructDefaults(ctx, defaultList.Rest, ref defaults);
                    break;

                default:
                    throw new InterpreterError("DEFSTRUCT: second arg must be an atom or list");
            }

            if (!ctx.IsRegisteredType(baseType))
            {
                throw new InterpreterError("DEFSTRUCT: unrecognized base type: " + baseType);
            }

            // field definitions
            var fields = new List<DefStructField>();
            var offset = defaults.StartOffset;
            foreach (var fieldSpec in args.Skip(2))
            {
                fields.Add(ParseDefStructField(ctx, defaults, offset++, fieldSpec));
            }

            if (!defaults.SuppressType)
            {
                // register the type
                ctx.RegisterType(name, ctx.GetTypePrim(baseType));
            }

            if (!defaults.SuppressDefaultCtor)
            {
                // define constructor macro

                string unparsedInitArgs;
                if (defaults.InitArgs != null)
                {
                    var sb = new StringBuilder();

                    foreach (var zo in defaults.InitArgs)
                    {
                        if (sb.Length > 0)
                            sb.Append(' ');

                        sb.Append(zo.ToStringContext(ctx, false, true));
                    }

                    unparsedInitArgs = sb.ToString();
                }
                else
                {
                    unparsedInitArgs = "";
                }

                var ctorMacroDef = MakeDefstructCtorMacro(name, baseType, fields, unparsedInitArgs);
                var oldFileName = ctx.CurrentFile;
                try
                {
                    ctx.CurrentFile = string.Format("<constructor for DEFSTRUCT {0}>", name);
                    Program.Evaluate(ctx, ctorMacroDef, true);
                }
                finally
                {
                    ctx.CurrentFile = oldFileName;
                }
            }

            if (defaults.CustomCtorSpec != null)
            {
                //XXX define custom constructor macro based on parameters in spec
            }

            // define field access macros
            foreach (var field in fields)
            {
                var accessMacroDef = MakeDefstructAccessMacro(name, field);
                var oldFileName = ctx.CurrentFile;
                try
                {
                    ctx.CurrentFile = string.Format("<accessor for field {0} of DEFSTRUCT {1}>", field.Name, name);
                    Program.Evaluate(ctx, accessMacroDef, true);
                }
                finally
                {
                    ctx.CurrentFile = oldFileName;
                }
            }

            return name;
        }

        private static string MakeDefstructCtorMacro(ZilAtom name, ZilAtom baseType, List<DefStructField> fields, string unparsedInitArgs)
        {
            Contract.Requires(name != null);
            Contract.Requires(baseType != null);
            Contract.Requires(fields != null);

            // the MAKE-[STRUCT] macro can be called with a parameter telling it to stuff values into an existing object:
            //   <MAKE-FOO 'FOO .MYFOO 'FOO-X 123>
            // in which case we want to return:
            //   <BIND ((RESULT .MYFOO)) <PUT .RESULT 1 123> .RESULT>

            // but without that parameter, we stuff the values into a temporary vector now, and
            // build a call to the base constructor:
            //   <CHTYPE <TABLE 123> FOO>

            // {0} = name
            // {1} = field count
            // {2} = COND clauses for tags ("existing object" mode, returning a FORM that PUTs into .RESULT)
            // {3} = COND clauses for tags ("new object" mode, PUTting into the temp vector .RESULT-INIT)
            // {4} = base constructor atom
            // {5} = unparsed INIT-ARGS, or empty string
            const string SMacroTemplate = @"
<DEFMAC MAKE-{0} (""ARGS"" A ""AUX"" RESULT-INIT)
    <COND (<AND <NOT <EMPTY? .A>> <=? <1 .A> '<QUOTE {0}>>>
           <SET RESULT-INIT <2 .A>>
           <SET A <REST .A 2>>
           <FORM BIND <LIST <LIST RESULT .RESULT-INIT>>
                 !<MAPF
                     ,LIST
                     <FUNCTION (""AUX"" N V)
                         <COND (<LENGTH? .A 1> <MAPSTOP>)>
                         <SET N <1 .A>>
                         <SET V <2 .A>>
                         <SET A <REST .A 2>>
                         <COND {2}
                               (T <ERROR INVALID-DEFSTRUCT-TAG!-ERRORS .N>)>>>
                 '.RESULT>)
          (T
           <SET RESULT-INIT <IVECTOR {1} <>>>
           <REPEAT ()
               <COND (<LENGTH? .A 1> <RETURN>)>
               <SET N <1 .A>>
               <SET V <2 .A>>
               <SET A <REST .A 2>>
               <COND {3}
                     (T <ERROR INVALID-DEFSTRUCT-TAG!-ERRORS .N>)>>
           <FORM CHTYPE <FORM {4} {5} !.RESULT-INIT> {0}>)>>
";

            // {0} = tag name
            // {1} = PUT atom
            // {2} = offset
            const string SExistingObjectCondClauseTemplate = "(<=? .N '<QUOTE {0}>> <FORM {1} '.RESULT {2} .V>)";
            const string SNewObjectCondClauseTemplate = "(<=? .N '<QUOTE {0}>> <PUT .RESULT-INIT {2} .V>)";

            var existingObjectClauses = new StringBuilder();
            var newObjectClauses = new StringBuilder();

            foreach (var field in fields)
            {
                // TODO: use default field values?

                existingObjectClauses.AppendFormat(
                    SExistingObjectCondClauseTemplate,
                    field.Name, field.PutFunc, field.Offset);
                newObjectClauses.AppendFormat(
                    SNewObjectCondClauseTemplate,
                    field.Name, field.PutFunc, field.Offset);
            }

            return string.Format(
                SMacroTemplate,
                name,
                fields.Count,
                existingObjectClauses,
                newObjectClauses,
                baseType,
                unparsedInitArgs);
        }

        private static string MakeDefstructAccessMacro(ZilAtom structName, DefStructField field)
        {
            Contract.Requires(structName != null);

            // {0} = field name
            // {1} = struct name
            // {2} = PUT atom
            // {3} = NTH atom
            // {4} = offset
            const string STemplate = @"
<DEFMAC {0} ('S ""OPT"" 'NV)
    <COND (<ASSIGNED? NV>
           <FORM {2} <CHTYPE [.S {1}] ADECL> {4} .NV>)
          (T
           <FORM {3} <CHTYPE [.S {1}] ADECL> {4}>)>>
";

            return string.Format(
                STemplate,
                field.Name,
                structName,
                field.PutFunc,
                field.NthFunc,
                field.Offset);
        }

        private static DefStructField ParseDefStructField(Context ctx, DefStructDefaults defaults, int offset, ZilObject fieldSpec)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(fieldSpec != null);

            var fieldList = fieldSpec as ZilList;

            if (fieldList == null || fieldList.GetTypeAtom(ctx).StdAtom != StdAtom.LIST)
                throw new InterpreterError("DEFSTRUCT: field definitions must be lists");

            var fieldName = fieldList.First as ZilAtom;
            if (fieldName == null)
                throw new InterpreterError("DEFSTRUCT: field names must be atoms");

            fieldList = fieldList.Rest;

            ZilObject fieldDecl;
            if (fieldList == null || (fieldDecl = fieldList.First) == null)
                throw new InterpreterError("DEFSTRUCT: field name must be followed by a decl");

            var result = new DefStructField()
            {
                Decl = fieldDecl,
                Name = fieldName,
                NthFunc = defaults.NthFunc,
                Offset = offset,
                PutFunc = defaults.PutFunc,
            };

            bool gotDefault = false;
            var quoteAtom = ctx.GetStdAtom(StdAtom.QUOTE);

            for (fieldList = fieldList.Rest; !fieldList.IsEmpty; fieldList = fieldList.Rest)
            {
                var item = fieldList.First;
                bool quoted = (item is ZilForm && ((ZilForm)item).First == quoteAtom);

                if (quoted && ((ZilForm)item).Rest.First is ZilFix)
                {
                    item = ((ZilForm)item).Rest.First;
                    quoted = false;
                }

                if (quoted)
                {
                    var tag = ((ZilForm)item).Rest.First as ZilAtom;
                    if (tag == null)
                        throw new InterpreterError("DEFSTRUCT: quoted value in field definition must be an atom or fix");

                    switch (tag.StdAtom)
                    {
                        case StdAtom.NTH:
                            fieldList = fieldList.Rest;
                            result.NthFunc = fieldList.First as ZilAtom;
                            if (result.NthFunc == null)
                                throw new InterpreterError("DEFSTRUCT: 'NTH must be followed by an atom");
                            break;

                        case StdAtom.PUT:
                            fieldList = fieldList.Rest;
                            result.PutFunc = fieldList.First as ZilAtom;
                            if (result.PutFunc == null)
                                throw new InterpreterError("DEFSTRUCT: 'PUT must be followed by an atom");
                            break;

                        case StdAtom.OFFSET:
                            fieldList = fieldList.Rest;
                            var fix = fieldList.First as ZilFix;
                            if (fix == null)
                                throw new InterpreterError("DEFSTRUCT: 'OFFSET must be followed by a FIX");
                            result.Offset = fix.Value;
                            break;

                        case StdAtom.NONE:
                            // nada
                            break;

                        default:
                            throw new InterpreterError("DEFSTRUCT: unrecognized tag in field definition: " + tag);
                    }
                }
                else if (!gotDefault)
                {
                    result.Default = item;
                    gotDefault = true;
                }
                else
                {
                    throw new InterpreterError("DEFSTRUCT: unrecognized non-quoted value in field definition: " + item);
                }
            }

            return result;
        }

        private static void ParseDefStructDefaults(Context ctx, ZilList fileDefaults, ref DefStructDefaults defaults)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(fileDefaults != null);

            ZilAtom tag;
            var quoteAtom = ctx.GetStdAtom(StdAtom.QUOTE);

            foreach (var part in fileDefaults)
            {
                var partForm = part as ZilForm;
                if (partForm != null && partForm.First == quoteAtom && (tag = partForm.Rest.First as ZilAtom) != null)
                {
                    switch (tag.StdAtom)
                    {
                        case StdAtom.NODECL:
                            // nada
                            break;

                        case StdAtom.NOTYPE:
                            defaults.SuppressType = true;
                            break;

                        case StdAtom.PRINTTYPE:
                            // nada
                            break;

                        case StdAtom.CONSTRUCTOR:
                            defaults.SuppressDefaultCtor = true;
                            break;

                        default:
                            throw new NotImplementedException("DEFSTRUCT: unrecognized part in defaults section: " + tag);
                    }
                }
                else
                {
                    var partList = part as ZilList;
                    if (partList == null || partList.GetTypeAtom(ctx).StdAtom != StdAtom.LIST)
                        throw new InterpreterError("DEFSTRUCT: parts of defaults section must be quoted atoms or lists");

                    var first = partList.First as ZilForm;
                    if (first == null || first.First != quoteAtom || (tag = first.Rest.First as ZilAtom) == null)
                        throw new InterpreterError("DEFSTRUCT: lists in defaults section must start with a quoted atom");

                    switch (tag.StdAtom)
                    {
                        case StdAtom.NTH:
                            partList = partList.Rest;
                            defaults.NthFunc = partList.First as ZilAtom;
                            if (defaults.NthFunc == null)
                                throw new InterpreterError("DEFSTRUCT: 'NTH must be followed by an atom");
                            break;

                        case StdAtom.PUT:
                            partList = partList.Rest;
                            defaults.PutFunc = partList.First as ZilAtom;
                            if (defaults.PutFunc == null)
                                throw new InterpreterError("DEFSTRUCT: 'PUT must be followed by an atom");
                            break;

                        case StdAtom.START_OFFSET:
                            partList = partList.Rest;
                            ZilFix fix = partList.First as ZilFix;
                            if (fix == null)
                                throw new InterpreterError("DEFSTRUCT: 'START-OFFSET must be followed by a FIX");
                            defaults.StartOffset = fix.Value;
                            break;

                        case StdAtom.PRINTTYPE:
                            partList = partList.Rest;
                            // TODO: implement PRINTTYPE
                            break;

                        case StdAtom.CONSTRUCTOR:
                            partList = partList.Rest;
                            defaults.CustomCtorSpec = partList;
                            break;

                        case StdAtom.INIT_ARGS:
                            partList = partList.Rest;
                            defaults.InitArgs = partList;
                            break;

                        default:
                            throw new InterpreterError("DEFSTRUCT: unrecognized tag in defaults section: " + tag);
                    }
                }
            }
        }
    }
}
