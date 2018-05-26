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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using Zilf.Common;
using Zilf.Diagnostics;
using Zilf.Emit;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Interpreter.Values.Tied;
using Zilf.Language;

namespace Zilf.ZModel.Values
{
    [BuiltinType(StdAtom.PROPDEF, PrimType.LIST)]
    sealed class ComplexPropDef : ZilTiedListBase
    {
        enum InputElementType
        {
/*
            Invalid = 0,
*/

            Opt,
            Many,
            Variable,
            Atom
        }

        struct InputElement
        {
            public readonly InputElementType Type;
            public readonly ZilAtom Variable;
            public readonly ZilObject Decl;

            public InputElement(InputElementType type, ZilAtom variable, ZilObject decl)
            {
                Type = type;
                Variable = variable;
                Decl = decl;
            }

            public override string ToString()
            {
                return $"Type={Type} Variable={Variable} Decl={Decl}";
            }

            public ZilObject ToZilObject()
            {
                switch (Type)
                {
                    case InputElementType.Atom:
                        return Variable;

                    case InputElementType.Many:
                        return ZilString.FromString("MANY");

                    case InputElementType.Opt:
                        return ZilString.FromString("OPT");

                    case InputElementType.Variable:
                        if (Decl != null)
                        {
                            return new ZilAdecl(Variable, Decl);
                        }
                        else
                        {
                            return Variable;
                        }

                    default:
                        throw UnhandledCaseException.FromEnum(Type);
                }
            }
        }

        enum OutputElementType
        {
/*
            Invalid = 0,
*/

            Length,
            Many,
            Word,
            Byte,
            Object,
            Room,
            Global,
            Noun,
            Adjective,
            Voc,
            String
        }

        struct OutputElement
        {
            public readonly OutputElementType Type;
            public readonly ZilAtom Constant, Variable, PartOfSpeech;
            public readonly ZilFix Fix;

            public OutputElement(OutputElementType type, ZilAtom constant, [CanBeNull] ZilAtom variable = null,
                [CanBeNull] ZilAtom partOfSpeech = null, [CanBeNull] ZilFix fix = null)
            {
                Type = type;
                Constant = constant;
                Variable = variable;
                PartOfSpeech = partOfSpeech;
                Fix = fix;
            }

            public override string ToString()
            {
                return $"Type={Type} Constant={Constant} Variable={Variable} PartOfSpeech={PartOfSpeech} Fix={Fix}";
            }

            [NotNull]
            public ZilObject ToZilObject()
            {
                ZilObject result;
                StdAtom head;

                switch (Type)
                {
                    case OutputElementType.Length:
                        result = Fix ?? FALSE;
                        break;

                    case OutputElementType.Many:
                        result = ZilString.FromString("MANY");
                        break;

                    case OutputElementType.Adjective:
                        head = StdAtom.ADJ;
                        goto TwoElementForm;
                    case OutputElementType.Byte:
                        head = StdAtom.BYTE;
                        goto TwoElementForm;
                    case OutputElementType.Global:
                        head = StdAtom.GLOBAL;
                        goto TwoElementForm;
                    case OutputElementType.Noun:
                        head = StdAtom.NOUN;
                        goto TwoElementForm;
                    case OutputElementType.Object:
                        head = StdAtom.OBJECT;
                        goto TwoElementForm;
                    case OutputElementType.Room:
                        head = StdAtom.ROOM;
                        goto TwoElementForm;
                    case OutputElementType.String:
                        head = StdAtom.STRING;
                        goto TwoElementForm;
                    case OutputElementType.Word:
                        head = StdAtom.WORD;

                    TwoElementForm:
                        result = new ZilForm(new[] {
                            GetStdAtom(head),
                            (ZilObject)Fix ?? new ZilForm(new[] {
                                GetStdAtom(StdAtom.LVAL),
                                Variable
                            })
                        });
                        break;

                    case OutputElementType.Voc:
                        result = new ZilForm(new[] {
                            GetStdAtom(StdAtom.VOC),
                            (ZilObject)Fix ?? new ZilForm(new[] {
                                GetStdAtom(StdAtom.LVAL),
                                Variable
                            }),
                            PartOfSpeech
                        });
                        break;

                    default:
                        throw UnhandledCaseException.FromEnum(Type);
                }

                if (Constant != null)
                {
                    return new ZilList(new[] { Constant, result });
                }

                return result;
            }
        }

