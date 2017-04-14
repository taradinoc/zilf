/* Copyright 2010-2017 Jesse McGrew
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
using Zilf.Common;

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
            ctx.CurrentFile.DefStructDefaults = defaults;
            return defaults;
        }

        struct DefStructDefaults
        {
            public ZilAtom NthFunc, PutFunc, PrintFunc;
            public int StartOffset;
            public bool SuppressType, SuppressDefaultCtor, SuppressDecl;
            public ZilList CustomCtorSpec;
            public ZilList InitArgs;
        }

        struct DefStructField
        {
            public ZilAtom Name;
            public ZilAtom NthFunc, PutFunc;
            public int Offset;
            public ZilObject Decl;
            public ZilObject Default;
            public bool NoDefault;
        }

        const string SDefstructDefaultsDecl =
            @"<LIST ATOM [REST <OR ''NODECL ''NOTYPE ''PRINTTYPE ''CONSTRUCTOR
                                   <LIST ''NTH ATOM> <LIST ''PUT ATOM> <LIST ''START-OFFSET FIX>
                                   <LIST ''PRINTTYPE ATOM> <LIST ''CONSTRUCTOR> <LIST ''INIT-ARGS>>]>";
        const string SDefstructFieldSpecDecl =
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
            [Either(typeof(ZilAtom), typeof(DefStructParams.DefaultsList), DefaultParamDesc = "base-type")]
            object baseTypeOrDefaults,
            [Required]
            DefStructParams.FieldSpecList[] fieldSpecs)
        {
            SubrContracts(ctx);

            // new type name
            if (ctx.IsRegisteredType(name))
                throw new InterpreterError(InterpreterMessages._0_Already_Defined_1, "DEFSTRUCT", name);

            // base type, and optional default field settings
            ZilAtom baseType;
            var defaults = new DefStructDefaults
            {
                NthFunc = ctx.GetStdAtom(StdAtom.NTH),
                PutFunc = ctx.GetStdAtom(StdAtom.PUT),
                StartOffset = 1
            };

            var fileDefaultList = ctx.CurrentFile.DefStructDefaults;
            if (fileDefaultList != null)
                ParseDefStructDefaults(ctx, fileDefaultList, ref defaults);

            if (baseTypeOrDefaults is ZilAtom atom)
            {
                baseType = atom;
            }
            else
            {
                var defaultsParam = (DefStructParams.DefaultsList)baseTypeOrDefaults;
                baseType = defaultsParam.BaseType;
                ParseDefStructDefaults(ctx, defaultsParam, ref defaults);
            }

            if (!ctx.IsRegisteredType(baseType))
            {
                throw new InterpreterError(InterpreterMessages._0_Unrecognized_1_2, "DEFSTRUCT", "base type", baseType);
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

                if (!defaults.SuppressDecl)
                {
                    var decl = MakeDefstructDecl(ctx, baseType, fields);
                    ctx.PutProp(name, ctx.GetStdAtom(StdAtom.DECL), decl);
                }
            }

            var initArgs = defaults.InitArgs ?? new ZilList(null, null);

            // define constructor macro
            if (!defaults.SuppressDefaultCtor)
            {
                var ctorMacroDef = MakeDefstructCtorMacro(ctx, name, baseType, fields, initArgs, defaults.StartOffset);

                using (ctx.PushFileContext(string.Format("<constructor for DEFSTRUCT {0}>", name)))
                {
                    ctorMacroDef.Eval(ctx);
                }
            }

            if (defaults.CustomCtorSpec != null)
            {
                if (defaults.CustomCtorSpec.IsEmpty || defaults.CustomCtorSpec.Rest.IsEmpty)
                    throw new InterpreterError(InterpreterMessages._0_Not_Enough_Elements_In_CONSTRUCTOR_Spec, "DEFSTRUCT");

                if (!(defaults.CustomCtorSpec.First is ZilAtom ctorName))
                    throw new InterpreterError(InterpreterMessages._0_Expected_1_After_2, "DEFSTRUCT", "an atom", "'CONSTRUCTOR");

                if (!(defaults.CustomCtorSpec.Rest.First is ZilList argspecList))
                    throw new InterpreterError(InterpreterMessages._0_Second_Element_After_CONSTRUCTOR_Must_Be_An_Argument_List, "DEFSTRUCT");

                var argspec = ArgSpec.Parse("DEFSTRUCT", ctorName, null, argspecList);
                var ctorMacroDef = MakeDefstructCustomCtorMacro(ctx, ctorName, name, baseType, fields, initArgs, defaults.StartOffset, argspec);

                using (ctx.PushFileContext(string.Format("<constructor {0} for DEFSTRUCT {1}>", ctorName, name)))
                {
                    ctorMacroDef.Eval(ctx);
                }
            }

            // define field access macros
            foreach (var field in fields)
            {
                var accessMacroDef = MakeDefstructAccessMacro(ctx, name, defaults, field);

                using (ctx.PushFileContext(string.Format("<accessor for field {0} of DEFSTRUCT {1}>", field.Name, name)))
                {
                    accessMacroDef.Eval(ctx);
                }
            }

            // set PRINTTYPE
            if (defaults.PrintFunc != null)
            {
                // annoyingly, the argument can be an atom naming a function that hasn't been defined yet
                if (defaults.PrintFunc is ZilAtom printFuncAtom)
                {
                    var handler = ctx.GetGlobalVal(printFuncAtom);
                    if (handler == null)
                    {
                        handler = Program.Parse(
                            ctx,
                            @"#FUNCTION ((X ""AUX"" (D ,{0})) <PRINTTYPE {1} .D> <APPLY .D .X>)",
                            printFuncAtom,
                            name).Single();
                    }

                    ctx.SetPrintType(name, handler);
                }
            }

            return name;
        }

        static ZilObject MakeDefstructDecl(Context ctx, ZilAtom baseType, List<DefStructField> fields)
        {
            var parts = new List<ZilObject>(1 + fields.Count);

            parts.Add(new ZilForm(new[] { ctx.GetStdAtom(StdAtom.PRIMTYPE), TYPEPRIM(ctx, baseType) }));
            parts.AddRange(fields.Select(f => f.Decl));

            return new ZilSegment(new ZilForm(parts));
        }

        static ZilObject MakeDefstructCustomCtorMacro(Context ctx, ZilAtom ctorName, ZilAtom typeName, ZilAtom baseType,
            List<DefStructField> fields, ZilList initArgs, int startOffset, ArgSpec argspec)
        {
            Contract.Requires(typeName != null);
            Contract.Requires(baseType != null);
            Contract.Requires(fields != null);
            Contract.Requires(initArgs != null);

            // {0} = constructor name
            // {1} = type name
            // {2} = argspec
            // {3} = field count
            // {4} = base constructor atom
            // {5} = list of INIT-ARGS, or empty list
            // {6} = list of PUT statements for fields
            const string SMacroTemplate = @"
<DEFMAC {0} {2}
    <BIND ((RESULT-INIT <IVECTOR {3} <>>))
        {6:SPLICE}
        <FORM CHTYPE <FORM {4} {5:SPLICE} !.RESULT-INIT> {1}>>>";

            var remainingFields = fields.ToDictionary(f => f.Name);

            var resultInitializers = new List<ZilObject>();
            foreach (var arg in argspec)
            {
                // NOTE: we don't handle NoDefault ('NONE) here because this ctor allocates a new object

                // {0} = offset
                // {1} = arg name
                // {2} = default value
                const string SRequiredArgInitializer = "<PUT .RESULT-INIT {0} .{1}>";
                const string SOptAuxArgInitializer = "<PUT .RESULT-INIT {0} <COND (<ASSIGNED? {1}> .{1}) (T {2})>>";

                if (remainingFields.TryGetValue(arg.Atom, out var field))
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
                        resultInitializers.Add(Program.Parse(
                            ctx,
                            SRequiredArgInitializer,
                            new ZilFix(field.Offset - startOffset + 1),
                            arg.Atom,
                            field.Default ?? DefaultForDecl(ctx, field.Decl))
                            .Single());
                        break;

                    case ArgItem.ArgType.Optional:
                    case ArgItem.ArgType.Auxiliary:
                        resultInitializers.Add(Program.Parse(
                            ctx,
                            SOptAuxArgInitializer,
                            new ZilFix(field.Offset - startOffset + 1),
                            arg.Atom,
                            field.Default ?? DefaultForDecl(ctx, field.Decl))
                            .Single());
                        break;

                    default:
                        throw UnhandledCaseException.FromEnum(arg.Type);
                }
            }

            foreach (var field in remainingFields.Values)
            {
                if (field.Default == null)
                    continue;

                // {0} = offset
                // {1} = default value
                const string SOmittedFieldInitializer = "<PUT .RESULT-INIT {0} {1}>";
                resultInitializers.Add(Program.Parse(
                    ctx,
                    SOmittedFieldInitializer,
                    new ZilFix(field.Offset - startOffset + 1),
                    field.Default)
                    .Single());
            }

            return Program.Parse(
                ctx,
                SMacroTemplate,
                ctorName,
                typeName,
                argspec.ToZilList(),
                new ZilFix(fields.Count),
                baseType,
                initArgs,
                new ZilList(resultInitializers))
                .Single();
        }

        static ZilObject DefaultForDecl(Context ctx, ZilObject decl)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(decl != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);

            foreach (var zo in LikelyDefaults(ctx))
            {
                try
                {
                    if (Decl.Check(ctx, zo, decl))
                        return zo;
                }
                catch (InterpreterError)
                {
                    // decl might be invalid if the struct references a NEWTYPE that hasn't been defined yet
                    break;
                }
            }

            return ctx.FALSE;
        }

        static IEnumerable<ZilObject> LikelyDefaults(Context ctx)
        {
            Contract.Requires(ctx != null);

            yield return ctx.FALSE;
            yield return ZilFix.Zero;
            yield return new ZilList(null, null);
            yield return new ZilVector();
            yield return ZilString.FromString("");
            yield return ctx.GetStdAtom(StdAtom.SORRY);
        }

        static ZilObject MakeDefstructCtorMacro(Context ctx, ZilAtom name, ZilAtom baseType, List<DefStructField> fields,
            ZilList initArgs, int startOffset)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(name != null);
            Contract.Requires(baseType != null);
            Contract.Requires(fields != null);
            Contract.Requires(initArgs != null);

            // the MAKE-[STRUCT] macro can be called with a parameter telling it to stuff values into an existing object:
            //   <MAKE-FOO 'FOO .MYFOO 'FOO-X 123>
            // in which case we want to return:
            //   <BIND ((RESULT .MYFOO)) <PUT .RESULT 1 123> .RESULT>

            // but without that parameter, we stuff the values into a temporary vector now, and
            // build a call to the base constructor:
            //   <CHTYPE <TABLE 123> FOO>

            // {0} = name
            // {1} = field count
            // {2} = list of COND clauses for tags ("existing object" mode, returning a FORM that PUTs into .RESULT)
            // {3} = list of COND clauses for tags ("new object" mode, PUTting into the temp vector .RESULT-INIT)
            // {4} = base constructor atom
            // {5} = list of INIT-ARGS, or empty list
            // {6} = list of COND clauses for indices ("BOA constructor" mode, PUTting into the temp vector .RESULT-INIT)
            // {7} = list of COND clauses for index defaults
            // {8} = list of COND statements for tag defaults ("new object" mode)
            // {9} = list of expressions returning a FORM or SPLICE for tag defaults ("existing object" mode)
            const string SMacroTemplate = @"
<DEFMAC %<PARSE <STRING ""MAKE-"" <SPNAME {0}>>> (""ARGS"" A ""AUX"" RESULT-INIT SEEN)
    ;""expand segments""
    <SET A <MAPF ,LIST
                 <FUNCTION (X)
                     <COND (<TYPE? .X SEGMENT> <MAPRET !<CHTYPE .X FORM>>)
                           (ELSE .X)>>
                 .A>>
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
                         <COND {2:SPLICE}
                               (T <ERROR INVALID-DEFSTRUCT-TAG!-ERRORS .N>)>>>
                 !<VECTOR {9:SPLICE}>
                 '.RESULT>)
          (<OR <EMPTY? .A>
               <NOT <TYPE? <1 .A> FORM>>
               <N==? <1 <1 .A>> QUOTE>>
           <SET RESULT-INIT <IVECTOR {1} <>>>
           <BIND ((I 1))
               <MAPF <>
                     <FUNCTION (V)
                         <COND {6:SPLICE}
                               (T <ERROR TOO-MANY-ARGS!-ERRORS .A>)>
                         <SET I <+ .I 1>>>
                     .A>
               <REPEAT ()
                   <COND (<G? .I {1}> <RETURN>)
                         {7:SPLICE}>
                   <SET I <+ .I 1>>>>
           <FORM CHTYPE <FORM {4} {5:SPLICE} !.RESULT-INIT> {0}>)
          (T
           <SET RESULT-INIT <IVECTOR {1} <>>>
           <SET SEEN '()>
           <REPEAT (N V)
               <COND (<LENGTH? .A 0> <RETURN>)
                       (<LENGTH? .A 1> <ERROR NOT-ENOUGH-ARGS!-ERRORS .A>)>
               <SET N <1 .A>>
               <SET V <2 .A>>
               <SET A <REST .A 2>>
               <COND {3:SPLICE}
                       (T <ERROR INVALID-DEFSTRUCT-TAG!-ERRORS .N>)>>
           {8:SPLICE}
           <FORM CHTYPE <FORM {4} {5:SPLICE} !.RESULT-INIT> {0}>)>>
";

            // {0} = tag name
            // {1} = PUT atom
            // {2} = offset in structure (for existing object) or RESULT-INIT (for others)
            // {3} = definition order (1-based)
            // {4} = default value
            const string SExistingObjectCondClauseTemplate = "(<=? .N '<QUOTE {0}>> <SET SEEN <CONS {0} .SEEN>> <FORM {1} '.RESULT {2} .V>)";
            const string SExistingObjectDefaultTemplate = "<COND (<MEMQ {0} .SEEN> #SPLICE ()) (T <FORM {1} '.RESULT {2} {4}>)>";
            const string SExistingObjectDefaultTemplate_NoDefault = "#SPLICE ()";
            const string SNewObjectCondClauseTemplate = "(<=? .N '<QUOTE {0}>> <SET SEEN <CONS {0} .SEEN>> <PUT .RESULT-INIT {2} .V>)";
            const string SNewObjectDefaultTemplate = "<OR <MEMQ {0} .SEEN> <PUT .RESULT-INIT {2} {4}>>";
            const string SBoaConstructorCondClauseTemplate = "(<=? .I {3}> <PUT .RESULT-INIT {2} .V>)";
            const string SBoaConstructorDefaultClauseTemplate = "(<=? .I {3}> <PUT .RESULT-INIT {2} {4}>)";

            var existingObjectClauses = new List<ZilObject>();
            var existingObjectDefaults = new List<ZilObject>();
            var newObjectClauses = new List<ZilObject>();
            var newObjectDefaults = new List<ZilObject>();
            var boaConstructorClauses = new List<ZilObject>();
            var boaConstructorDefaultClauses = new List<ZilObject>();

            int definitionOrder = 1;
            foreach (var field in fields)
            {
                var defaultValue = field.Default ?? DefaultForDecl(ctx, field.Decl);
                var actualOffset = new ZilFix(field.Offset);
                var adjustedOffset = new ZilFix(field.Offset - startOffset + 1);
                var orderFix = new ZilFix(definitionOrder);

                existingObjectDefaults.Add(Program.Parse(
                    ctx,
                    field.NoDefault ? SExistingObjectDefaultTemplate_NoDefault : SExistingObjectDefaultTemplate,
                    field.Name, field.PutFunc, actualOffset, orderFix, defaultValue)
                    .Single());
                newObjectDefaults.Add(Program.Parse(
                    ctx,
                    SNewObjectDefaultTemplate,
                    field.Name, field.PutFunc, adjustedOffset, orderFix, defaultValue)
                    .Single());
                boaConstructorDefaultClauses.Add(Program.Parse(
                    ctx,
                    SBoaConstructorDefaultClauseTemplate,
                    field.Name, field.PutFunc, adjustedOffset, orderFix, defaultValue)
                    .Single());

                existingObjectClauses.Add(Program.Parse(
                    ctx,
                    SExistingObjectCondClauseTemplate,
                    field.Name, field.PutFunc, actualOffset, orderFix, defaultValue)
                    .Single());
                newObjectClauses.Add(Program.Parse(
                    ctx,
                    SNewObjectCondClauseTemplate,
                    field.Name, field.PutFunc, adjustedOffset, orderFix, defaultValue)
                    .Single());
                boaConstructorClauses.Add(Program.Parse(
                    ctx,
                    SBoaConstructorCondClauseTemplate,
                    field.Name, field.PutFunc, adjustedOffset, orderFix, defaultValue)
                    .Single());

                definitionOrder++;
            }

            return Program.Parse(
                ctx,
                SMacroTemplate,
                name,
                new ZilFix(fields.Count),
                new ZilList(existingObjectClauses),
                new ZilList(newObjectClauses),
                baseType,
                initArgs,
                new ZilList(boaConstructorClauses),
                new ZilList(boaConstructorDefaultClauses),
                new ZilList(newObjectDefaults),
                new ZilList(existingObjectDefaults))
                .Single();
        }

        static ZilObject MakeDefstructAccessMacro(Context ctx, ZilAtom structName, DefStructDefaults defaults,
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

            return Program.Parse(
                ctx,
                template,
                field.Name,
                structName,
                field.PutFunc,
                field.NthFunc,
                new ZilFix(field.Offset),
                field.Decl)
                .Single();
        }

        static DefStructField ParseDefStructField(Context ctx, DefStructDefaults defaults, ref int offset,
            DefStructParams.FieldSpecList fieldSpec)
        {
            Contract.Requires(ctx != null);

            var result = new DefStructField
            {
                Decl = fieldSpec.Decl,
                Name = fieldSpec.Name,
                NthFunc = defaults.NthFunc,
                Offset = offset,
                PutFunc = defaults.PutFunc
            };

            bool gotDefault = false, gotOffset = false;

            foreach (var part in fieldSpec.Parts)
            {
                switch (part)
                {
                    case DefStructParams.AtomFieldSequence af:
                        switch (af.ClauseType)
                        {
                            case StdAtom.NTH:
                                result.NthFunc = af.Atom;
                                break;

                            case StdAtom.PUT:
                                result.PutFunc = af.Atom;
                                break;

                            default:
                                throw UnhandledCaseException.FromEnum(af.ClauseType, "atom clause type");
                        }
                        break;

                    case DefStructParams.FixFieldSequence ff:
                        switch (ff.ClauseType)
                        {
                            case StdAtom.OFFSET:
                                result.Offset = ff.Fix;
                                gotOffset = true;
                                break;

                            default:
                                throw UnhandledCaseException.FromEnum(ff.ClauseType, "FIX clause type");
                        }
                        break;

                    case DefStructParams.NullaryFieldSequence nf:
                        switch (nf.ClauseType)
                        {
                            case StdAtom.NONE:
                                if (gotDefault)
                                    throw new InterpreterError(InterpreterMessages._0_NONE_Is_Not_Allowed_After_A_Default_Field_Value, "DEFSTRUCT");
                                result.NoDefault = true;
                                gotDefault = true;
                                break;

                            default:
                                throw UnhandledCaseException.FromEnum(nf.ClauseType, "nullary clause type");
                        }
                        break;

                    case ZilObject zo when (!gotDefault):
                        result.Default = zo;
                        gotDefault = true;
                        break;

                    default:
                        throw new InterpreterError(InterpreterMessages._0_Unrecognized_1_2, "DEFSTRUCT", "object in field definition", part);
                }
            }

            if (!gotOffset)
                offset++;

            return result;
        }

        // TODO: delete once SET-DEFSTRUCT-FILE-DEFAULTS is using ArgDecoder
        static void ParseDefStructDefaults(Context ctx, ZilList fileDefaults, ref DefStructDefaults defaults)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(fileDefaults != null);

            var quoteAtom = ctx.GetStdAtom(StdAtom.QUOTE);

            foreach (var part in fileDefaults)
            {
                if (part is ZilForm partForm &&
                    partForm.First == quoteAtom &&
                    partForm.Rest.First is ZilAtom tag)
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
                            throw UnhandledCaseException.FromEnum(tag.StdAtom, "tag in defaults section");
                    }
                }
                else
                {
                    if (!(part is ZilList partList))
                        throw new InterpreterError(InterpreterMessages._0_Parts_Of_Defaults_Section_Must_Be_Quoted_Atoms_Or_Lists, "DEFSTRUCT");

                    if (!(partList.First is ZilForm first) ||
                        first.First != quoteAtom ||
                        !(first.Rest.First is ZilAtom tag2))
                    {
                        throw new InterpreterError(InterpreterMessages._0_Lists_In_Defaults_Section_Must_Start_With_A_Quoted_Atom, "DEFSTRUCT");
                    }

                    switch (tag2.StdAtom)
                    {
                        case StdAtom.NTH:
                            partList = partList.Rest;
                            defaults.NthFunc = partList.First as ZilAtom;
                            if (defaults.NthFunc == null)
                                throw new InterpreterError(InterpreterMessages._0_Expected_1_After_2, "DEFSTRUCT", "an atom", first);
                            break;

                        case StdAtom.PUT:
                            partList = partList.Rest;
                            defaults.PutFunc = partList.First as ZilAtom;
                            if (defaults.PutFunc == null)
                                throw new InterpreterError(InterpreterMessages._0_Expected_1_After_2, "DEFSTRUCT", "an atom", first);
                            break;

                        case StdAtom.START_OFFSET:
                            partList = partList.Rest;
                            if (!(partList.First is ZilFix fix))
                                throw new InterpreterError(InterpreterMessages._0_Expected_1_After_2, "DEFSTRUCT", "a FIX", first);
                            defaults.StartOffset = fix.Value;
                            break;

                        case StdAtom.PRINTTYPE:
                            partList = partList.Rest;
                            defaults.PrintFunc = partList.First as ZilAtom;
                            if (defaults.PrintFunc == null)
                                throw new InterpreterError(InterpreterMessages._0_Expected_1_After_2, "DEFSTRUCT", "an atom", first);
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
                            throw new InterpreterError(InterpreterMessages._0_Unrecognized_1_2, "DEFSTRUCT", "tag in defaults section", first);
                    }
                }
            }
        }

        static void ParseDefStructDefaults(Context ctx, DefStructParams.DefaultsList param, ref DefStructDefaults defaults)
        {
            Contract.Requires(ctx != null);

            foreach (var clause in param.Clauses)
            {
                switch (clause)
                {
                    case DefStructParams.NullaryDefaultClause ec:
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
                                throw UnhandledCaseException.FromEnum(ec.ClauseType, "nullary clause type");
                        }
                        break;

                    case DefStructParams.AtomDefaultClause ac:
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
                                throw UnhandledCaseException.FromEnum(ac.ClauseType, "atom clause type");
                        }
                        break;

                    case DefStructParams.FixDefaultClause fc:
                        switch (fc.ClauseType)
                        {
                            case StdAtom.START_OFFSET:
                                defaults.StartOffset = fc.Fix;
                                break;

                            default:
                                throw UnhandledCaseException.FromEnum(fc.ClauseType, "FIX clause type");
                        }
                        break;

                    case DefStructParams.VarargsDefaultClause vc:
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
                                throw UnhandledCaseException.FromEnum(vc.ClauseType, "varargs clause type");
                        }
                        break;

                    default:
                        throw UnhandledCaseException.FromTypeOf(clause, "clause");
                }
            }
        }
    }
}
