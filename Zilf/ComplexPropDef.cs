using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Zilf.Emit;

namespace Zilf
{
    [BuiltinType(StdAtom.PROPDEF, Zilf.PrimType.LIST)]
    sealed class ComplexPropDef : ZilObject
    {
        private enum InputElementType
        {
            Invalid = 0,

            Opt,
            Many,
            Variable,
            Atom,
        }

        private struct InputElement
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

        private enum OutputElementType
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

        private struct OutputElement
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

        private struct Pattern
        {
            public readonly InputElement[] Inputs;
            public readonly OutputElement[] Outputs;

            public Pattern(InputElement[] inputs, OutputElement[] outputs)
            {
                this.Inputs = inputs;
                this.Outputs = outputs;
            }
        }

        private readonly List<Pattern> patterns;

        private ComplexPropDef(IEnumerable<Pattern> patterns)
        {
            this.patterns = new List<Pattern>(patterns);
        }

        public static ComplexPropDef Parse(IEnumerable<ZilObject> spec, Context ctx)
        {
            var inputs = new List<InputElement>();
            var outputs = new List<OutputElement>();
            var patterns = new List<Pattern>();

            foreach (var patternObj in spec)
            {
                if (patternObj.GetTypeAtom(ctx).StdAtom != StdAtom.LIST)
                    throw new InterpreterError("PROPDEF patterns must be lists");

                var list = (ZilList)patternObj;
                bool gotEq = false;
                foreach (var element in list)
                {
                    if (!gotEq)
                    {
                        // inputs
                        switch (element.GetTypeAtom(ctx).StdAtom)
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
                                        throw new InterpreterError("strings in PROPDEF patterns must be \"OPT\" or \"MANY\"");
                                }
                                break;
                        }
                    }
                    else
                    {
                        // outputs
                        ZilAtom constant = null;
                        var output = element;
                        var type = output.GetTypeAtom(ctx).StdAtom;

                        if (type == StdAtom.LIST)
                        {
                            var elemList = (ZilList)element;

                            if (elemList.Rest == null ||
                                elemList.Rest.Rest == null ||
                                elemList.Rest.Rest.First != null)
                            {
                                throw new InterpreterError("list in PROPDEF output pattern must have length 2");
                            }

                            constant = (ZilAtom)elemList.First;     //XXX check before cast and raise error
                            output = elemList.Rest.First;
                        }

                        switch (output.GetTypeAtom(ctx).StdAtom)
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
                                        throw new InterpreterError("string in PROPDEF output pattern must be \"MANY\"");
                                }
                                break;

                            case StdAtom.SEMI:
                                // ignore
                                break;

                            default:
                                throw new InterpreterError("PROPDEF output elements must be FIX, FALSE, FORM, STRING, or SEMI");
                        }
                    }
                }

                if (inputs.Count == 0 || inputs[0].Type != InputElementType.Atom)
                {
                    throw new InterpreterError("PROPDEF pattern must start with an atom");
                }
                inputs.RemoveAt(0);

                if (outputs.Skip(1).Any(e => e.Type == OutputElementType.Length))
                {
                    throw new InterpreterError("FIX/FALSE in PROPDEF output pattern must be at the beginning");
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
                    throw new InterpreterError("variable in PROPDEF output pattern is not captured by input pattern: " + badCapture);
                }

                patterns.Add(new Pattern(inputs.ToArray(), outputs.ToArray()));
                inputs.Clear();
                outputs.Clear();
            }

            return new ComplexPropDef(patterns);
        }

        private static OutputElement ConvertOutputForm(ZilForm form, ZilAtom constant)
        {
            // validate and parse
            var atom = form.First as ZilAtom;
            if (atom == null)
            {
                throw new InterpreterError(form, "FORM in PROPDEF pattern must start with an atom");
            }

            int length = 2;
            OutputElementType type;

            const string SUnexpectedForm = "FORM in PROPDEF output pattern must be BYTE, WORD, STRING, OBJECT, ROOM, GLOBAL, NOUN, ADJ, or VOC";

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
                    throw new InterpreterError(form, SUnexpectedForm);
            }

            if (((IStructure)form).GetLength(length) != length)
            {
                throw new InterpreterError(form, string.Format("{0} FORM in PROPDEF output pattern must have length {1}", atom, length));
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
                throw new InterpreterError(form, string.Format("{0}: first argument must be an LVAL or FIX", atom));
            }

            ZilAtom partOfSpeech = null;
            if (atom.StdAtom == StdAtom.VOC)
            {
                partOfSpeech = form.Rest.Rest.First as ZilAtom;
                if (partOfSpeech == null)
                {
                    throw new InterpreterError(form, string.Format("{0}: second argument must be an atom", atom));
                }
            }

            // done
            return new OutputElement(type, constant, variable: variable, partOfSpeech: partOfSpeech, fix: fix);
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
                        int size = OutputElementSize(output, ctx);
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
                            throw new InterpreterError(string.Format(
                                "PROPDEF constant '{0}' defined at conflicting positions", output.Constant));
                    }
                    else
                    {
                        yield return new KeyValuePair<ZilAtom, int>(output.Constant, constantValue);
                        used.Add(output.Constant, constantValue);
                    }
                }
            }
        }

        private static int OutputElementSize(OutputElement output, Context ctx)
        {
            switch (output.Type)
            {
                case OutputElementType.Adjective:
                    if (ctx.ZEnvironment.ZVersion == 3)
                    {
                        return 1;
                    }
                    else
                    {
                        return 2;
                    }

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
                    else
                    {
                        return 2;
                    }

                case OutputElementType.Room:
                    if (ctx.ZEnvironment.ZVersion == 3 ||
                        ctx.ZEnvironment.ObjectOrdering == ObjectOrdering.RoomsLast ||
                        ctx.ZEnvironment.ObjectOrdering == ObjectOrdering.RoomsAndLocalGlobalsFirst)
                    {
                        return 1;
                    }
                    else
                    {
                        return 2;
                    }

                default:
                    return 0;
            }
        }

        public override string ToString()
        {
            throw new NotImplementedException();
        }

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return ctx.GetStdAtom(StdAtom.PROPDEF);
        }

        public override PrimType PrimType
        {
            get { return PrimType.LIST; }
        }

        public override ZilObject GetPrimitive(Context ctx)
        {
            throw new NotImplementedException();
        }

        [ChtypeMethod]
        public ComplexPropDef(ZilList list)
        {
            throw new NotImplementedException();
        }

        public struct ElementPreBuilders
        {
            public Action<ZilAtom, ZilAtom> CreateVocabWord;
            public Action<ZilAtom> ReserveGlobal;
        }

        public struct ElementConverters
        {
            public Func<ZilObject, IOperand> CompileConstant;
            public Func<ZilAtom, IOperand> GetGlobalNumber;
            public Func<ZilAtom, IOperand> GetAdjectiveValue;       // word => A?constant or word builder
            public Func<ZilAtom, ZilAtom, IOperand> GetVocabWord;   // (word, part of speech) => word builder
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
            var captures = new Dictionary<ZilAtom, Queue<ZilObject>>();

            foreach (var p in patterns)
            {
                // try to match pattern and capture values
                var propBody = prop.Rest;
                captures.Clear();

                if (MatchPartialPattern(ctx, ref propBody, p.Inputs, 0, captures) && propBody.IsEmpty)
                {
                    PartialPreBuild(ctx, captures, preBuilders, p.Outputs, 0);
                    return;
                }
            }

            throw new InterpreterError(string.Format("property '{0}' initializer doesn't match any supported patterns", prop.First));
        }

        public void BuildProperty(Context ctx, ZilList prop, ITableBuilder tb, ElementConverters converters)
        {
            var captures = new Dictionary<ZilAtom, Queue<ZilObject>>();

            foreach (var p in patterns)
            {
                // try to match pattern and capture values
                var propBody = prop.Rest;
                captures.Clear();

                if (MatchPartialPattern(ctx, ref propBody, p.Inputs, 0, captures) && propBody.IsEmpty)
                {
                    // build output
                    WritePartialOutput(ctx, tb, converters, captures, p.Outputs, 0);
                    return;
                }
            }

            throw new InterpreterError(string.Format("property '{0}' initializer doesn't match any supported patterns", prop.First));
        }

        // may change prop even for an unsuccessful match
        // may not match the entire property (check prop.IsEmpty on return)
        private bool MatchPartialPattern(Context ctx, ref ZilList prop, InputElement[] inputs, int startIndex,
            Dictionary<ZilAtom, Queue<ZilObject>> captures)
        {
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

        private bool CheckInputDecl(Context ctx, ZilObject value, ZilObject decl)
        {
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

        private bool PartialPreBuild(Context ctx, Dictionary<ZilAtom, Queue<ZilObject>> captures,
            ElementPreBuilders preBuilders, OutputElement[] outputs, int startIndex)
        {
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
                        preBuilders.CreateVocabWord((ZilAtom)capturedValue, ctx.GetStdAtom(StdAtom.ADJ));
                        break;

                    case OutputElementType.Noun:
                        preBuilders.CreateVocabWord((ZilAtom)capturedValue, ctx.GetStdAtom(StdAtom.OBJECT));
                        break;

                    case OutputElementType.Voc:
                        preBuilders.CreateVocabWord((ZilAtom)capturedValue, output.PartOfSpeech);
                        break;

                    case OutputElementType.Global:
                        preBuilders.ReserveGlobal((ZilAtom)capturedValue);
                        break;

                    case OutputElementType.Many:
                        while (PartialPreBuild(ctx, captures, preBuilders, outputs, i + 1))
                        {
                            // repeat
                        }
                        break;
                }
            }

            return true;
        }

        private bool WritePartialOutput(Context ctx, ITableBuilder tb, ElementConverters converters,
            Dictionary<ZilAtom, Queue<ZilObject>> captures, OutputElement[] outputs, int startIndex)
        {
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

                switch (output.Type)
                {
                    case OutputElementType.Length:
                        length = output.Fix;
                        break;

                    case OutputElementType.Byte:
                        if (capturedValue != null)
                        {
                            tb.AddByte(converters.CompileConstant(capturedValue));
                        }
                        else
                        {
                            tb.AddByte((byte)output.Fix.Value);
                        }
                        break;

                    case OutputElementType.Word:
                        if (capturedValue != null)
                        {
                            tb.AddShort(converters.CompileConstant(capturedValue));
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
                            tb.AddByte(converters.CompileConstant(capturedValue));
                        }
                        else
                        {
                            tb.AddShort(converters.CompileConstant(capturedValue));
                        }
                        break;

                    case OutputElementType.String:
                        tb.AddShort(converters.CompileConstant(capturedValue));
                        break;

                    case OutputElementType.Global:
                        tb.AddByte(converters.GetGlobalNumber((ZilAtom)capturedValue));
                        break;

                    case OutputElementType.Adjective:
                        if (OutputElementSize(output, ctx) == 1)
                        {
                            tb.AddByte(converters.GetAdjectiveValue((ZilAtom)capturedValue));
                        }
                        else
                        {
                            tb.AddShort(converters.GetAdjectiveValue((ZilAtom)capturedValue));
                        }
                        break;

                    case OutputElementType.Noun:
                        tb.AddShort(converters.GetVocabWord((ZilAtom)capturedValue, ctx.GetStdAtom(StdAtom.OBJECT)));
                        break;

                    case OutputElementType.Voc:
                        tb.AddShort(converters.GetVocabWord((ZilAtom)capturedValue, output.PartOfSpeech));
                        break;

                    case OutputElementType.Many:
                        while (WritePartialOutput(ctx, tb, converters, captures, outputs, i + 1))
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