        struct Pattern
        {
            public readonly InputElement[] Inputs;
            public readonly OutputElement[] Outputs;

            public Pattern(InputElement[] inputs, OutputElement[] outputs)
            {
                Inputs = inputs;
                Outputs = outputs;
            }
        }

        readonly List<Pattern> patterns;

        ComplexPropDef([NotNull] IEnumerable<Pattern> patterns)
        {
            this.patterns = new List<Pattern>(patterns);
        }

        /// <exception cref="InterpreterError">The PROPDEF pattern syntax is invalid.</exception>
        [NotNull]
        public static ComplexPropDef Parse([NotNull] IEnumerable<ZilObject> spec)
        {

            var inputs = new List<InputElement>();
            var outputs = new List<OutputElement>();
            var patterns = new List<Pattern>();

            foreach (var patternObj in spec)
            {
                if (!(patternObj is ZilList list))
                    throw new InterpreterError(InterpreterMessages._0_Must_Be_1, "PROPDEF patterns", "lists");

                bool gotEq = false;
                foreach (var element in list)
                {
                    if (!gotEq)
                    {
                        // inputs
                        switch (element)
                        {
                            case ZilAdecl adecl:
                                inputs.Add(new InputElement(
                                    InputElementType.Variable,
                                    (ZilAtom)adecl.First,
                                    adecl.Second));
                                break;

                            case ZilAtom atom:
                                if (atom.StdAtom == StdAtom.Eq)
                                {
                                    gotEq = true;
                                }
                                else
                                {
                                    inputs.Add(new InputElement(
                                        InputElementType.Atom,
                                        atom,
                                        null));
                                }
                                break;

                            case ZilString str:
                                switch (str.Text)
                                {
                                    case "OPT":
                                    case "OPTIONAL":
                                        inputs.Add(new InputElement(
                                            InputElementType.Opt,
                                            null,
                                            null));
                                        break;

                                    case "MANY":
                                        inputs.Add(new InputElement(
                                            InputElementType.Many,
                                            null,
                                            null));
                                        break;

                                    default:
                                        throw new InterpreterError(InterpreterMessages._0_In_1_Must_Be_2, "strings", "PROPDEF patterns", "\"OPT\" or \"MANY\"");
                                }
                                break;
                        }
                    }
                    else
                    {
                        // outputs
                        ZilAtom constant = null;
                        var output = element;

                        if (element is ZilList elemList)
                        {
                            if (elemList.GetLength(2) != 2)
                            {
                                throw new InterpreterError(InterpreterMessages._0_In_1_Must_Have_2_Element2s, "list", "PROPDEF output pattern", 2);
                            }

                            constant = elemList.First as ZilAtom;
                            if (constant == null)
                            {
                                throw new InterpreterError(InterpreterMessages.Element_0_Of_1_In_2_Must_Be_3, "1", "list", "PROPDEF output pattern", "an atom");
                            }

                            Debug.Assert(elemList.Rest != null);
                            output = elemList.Rest.First;
                            Debug.Assert(output != null);
                        }

                        switch (output)
                        {
                            case ZilFix fix:
                                outputs.Add(new OutputElement(OutputElementType.Length, constant, fix: fix));
                                break;

                            case ZilFalse _:
                                outputs.Add(new OutputElement(OutputElementType.Length, constant));
                                break;

                            case ZilForm form:
                                outputs.Add(ConvertOutputForm(form, constant));
                                break;

                            case ZilString str:
                                switch (str.Text)
                                {
                                    case "MANY":
                                        outputs.Add(new OutputElement(OutputElementType.Many, constant));
                                        break;

                                    default:
                                        throw new InterpreterError(InterpreterMessages._0_In_1_Must_Be_2, "string", "PROPDEF output pattern", @"""MANY""");
                                }
                                break;

                            default:
                                if (output.StdTypeAtom != StdAtom.SEMI)
                                {
                                    throw new InterpreterError(
                                        InterpreterMessages._0_In_1_Must_Be_2,
                                        "elements",
                                        "PROPDEF output pattern",
                                        "FIX, FALSE, FORM, STRING, or SEMI");
                                }
                                break;
                        }
                    }
                }

                if (inputs.Count == 0 || inputs[0].Type != InputElementType.Atom)
                {
                    throw new InterpreterError(
                        InterpreterMessages.Element_0_Of_1_Must_Be_2,
                        1,
                        "PROPDEF pattern",
                        "an atom");
                }

                if (outputs.Skip(1).Any(e => e.Type == OutputElementType.Length))
                {
                    throw new InterpreterError(InterpreterMessages.FIXFALSE_In_PROPDEF_Output_Pattern_Must_Be_At_The_Beginning);
                }

                if (outputs.Count >= 1 && outputs[0].Type == OutputElementType.Length && outputs[0].Fix == null)
                {
                    // discard <>
                    outputs.RemoveAt(0);
                }

                var capturedVariables = new HashSet<ZilAtom>(from i in inputs
                                                             where i.Type == InputElementType.Variable
                                                             select i.Variable);
                var badCapture = (from o in outputs
                                  where o.Variable != null
                                  select o.Variable).FirstOrDefault(v => !capturedVariables.Contains(v));
                if (badCapture != null)
                {
                    throw new InterpreterError(InterpreterMessages.Variable_In_PROPDEF_Output_Pattern_Is_Not_Captured_By_Input_Pattern_0, badCapture);
                }

                patterns.Add(new Pattern(inputs.ToArray(), outputs.ToArray()));
                inputs.Clear();
                outputs.Clear();
            }

            return new ComplexPropDef(patterns);
        }

