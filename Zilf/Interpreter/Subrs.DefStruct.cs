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
            public ZilAtom NthFunc, PutFunc, PrintFunc;
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
            public bool NoDefault;
        }

        private const string SDefstructDefaultsDecl =
            @"<LIST ATOM [REST <OR ''NODECL ''NOTYPE ''PRINTTYPE ''CONSTRUCTOR
                                   <LIST ''NTH ATOM> <LIST ''PUT ATOM> <LIST ''START-OFFSET FIX>
                                   <LIST ''PRINTTYPE ATOM> <LIST ''CONSTRUCTOR> <LIST ''INIT-ARGS>>]>";
        private const string SDefstructFieldSpecDecl =
            @"<LIST ATOM ANY [REST <OR FORM LIST ATOM FIX FALSE>]>";

#pragma warning disable 649     // field is never assigned and will always have its default value
        [ZilStructuredParam(StdAtom.LIST)]
        private struct DefStructDefaultsParam
        {
            public ZilAtom BaseType;

            [Either(typeof(EmptyClause), typeof(AtomClause), typeof(FixClause), typeof(VarargsClause))]
            public object[] Clauses;
        }

        [ZilStructuredParam(StdAtom.FORM)]
        private struct EmptyClause
        {
            [Decl("'QUOTE")]
            public ZilAtom QuoteAtom;
            [Decl("<OR 'NODECL 'NOTYPE 'PRINTTYPE 'CONSTRUCTOR>")]
            public ZilAtom Atom;

            public StdAtom ClauseType
            {
                get { return Atom.StdAtom; }
            }
        }

        [ZilStructuredParam(StdAtom.LIST)]
        private struct AtomClause
        {
            [Decl("<OR ''NTH ''PUT ''PRINTTYPE>")]
            public ZilForm Form;
            public ZilAtom Atom;

            public StdAtom ClauseType
            {
                get { return ((ZilAtom)Form.Rest.First).StdAtom; }
            }
        }

        [ZilStructuredParam(StdAtom.LIST)]
        private struct FixClause
        {
            [Decl("''START-OFFSET")]
            public ZilForm Form;
            public int Fix;

            public StdAtom ClauseType
            {
                get { return ((ZilAtom)Form.Rest.First).StdAtom; }
            }
        }

        [ZilStructuredParam(StdAtom.LIST)]
        private struct VarargsClause
        {
            [Decl("<OR ''CONSTRUCTOR ''INIT-ARGS>")]
            public ZilForm Form;
            public ZilObject[] Body;

            public StdAtom ClauseType
            {
                get { return ((ZilAtom)Form.Rest.First).StdAtom; }
            }
        }
