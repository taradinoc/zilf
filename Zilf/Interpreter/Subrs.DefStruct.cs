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
using Zilf.Diagnostics;

namespace Zilf.Interpreter
{
    static partial class Subrs
    {
        // TODO: use ArgDecoder
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
            public bool SuppressType, SuppressDefaultCtor, SuppressDecl;
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

        public static class DefStructParams
        {
#pragma warning disable 649     // field is never assigned and will always have its default value
            [ZilStructuredParam(StdAtom.LIST)]
            public struct DefaultsList
            {
                public ZilAtom BaseType;

                [Either(typeof(NullaryDefaultClause), typeof(AtomDefaultClause),
                    typeof(FixDefaultClause), typeof(VarargsDefaultClause))]
                public object[] Clauses;
            }

            [ZilStructuredParam(StdAtom.FORM)]
            public struct NullaryDefaultClause
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
            public struct AtomDefaultClause
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
            public struct FixDefaultClause
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
            public struct VarargsDefaultClause
            {
                [Decl("<OR ''CONSTRUCTOR ''INIT-ARGS>")]
                public ZilForm Form;
                public ZilObject[] Body;

                public StdAtom ClauseType
                {
                    get { return ((ZilAtom)Form.Rest.First).StdAtom; }
                }
            }

            [ZilStructuredParam(StdAtom.LIST)]
            public struct FieldSpecList
            {
                public ZilAtom Name;
                public ZilObject Decl;

                [Either(typeof(NullaryFieldSequence), typeof(AtomFieldSequence),
                    typeof(FixFieldSequence), typeof(ZilObject))]
                public object[] Parts;
            }

            [ZilSequenceParam]
            public struct NullaryFieldSequence
            {
                [Decl("''NONE")]
                public ZilForm Form;

                public StdAtom ClauseType
                {
                    get { return ((ZilAtom)Form.Rest.First).StdAtom; }
                }
            }

            [ZilSequenceParam]
            public struct AtomFieldSequence
            {
                [Decl("<OR ''NTH ''PUT>")]
                public ZilForm Form;
                public ZilAtom Atom;

                public StdAtom ClauseType
                {
                    get { return ((ZilAtom)Form.Rest.First).StdAtom; }
                }
            }

            [ZilSequenceParam]
            public struct FixFieldSequence
            {
                [Decl("''OFFSET")]
                public ZilForm Form;
                public int Fix;

                public StdAtom ClauseType
                {
                    get { return ((ZilAtom)Form.Rest.First).StdAtom; }
                }
            }
        }
#pragma warning restore 649