        static OutputElement ConvertOutputForm([NotNull] ZilForm form, ZilAtom constant)
        {

            // validate and parse
            if (!(form.First is ZilAtom head))
            {
                throw new InterpreterError(
                    form,
                    InterpreterMessages.Element_0_Of_1_In_2_Must_Be_3,
                    1,
                    "FORM",
                    "PROPDEF pattern",
                    "an atom");
            }

            int length = 2;
            OutputElementType type;

            switch (head.StdAtom)
            {
                case StdAtom.BYTE:
                    type = OutputElementType.Byte;
                    break;

                case StdAtom.WORD:
                    type = OutputElementType.Word;
                    break;

                case StdAtom.OBJECT:
                    type = OutputElementType.Object;
                    break;

                case StdAtom.ROOM:
                    type = OutputElementType.Room;
                    break;

                case StdAtom.GLOBAL:
                    type = OutputElementType.Global;
                    break;

                case StdAtom.NOUN:
                    type = OutputElementType.Noun;
                    break;

                case StdAtom.ADJ:
                case StdAtom.ADJECTIVE:
                    type = OutputElementType.Adjective;
                    break;

                case StdAtom.VOC:
                    type = OutputElementType.Voc;
                    length = 3;
                    break;

                case StdAtom.STRING:
                    type = OutputElementType.String;
                    break;

                default:
                    throw new InterpreterError(form, InterpreterMessages.FORM_In_PROPDEF_Output_Pattern_Must_Be_BYTE_WORD_STRING_OBJECT_ROOM_GLOBAL_NOUN_ADJ_Or_VOC);
            }

            if (((IStructure)form).GetLength(length) != length)
            {
                throw new InterpreterError(form, InterpreterMessages._0_FORM_In_PROPDEF_Output_Pattern_Must_Have_Length_1, head, length);
            }

            Debug.Assert(form.Rest?.Rest != null);

            ZilAtom outVariable;
            ZilFix outFix;
            switch (form.Rest.First)
            {
                case ZilObject zo when zo.IsLVAL(out var atom):
                    outVariable = atom;
                    outFix = null;
                    break;

                case ZilFix fix:
                    outVariable = null;
                    outFix = fix;
                    break;

                default:
                    throw new InterpreterError(
                        form,
                        InterpreterMessages._0_Expected_1,
                        head + ": arg 1",
                        "an LVAL or FIX");
            }

            ZilAtom partOfSpeech = null;
            if (head.StdAtom == StdAtom.VOC)
            {
                partOfSpeech = form.Rest.Rest.First as ZilAtom;
                if (partOfSpeech == null)
                {
                    throw new InterpreterError(
                        form,
                        InterpreterMessages._0_Expected_1,
                        head + ": arg 2",
                        "an atom");
                }
            }

            // done
            return new OutputElement(type, constant, outVariable, partOfSpeech, outFix);
        }

