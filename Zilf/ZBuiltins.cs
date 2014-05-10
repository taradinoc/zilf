using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Zilf.Emit;

namespace Zilf
{
    partial class Compiler
    {
        struct VoidCall
        {
            public CompileCtx cc;
            public IRoutineBuilder rb;
            public ZilForm form;
        }

        struct ValueCall
        {
            public CompileCtx cc;
            public IRoutineBuilder rb;
            public ZilForm form;

            public IVariable resultStorage;
        }

        struct PredCall
        {
            public CompileCtx cc;
            public IRoutineBuilder rb;
            public ZilForm form;

            public ILabel label;
            public bool polarity;
        }

        static class ZBuiltins
        {
            #region Attributes

            [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
            class BuiltinAttribute : Attribute
            {
                public BuiltinAttribute(string name)
                    : this(name, null)
                {
                }

                public BuiltinAttribute(string name, params string[] aliases)
                {
                    this.name = name;
                    this.aliases = aliases;

                    MinVersion = 1;
                    MaxVersion = 6;
                }

                private readonly string name;
                private readonly string[] aliases;

                public IEnumerable<string> Names
                {
                    get
                    {
                        yield return name;

                        if (aliases != null)
                        {
                            foreach (var a in aliases)
                                yield return a;
                        }
                    }
                }

                public object Data { get; set; }
                public int MinVersion { get; set; }
                public int MaxVersion { get; set; }
                public bool HasSideEffect { get; set; }
            }

            /// <summary>
            /// Indicates the parameter where the value of <see cref="BuiltinAttribute.Data"/>
            /// should be passed in. This does not correspond to a ZIL parameter.
            /// </summary>
            [AttributeUsage(AttributeTargets.Parameter)]
            class DataAttribute : Attribute
            {
            }

            /// <summary>
            /// Indicates that the parameter will be used as an object.
            /// </summary>
            [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
            class ObjectAttribute : Attribute
            {
            }

            /// <summary>
            /// Indicates that the parameter will be used as a table address.
            /// </summary>
            [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
            class TableAttribute : Attribute
            {
            }

            /// <summary>
            /// Indicates that the parameter will be used as a variable index.
            /// The caller may pass an atom, which will be interpreted as the name of
            /// a variable, and its index will be used instead of its value.
            /// </summary>
            /// <seealso cref="IVariable.Indirect"/>
            [AttributeUsage(AttributeTargets.Parameter)]
            class VariableAttribute : Attribute
            {
                /// <summary>
                /// If true, then even a reference to a variable's value via
                /// &lt;LVAL X&gt; or &lt;GVAL X&gt; (or .X or ,X) will be interpreted
                /// as referring to its index. Use &lt;VALUE X&gt; to force the
                /// value to be used.
                /// </summary>
                public bool QuirksMode { get; set; }
            }

            #endregion

            private class BuiltinSpec
            {
                public readonly int MinArgs;
                public readonly int? MaxArgs;
                public readonly Type CallType;

                public readonly BuiltinAttribute Attr;
                public readonly MethodInfo Method;

                public BuiltinSpec(BuiltinAttribute attr, MethodInfo method)
                {
                    try
                    {
                        this.Attr = attr;
                        this.Method = method;

                        // count args and find call type
                        int min = 0;
                        int? max = 0;
                        Type dataParamType = null;
                        var parameters = method.GetParameters();

                        for (int i = 0; i < parameters.Length; i++)
                        {
                            var pi = parameters[i];

                            if (i == 0)
                            {
                                // first parameter: call type
                                CallType = pi.ParameterType;

                                if (CallType != typeof(VoidCall) && CallType != typeof(ValueCall) && CallType != typeof(PredCall))
                                    throw new ArgumentException("Unexpected call parameter type");

                                continue;
                            }

                            var pattrs = pi.GetCustomAttributes(false);

                            if (pattrs.Any(a => a is DataAttribute))
                            {
                                // data parameter: must be the second parameter
                                if (pi.Position != 1)
                                    throw new ArgumentException("[Data] parameter must be the second parameter");

                                dataParamType = pi.ParameterType;
                                continue;
                            }

                            if (pi.ParameterType == typeof(IOperand) || pi.ParameterType == typeof(string))
                            {
                                // regular operand: may be optional
                                max++;
                                if (!pi.IsOptional)
                                    min++;
                                continue;
                            }

                            if (pi.ParameterType == typeof(IVariable))
                            {
                                // indirect variable operand: must have [Variable]
                                if (!pattrs.Any(a => a is VariableAttribute))
                                    throw new ArgumentException("IVariable parameter must be marked [Variable]");

                                max++;
                                min++;
                                continue;
                            }

                            if (pi.ParameterType == typeof(IOperand[]))
                            {
                                // varargs: must be the last parameter and marked [Params]
                                if (i != parameters.Length - 1)
                                    throw new ArgumentException("Operand array must be the last parameter");
                                if (!pattrs.Any(a => a is ParamArrayAttribute))
                                    throw new ArgumentException("Operand array must be marked [ParamArray]");

                                max = null;
                                continue;
                            }

                            // unrecognized type
                            throw new ArgumentException("Inscrutable parameter: " + pi.Name);
                        }

                        this.MinArgs = min;
                        this.MaxArgs = max;

                        // validate [Data] parameter vs. Data attribute property
                        if (dataParamType != null)
                        {
                            if (attr.Data == null || attr.Data.GetType() != dataParamType)
                            {
                                throw new ArgumentException("BuiltinAttribute.Data type must match the [Data] parameter");
                            }
                        }
                        else if (attr.Data != null)
                        {
                            throw new ArgumentException("BuiltinAttribute.Data must be null if no [Data] parameter");
                        }

                        // validate return type vs. call type
                        if (CallType == typeof(ValueCall) && method.ReturnType != typeof(IOperand))
                        {
                            throw new ArgumentException("Value call must return IOperand");
                        }
                        else if (CallType == typeof(VoidCall) && method.ReturnType != typeof(void))
                        {
                            throw new ArgumentException("Void call must return void");
                        }
                        else if (CallType == typeof(PredCall) && method.ReturnType != typeof(void))
                        {
                            throw new ArgumentException("Predicate call must return void");
                        }
                    }
                    catch (ArgumentException ex)
                    {
                        throw new ArgumentException(
                            string.Format(
                                "Bad attribute {0} on method {1}",
                                attr.Names.First(), method.Name),
                            ex);
                    }
                }

                public static bool VersionMatches(int candidate, int rangeMin, int rangeMax)
                {
                    // treat V7-8 just like V5 for the purpose of this check
                    if (candidate == 7 || candidate == 8)
                        candidate = 5;

                    return (candidate >= rangeMin) && (candidate <= rangeMax);
                }

                public bool AppliesTo(int zversion, int argCount, Type callType)
                {
                    if (!VersionMatches(zversion, Attr.MinVersion, Attr.MaxVersion))
                        return false;

                    if (argCount < MinArgs || (MaxArgs != null && argCount > MaxArgs))
                        return false;

                    if (this.CallType != callType)
                        return false;

                    return true;
                }
            }

            private static ILookup<string, BuiltinSpec> builtins;

            static ZBuiltins()
            {
                var query = from mi in typeof(ZBuiltins).GetMethods(BindingFlags.Public | BindingFlags.Static)
                            from BuiltinAttribute a in mi.GetCustomAttributes(typeof(BuiltinAttribute), false)
                            from name in a.Names
                            select new { Name = name, Attr = a, Method = mi };

                builtins = query.ToLookup(r => r.Name, r => new BuiltinSpec(r.Attr, r.Method));
            }

            public static bool IsBuiltinValueCall(string name, int zversion, int argCount)
            {
                return builtins[name].Any(s => s.AppliesTo(zversion, argCount, typeof(ValueCall)));
            }

            public static bool IsBuiltinVoidCall(string name, int zversion, int argCount)
            {
                return builtins[name].Any(s => s.AppliesTo(zversion, argCount, typeof(VoidCall)));
            }

            public static bool IsBuiltinPredCall(string name, int zversion, int argCount)
            {
                return builtins[name].Any(s => s.AppliesTo(zversion, argCount, typeof(PredCall)));
            }

            private delegate void InvalidArgumentDelegate(int index, string message);

            private static void ValidateArguments(
                CompileCtx cc, BuiltinSpec spec, ParameterInfo[] builtinParamInfos,
                ZilObject[] args, InvalidArgumentDelegate error)
            {
                // args may be short (for optional params)

                for (int i = 0, j = spec.Attr.Data == null ? 1 : 2; i < args.Length; i++, j++)
                {
                    var arg = args[i];
                    var pi = builtinParamInfos[j];

                    if (pi.ParameterType == typeof(IVariable))
                    {
                        // arg must be an atom, or <GVAL atom> or <LVAL atom> in quirks mode
                        ZilAtom atom = arg as ZilAtom;
                        bool quirks = false;
                        if (arg == null)
                        {
                            var attr = pi.GetCustomAttributes(typeof(VariableAttribute), false).Cast<VariableAttribute>().Single();
                            quirks = attr.QuirksMode;
                            if (quirks && arg is ZilForm)
                            {
                                var form = (ZilForm)arg;
                                var fatom = form.First as ZilAtom;
                                if (atom != null &&
                                    (atom.StdAtom == StdAtom.GVAL || atom.StdAtom == StdAtom.LVAL) &&
                                    form.Rest.First is ZilAtom &&
                                    form.Rest.Rest.IsEmpty)
                                {
                                    atom = (ZilAtom)form.Rest.First;
                                }
                            }
                        }

                        if (atom == null)
                        {
                            const string SMustBeVar = "argument must be a variable";
                            const string SMustBeVarQuirks = "argument must be a variable or GVAL/LVAL reference";
                            error(i, quirks ? SMustBeVarQuirks : SMustBeVar);
                        }
                        else
                        {
                            ILocalBuilder local;
                            IGlobalBuilder global;

                            if (!cc.Locals.TryGetValue(atom, out local) && !cc.Globals.TryGetValue(atom, out global))
                                error(i, "no such variable: " + atom.ToString());
                        }
                    }
                    else if (pi.ParameterType == typeof(string))
                    {
                        // arg must be a string
                        if (!(arg is ZilString))
                            error(i, "argument must be a literal string");
                    }
                    else if (pi.ParameterType == typeof(IOperand[]))
                    {
                        // this absorbs the rest of the args, so we're done
                        break;
                    }
                }
            }

            private static void PartitionArguments(
                BuiltinSpec spec, ParameterInfo[] builtinParamInfos,
                ZilObject[] args,
                out ZilObject[] unevaled, out ZilObject[] evaled)
            {
                /* args.Length may differ from builtinParamInfos.Length, due to
                 * optional arguments and params arrays. */

                int startIdx = spec.Attr.Data == null ? 1 : 2;
                var unevaledList = new List<ZilObject>();
                var evaledList = new List<ZilObject>(builtinParamInfos.Length - startIdx);

                for (int i = startIdx, j = 0; j < args.Length; i++, j++)
                {
                    // partition args[j] based on builtinParamInfos[i]
                    var pi = builtinParamInfos[i];

                    if (pi.ParameterType == typeof(IOperand))
                    {
                        evaledList.Add(args[j]);
                    }
                    else if (pi.ParameterType == typeof(IOperand[]))
                    {
                        evaledList.AddRange(args.Skip(j));
                        break;
                    }
                    else if (pi.ParameterType == typeof(IVariable) || pi.ParameterType == typeof(string))
                    {
                        unevaledList.Add(args[j]);
                    }
                    else
                    {
                        throw new NotImplementedException("Unexpected parameter type");
                    }
                }

                unevaled = unevaledList.ToArray();
                evaled = evaledList.ToArray();
            }

            private static List<object> MakeBuiltinMethodParams(
                CompileCtx cc, BuiltinSpec spec,
                ParameterInfo[] builtinParamInfos, object call,
                ZilObject[] unevaledOperands, Operands evaledOperands)
            {
                /* unevaledOperands.Length + evaledOperands.Count may differ from
                 * builtinParamInfos.Length, due to optional arguments and params arrays. */

                var result = new List<object>(builtinParamInfos.Length);

                // call
                result.Add(call);

                // data (optional)
                int i = 1;
                if (spec.Attr.Data != null)
                {
                    result.Add(spec.Attr.Data);
                    i++;
                }

                // operands
                for (int u = 0, e = 0; i < builtinParamInfos.Length; i++)
                {
                    var pi = builtinParamInfos[i];

                    if (pi.ParameterType == typeof(IOperand[]))
                    {
                        // add all remaining evaled operands as a param array
                        if (e >= evaledOperands.Count)
                        {
                            result.Add(new IOperand[0]);
                        }
                        else
                        {
                            result.Add(evaledOperands.Skip(e).ToArray());
                        }
                        break;
                    }
                    else if (pi.ParameterType == typeof(IOperand))
                    {
                        if (e >= evaledOperands.Count)
                        {
                            result.Add(pi.DefaultValue);
                        }
                        else
                        {
                            // XXX check for [Variable]

                            // one evaled operand
                            result.Add(evaledOperands[e++]);
                        }
                    }
                    else if (pi.ParameterType == typeof(string))
                    {
                        if (u >= unevaledOperands.Length)
                        {
                            result.Add(pi.DefaultValue);
                        }
                        else
                        {
                            // one unevaled operand: convert it to a string
                            var obj = unevaledOperands[u++];
                            System.Diagnostics.Debug.Assert(obj is ZilString);
                            result.Add(Compiler.TranslateString(((ZilString)obj).Text));
                        }
                    }
                    else if (pi.ParameterType == typeof(IVariable))
                    {
                        if (u >= unevaledOperands.Length)
                        {
                            result.Add(pi.DefaultValue);
                        }
                        else
                        {
                            // one unevaled operand: convert it to a variable
                            var obj = unevaledOperands[u++];
                            var atom = obj as ZilAtom;

                            if (atom == null)
                            {
                                System.Diagnostics.Debug.Assert(obj is ZilForm);
                                var form = (ZilForm)obj;
                                System.Diagnostics.Debug.Assert(form.Rest != null && form.Rest.First is ZilAtom);
                                atom = (ZilAtom)form.Rest.First;
                            }

                            ILocalBuilder local;
                            IGlobalBuilder global;

                            if (cc.Locals.TryGetValue(atom, out local))
                            {
                                result.Add(local);
                            }
                            else if (cc.Globals.TryGetValue(atom, out global))
                            {
                                result.Add(global);
                            }
                            else
                            {
                                // shouldn't get here
                                throw new NotImplementedException("undefined variable for IVariable parameter");
                            }
                        }
                    }
                }

                return result;
            }

            private static object CompileBuiltinCall<TCall>(string name, CompileCtx cc, IRoutineBuilder rb, ZilForm form, TCall call)
            {
                int zversion = cc.Context.ZEnvironment.ZVersion;
                var argList = form.Rest;
                var args = argList.ToArray();
                var spec = builtins[name].Single(s => s.AppliesTo(zversion, args.Length, typeof(TCall)));
                var builtinParamInfos = spec.Method.GetParameters();

                // validate arguments
                bool valid = true;
                ValidateArguments(cc, spec, builtinParamInfos, args,
                    (i, msg) =>
                    {
                        Errors.CompError(cc.Context, form, "{0} argument {1}: {2}",
                            name, i + 1, msg);
                        valid = false;
                    });

                if (!valid)
                    return cc.Game.Zero;

                // partition unevaluated vs. evaluated arguments
                ZilObject[] unevaled, evaled;
                PartitionArguments(spec, builtinParamInfos, args, out unevaled, out evaled);

                // generate code for arguments
                using (var operands = Operands.Compile(cc, rb, evaled))
                {
                    // call the spec method to generate code for the builtin
                    var builtinParams = MakeBuiltinMethodParams(cc, spec, builtinParamInfos, call, unevaled, operands);
                    System.Diagnostics.Debug.Assert(builtinParams.Count == builtinParamInfos.Length);
                    return spec.Method.Invoke(null, builtinParams.ToArray());
                }
            }

            public static IOperand CompileValueCall(string name, CompileCtx cc, IRoutineBuilder rb, ZilForm form, IVariable resultStorage)
            {
                // TODO: allow resultStorage to be passed as null to handlers that want it? are there any?
                return (IOperand)CompileBuiltinCall(name, cc, rb, form, 
                    new ValueCall() { cc = cc, rb = rb, form = form, resultStorage = resultStorage ?? rb.Stack });
            }

            public static void CompileVoidCall(string name, CompileCtx cc, IRoutineBuilder rb, ZilForm form)
            {
                CompileBuiltinCall(name, cc, rb, form, new VoidCall() { cc = cc, rb = rb, form = form });
            }

            public static void CompilePredCall(string name, CompileCtx cc, IRoutineBuilder rb, ZilForm form, ILabel label, bool polarity)
            {
                CompileBuiltinCall(name, cc, rb, form,
                    new PredCall() { cc = cc, rb = rb, form = form, label = label, polarity = polarity });
            }

            [Builtin("EQUAL?", "=?", "==?")]
            public static void VarargsEqualityOp(
                PredCall c, IOperand arg1, IOperand arg2,
                IOperand arg3 = null, IOperand arg4 = null)
            {
                // TODO: there should really just be one BranchIfEqual with optional params
                if (arg4 != null)
                {
                    c.rb.BranchIfEqual(arg1, arg2, arg3, arg4, c.label, c.polarity);
                }
                else if (arg3 != null)
                {
                    c.rb.BranchIfEqual(arg1, arg2, arg3, c.label, c.polarity);
                }
                else
                {
                    c.rb.BranchIfEqual(arg1, arg2, c.label, c.polarity);
                }
            }

            [Builtin("PUT", Data = TernaryOp.PutWord, HasSideEffect = true)]
            [Builtin("PUTB", Data = TernaryOp.PutByte, HasSideEffect = true)]
            public static void TernaryTableVoidOp(
                VoidCall c, [Data] TernaryOp op,
                [Table] IOperand left, IOperand center, IOperand right)
            {
                c.rb.EmitTernary(op, left, center, right, null);
            }

            [Builtin("PUTP", Data = TernaryOp.PutProperty, HasSideEffect = true)]
            public static void TernaryObjectVoidOp(
                VoidCall c, [Data] TernaryOp op,
                [Object] IOperand left, IOperand center, IOperand right)
            {
                c.rb.EmitTernary(op, left, center, right, null);
            }

            [Builtin("COPYT", Data = TernaryOp.CopyTable, HasSideEffect = true, MinVersion = 5)]
            public static void TernaryTableTableVoidOp(
                VoidCall c, [Data] TernaryOp op,
                [Table] IOperand left, [Table] IOperand center, IOperand right)
            {
                c.rb.EmitTernary(op, left, center, right, null);
            }

            [Builtin("ADD", "+", Data = BinaryOp.Add)]
            [Builtin("SUB", "-", Data = BinaryOp.Sub)]
            [Builtin("MUL", "*", Data = BinaryOp.Mul)]
            [Builtin("DIV", "/", Data = BinaryOp.Div)]
            [Builtin("MOD", Data = BinaryOp.Mod)]
            [Builtin("BAND", "ANDB", Data = BinaryOp.And)]
            [Builtin("BOR", "ORB", Data = BinaryOp.Or)]
            [Builtin("ASH", "ASHIFT", Data = BinaryOp.ArtShift, MinVersion = 5)]
            [Builtin("LSH", "SHIFT", Data = BinaryOp.LogShift, MinVersion = 5)]
            public static IOperand BinaryValueOp(
                ValueCall c, [Data] BinaryOp op, IOperand left, IOperand right)
            {
                c.rb.EmitBinary(op, left, right, c.resultStorage);
                return c.resultStorage;
            }

            [Builtin("REST", Data = BinaryOp.Add)]
            [Builtin("BACK", Data = BinaryOp.Sub)]
            public static IOperand RestOrBackOp(
                ValueCall c, [Data] BinaryOp op, IOperand left, IOperand right = null)
            {
                return BinaryValueOp(c, op, left, right ?? c.cc.Game.One);
            }

            [Builtin("CURSET", Data = BinaryOp.SetCursor, MinVersion = 4, HasSideEffect = true)]
            [Builtin("COLOR", Data = BinaryOp.SetColor, MinVersion = 5, HasSideEffect = true)]
            [Builtin("DIROUT", Data = BinaryOp.DirectOutput, HasSideEffect = true)]
            [Builtin("THROW", Data = BinaryOp.Throw, HasSideEffect = true, MinVersion = 5)]
            public static void BinaryVoidOp(
                VoidCall c, [Data] BinaryOp op, IOperand left, IOperand right)
            {
                c.rb.EmitBinary(op, left, right, null);
            }

            [Builtin("GRTR?", "G?", Data = Condition.Greater)]
            [Builtin("LESS?", "L?", Data = Condition.Less)]
            [Builtin("BTST", Data = Condition.TestBits)]
            public static void BinaryPredOp(
                PredCall c, [Data] Condition cond, IOperand left, IOperand right)
            {
                c.rb.Branch(cond, left, right, c.label, c.polarity);
            }

            [Builtin("DLESS?", Data = Condition.DecCheck)]
            [Builtin("IGRTR?", Data = Condition.IncCheck)]
            public static void BinaryVariablePredOp(
                PredCall c, [Data] Condition cond, [Variable] IVariable left, IOperand right)
            {
                c.rb.Branch(cond, left, right, c.label, c.polarity);
            }

            [Builtin("GETP", Data = BinaryOp.GetProperty)]
            [Builtin("NEXTP", Data = BinaryOp.GetNextProp)]
            public static IOperand BinaryObjectValueOp(
                ValueCall c, [Data] BinaryOp op, [Object] IOperand left, IOperand right)
            {
                c.rb.EmitBinary(op, left, right, c.resultStorage);
                return c.resultStorage;
            }

            [Builtin("FSET", Data = BinaryOp.SetFlag, HasSideEffect = true)]
            [Builtin("FCLEAR", Data = BinaryOp.ClearFlag, HasSideEffect = true)]
            public static void BinaryObjectVoidOp(
                VoidCall c, [Data] BinaryOp op, [Object] IOperand left, IOperand right)
            {
                c.rb.EmitBinary(op, left, right, null);
            }

            [Builtin("MOVE", Data = BinaryOp.MoveObject, HasSideEffect = true)]
            public static void BinaryObjectObjectVoidOp(
                VoidCall c, [Data] BinaryOp op, [Object] IOperand left, [Object] IOperand right)
            {
                c.rb.EmitBinary(op, left, right, null);
            }

            [Builtin("GETPT", Data = BinaryOp.GetProperty)]
            [return: Table]
            public static IOperand BinaryObjectToTableValueOp(
                ValueCall c, [Data] BinaryOp op, [Object] IOperand left, IOperand right)
            {
                c.rb.EmitBinary(op, left, right, c.resultStorage);
                return c.resultStorage;
            }

            [Builtin("GET", "NTH", Data = BinaryOp.GetWord)]
            [Builtin("GETB", Data = BinaryOp.GetByte)]
            public static IOperand BinaryTableValueOp(
                ValueCall c, [Data] BinaryOp op, [Table] IOperand left, IOperand right)
            {
                c.rb.EmitBinary(op, left, right, c.resultStorage);
                return c.resultStorage;
            }

            [Builtin("BCOM", Data = UnaryOp.Not)]
            [Builtin("RANDOM", Data = UnaryOp.Random, HasSideEffect = true)]
            [Builtin("SUB", "-", Data = UnaryOp.Neg)]
            [Builtin("FONT", Data = UnaryOp.SetFont, MinVersion = 5, HasSideEffect = true)]
            [Builtin("CHECKU", Data = UnaryOp.CheckUnicode, MinVersion = 5)]
            public static IOperand UnaryValueOp(
                ValueCall c, [Data] UnaryOp op, IOperand value)
            {
                c.rb.EmitUnary(op, value, c.resultStorage);
                return c.resultStorage;
            }

            [Builtin("DIRIN", Data = UnaryOp.DirectInput, HasSideEffect = true)]
            [Builtin("DIROUT", Data = UnaryOp.DirectOutput, HasSideEffect = true)]
            [Builtin("BUFOUT", Data = UnaryOp.OutputBuffer, MinVersion = 4, HasSideEffect = true)]
            [Builtin("HLIGHT", Data = UnaryOp.OutputStyle, HasSideEffect = true)]
            [Builtin("CLEAR", Data = UnaryOp.ClearWindow, MinVersion = 4, HasSideEffect = true)]
            [Builtin("SCREEN", Data = UnaryOp.SelectWindow, HasSideEffect = true)]
            [Builtin("SPLIT", Data = UnaryOp.SplitWindow, HasSideEffect = true)]
            [Builtin("ERASE", Data = UnaryOp.EraseLine, MinVersion = 4, HasSideEffect = true)]
            public static void UnaryVoidOp(
                VoidCall c, [Data] UnaryOp op, IOperand value)
            {
                c.rb.EmitUnary(op, value, null);
            }

            [Builtin("ZERO?", "0?")]
            public static void ZeroPredOp(PredCall c, IOperand value)
            {
                c.rb.BranchIfZero(value, c.label, c.polarity);
            }

            [Builtin("1?")]
            public static void OnePredOp(PredCall c, IOperand value)
            {
                c.rb.BranchIfEqual(value, c.cc.Game.One, c.label, c.polarity);
            }

            [Builtin("FIRST?", Data = UnaryOp.GetChild)]
            [Builtin("NEXT?", Data = UnaryOp.GetSibling)]
            [Builtin("LOC", Data = UnaryOp.GetParent)]
            public static IOperand UnaryObjectValueOp(
                ValueCall c, [Data] UnaryOp op, [Object] IOperand obj)
            {
                c.rb.EmitUnary(op, obj, c.resultStorage);
                return c.resultStorage;
            }

            [Builtin("FIRST?", Data = Condition.HasChild)]
            [Builtin("NEXT?", Data = Condition.HasSibling)]
            public static void UnaryObjectPredOp(
                PredCall c, [Data] Condition cond, [Object] IOperand obj)
            {
                c.rb.Branch(cond, obj, null, c.label, c.polarity);
            }

            [Builtin("PTSIZE", Data = UnaryOp.GetPropSize)]
            public static IOperand UnaryTableValueOp(
                ValueCall c, [Data] UnaryOp op, [Table] IOperand value)
            {
                c.rb.EmitUnary(op, value, c.resultStorage);
                return c.resultStorage;
            }

            [Builtin("REMOVE", Data = UnaryOp.RemoveObject, HasSideEffect = true)]
            public static void UnaryObjectVoidOp(
                VoidCall c, [Data] UnaryOp op, [Object] IOperand value)
            {
                c.rb.EmitUnary(op, value, null);
            }

            [Builtin("ASSIGNED?", Data = Condition.ArgProvided, MinVersion = 5)]
            public static void UnaryVariablePredOp(
                PredCall c, [Data] Condition cond, [Variable] IVariable var)
            {
                c.rb.Branch(cond, var, null, c.label, c.polarity);
            }

            [Builtin("CURGET", Data = UnaryOp.GetCursor, MinVersion = 4, HasSideEffect = true)]
            public static void UnaryTableVoidOp(
                VoidCall c, [Data] UnaryOp op, [Table] IOperand value)
            {
                c.rb.EmitUnary(op, value, null);
            }

            [Builtin("PRINT", Data = PrintOp.PackedAddr, HasSideEffect = true)]
            [Builtin("PRINTB", Data = PrintOp.Address, HasSideEffect = true)]
            [Builtin("PRINTC", Data = PrintOp.Character, HasSideEffect = true)]
            [Builtin("PRINTD", Data = PrintOp.Object, HasSideEffect = true)]
            [Builtin("PRINTN", Data = PrintOp.Number, HasSideEffect = true)]
            [Builtin("PRINTU", Data = PrintOp.Unicode, HasSideEffect = true)]
            public static void UnaryPrintVoidOp(
                VoidCall c, [Data] PrintOp op, IOperand value)
            {
                c.rb.EmitPrint(op, value);
            }

            [Builtin("PRINTT", HasSideEffect = true)]
            public static void PrintTableOp(
                VoidCall c, [Table] IOperand table, IOperand width,
                IOperand height = null, IOperand skip = null)
            {
                c.rb.EmitPrintTable(table, width, height, skip);
            }

            [Builtin("PRINTI", Data = false, HasSideEffect = true)]
            [Builtin("PRINTR", Data = true, HasSideEffect = true)]
            public static void UnaryPrintStringOp(
                VoidCall c, [Data] bool crlfRtrue, string text)
            {
                c.rb.EmitPrint(text, crlfRtrue);
            }

            [Builtin("SET", "SETG", HasSideEffect = true)]
            public static IOperand SetValueOp(
                ValueCall c, [Variable(QuirksMode = true)] IVariable dest, IOperand value)
            {
                // in value context, we need to be able to return the newly set value,
                // so dest is IVariable. this means <SET <fancy-expression> value> isn't
                // supported in value context.
                c.rb.EmitStore(dest, value);
                return dest;
            }

            /* XXX need a test case for this!
            [Builtin("SET", "SETG")]
            public static void SetVoidOp(
                VoidCall c, [Variable(QuirksMode = true)] IOperand dest, IOperand value)
            {
                // in void context, we don't need to return the newly set value, so we
                // can support <SET <fancy-expression> value>.
                c.rb.EmitBinary(BinaryOp.StoreIndirect, dest, value, null);
            }*/

            [Builtin("INC", Data = BinaryOp.Add, HasSideEffect = true)]
            [Builtin("DEC", Data = BinaryOp.Sub, HasSideEffect = true)]
            public static IOperand IncValueOp(ValueCall c, [Data] BinaryOp op,
                [Variable] IVariable victim)
            {
                c.rb.EmitBinary(op, victim, c.cc.Game.One, victim);
                return victim;
            }

            [Builtin("POP", MaxVersion = 5, HasSideEffect = true)]
            public static void PopVoidOp(VoidCall c, [Variable] IVariable var)
            {
                c.rb.EmitStore(var, c.rb.Stack);
            }

            [Builtin("PUSH", HasSideEffect = true)]
            public static void PushVoidOp(VoidCall c, IOperand value)
            {
                c.rb.EmitStore(c.rb.Stack, value);
            }

            [Builtin("CRLF", HasSideEffect = true)]
            public static void CrlfVoidOp(VoidCall c)
            {
                c.rb.EmitPrintNewLine();
            }

            [Builtin("CATCH", Data = NullaryOp.Catch, MinVersion = 5)]
            [Builtin("ISAVE", Data = NullaryOp.SaveUndo, HasSideEffect = true, MinVersion = 5)]
            [Builtin("IRESTORE", Data = NullaryOp.RestoreUndo, HasSideEffect = true, MinVersion = 5)]
            [Builtin("USL", Data = NullaryOp.ShowStatus, HasSideEffect = true)]
            public static IOperand NullaryValueOp(ValueCall c, [Data] NullaryOp op)
            {
                c.rb.EmitNullary(NullaryOp.Catch, c.resultStorage);
                return c.resultStorage;
            }

            [Builtin("ORIGINAL?", Data = Condition.Original, MinVersion = 5)]
            [Builtin("VERIFY", Data = Condition.Verify)]
            public static void NullaryPredOp(PredCall c, [Data] Condition cond)
            {
                c.rb.Branch(cond, null, null, c.label, c.polarity);
            }

            [Builtin("RTRUE", Data = 1, HasSideEffect = true)]
            [Builtin("RFALSE", Data = 0, HasSideEffect = true)]
            [Builtin("RSTACK", Data = -1, HasSideEffect = true)]
            public static void NullaryReturnOp(VoidCall c, [Data] int what)
            {
                var operand =
                    (what == 1) ? c.cc.Game.One :
                    (what == 0) ? c.cc.Game.Zero : c.rb.Stack;

                c.rb.Return(operand);
            }

            [Builtin("RESTART", HasSideEffect = true)]
            public static void RestartOp(VoidCall c)
            {
                c.rb.EmitRestart();
            }

            [Builtin("QUIT", HasSideEffect = true)]
            public static void QuitOp(VoidCall c)
            {
                c.rb.EmitQuit();
            }

            [Builtin("READ", MaxVersion = 3, HasSideEffect = true)]
            public static void ReadOp_V3(VoidCall c, IOperand text, IOperand parse)
            {
                c.rb.EmitRead(text, parse, null, null, null);
            }

            [Builtin("READ", MinVersion = 4, MaxVersion = 4, HasSideEffect = true)]
            public static void ReadOp_V4(VoidCall c, IOperand text, IOperand parse,
                IOperand time = null, IOperand routine = null)
            {
                c.rb.EmitRead(text, parse, time, routine, null);
            }

            [Builtin("READ", MinVersion = 5, HasSideEffect = true)]
            public static IOperand ReadOp_V5(ValueCall c, IOperand text,
                IOperand parse = null, IOperand time = null, IOperand routine = null)
            {
                c.rb.EmitRead(text, parse, time, routine, c.resultStorage);
                return c.resultStorage;
            }

            [Builtin("SOUND", MaxVersion = 4, HasSideEffect = true)]
            public static void SoundOp_V3(VoidCall c, IOperand number,
                IOperand effect = null, IOperand volume = null)
            {
                c.rb.EmitPlaySound(number, effect, volume, null);
            }

            [Builtin("SOUND", MinVersion = 5, HasSideEffect = true)]
            public static void SoundOp_V5(VoidCall c, IOperand number,
                IOperand effect = null, IOperand volume = null, IOperand routine = null)
            {
                c.rb.EmitPlaySound(number, effect, volume, null);
            }

            [Builtin("ZWSTR", MinVersion = 5, HasSideEffect = true)]
            public static void EncodeTextOp(VoidCall c,
                [Table] IOperand src, IOperand length,
                IOperand srcOffset, [Table] IOperand dest)
            {
                c.rb.EmitEncodeText(src, length, srcOffset, dest);
            }

            [Builtin("RESTORE", MaxVersion = 3, HasSideEffect = true)]
            public static void RestoreOp_V3(PredCall c)
            {
                if (c.rb.HasBranchSave)
                {
                    c.rb.EmitRestore(c.label, c.polarity);
                }
                else
                {
                    throw new NotImplementedException("RestoreOp_V3 without HasBranchSave");
                }
            }

            [Builtin("RESTORE", MinVersion = 4, MaxVersion = 4, HasSideEffect = true)]
            public static IOperand RestoreOp_V4(ValueCall c)
            {
                if (c.rb.HasStoreSave)
                {
                    c.rb.EmitRestore(c.resultStorage);
                    return c.resultStorage;
                }
                else
                {
                    throw new NotImplementedException("RestoreOp_V4 without HasStoreSave");
                }
            }

            [Builtin("RESTORE", MinVersion = 5, HasSideEffect = true)]
            public static IOperand RestoreOp_V5(ValueCall c, [Table] IOperand table,
                IOperand bytes, [Table] IOperand name)
            {
                if (c.rb.HasExtendedSave)
                {
                    c.rb.EmitRestore(table, bytes, name, c.resultStorage);
                    return c.resultStorage;
                }
                else
                {
                    throw new NotImplementedException("RestoreOp_V5 without HasExtendedSave");
                }
            }

            [Builtin("SAVE", MaxVersion = 3, HasSideEffect = true)]
            public static void SaveOp_V3(PredCall c)
            {
                if (c.rb.HasBranchSave)
                {
                    c.rb.EmitSave(c.label, c.polarity);
                }
                else
                {
                    throw new NotImplementedException("SaveOp_V3 without HasBranchSave");
                }
            }

            [Builtin("SAVE", MinVersion = 4, MaxVersion = 4, HasSideEffect = true)]
            public static IOperand SaveOp_V4(ValueCall c)
            {
                if (c.rb.HasStoreSave)
                {
                    c.rb.EmitSave(c.resultStorage);
                    return c.resultStorage;
                }
                else
                {
                    throw new NotImplementedException("SaveOp_V4 without HasStoreSave");
                }
            }

            [Builtin("SAVE", MinVersion = 5, HasSideEffect = true)]
            public static IOperand SaveOp_V5(ValueCall c, [Table] IOperand table,
                IOperand bytes, [Table] IOperand name)
            {
                if (c.rb.HasExtendedSave)
                {
                    c.rb.EmitSave(table, bytes, name, c.resultStorage);
                    return c.resultStorage;
                }
                else
                {
                    throw new NotImplementedException("SaveOp_V5 without HasExtendedSave");
                }
            }

            [Builtin("RETURN", HasSideEffect = true)]
            public static void ReturnOp(VoidCall c, IOperand value = null)
            {
                if (c.cc.ReturnLabel == null)
                {
                    // return from routine
                    c.rb.Return(value ?? c.cc.Game.One);
                }
                else
                {
                    // return from enclosing PROG/REPEAT
                    if ((c.cc.ReturnState & BlockReturnState.WantResult) != 0)
                    {
                        if (value != c.rb.Stack)
                            c.rb.EmitStore(c.rb.Stack, value);
                    }
                    else
                    {
                        if (value == c.rb.Stack)
                            c.rb.EmitPopStack();

                        Errors.CompWarning(c.cc.Context, c.form, "RETURN value ignored: enclosing block is in void context");
                    }

                    c.cc.ReturnState |= BlockReturnState.Returned;
                    c.rb.Branch(c.cc.ReturnLabel);
                }
            }

            [Builtin("APPLY", "CALL", HasSideEffect = true)]
            public static IOperand CallValueOp(ValueCall c,
                IOperand routine, params IOperand[] args)
            {
                if (args.Length > c.cc.Game.MaxCallArguments)
                {
                    Errors.CompError(
                        c.cc.Context,
                        c.form,
                        "too many call arguments: only {0} allowed in V{1}",
                        c.cc.Game.MaxCallArguments, c.cc.Context.ZEnvironment.ZVersion);
                    return c.cc.Game.Zero;
                }

                c.rb.EmitCall(routine, args, c.resultStorage);
                return c.resultStorage;
            }

            [Builtin("APPLY", "CALL", MinVersion = 5, HasSideEffect = true)]
            public static void CallVoidOp(VoidCall c,
                IOperand routine, params IOperand[] args)
            {
                if (args.Length > c.cc.Game.MaxCallArguments)
                {
                    Errors.CompError(
                        c.cc.Context,
                        c.form,
                        "too many call arguments: only {0} allowed in V{1}",
                        c.cc.Game.MaxCallArguments, c.cc.Context.ZEnvironment.ZVersion);
                    return;
                }

                c.rb.EmitCall(routine, args, null);
            }
        }
    }
}