        [FSubr]
        public static ZilObject DEFSTRUCT(Context ctx, ZilAtom name,
            [Either(typeof(ZilAtom), typeof(DefStructParams.DefaultsList))]
            object baseTypeOrDefaults,
            [Required]
            DefStructParams.FieldSpecList[] fieldSpecs)
        {
            SubrContracts(ctx);

            // new type name
            if (ctx.IsRegisteredType(name))
                throw new InterpreterError(InterpreterMessages._0_Type_Is_Already_Registered_1, "DEFSTRUCT", name);

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
            else if (baseTypeOrDefaults is DefStructParams.DefaultsList)
            {
                var defaultsParam = (DefStructParams.DefaultsList)baseTypeOrDefaults;
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
                throw new InterpreterError(InterpreterMessages._0_Unrecognized_Base_Type_1, "DEFSTRUCT", baseType);
            }

            // field definitions
            var fields = new List<DefStructField>();
            var offset = defaults.StartOffset;
            foreach (var fieldSpec in fieldSpecs)
            {
                fields.Add(ParseDefStructField(ctx, defaults, ref offset, fieldSpec));
            }

            var decl = MakeDefstructDecl(ctx, baseType, defaults, fields);

            if (!defaults.SuppressType)
            {
                // register the type
                ctx.RegisterType(name, ctx.GetTypePrim(baseType));

                if (!defaults.SuppressDecl)
                    ctx.PutProp(name, ctx.GetStdAtom(StdAtom.DECL), decl);
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
                    throw new InterpreterError(InterpreterMessages._0_Not_Enough_Elements_In_CONSTRUCTOR_Spec, "DEFSTRUCT");

                var ctorName = defaults.CustomCtorSpec.First as ZilAtom;
                if (ctorName == null)
                    throw new InterpreterError(InterpreterMessages._0_Element_After_CONSTRUCTOR_Must_Be_An_Atom, "DEFSTRUCT");

                var argspecList = defaults.CustomCtorSpec.Rest.First as ZilList;
                if (argspecList == null || argspecList.GetTypeAtom(ctx).StdAtom != StdAtom.LIST)
                    throw new InterpreterError(InterpreterMessages._0_Second_Element_After_CONSTRUCTOR_Must_Be_An_Argument_List, "DEFSTRUCT");

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
                var accessMacroDef = MakeDefstructAccessMacro(ctx, name, defaults, field);
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
                            null,
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

        private static ZilObject MakeDefstructDecl(Context ctx, ZilAtom baseType, DefStructDefaults defaults, List<DefStructField> fields)
        {
            var parts = new List<ZilObject>(1 + fields.Count);

            parts.Add(new ZilForm(new[] { ctx.GetStdAtom(StdAtom.PRIMTYPE), TYPEPRIM(ctx, baseType) }));
            parts.AddRange(fields.Select(f => f.Decl));

            return new ZilSegment(new ZilForm(parts));
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
                try
                {
                    if (Decl.Check(ctx, zo, decl))
                        return zo.ToStringContext(ctx, false);
                }
                catch (InterpreterError)
                {
                    // decl might be invalid if the struct references a NEWTYPE that hasn't been defined yet
                    break;
                }
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

        private static string MakeDefstructAccessMacro(Context ctx, ZilAtom structName, DefStructDefaults defaults,
            DefStructField field)
        {
            Contract.Requires(structName != null);

            // {0} = field name
            // {1} = struct name
            // {2} = PUT atom
            // {3} = NTH atom
            // {4} = offset
            // {5} = field decl
            const string SFullCheckTemplate = @"
<DEFMAC {0} ('S ""OPT"" 'NV)
    <COND (<ASSIGNED? NV>
           <FORM {2} <CHTYPE [.S {1}] ADECL> {4} <CHTYPE [.NV <QUOTE {5}>] ADECL>>)
          (T
           <CHTYPE [<FORM {3} <CHTYPE [.S {1}] ADECL> {4}> <QUOTE {5}>] ADECL>)>>
";
            const string SFieldCheckTemplate = @"
<DEFMAC {0} ('S ""OPT"" 'NV)
    <COND (<ASSIGNED? NV>
           <FORM {2} .S {4} <CHTYPE [.NV <QUOTE {5}>] ADECL>>)
          (T
           <CHTYPE [<FORM {3} .S {4}> <QUOTE {5}>] ADECL>)>>
";
            const string SNoCheckTemplate = @"
<DEFMAC {0} ('S ""OPT"" 'NV)
    <COND (<ASSIGNED? NV>
           <FORM {2} .S {4} .NV>)
          (T
           <FORM {3} .S {4}>)>>
";

            string template;
            if (defaults.SuppressDecl)
                template = SNoCheckTemplate;
            else if (defaults.SuppressType)
                template = SFieldCheckTemplate;
            else
                template = SFullCheckTemplate;

            return string.Format(
                template,
                field.Name,
                structName,
                field.PutFunc,
                field.NthFunc,
                field.Offset,
                field.Decl.ToStringContext(ctx, false));
        }

        private static DefStructField ParseDefStructField(Context ctx, DefStructDefaults defaults, ref int offset,
            DefStructParams.FieldSpecList fieldSpec)
        {
            Contract.Requires(ctx != null);

            var result = new DefStructField()
            {
                Decl = fieldSpec.Decl,
                Name = fieldSpec.Name,
                NthFunc = defaults.NthFunc,
                Offset = offset,
                PutFunc = defaults.PutFunc,
            };

            bool gotDefault = false, gotOffset = false;

            foreach (var part in fieldSpec.Parts)
            {
                if (part is DefStructParams.AtomFieldSequence)
                {
                    var af = (DefStructParams.AtomFieldSequence)part;

                    switch (af.ClauseType)
                    {
                        case StdAtom.NTH:
                            result.NthFunc = af.Atom;
                            break;

                        case StdAtom.PUT:
                            result.PutFunc = af.Atom;
                            break;

                        default:
                            // shouldn't get here
                            throw new NotImplementedException();
                    }
                }
                else if (part is DefStructParams.FixFieldSequence)
                {
                    var ff = (DefStructParams.FixFieldSequence)part;

                    switch (ff.ClauseType)
                    {
                        case StdAtom.OFFSET:
                            result.Offset = ff.Fix;
                            gotOffset = true;
                            break;

                        default:
                            // shouldn't get here
                            throw new NotImplementedException();
                    }
                }
                else if (part is DefStructParams.NullaryFieldSequence)
                {
                    var nf = (DefStructParams.NullaryFieldSequence)part;

                    switch (nf.ClauseType)
                    {
                        case StdAtom.NONE:
                            if (gotDefault)
                                throw new InterpreterError(InterpreterMessages._0_NONE_Is_Not_Allowed_After_A_Default_Field_Value, "DEFSTRUCT");
                            result.NoDefault = true;
                            gotDefault = true;
                            break;

                        default:
                            // shouldn't get here
                            throw new NotImplementedException();
                    }
                }
                else if (part is ZilObject && !gotDefault)
                {
                    result.Default = (ZilObject)part;
                    gotDefault = true;
                }
                else
                {
                    throw new InterpreterError(InterpreterMessages._0_Unrecognized_Nonquoted_Value_In_Field_Definition_1, "DEFSTRUCT", part);
                }
            }

            if (!gotOffset)
                offset++;

            return result;
        }

        // TODO: delete once SET-DEFSTRUCT-FILE-DEFAULTS is using ArgDecoder
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
                            defaults.SuppressDecl = true;
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
                        throw new InterpreterError(InterpreterMessages._0_Parts_Of_Defaults_Section_Must_Be_Quoted_Atoms_Or_Lists, "DEFSTRUCT");

                    var first = partList.First as ZilForm;
                    if (first == null || first.First != quoteAtom || (tag = first.Rest.First as ZilAtom) == null)
                        throw new InterpreterError(InterpreterMessages._0_Lists_In_Defaults_Section_Must_Start_With_A_Quoted_Atom, "DEFSTRUCT");

                    switch (tag.StdAtom)
                    {
                        case StdAtom.NTH:
                            partList = partList.Rest;
                            defaults.NthFunc = partList.First as ZilAtom;
                            if (defaults.NthFunc == null)
                                throw new InterpreterError(InterpreterMessages._0_1_Must_Be_Followed_By_An_Atom, "DEFSTRUCT", first);
                            break;

                        case StdAtom.PUT:
                            partList = partList.Rest;
                            defaults.PutFunc = partList.First as ZilAtom;
                            if (defaults.PutFunc == null)
                                throw new InterpreterError(InterpreterMessages._0_1_Must_Be_Followed_By_An_Atom, "DEFSTRUCT", first);
                            break;

                        case StdAtom.START_OFFSET:
                            partList = partList.Rest;
                            ZilFix fix = partList.First as ZilFix;
                            if (fix == null)
                                throw new InterpreterError(InterpreterMessages._0_STARTOFFSET_Must_Be_Followed_By_A_FIX, "DEFSTRUCT");
                            defaults.StartOffset = fix.Value;
                            break;

                        case StdAtom.PRINTTYPE:
                            partList = partList.Rest;
                            defaults.PrintFunc = partList.First as ZilAtom;
                            if (defaults.PrintFunc == null)
                                throw new InterpreterError(InterpreterMessages._0_1_Must_Be_Followed_By_An_Atom, "DEFSTRUCT", first);
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
                            throw new InterpreterError(InterpreterMessages._0_Unrecognized_Tag_In_Defaults_Section_1, "DEFSTRUCT", first);
                    }
                }
            }
        }

        private static void ParseDefStructDefaults(Context ctx, DefStructParams.DefaultsList param, ref DefStructDefaults defaults)
        {
            Contract.Requires(ctx != null);

            foreach (var clause in param.Clauses)
            {
                if (clause is DefStructParams.NullaryDefaultClause)
                {
                    var ec = (DefStructParams.NullaryDefaultClause)clause;
                    switch (ec.ClauseType)
                    {
                        case StdAtom.NODECL:
                            defaults.SuppressDecl = true;
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
                else if (clause is DefStructParams.AtomDefaultClause)
                {
                    var ac = (DefStructParams.AtomDefaultClause)clause;
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
                else if (clause is DefStructParams.FixDefaultClause)
                {
                    var fc = (DefStructParams.FixDefaultClause)clause;
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
                else if (clause is DefStructParams.VarargsDefaultClause)
                {
                    var vc = (DefStructParams.VarargsDefaultClause)clause;
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