        /// <exception cref="InterpreterError">A constant was defined at conflicting positions across definitions.</exception>
        public IEnumerable<KeyValuePair<ZilAtom, int>> GetConstants(Context ctx)
        {
            var used = new Dictionary<ZilAtom, int>();

            foreach (var pat in patterns)
            {
                int offset = 0;

                foreach (var output in pat.Outputs)
                {
                    int constantValue;
                    if (output.Type == OutputElementType.Length)
                    {
                        constantValue = output.Fix.Value;
                    }
                    else
                    {
                        // calculate the size of this element
                        var size = OutputElementSize(output, ctx);
                        if (size == 0)
                            continue;

                        // byte offset for bytes, word offset for words
                        constantValue = offset / size;
                        offset += size;
                    }

                    // if we don't need to create a constant for this element, move on
                    if (output.Constant == null)
                        continue;

                    // create the constant, or confirm its value
                    if (used.TryGetValue(output.Constant, out int previousValue))
                    {
                        if (constantValue != previousValue)
                            throw new InterpreterError(InterpreterMessages.PROPDEF_Constant_0_Defined_At_Conflicting_Positions, output.Constant);
                    }
                    else
                    {
                        yield return new KeyValuePair<ZilAtom, int>(output.Constant, constantValue);
                        used.Add(output.Constant, constantValue);
                    }
                }
            }
        }

        static int OutputElementSize(OutputElement output, Context ctx)
        {
            switch (output.Type)
            {
                case OutputElementType.Adjective:
                    if (ctx.ZEnvironment.ZVersion == 3)
                        return 1;

                    return 2;

                case OutputElementType.Byte:
                case OutputElementType.Global:
                    return 1;

                case OutputElementType.Noun:
                case OutputElementType.Voc:
                case OutputElementType.Word:
                case OutputElementType.String:
                    return 2;

                case OutputElementType.Object:
                    if (ctx.ZEnvironment.ZVersion == 3 ||
                        ctx.ZEnvironment.ObjectOrdering == ObjectOrdering.RoomsLast)
                    {
                        return 1;
                    }

                    return 2;

                case OutputElementType.Room:
                    if (ctx.ZEnvironment.ZVersion == 3 ||
                        ctx.ZEnvironment.ObjectOrdering == ObjectOrdering.RoomsFirst ||
                        ctx.ZEnvironment.ObjectOrdering == ObjectOrdering.RoomsAndLocalGlobalsFirst)
                    {
                        return 1;
                    }

                    return 2;

                default:
                    return 0;
            }
        }

        public override StdAtom StdTypeAtom => StdAtom.PROPDEF;

        protected override TiedLayout GetLayout()
        {
            return TiedLayout.Create<ComplexPropDef>()
                .WithCatchAll<ComplexPropDef>(x => x.PatternsList);
        }

