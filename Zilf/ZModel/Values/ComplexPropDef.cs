/* Copyright 2010-2016 Jesse McGrew
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
using Zilf.Diagnostics;
using Zilf.Emit;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;

namespace Zilf.ZModel.Values
{
    [BuiltinType(StdAtom.PROPDEF, PrimType.LIST)]
    sealed class ComplexPropDef : ZilObject
    {
        enum InputElementType
        {
            Invalid = 0,

            Opt,
            Many,
            Variable,
            Atom,
        }

        struct InputElement
        {
            public readonly InputElementType Type;
            public readonly ZilAtom Variable;
            public readonly ZilObject Decl;

            public InputElement(InputElementType type, ZilAtom variable, ZilObject decl)
            {
                this.Type = type;
                this.Variable = variable;
                this.Decl = decl;
            }

            public override string ToString()
            {
                return string.Format("Type={0} Variable={1} Decl={2}", Type, Variable, Decl);
            }
        }

        enum OutputElementType
        {
            Invalid = 0,

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
            String,
        }

        struct OutputElement
        {
            public readonly OutputElementType Type;
            public readonly ZilAtom Constant, Variable, PartOfSpeech;
            public readonly ZilFix Fix;

            public OutputElement(OutputElementType type, ZilAtom constant, ZilAtom variable = null, ZilAtom partOfSpeech = null, ZilFix fix = null)
            {
                this.Type = type;
                this.Constant = constant;
                this.Variable = variable;
                this.PartOfSpeech = partOfSpeech;
                this.Fix = fix;
            }

            public override string ToString()
            {
                return string.Format("Type={0} Constant={1} Variable={2} PartOfSpeech={3} Fix={4}",
                    Type, Constant, Variable, PartOfSpeech, Fix);
            }
        }

        struct Pattern
        {
            public readonly InputElement[] Inputs;
            public readonly OutputElement[] Outputs;

            public Pattern(InputElement[] inputs, OutputElement[] outputs)
            {
                this.Inputs = inputs;
                this.Outputs = outputs;
            }
        }

        readonly List<Pattern> patterns;

        ComplexPropDef(IEnumerable<Pattern> patterns)
        {
            this.patterns = new List<Pattern>(patterns);
        }

        public static ComplexPropDef Parse(IEnumerable<ZilObject> spec, Context ctx)
        {
            Contract.Requires(spec != null);
            Contract.Requires(ctx != null);
            Contract.Ensures(Contract.Result<ComplexPropDef>() != null);

            var inputs = new List<InputElement>();
            var outputs = new List<OutputElement>();
            var patterns = new List<Pattern>();

            foreach (var patternObj in spec)
            {
                if (patternObj.StdTypeAtom != StdAtom.LIST)
                    throw new InterpreterError(InterpreterMessages._0_Must_Be_1, "PROPDEF patterns", "lists");

                var list = (ZilList)patternObj;
                bool gotEq = false;
                foreach (var element in list)
                {
                    if (!gotEq)
                    {
                        // inputs
                        switch (element.StdTypeAtom)
                        {
                            case StdAtom.ADECL:
                                var adecl = (ZilAdecl)element;
                                inputs.Add(new InputElement(
                                    InputElementType.Variable,
                                    (ZilAtom)adecl.First,
                                    adecl.Second));
                                break;

                            case StdAtom.ATOM:
                                var atom = (ZilAtom)element;
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

                            case StdAtom.STRING:
                                switch (((ZilString)element).Text)
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
                        var type = output.StdTypeAtom;

                        if (type == StdAtom.LIST)
                        {
                            var elemList = (ZilList)element;

                            if (elemList.Rest == null ||
                                elemList.Rest.Rest == null ||
                                elemList.Rest.Rest.First != null)
                            {
                                throw new InterpreterError(InterpreterMessages._0_In_1_Must_Have_2_Element2s, "list", "PROPDEF output pattern", 2);
                            }

                            constant = elemList.First as ZilAtom;
                            if (constant == null)
                            {
                                throw new InterpreterError(InterpreterMessages.Element_0_Of_1_In_2_Must_Be_3, "1", "list", "PROPDEF output pattern", "an atom");
                            }

                            output = elemList.Rest.First;
                            Contract.Assert(output != null);
                        }

                        switch (output.StdTypeAtom)
                        {
                            case StdAtom.FIX:
                                outputs.Add(new OutputElement(OutputElementType.Length, constant, fix: (ZilFix)output));
                                break;

                            case StdAtom.FALSE:
                                outputs.Add(new OutputElement(OutputElementType.Length, constant, fix: null));
                                break;

                            case StdAtom.FORM:
                                outputs.Add(ConvertOutputForm((ZilForm)output, constant));
                                break;

                            case StdAtom.STRING:
                                switch (((ZilString)output).Text)
                                {
                                    case "MANY":
                                        outputs.Add(new OutputElement(OutputElementType.Many, constant));
                                        break;

                                    default:
                                        throw new InterpreterError(InterpreterMessages._0_In_1_Must_Be_2, "string", "PROPDEF output pattern", @"""MANY""");
                                }
                                break;

                            case StdAtom.SEMI:
                                // ignore
                                break;

                            default:
                                throw new InterpreterError(
                                    InterpreterMessages._0_In_1_Must_Be_2,
                                    "elements",
                                    "PROPDEF output pattern",
                                    "FIX, FALSE, FORM, STRING, or SEMI");
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
                inputs.RemoveAt(0);

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

        static OutputElement ConvertOutputForm(ZilForm form, ZilAtom constant)
        {
            Contract.Requires(form != null);

            // validate and parse
            var atom = form.First as ZilAtom;
            if (atom == null)
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

            switch (atom.StdAtom)
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
                throw new InterpreterError(form, InterpreterMessages._0_FORM_In_PROPDEF_Output_Pattern_Must_Have_Length_1, atom, length);
            }

            ZilAtom variable;
            ZilFix fix;
            if (form.Rest.First.IsLVAL())
            {
                variable = (ZilAtom)((ZilForm)form.Rest.First).Rest.First;
                fix = null;
            }
            else if (form.Rest.First is ZilFix)
            {
                variable = null;
                fix = (ZilFix)form.Rest.First;
            }
            else
            {
                throw new InterpreterError(
                    form,
                    InterpreterMessages._0_Expected_1,
                    atom + ": arg 1",
                    "an LVAL or FIX");
            }

            ZilAtom partOfSpeech = null;
            if (atom.StdAtom == StdAtom.VOC)
            {
                partOfSpeech = form.Rest.Rest.First as ZilAtom;
                if (partOfSpeech == null)
                {
                    throw new InterpreterError(
                        form,
                        InterpreterMessages._0_Expected_1,
                        atom + ": arg 2",
                        "an atom");
                }
            }

            // done
            return new OutputElement(type, constant, variable, partOfSpeech, fix);
        }

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
                    int previousValue;
                    if (used.TryGetValue(output.Constant, out previousValue))
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

        public override string ToString()
        {
            return ToStringImpl(zo => zo.ToString());
        }

        protected override string ToStringContextImpl(Context ctx, bool friendly)
        {
            return ToStringImpl(zo => zo.ToStringContext(ctx, friendly));
        }

        string ToStringImpl(Func<ZilObject, string> convert)
        {
            var sb = new StringBuilder();

            sb.Append("#PROPDEF (");

            bool first = true;

            foreach (var p in patterns)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    sb.Append(' ');
                }

                sb.Append('(');

                foreach (var i in p.Inputs)
                {
                    switch (i.Type)
                    {
                        case InputElementType.Atom:
                            sb.Append(convert(i.Variable));
                            break;

                        case InputElementType.Many:
                            sb.Append("\"MANY\"");
                            break;

                        case InputElementType.Opt:
                            sb.Append("\"OPT\"");
                            break;

                        case InputElementType.Variable:
                            sb.Append(convert(i.Variable));
                            if (i.Decl != null)
                            {
                                sb.Append(':');
                                sb.Append(convert(i.Decl));
                            }
                            break;

                        default:
                            throw new NotImplementedException();
                    }

                    sb.Append(' ');
                }

                sb.Append('=');

                foreach (var o in p.Outputs)
                {
                    sb.Append(' ');

                    if (o.Constant != null)
                    {
                        sb.Append('(');
                        sb.Append(convert(o.Constant));
                        sb.Append(' ');
                    }

                    switch (o.Type)
                    {
                        case OutputElementType.Length:
                            if (o.Fix != null)
                                sb.Append(o.Fix.Value);
                            else
                                sb.Append("<>");
                            break;

                        case OutputElementType.Many:
                            sb.Append("\"MANY\"");
                            break;

                        case OutputElementType.Adjective:
                            sb.AppendFormat("<ADJ {0}>", convert((ZilObject)o.Fix ?? o.Variable));
                            break;
                        case OutputElementType.Byte:
                            sb.AppendFormat("<BYTE {0}>", convert((ZilObject)o.Fix ?? o.Variable));
                            break;
                        case OutputElementType.Global:
                            sb.AppendFormat("<GLOBAL {0}>", convert((ZilObject)o.Fix ?? o.Variable));
                            break;
                        case OutputElementType.Noun:
                            sb.AppendFormat("<NOUN {0}>", convert((ZilObject)o.Fix ?? o.Variable));
                            break;
                        case OutputElementType.Object:
                            sb.AppendFormat("<OBJECT {0}>", convert((ZilObject)o.Fix ?? o.Variable));
                            break;
                        case OutputElementType.Room:
                            sb.AppendFormat("<ROOM {0}>", convert((ZilObject)o.Fix ?? o.Variable));
                            break;
                        case OutputElementType.String:
                            sb.AppendFormat("<STRING {0}>", convert((ZilObject)o.Fix ?? o.Variable));
                            break;
                        case OutputElementType.Voc:
                            sb.AppendFormat("<VOC {0} {1}>", convert((ZilObject)o.Fix ?? o.Variable), convert(o.PartOfSpeech));
                            break;
                        case OutputElementType.Word:
                            sb.AppendFormat("<WORD {0}>", convert((ZilObject)o.Fix ?? o.Variable));
                            break;

                        default:
                            throw new NotImplementedException();
                    }

                    if (o.Constant != null)
                    {
                        sb.Append(')');
                    }
                }

                sb.Append(')');
            }

            sb.Append(')');

            return sb.ToString();
        }

        public override StdAtom StdTypeAtom => StdAtom.PROPDEF;

        public override PrimType PrimType => PrimType.LIST;

        public override ZilObject GetPrimitive(Context ctx)
        {
            throw new NotImplementedException();
        }

        [ChtypeMethod]
#pragma warning disable RECS0154 // Parameter is never used
        public ComplexPropDef(ZilList list)
#pragma warning restore RECS0154 // Parameter is never used
        {
            throw new NotImplementedException();
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
                if (MatchPartialPattern(ctx, ref prop2, p.Inputs, 0, null) && prop2.IsEmpty)
                    return true;
            }

            return false;
        }

        public void PreBuildProperty(Context ctx, ZilList prop, ElementPreBuilders preBuilders)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(prop != null);
            
            var captures = new Dictionary<ZilAtom, Queue<ZilObject>>();

            foreach (var p in patterns)
            {
                // try to match pattern and capture values
                var propBody = prop.Rest;
                captures.Clear();

                if (MatchPartialPattern(ctx, ref propBody, p.Inputs, 0, captures) && propBody.IsEmpty)
                {
                    PartialPreBuild(ctx, captures, preBuilders, p.Outputs, 0, prop.SourceLine);
                    return;
                }
            }

            throw new InterpreterError(InterpreterMessages.Property_0_Initializer_Doesnt_Match_Any_Supported_Patterns, prop.First);
        }

        public void BuildProperty(Context ctx, ZilList prop, ITableBuilder tb, ElementConverters converters)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(prop != null);
            Contract.Requires(tb != null);

            var propName = (ZilAtom)prop.First;

            var captures = new Dictionary<ZilAtom, Queue<ZilObject>>();

            foreach (var p in patterns)
            {
                // try to match pattern and capture values
                var propBody = prop.Rest;
                captures.Clear();

                if (MatchPartialPattern(ctx, ref propBody, p.Inputs, 0, captures) && propBody.IsEmpty)
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
        bool MatchPartialPattern(Context ctx, ref ZilList prop, InputElement[] inputs, int startIndex,
            Dictionary<ZilAtom, Queue<ZilObject>> captures)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(prop != null);
            Contract.Requires(inputs != null);
            Contract.Requires(startIndex >= 0 && startIndex < inputs.Length);
            Contract.Ensures(Contract.ValueAtReturn(out prop) != null);

            for (int i = startIndex; i < inputs.Length; i++)
            {
                var input = inputs[i];
                switch (input.Type)
                {
                    case InputElementType.Atom:
                        if (prop.First != input.Variable)
                            return false;

                        prop = prop.Rest;
                        break;

                    case InputElementType.Variable:
                        if (!CheckInputDecl(ctx, prop.First, input.Decl))
                            return false;

                        if (captures != null)
                        {
                            var atom = input.Variable;
                            Queue<ZilObject> queue;
                            if (!captures.TryGetValue(atom, out queue))
                            {
                                queue = new Queue<ZilObject>(1);
                                captures.Add(atom, queue);
                            }
                            Contract.Assume(queue != null);
                            queue.Enqueue(prop.First);
                        }

                        prop = prop.Rest;
                        break;

                    case InputElementType.Opt:
                        if (!prop.IsEmpty)
                        {
                            if (!MatchPartialPattern(ctx, ref prop, inputs, i + 1, captures))
                                return false;
                        }
                        return true;

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

        bool CheckInputDecl(Context ctx, ZilObject value, ZilObject decl)
        {
            // value can be the name of a constant, in which case we need to check the constant value instead
            if (value is ZilAtom)
            {
                var constant = ctx.GetZVal((ZilAtom)value) as ZilConstant;
                if (constant != null)
                    value = constant.Value;
            }

            var declAtom = decl as ZilAtom;
            if (declAtom != null)
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

        bool PartialPreBuild(Context ctx, Dictionary<ZilAtom, Queue<ZilObject>> captures,
            ElementPreBuilders preBuilders, OutputElement[] outputs, int startIndex, ISourceLine src)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(captures != null);
            Contract.Requires(outputs != null);
            Contract.Requires(startIndex >= 0);
            Contract.Requires(startIndex <= outputs.Length);

            for (int i = startIndex; i < outputs.Length; i++)
            {
                var output = outputs[i];

                ZilObject capturedValue;
                if (output.Variable != null)
                {
                    Queue<ZilObject> queue;
                    if (captures.TryGetValue(output.Variable, out queue))
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

                switch (output.Type)
                {
                    case OutputElementType.Adjective:
                        preBuilders.CreateVocabWord((ZilAtom)capturedValue, ctx.GetStdAtom(StdAtom.ADJ), src);
                        break;

                    case OutputElementType.Noun:
                        preBuilders.CreateVocabWord((ZilAtom)capturedValue, ctx.GetStdAtom(StdAtom.OBJECT), src);
                        break;

                    case OutputElementType.Voc:
                        preBuilders.CreateVocabWord((ZilAtom)capturedValue, output.PartOfSpeech, src);
                        break;

                    case OutputElementType.Global:
                        preBuilders.ReserveGlobal((ZilAtom)capturedValue);
                        break;

                    case OutputElementType.Many:
                        while (PartialPreBuild(ctx, captures, preBuilders, outputs, i + 1, src))
                        {
                            // repeat
                        }
                        break;
                }
            }

            return true;
        }

        bool WritePartialOutput(Context ctx, ITableBuilder tb, ElementConverters converters,
            Dictionary<ZilAtom, Queue<ZilObject>> captures, OutputElement[] outputs, int startIndex,
            ZilAtom propName, ISourceLine src)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(tb != null);
            Contract.Requires(captures != null);
            Contract.Requires(outputs != null);
            Contract.Requires(startIndex >= 0);
            Contract.Requires(startIndex <= outputs.Length);

            ZilFix length;

            for (int i = startIndex; i < outputs.Length; i++)
            {
                var output = outputs[i];

                ZilObject capturedValue;
                if (output.Variable != null)
                {
                    Queue<ZilObject> queue;
                    if (captures.TryGetValue(output.Variable, out queue))
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
                if (capturedValue != null &&
                    output.Type != OutputElementType.Global && output.Type != OutputElementType.Adjective &&
                    output.Type != OutputElementType.Noun && output.Type != OutputElementType.Voc)
                {
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
                        Contract.Assume(capturedConstantValue != null);
                    }
                }
                else
                {
                    capturedConstantValue = null;
                }

                switch (output.Type)
                {
                    case OutputElementType.Length:
                        // TODO: verify length
                        length = output.Fix;
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
                        tb.AddByte(converters.GetGlobalNumber((ZilAtom)capturedValue));
                        break;

                    case OutputElementType.Adjective:
                        if (OutputElementSize(output, ctx) == 1)
                        {
                            tb.AddByte(converters.GetAdjectiveValue((ZilAtom)capturedValue, src));
                        }
                        else
                        {
                            tb.AddShort(converters.GetAdjectiveValue((ZilAtom)capturedValue, src));
                        }
                        break;

                    case OutputElementType.Noun:
                        tb.AddShort(converters.GetVocabWord((ZilAtom)capturedValue, ctx.GetStdAtom(StdAtom.OBJECT), src));
                        break;

                    case OutputElementType.Voc:
                        tb.AddShort(converters.GetVocabWord((ZilAtom)capturedValue, output.PartOfSpeech, src));
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