#pragma warning restore 649

        [FSubr]
        public static ZilObject DEFSTRUCT(Context ctx, ZilAtom name,
            [Either(typeof(ZilAtom), typeof(DefStructDefaultsParam))]
            object baseTypeOrDefaults,
            [Required, Decl("<LIST [REST " + SDefstructFieldSpecDecl + "]>")]
            ZilObject[] fieldSpecs)
        {
            SubrContracts(ctx);

            // new type name
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

            if (baseTypeOrDefaults is ZilAtom)
            {
                baseType = (ZilAtom)baseTypeOrDefaults;
            }
            else if (baseTypeOrDefaults is DefStructDefaultsParam)
            {
                var defaultsParam = (DefStructDefaultsParam)baseTypeOrDefaults;
                baseType = defaultsParam.BaseType;
                ParseDefStructDefaults(ctx, defaultsParam, ref defaults);
            }
            else
            {
                // shouldn't get here
                throw new NotImplementedException();
            }

            if (!ctx.IsRegisteredType(baseType))
            {
                throw new InterpreterError("DEFSTRUCT: unrecognized base type: " + baseType);
            }

            // field definitions
            var fields = new List<DefStructField>();
            var offset = defaults.StartOffset;
            foreach (var fieldSpec in fieldSpecs)
            {
                fields.Add(ParseDefStructField(ctx, defaults, ref offset, fieldSpec));
            }

            if (!defaults.SuppressType)
            {
                // register the type
                ctx.RegisterType(name, ctx.GetTypePrim(baseType));
            }

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

            // define constructor macro
            if (!defaults.SuppressDefaultCtor)
            {
                var ctorMacroDef = MakeDefstructCtorMacro(ctx, name, baseType, fields, unparsedInitArgs, defaults.StartOffset);
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
                if (defaults.CustomCtorSpec.IsEmpty || defaults.CustomCtorSpec.Rest.IsEmpty)
                    throw new InterpreterError("DEFSTRUCT: not enough elements in 'CONSTRUCTOR spec");

                var ctorName = defaults.CustomCtorSpec.First as ZilAtom;
                if (ctorName == null)
                    throw new InterpreterError("DEFSTRUCT: element after 'CONSTRUCTOR must be an atom");

                var argspecList = defaults.CustomCtorSpec.Rest.First as ZilList;
                if (argspecList == null || argspecList.GetTypeAtom(ctx).StdAtom != StdAtom.LIST)
                    throw new InterpreterError("DEFSTRUCT: second element after 'CONSTRUCTOR must be an argument list");

                var argspec = new ArgSpec(ctorName, null, argspecList);
                var ctorMacroDef = MakeDefstructCustomCtorMacro(ctx, ctorName, name, baseType, fields, unparsedInitArgs, defaults.StartOffset, argspec);

                var oldFileName = ctx.CurrentFile;
                try
                {
                    ctx.CurrentFile = string.Format("<constructor {0} for DEFSTRUCT {1}>", ctorName, name);
                    Program.Evaluate(ctx, ctorMacroDef, true);
                }
                finally
                {
                    ctx.CurrentFile = oldFileName;
                }
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

            // set PRINTTYPE
            if (defaults.PrintFunc != null)
            {
                // annoyingly, the argument can be an atom naming a function that hasn't been defined yet
                var printFuncAtom = defaults.PrintFunc as ZilAtom;
                if (printFuncAtom != null)
                {
                    var handler = ctx.GetGlobalVal(printFuncAtom);
                    if (handler == null)
                    {
                        // #FUNCTION ((X "AUX" (D ,printFuncAtom)) <PRINTTYPE name .D> <APPLY .D .X>)
                        var xAtom = ctx.GetStdAtom(StdAtom.X);
                        var dAtom = ctx.GetStdAtom(StdAtom.D);
                        handler = new ZilFunction(
                            null,
                            null,
                            new ZilObject[]
                            {
                                xAtom,
                                ZilString.FromString("AUX"),
                                new ZilList(new ZilObject[]
                                {
                                    dAtom,
                                    new ZilForm(new ZilObject[] { ctx.GetStdAtom(StdAtom.GVAL), printFuncAtom }),
                                }),
                            },
                            new ZilObject[]
                            {
                                new ZilForm(new ZilObject[]
                                {
                                    ctx.GetStdAtom(StdAtom.PRINTTYPE),
                                    name,
                                    new ZilForm(new ZilObject[] { ctx.GetStdAtom(StdAtom.LVAL), dAtom }),
                                }),
                                new ZilForm(new ZilObject[]
                                {
                                    ctx.GetStdAtom(StdAtom.APPLY),
                                    new ZilForm(new ZilObject[] { ctx.GetStdAtom(StdAtom.LVAL), dAtom }),
                                    new ZilForm(new ZilObject[] { ctx.GetStdAtom(StdAtom.LVAL), xAtom }),
                                })
                            });
                    }

                    ctx.SetPrintType(name, handler);
                }
            }

            return name;
        }

        private static string MakeDefstructCustomCtorMacro(Context ctx, ZilAtom ctorName, ZilAtom typeName, ZilAtom baseType,
            List<DefStructField> fields, string unparsedInitArgs, int startOffset, ArgSpec argspec)
        {
            Contract.Requires(typeName != null);
            Contract.Requires(baseType != null);
            Contract.Requires(fields != null);
            Contract.Requires(unparsedInitArgs != null);

            // {0} = constructor name
            // {1} = type name
            // {2} = argspec
            // {3} = field count
            // {4} = base constructor atom
            // {5} = unparsed INIT-ARGS, or empty string
            // {6} = PUT statements for fields
            const string SMacroTemplate = @"
<DEFMAC {0} {2}
    <BIND ((RESULT-INIT <IVECTOR {3} <>>))
        {6}
        <FORM CHTYPE <FORM {4} {5} !.RESULT-INIT> {1}>>>";

            var remainingFields = fields.ToDictionary(f => f.Name);

            var resultInitializers = new StringBuilder();
            foreach (var arg in argspec)
            {
                // NOTE: we don't handle NoDefault ('NONE) here because this ctor allocates a new object

                // {0} = offset
                // {1} = arg name
                // {2} = default value
                const string SRequiredArgInitializer = "<PUT .RESULT-INIT {0} .{1}>";
                const string SOptAuxArgInitializer = "<PUT .RESULT-INIT {0} <COND (<ASSIGNED? {1}> .{1}) (T {2})>>";

                DefStructField field;
                if (remainingFields.TryGetValue(arg.Atom, out field))
                {
                    remainingFields.Remove(arg.Atom);
                }
                else
                {
                    continue;
                }

                // generate code
                switch (arg.Type)
                {
                    case ArgItem.ArgType.Required:
                        resultInitializers.AppendFormat(
                            SRequiredArgInitializer,
                            field.Offset - startOffset + 1,
                            arg.Atom.ToStringContext(ctx, false),
                            field.Default == null ? UnparsedDefaultForDecl(ctx, field.Decl) : field.Default.ToStringContext(ctx, false));
                        break;

                    case ArgItem.ArgType.Optional:
                    case ArgItem.ArgType.Auxiliary:
                        resultInitializers.AppendFormat(
                            SOptAuxArgInitializer,
                            field.Offset - startOffset + 1,
                            arg.Atom.ToStringContext(ctx, false),
                            field.Default == null ? UnparsedDefaultForDecl(ctx, field.Decl) : field.Default.ToStringContext(ctx, false));
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }

            foreach (var field in remainingFields.Values)
            {
                if (field.Default == null)
                    continue;

                // {0} = offset
                // {1} = default value
                const string SOmittedFieldInitializer = "<PUT .RESULT-INIT {0} {1}>";
                resultInitializers.AppendFormat(
                    SOmittedFieldInitializer,
                    field.Offset - startOffset + 1,
                    field.Default.ToStringContext(ctx, false));
            }

            return string.Format(
                SMacroTemplate,
                ctorName.ToStringContext(ctx, false),
                typeName.ToStringContext(ctx, false),
                argspec.ToString(zo => zo.ToStringContext(ctx, false)),
                fields.Count,
                baseType.ToStringContext(ctx, false),
                unparsedInitArgs,
                resultInitializers);
        }

        private static string UnparsedDefaultForDecl(Context ctx, ZilObject decl)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(decl != null);
            Contract.Ensures(Contract.Result<string>() != null);

            foreach (var zo in LikelyDefaults(ctx))
            {
                if (Decl.Check(ctx, zo, decl))
                    return zo.ToStringContext(ctx, false);
            }

            return "<>";
        }

        private static IEnumerable<ZilObject> LikelyDefaults(Context ctx)
        {
            Contract.Requires(ctx != null);

            yield return ctx.FALSE;
            yield return ZilFix.Zero;
            yield return new ZilList(null, null);
            yield return new ZilVector();
            yield return ZilString.FromString("");
            yield return ctx.GetStdAtom(StdAtom.SORRY);
        }

        private static string MakeDefstructCtorMacro(Context ctx, ZilAtom name, ZilAtom baseType, List<DefStructField> fields,
            string unparsedInitArgs, int startOffset)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(name != null);
            Contract.Requires(baseType != null);
            Contract.Requires(fields != null);
            Contract.Requires(unparsedInitArgs != null);

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
            // {6} = COND clauses for indices ("BOA constructor" mode, PUTting into the temp vector .RESULT-INIT)
            // {7} = COND clauses for index defaults
            // {8} = COND statements for tag defaults ("new object" mode)
            // {9} = expressions returning a FORM or SPLICE for tag defaults ("existing object" mode)
            const string SMacroTemplate = @"
<DEFMAC MAKE-{0} (""ARGS"" A ""AUX"" RESULT-INIT SEEN)
    <COND (<AND <NOT <EMPTY? .A>>
                <=? <1 .A> '<QUOTE {0}>>>
           <SET RESULT-INIT <2 .A>>
           <SET SEEN '()>
           <SET A <REST .A 2>>
           <FORM BIND <LIST <LIST RESULT .RESULT-INIT>>
                 !<MAPF
                     ,LIST
                     <FUNCTION (""AUX"" N V)
                         <COND (<LENGTH? .A 0> <MAPSTOP>)
                               (<LENGTH? .A 1> <ERROR NOT-ENOUGH-ARGS!-ERRORS .A>)>
                         <SET N <1 .A>>
                         <SET V <2 .A>>
                         <SET A <REST .A 2>>
                         <COND {2}
                               (T <ERROR INVALID-DEFSTRUCT-TAG!-ERRORS .N>)>>>
                 !<VECTOR {9}>
                 '.RESULT>)
          (<OR <EMPTY? .A>
               <NOT <TYPE? <1 .A> FORM>>
               <N==? <1 <1 .A>> QUOTE>>
           <SET RESULT-INIT <IVECTOR {1} <>>>
           <BIND ((I 1))
               <MAPF <>
                     <FUNCTION (V)
                         <COND {6}
                               (T <ERROR TOO-MANY-ARGS!-ERRORS .A>)>
                         <SET I <+ .I 1>>>
                     .A>
               <REPEAT ()
                   <COND (<G? .I {1}> <RETURN>)
                         {7}>
                   <SET I <+ .I 1>>>>
           <FORM CHTYPE <FORM {4} {5} !.RESULT-INIT> {0}>)
          (T
           <SET RESULT-INIT <IVECTOR {1} <>>>
           <SET SEEN '()>
           <REPEAT (N V)
               <COND (<LENGTH? .A 0> <RETURN>)
                       (<LENGTH? .A 1> <ERROR NOT-ENOUGH-ARGS!-ERRORS .A>)>
               <SET N <1 .A>>
               <SET V <2 .A>>
               <SET A <REST .A 2>>
               <COND {3}
                       (T <ERROR INVALID-DEFSTRUCT-TAG!-ERRORS .N>)>>
           {8}
           <FORM CHTYPE <FORM {4} {5} !.RESULT-INIT> {0}>)>>
";

            // {0} = tag name
            // {1} = PUT atom
            // {2} = offset in structure (for existing object) or RESULT-INIT (for others)
            // {3} = definition order (1-based)
            // {4} = unparsed default value
            const string SExistingObjectCondClauseTemplate = "(<=? .N '<QUOTE {0}>> <SET SEEN <CONS {0} .SEEN>> <FORM {1} '.RESULT {2} .V>)";
            const string SExistingObjectDefaultTemplate = "<COND (<MEMQ {0} .SEEN> #SPLICE ()) (T <FORM {1} '.RESULT {2} {4}>)>";
            const string SExistingObjectDefaultTemplate_NoDefault = "#SPLICE ()";
            const string SNewObjectCondClauseTemplate = "(<=? .N '<QUOTE {0}>> <SET SEEN <CONS {0} .SEEN>> <PUT .RESULT-INIT {2} .V>)";
            const string SNewObjectDefaultTemplate = "<OR <MEMQ {0} .SEEN> <PUT .RESULT-INIT {2} {4}>>";
            const string SBoaConstructorCondClauseTemplate = "(<=? .I {3}> <PUT .RESULT-INIT {2} .V>)";
            const string SBoaConstructorDefaultClauseTemplate = "(<=? .I {3}> <PUT .RESULT-INIT {2} {4}>)";

            var existingObjectClauses = new StringBuilder();
            var existingObjectDefaults = new StringBuilder();
            var newObjectClauses = new StringBuilder();
            var newObjectDefaults = new StringBuilder();
            var boaConstructorClauses = new StringBuilder();
            var boaConstructorDefaultClauses = new StringBuilder();

            int definitionOrder = 1;
            foreach (var field in fields)
            {
                string unparsedDefault;
                if (field.Default != null)
                {
                    unparsedDefault = field.Default.ToStringContext(ctx, false, true);
                }
                else
                {
                    unparsedDefault = UnparsedDefaultForDecl(ctx, field.Decl);
                }

                existingObjectDefaults.AppendFormat(
                    field.NoDefault ? SExistingObjectDefaultTemplate_NoDefault : SExistingObjectDefaultTemplate,
                    field.Name, field.PutFunc, field.Offset, definitionOrder, unparsedDefault);
                newObjectDefaults.AppendFormat(
                    SNewObjectDefaultTemplate,
                    field.Name, field.PutFunc, field.Offset - startOffset + 1, definitionOrder, unparsedDefault);
                boaConstructorDefaultClauses.AppendFormat(
                    SBoaConstructorDefaultClauseTemplate,
                    field.Name, field.PutFunc, field.Offset - startOffset + 1, definitionOrder, unparsedDefault);

                existingObjectClauses.AppendFormat(
                    SExistingObjectCondClauseTemplate,
                    field.Name, field.PutFunc, field.Offset, definitionOrder, unparsedDefault);
                newObjectClauses.AppendFormat(
                    SNewObjectCondClauseTemplate,
                    field.Name, field.PutFunc, field.Offset - startOffset + 1, definitionOrder, unparsedDefault);
                boaConstructorClauses.AppendFormat(
                    SBoaConstructorCondClauseTemplate,
                    field.Name, field.PutFunc, field.Offset - startOffset + 1, definitionOrder, unparsedDefault);

                definitionOrder++;
            }

            return string.Format(
                SMacroTemplate,
                name.ToStringContext(ctx, false),
                fields.Count,
                existingObjectClauses,
                newObjectClauses,
                baseType.ToStringContext(ctx, false),
                unparsedInitArgs,
                boaConstructorClauses,
                boaConstructorDefaultClauses,
                newObjectDefaults,
                existingObjectDefaults);
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

        private static DefStructField ParseDefStructField(Context ctx, DefStructDefaults defaults, ref int offset, ZilObject fieldSpec)
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

            bool gotDefault = false, gotOffset = false;
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
                            gotOffset = true;
                            break;

                        case StdAtom.NONE:
                            if (gotDefault)
                                throw new InterpreterError("DEFSTRUCT: 'NONE is not allowed after a default field value");
                            result.NoDefault = true;
                            gotDefault = true;
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

            if (!gotOffset)
                offset++;

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
                            defaults.PrintFunc = null;
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
                            defaults.PrintFunc = partList.First as ZilAtom;
                            if (defaults.PrintFunc == null)
                                throw new InterpreterError("DEFSTRUCT: 'PRINTTYPE must be followed by an atom");
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

        private static void ParseDefStructDefaults(Context ctx, DefStructDefaultsParam param, ref DefStructDefaults defaults)
        {
            Contract.Requires(ctx != null);

            foreach (var clause in param.Clauses)
            {
                if (clause is EmptyClause)
                {
                    var ec = (EmptyClause)clause;
                    switch (ec.ClauseType)
                    {
                        case StdAtom.NODECL:
                            // nada
                            break;

                        case StdAtom.NOTYPE:
                            defaults.SuppressType = true;
                            break;

                        case StdAtom.PRINTTYPE:
                            defaults.PrintFunc = null;
                            break;

                        case StdAtom.CONSTRUCTOR:
                            defaults.SuppressDefaultCtor = true;
                            break;

                        default:
                            // shouldn't get here
                            throw new NotImplementedException();
                    }
                }
                else if (clause is AtomClause)
                {
                    var ac = (AtomClause)clause;
                    switch (ac.ClauseType)
                    {
                        case StdAtom.NTH:
                            defaults.NthFunc = ac.Atom;
                            break;

                        case StdAtom.PUT:
                            defaults.PutFunc = ac.Atom;
                            break;

                        case StdAtom.PRINTTYPE:
                            defaults.PrintFunc = ac.Atom;
                            break;

                        default:
                            // shouldn't get here
                            throw new NotImplementedException();
                    }
                }
                else if (clause is FixClause)
                {
                    var fc = (FixClause)clause;
                    switch (fc.ClauseType)
                    {
                        case StdAtom.START_OFFSET:
                            defaults.StartOffset = fc.Fix;
                            break;

                        default:
                            // shouldn't get here
                            throw new NotImplementedException();
                    }
                }
                else if (clause is VarargsClause)
                {
                    var vc = (VarargsClause)clause;
                    var body = new ZilList(vc.Body);
                    switch (vc.ClauseType)
                    {
                        case StdAtom.CONSTRUCTOR:
                            defaults.CustomCtorSpec = body;
                            break;

                        case StdAtom.INIT_ARGS:
                            defaults.InitArgs = body;
                            break;

                        default:
                            // shouldn't get here
                            throw new NotImplementedException();
                    }
                }
                else
                {
                    // shouldn't get here
                    throw new NotImplementedException();
                }
            }
        }
    }
}