        [NotNull]
        public ZilList PatternsList
        {
            get
            {
                var result = new List<ZilObject>();
                var pattern = new List<ZilObject>();

                foreach (var p in patterns)
                {
                    pattern.Clear();

                    pattern.AddRange(p.Inputs.Select(i => i.ToZilObject()));
                    pattern.Add(GetStdAtom(StdAtom.Eq));
                    pattern.AddRange(p.Outputs.Select(o => o.ToZilObject()));

                    result.Add(new ZilList(pattern));
                }

                return new ZilList(result);
            }
        }

        [NotNull]
        [ChtypeMethod]
        public static ComplexPropDef FromList([NotNull] Context ctx, [NotNull] ZilListBase list)
        {
            return Parse(list);
        }

        public struct ElementPreBuilders
        {
            public Action<ZilAtom, ZilAtom, ISourceLine> CreateVocabWord;
            public Action<ZilAtom> ReserveGlobal;
        }

        public struct ElementConverters
        {
            public Func<ZilObject, IOperand> CompileConstant;
            public Func<ZilAtom, IOperand> GetGlobalNumber;
            public Func<ZilAtom, ISourceLine, IOperand> GetAdjectiveValue;       // (word, src) => A?constant or word builder
            public Func<ZilAtom, ZilAtom, ISourceLine, IOperand> GetVocabWord;   // (word, part of speech, src) => word builder
        }

        public bool Matches(Context ctx, ZilList prop)
        {
            foreach (var p in patterns)
            {
                var prop2 = prop.Rest;
                if (MatchPartialPattern(ctx, ref prop2, p.Inputs, 1, null) && prop2.IsEmpty)
                    return true;
            }

            return false;
        }

        /// <exception cref="InterpreterError"><paramref name="prop"/> doesn't match any of the supported patterns.</exception>
        public void PreBuildProperty([NotNull] Context ctx, [NotNull] ZilList prop, ElementPreBuilders preBuilders)
        {
            
            var captures = new Dictionary<ZilAtom, Queue<ZilObject>>();

            foreach (var p in patterns)
            {
                // try to match pattern and capture values
                var propBody = prop.Rest;
                captures.Clear();

                if (MatchPartialPattern(ctx, ref propBody, p.Inputs, 1, captures) && propBody.IsEmpty)
                {
                    PartialPreBuild(ctx, captures, preBuilders, p.Outputs, 0, prop.SourceLine);
                    return;
                }
            }

            throw new InterpreterError(InterpreterMessages.Property_0_Initializer_Doesnt_Match_Any_Supported_Patterns, prop.First);
        }

        /// <exception cref="InterpreterError"><paramref name="prop"/> doesn't match any of the supported patterns.</exception>
        public void BuildProperty([NotNull] Context ctx, [NotNull] ZilList prop, [NotNull] ITableBuilder tb, ElementConverters converters)
        {

            var propName = (ZilAtom)prop.First;

            var captures = new Dictionary<ZilAtom, Queue<ZilObject>>();

            foreach (var p in patterns)
            {
                // try to match pattern and capture values
                var propBody = prop.Rest;
                captures.Clear();

                if (MatchPartialPattern(ctx, ref propBody, p.Inputs, 1, captures) && propBody.IsEmpty)
                {
                    // build output
                    WritePartialOutput(ctx, tb, converters, captures, p.Outputs, 0, propName, prop.SourceLine);
                    return;
                }
            }

            throw new InterpreterError(InterpreterMessages.Property_0_Initializer_Doesnt_Match_Any_Supported_Patterns, prop.First);
        }

        // may change prop even for an unsuccessful match
        // may not match the entire property (check prop.IsEmpty on return)
        bool MatchPartialPattern([NotNull] Context ctx, [NotNull] ref ZilList prop, [NotNull] InputElement[] inputs, int startIndex,
            [CanBeNull] Dictionary<ZilAtom, Queue<ZilObject>> captures)
        {

            for (int i = startIndex; i < inputs.Length; i++)
            {
                var input = inputs[i];
                switch (input.Type)
                {
                    case InputElementType.Atom:
                        if (prop.First == input.Variable)
                        {
                            Debug.Assert(prop.Rest != null);
                            prop = prop.Rest;
                            break;
                        }

                        return false;

                    case InputElementType.Variable:
                        if (prop.First == null || !CheckInputDecl(ctx, prop.First, input.Decl))
                            return false;

                        Debug.Assert(prop.Rest != null);

                        if (captures != null)
                        {
                            var atom = input.Variable;
                            if (!captures.TryGetValue(atom, out var queue))
                            {
                                queue = new Queue<ZilObject>(1);
                                captures.Add(atom, queue);
                            }
                            queue.Enqueue(prop.First);
                        }

                        prop = prop.Rest;
                        break;

                    case InputElementType.Opt:
                        return prop.IsEmpty || MatchPartialPattern(ctx, ref prop, inputs, i + 1, captures);

                    case InputElementType.Many:
                        while (!prop.IsEmpty)
                        {
                            if (!MatchPartialPattern(ctx, ref prop, inputs, i + 1, captures))
                                return false;
                        }
                        return true;
                }
            }

            return true;
        }

        [ContractAnnotation("decl: null => false")]
        static bool CheckInputDecl([NotNull] Context ctx, [NotNull] ZilObject value, [CanBeNull] ZilObject decl)
        {

            // value can be the name of a constant, in which case we need to check the constant value instead
            if (value is ZilAtom valueAtom && ctx.GetZVal(valueAtom) is ZilConstant constant)
                value = constant.Value;

            if (decl is ZilAtom declAtom)
            {
                switch (declAtom.StdAtom)
                {
                    case StdAtom.NOUN:
                    case StdAtom.ADJ:
                    case StdAtom.ADJECTIVE:
                    case StdAtom.ROOM:
                    case StdAtom.OBJECT:
                    case StdAtom.FCN:
                    case StdAtom.ROUTINE:
                    case StdAtom.GLOBAL:
                        return value is ZilAtom;

                    default:
                        return value.GetTypeAtom(ctx) == declAtom;
                }
            }

            return false;
        }

        static bool PartialPreBuild([NotNull] Context ctx, [NotNull] Dictionary<ZilAtom, Queue<ZilObject>> captures,
            ElementPreBuilders preBuilders, [NotNull] OutputElement[] outputs, int startIndex, ISourceLine src)
        {

            for (int i = startIndex; i < outputs.Length; i++)
            {
                var output = outputs[i];

                ZilObject capturedValue;
                if (output.Variable != null)
                {
                    if (captures.TryGetValue(output.Variable, out var queue))
                    {
                        if (queue.Count == 0)
                            return false;

                        capturedValue = queue.Dequeue();
                    }
                    else
                    {
                        capturedValue = ctx.FALSE;
                    }
                }
                else
                {
                    capturedValue = null;
                }

                if (output.Type == OutputElementType.Many)
                {
                    while (PartialPreBuild(ctx, captures, preBuilders, outputs, i + 1, src))
                    {
                        // repeat
                    }
                }
                else
                {
                    var atom = capturedValue as ZilAtom;

                    switch (output.Type)
                    {
                        case OutputElementType.Adjective:
                            preBuilders.CreateVocabWord(atom, ctx.GetStdAtom(StdAtom.ADJ), src);
                            break;

                        case OutputElementType.Noun:
                            preBuilders.CreateVocabWord(atom, ctx.GetStdAtom(StdAtom.OBJECT), src);
                            break;
                            
                        case OutputElementType.Voc:
                            preBuilders.CreateVocabWord(atom, output.PartOfSpeech, src);
                            break;

                        case OutputElementType.Global:
                            preBuilders.ReserveGlobal(atom);
                            break;
                    }
                }
            }

            return true;
        }

        static bool WritePartialOutput([NotNull] Context ctx, [NotNull] ITableBuilder tb, ElementConverters converters,
            [NotNull] Dictionary<ZilAtom, Queue<ZilObject>> captures, [NotNull] OutputElement[] outputs, int startIndex,
            ZilAtom propName, ISourceLine src)
        {

            for (int i = startIndex; i < outputs.Length; i++)
            {
                var output = outputs[i];

                ZilObject capturedValue;
                if (output.Variable != null)
                {
                    if (captures.TryGetValue(output.Variable, out var queue))
                    {
                        if (queue.Count == 0)
                            return false;

                        capturedValue = queue.Dequeue();
                    }
                    else
                    {
                        capturedValue = ctx.FALSE;
                    }
                }
                else
                {
                    capturedValue = null;
                }

                IOperand capturedConstantValue;
                ZilAtom capturedAtom;
                if (capturedValue != null)
                {
                    switch (output.Type)
                    {
                        case OutputElementType.Global:
                        case OutputElementType.Adjective:
                        case OutputElementType.Noun:
                        case OutputElementType.Voc:
                            capturedAtom = (ZilAtom)capturedValue;
                            capturedConstantValue = null;
                            break;

                        default:
                            capturedConstantValue = converters.CompileConstant(capturedValue);
                            if (capturedConstantValue == null)
                            {
                                ctx.HandleError(new CompilerError(
                                    src,
                                    CompilerMessages.Nonconstant_Initializer_For_0_1_2,
                                    "property",
                                    propName,
                                    capturedValue));
                                capturedConstantValue = converters.CompileConstant(ctx.FALSE);
                            }
                            capturedAtom = null;
                            break;
                    }
                }
                else
                {
                    capturedAtom = null;
                    capturedConstantValue = null;
                }

                switch (output.Type)
                {
                    case OutputElementType.Length:
                        // TODO: verify length
                        break;

                    case OutputElementType.Byte:
                        if (capturedValue != null)
                        {
                            tb.AddByte(capturedConstantValue);
                        }
                        else
                        {
                            tb.AddByte((byte)output.Fix.Value);
                        }
                        break;

                    case OutputElementType.Word:
                        if (capturedValue != null)
                        {
                            tb.AddShort(capturedConstantValue);
                        }
                        else
                        {
                            tb.AddShort((short)output.Fix.Value);
                        }
                        break;

                    case OutputElementType.Room:
                    case OutputElementType.Object:
                        if (OutputElementSize(output, ctx) == 1)
                        {
                            tb.AddByte(capturedConstantValue);
                        }
                        else
                        {
                            tb.AddShort(capturedConstantValue);
                        }
                        break;

                    case OutputElementType.String:
                        tb.AddShort(capturedConstantValue);
                        break;

                    case OutputElementType.Global:
                        tb.AddByte(converters.GetGlobalNumber(capturedAtom));
                        break;

                    case OutputElementType.Adjective:
                        if (OutputElementSize(output, ctx) == 1)
                        {
                            tb.AddByte(converters.GetAdjectiveValue(capturedAtom, src));
                        }
                        else
                        {
                            tb.AddShort(converters.GetAdjectiveValue(capturedAtom, src));
                        }
                        break;

                    case OutputElementType.Noun:
                        tb.AddShort(converters.GetVocabWord(capturedAtom, ctx.GetStdAtom(StdAtom.OBJECT), src));
                        break;

                    case OutputElementType.Voc:
                        tb.AddShort(converters.GetVocabWord(capturedAtom, output.PartOfSpeech, src));
                        break;

                    case OutputElementType.Many:
                        while (WritePartialOutput(ctx, tb, converters, captures, outputs, i + 1, propName, src))
                        {
                            // repeat
                        }
                        break;
                }
            }

            return true;
        }
    }
}
