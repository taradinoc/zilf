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

        internal struct ArgCountRange
        {
            public int MinArgs;
            public int? MaxArgs;

            public ArgCountRange(int min, int? max)
            {
                this.MinArgs = min;
                this.MaxArgs = max;
            }
        }

        private static IEnumerable<T> Collapse<T>(IEnumerable<T> sequence,
            Func<T, T, bool> match, Func<T, T, T> combine)
        {
            var tor = sequence.GetEnumerator();
            if (tor.MoveNext())
            {
                var last = tor.Current;

                while (tor.MoveNext())
                {
                    var current = tor.Current;
                    if (match(last, current))
                    {
                        last = combine(last, current);
                    }
                    else
                    {
                        yield return last;
                        last = current;
                    }
                }

                yield return last;
            }
        }

        private static string EnglishJoin(IEnumerable<string> sequence, string conjunction)
        {
            var items = sequence.ToArray();

            switch (items.Length)
            {
                case 0:
                    return "";
                case 1:
                    return items[0];
                case 2:
                    return items[0] + " " + conjunction + " " + items[1];
                default:
                    var last = items.Length - 1;
                    items[last] = conjunction + " " + items[last];
                    return string.Join(", ", items);
            }
        }

        internal static string FormatArgCount(IEnumerable<ArgCountRange> ranges)
        {
            var allCounts = new List<int>();
            bool uncapped = false;
            foreach (var r in ranges)
            {
                if (r.MaxArgs == null)
                {
                    uncapped = true;
                    allCounts.Add(r.MinArgs);
                }
                else
                {
                    for (int i = r.MinArgs; i <= r.MaxArgs; i++)
                        allCounts.Add(i);
                }
            }

            if (allCounts.Count == 0)
                throw new ArgumentException("No ranges provided");

            allCounts.Sort();

            // (1,2), (2,4) => (1,4)
            var collapsed = Collapse(
                allCounts.Select(c => new { min = c, max = c }),
                (a, b) => b.min <= a.max + 1,
                (a, b) => new { a.min, b.max })
                .ToArray();

            if (collapsed.Length == 1)
            {
                var r = collapsed[0];

                // (1,_) uncapped => "1 or more arguments"
                if (uncapped)
                    return string.Format("{0} or more arguments", r.min);

                // (1,1) => "exactly 1 argument"
                if (r.max == r.min)
                    return string.Format("exactly {0} argument{1}",
                        r.min, r.min == 1 ? "" : "s");

                // (1,2) => "1 or 2 arguments"
                if (r.max == r.min + 1)
                    return string.Format("{0} or {1} arguments", r.min, r.max);

                // (1,3) => "1 to 3 arguments"
                return string.Format("{0} to {1} arguments", r.min, r.max);
            }
            else
            {
                // disjoint ranges
                var unrolled = from r in collapsed
                               from n in Enumerable.Range(r.min, r.max - r.min + 1)
                               select n.ToString();
                if (uncapped)
                    unrolled = unrolled.Concat(Enumerable.Repeat("more", 1));

                return EnglishJoin(unrolled, "or") + " arguments";
            }
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
            /// Indicates that the parameter will be used as a routine address.
            /// </summary>
            [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
            class RoutineAttribute : Attribute
            {
            }

            [Flags]
            enum QuirksMode
            {
                None = 0,
                Local = 1,
                Global = 2,
                Both = 3,
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
                public QuirksMode QuirksMode { get; set; }
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

                public bool AppliesTo(int zversion, int argCount, Type callType = null)
                {
                    if (!VersionMatches(zversion, Attr.MinVersion, Attr.MaxVersion))
                        return false;

                    if (argCount < MinArgs || (MaxArgs != null && argCount > MaxArgs))
                        return false;

                    if (callType != null && this.CallType != callType)
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

            public static bool IsBuiltinWithSideEffects(string name, int zversion, int argCount)
            {
                // true if there's a void, value, or predicate version with side effects
                return builtins[name].Any(s => s.AppliesTo(zversion, argCount) && s.Attr.HasSideEffect);
            }

            public static bool IsNearMatchBuiltin(string name, int zversion, int argCount, out string errorMsg)
            {
                // is there a match with this zversion but any arg count?
                var wrongArgCount =
                    builtins[name].Where(s => BuiltinSpec.VersionMatches(
                        zversion, s.Attr.MinVersion, s.Attr.MaxVersion))
                    .ToArray();
                if (wrongArgCount.Length > 0)
                {
                    var counts = wrongArgCount.Select(s => new ArgCountRange(s.MinArgs, s.MaxArgs));
                    errorMsg = name + " requires " + Compiler.FormatArgCount(counts);

                    // be a little more helpful if this arg count would work in another zversion
                    if (builtins[name].Any(
                        s => argCount >= s.MinArgs && (s.MaxArgs == null || argCount <= s.MaxArgs)))
                    {
                        errorMsg += " in this Z-machine version";
                    }

                    return true;
                }

                // is there a match with any zversion?
                if (builtins.Contains(name))
                {
                    errorMsg = string.Format("{0} is not supported in this Z-machine version", name);
                    return true;
                }

                // not a near match
                errorMsg = null;
                return false;
            }

            private delegate void InvalidArgumentDelegate(int index, string message);

            private enum BuiltinArgType
            {
                /// <summary>
                /// An IOperand or other value ready to pass into the spec method.
                /// </summary>
                Operand,
                /// <summary>
                /// A ZilObject that must be evaluated before passing to the spec method.
                /// </summary>
                NeedsEval,
            }

            private struct BuiltinArg
            {
                public BuiltinArgType Type;
                public object Value;

                public BuiltinArg(BuiltinArgType type, object value)
                {
                    this.Type = type;
                    this.Value = value;
                }
            }

            private static IList<BuiltinArg> ValidateArguments(
                CompileCtx cc, BuiltinSpec spec, ParameterInfo[] builtinParamInfos,
                ZilObject[] args, InvalidArgumentDelegate error)
            {
                // args may be short (for optional params)

                var result = new List<BuiltinArg>(args.Length);

                for (int i = 0, j = spec.Attr.Data == null ? 1 : 2; i < args.Length; i++, j++)
                {
                    var arg = args[i];
                    var pi = builtinParamInfos[j];

                    if (pi.ParameterType == typeof(IVariable))
                    {
                        // arg must be an atom, or <GVAL atom> or <LVAL atom> in quirks mode
                        ZilAtom atom = arg as ZilAtom;
                        QuirksMode quirks = QuirksMode.None;
                        if (arg == null)
                        {
                            var attr = pi.GetCustomAttributes(typeof(VariableAttribute), false).Cast<VariableAttribute>().Single();
                            quirks = attr.QuirksMode;
                            if (quirks != QuirksMode.None && arg is ZilForm)
                            {
                                var form = (ZilForm)arg;
                                var fatom = form.First as ZilAtom;
                                if (atom != null &&
                                    (((quirks & QuirksMode.Global) != 0 && atom.StdAtom == StdAtom.GVAL) ||
                                     ((quirks & QuirksMode.Local) != 0 && atom.StdAtom == StdAtom.LVAL)) &&
                                    form.Rest.First is ZilAtom &&
                                    form.Rest.Rest.IsEmpty)
                                {
                                    atom = (ZilAtom)form.Rest.First;
                                }
                            }
                        }

                        if (atom == null)
                        {
                            error(i, "argument must be a variable");
                        }
                        else
                        {
                            ILocalBuilder local;
                            IGlobalBuilder global;

                            if (!cc.Locals.TryGetValue(atom, out local) && !cc.Globals.TryGetValue(atom, out global))
                                error(i, "no such variable: " + atom.ToString());
                        }

                        result.Add(new BuiltinArg(BuiltinArgType.Operand, GetVariable(cc, arg, quirks)));
                    }
                    else if (pi.ParameterType == typeof(string))
                    {
                        // arg must be a string
                        var zstr = arg as ZilString;
                        if (zstr == null)
                        {
                            error(i, "argument must be a literal string");
                            result.Add(new BuiltinArg(BuiltinArgType.Operand, null));
                        }
                        else
                        {
                            result.Add(new BuiltinArg(BuiltinArgType.Operand, Compiler.TranslateString(zstr.Text)));
                        }
                    }
                    else if (pi.ParameterType == typeof(IOperand))
                    {
                        // if marked with [Variable], allow a variable reference
                        var varAttr = (VariableAttribute)pi.GetCustomAttributes(typeof(VariableAttribute), false).SingleOrDefault();
                        IVariable variable;
                        if (varAttr != null && (variable = GetVariable(cc, arg, varAttr.QuirksMode)) != null)
                        {
                            result.Add(new BuiltinArg(BuiltinArgType.Operand, variable.Indirect));
                        }
                        else
                        {
                            result.Add(new BuiltinArg(BuiltinArgType.NeedsEval, arg));
                        }
                    }
                    else if (pi.ParameterType == typeof(IOperand[]))
                    {
                        // this absorbs the rest of the args
                        while (i < args.Length)
                        {
                            result.Add(new BuiltinArg(BuiltinArgType.NeedsEval, args[i++]));
                        }
                        break;
                    }
                    else
                    {
                        throw new NotImplementedException("Unexpected parameter type");
                    }
                }

                System.Diagnostics.Debug.Assert(result.Count == args.Length);
                return result;
            }

            private static IVariable GetVariable(CompileCtx cc, ZilObject expr, QuirksMode quirks = QuirksMode.None)
            {
                ZilAtom atom = expr as ZilAtom;

                if (atom == null)
                {
                    if (quirks == QuirksMode.None)
                        return null;

                    var form = expr as ZilForm;
                    if (form == null || !(form.First is ZilAtom))
                        return null;

                    switch (((ZilAtom)form.First).StdAtom)
                    {
                        case StdAtom.GVAL:
                            if ((quirks & QuirksMode.Global) == 0)
                                return null;
                            break;
                        case StdAtom.LVAL:
                            if ((quirks & QuirksMode.Local) == 0)
                                return null;
                            break;
                    }

                    if (form.Rest == null || !(form.Rest.First is ZilAtom))
                        return null;

                    atom = (ZilAtom)form.Rest.First;
                }

                ILocalBuilder lb;
                IGlobalBuilder gb;

                if (cc.Locals.TryGetValue(atom, out lb))
                    return lb;
                if (cc.Globals.TryGetValue(atom, out gb))
                    return gb;

                return null;
            }

            private static List<object> MakeBuiltinMethodParams(
                CompileCtx cc, BuiltinSpec spec,
                ParameterInfo[] builtinParamInfos, object call,
                IList<BuiltinArg> args)
            {
                System.Diagnostics.Debug.Assert(args.All(a => a.Type == BuiltinArgType.Operand));

                /* args.Length (plus call and data) may differ from builtinParamInfos.Length,
                 * due to optional arguments and params arrays. */

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
                for (int j = 0; i < builtinParamInfos.Length; i++, j++)
                {
                    var pi = builtinParamInfos[i];

                    if (pi.ParameterType == typeof(IOperand[]))
                    {
                        // add all remaining operands as a param array
                        if (j >= args.Count)
                        {
                            result.Add(new IOperand[0]);
                        }
                        else
                        {
                            result.Add(args.Skip(j).Select(a => (IOperand)a.Value).ToArray());
                        }
                        break;
                    }
                    else
                    {
                        if (j >= args.Count)
                        {
                            result.Add(pi.DefaultValue);
                        }
                        else
                        {
                            result.Add(args[j].Value);
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
                var validatedArgs = ValidateArguments(cc, spec, builtinParamInfos, args,
                    (i, msg) =>
                    {
                        Errors.CompError(cc.Context, form, "{0} argument {1}: {2}",
                            name, i + 1, msg);
                        valid = false;
                    });

                if (!valid)
                    return cc.Game.Zero;

                // extract the arguments that need evaluation, and remember their original indexes
                var needEval =
                    validatedArgs
                    .Select((a, oidx) => new { a, oidx })
                    .Where(p => p.a.Type == BuiltinArgType.NeedsEval)
                    .ToArray();
                var needEvalExprs = Array.ConvertAll(needEval, p => (ZilObject)p.a.Value);

                // generate code for arguments
                using (var operands = Operands.Compile(cc, rb, needEvalExprs))
                {
                    // update validatedArgs with the evaluated operands
                    for (int i = 0; i < operands.Count; i++)
                    {
                        var oidx = needEval[i].oidx;
                        validatedArgs[oidx] = new BuiltinArg(BuiltinArgType.Operand, operands[i]);
                    }

                    // call the spec method to generate code for the builtin
                    var builtinParams = MakeBuiltinMethodParams(cc, spec, builtinParamInfos, call, validatedArgs);
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

            [Builtin("N=?", "N==?")]
            public static void NegatedVarargsEqualityOp(
                PredCall c, IOperand arg1, IOperand arg2,
                IOperand arg3 = null, IOperand arg4 = null)
            {
                c.polarity = !c.polarity;
                VarargsEqualityOp(c, arg1, arg2, arg3, arg4);
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
            [Builtin("THROW", Data = BinaryOp.Throw, MinVersion = 5, HasSideEffect = true)]
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

            [Builtin("L=?", Data = Condition.Greater)]
            [Builtin("G=?", Data = Condition.Less)]
            public static void NegatedBinaryPredOp(
                PredCall c, [Data] Condition cond, IOperand left, IOperand right)
            {
                c.rb.Branch(cond, left, right, c.label, !c.polarity);
            }

            [Builtin("DLESS?", Data = Condition.DecCheck, HasSideEffect = true)]
            [Builtin("IGRTR?", Data = Condition.IncCheck, HasSideEffect = true)]
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

            [Builtin("FSET?", Data = Condition.TestAttr)]
            public static void BinaryObjectPredOp(
                PredCall c, [Data] Condition cond, [Object] IOperand left, IOperand right)
            {
                c.rb.Branch(cond, left, right, c.label, c.polarity);
            }

            [Builtin("IN?", Data = Condition.Inside)]
            public static void BinaryObjectObjectPredOp(
                PredCall c, [Data] Condition cond, [Object] IOperand left, [Object] IOperand right)
            {
                c.rb.Branch(cond, left, right, c.label, c.polarity);
            }

            [Builtin("MOVE", Data = BinaryOp.MoveObject, HasSideEffect = true)]
            public static void BinaryObjectObjectVoidOp(
                VoidCall c, [Data] BinaryOp op, [Object] IOperand left, [Object] IOperand right)
            {
                c.rb.EmitBinary(op, left, right, null);
            }

            [Builtin("GETPT", Data = BinaryOp.GetPropAddress)]
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

            [Builtin("SET", HasSideEffect = true)]
            public static IOperand SetValueOp(
                ValueCall c, [Variable(QuirksMode = QuirksMode.Local)] IVariable dest, IOperand value)
            {
                // in value context, we need to be able to return the newly set value,
                // so dest is IVariable. this means <SET <fancy-expression> value> isn't
                // supported in value context.
                c.rb.EmitStore(dest, value);
                return dest;
            }

            [Builtin("SETG", HasSideEffect = true)]
            public static IOperand SetgValueOp(
                ValueCall c, [Variable(QuirksMode = QuirksMode.Global)] IVariable dest, IOperand value)
            {
                // in value context, we need to be able to return the newly set value,
                // so dest is IVariable. this means <SET <fancy-expression> value> isn't
                // supported in value context.
                c.rb.EmitStore(dest, value);
                return dest;
            }

            [Builtin("SET")]
            public static void SetVoidOp(
                VoidCall c, [Variable(QuirksMode = QuirksMode.Local)] IOperand dest, IOperand value)
            {
                // in void context, we don't need to return the newly set value, so we
                // can support <SET <fancy-expression> value>.
                c.rb.EmitBinary(BinaryOp.StoreIndirect, dest, value, null);
            }

            [Builtin("SETG")]
            public static void SetgVoidOp(
                VoidCall c, [Variable(QuirksMode = QuirksMode.Global)] IOperand dest, IOperand value)
            {
                // in void context, we don't need to return the newly set value, so we
                // can support <SET <fancy-expression> value>.
                c.rb.EmitBinary(BinaryOp.StoreIndirect, dest, value, null);
            }

            [Builtin("INC", Data = BinaryOp.Add, HasSideEffect = true)]
            [Builtin("DEC", Data = BinaryOp.Sub, HasSideEffect = true)]
            public static IOperand IncValueOp(ValueCall c, [Data] BinaryOp op,
                [Variable] IVariable victim)
            {
                c.rb.EmitBinary(op, victim, c.cc.Game.One, victim);
                return victim;
            }

            [Builtin("PUSH", HasSideEffect = true)]
            public static void PushVoidOp(VoidCall c, IOperand value)
            {
                c.rb.EmitStore(c.rb.Stack, value);
            }

            // TODO: support the IVariable and IOperand versions side by side? that way we can skip emitting an instruction for <VALUE VARNAME>
            /*[Builtin("VALUE")]
            public static IOperand ValueOp_Variable(ValueCall c, [Variable] IVariable var)
            {
                if (var == c.rb.Stack)
                {
                    c.rb.EmitUnary(UnaryOp.LoadIndirect, var.Indirect, c.resultStorage);
                    return c.resultStorage;
                }
                else
                {
                    return var;
                }
            }*/

            [Builtin("VALUE")]
            public static IOperand ValueOp_Operand(ValueCall c, [Variable] IOperand value)
            {
                c.rb.EmitUnary(UnaryOp.LoadIndirect, value, c.resultStorage);
                return c.resultStorage;
            }

            [Builtin("CRLF", HasSideEffect = true)]
            public static void CrlfVoidOp(VoidCall c)
            {
                c.rb.EmitPrintNewLine();
            }

            [Builtin("CATCH", Data = NullaryOp.Catch, MinVersion = 5)]
            [Builtin("ISAVE", Data = NullaryOp.SaveUndo, HasSideEffect = true, MinVersion = 5)]
            [Builtin("IRESTORE", Data = NullaryOp.RestoreUndo, HasSideEffect = true, MinVersion = 5)]
            [Builtin("USL", Data = NullaryOp.ShowStatus, MinVersion = 3, MaxVersion = 3, HasSideEffect = true)]
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
                IOperand time = null, [Routine] IOperand routine = null)
            {
                c.rb.EmitRead(text, parse, time, routine, null);
            }

            [Builtin("READ", MinVersion = 5, HasSideEffect = true)]
            public static IOperand ReadOp_V5(ValueCall c, IOperand text,
                IOperand parse = null, IOperand time = null,
                [Routine] IOperand routine = null)
            {
                c.rb.EmitRead(text, parse, time, routine, c.resultStorage);
                return c.resultStorage;
            }

            [Builtin("INPUT", MinVersion = 4, HasSideEffect = true)]
            public static IOperand InputOp(ValueCall c, IOperand dummy,
                IOperand interval = null, [Routine] IOperand routine = null)
            {
                if (!(c.form.Rest.First is ZilFix && ((ZilFix)c.form.Rest.First).Value == 1))
                {
                    Errors.CompError(c.cc.Context, c.form, "INPUT: argument 1 must be 1");
                    return c.cc.Game.Zero;
                }

                c.rb.EmitReadChar(interval, routine, c.resultStorage);
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
                IOperand effect = null, IOperand volume = null,
                [Routine] IOperand routine = null)
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

            [Builtin("LEX", MinVersion = 5, HasSideEffect = true)]
            public static void LexOp(VoidCall c,
                [Table] IOperand text, [Table] IOperand parse,
                [Table] IOperand dictionary = null, IOperand flag = null)
            {
                c.rb.EmitTokenize(text, parse, dictionary, flag);
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

            [Builtin("RESTORE", MinVersion = 4, HasSideEffect = true)]
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

            [Builtin("SAVE", MinVersion = 4, HasSideEffect = true)]
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
                        if (value == null)
                            c.rb.EmitStore(c.rb.Stack, c.cc.Game.One);
                        else if (value != c.rb.Stack)
                            c.rb.EmitStore(c.rb.Stack, value);
                    }
                    else if (value != null)
                    {
                        if (value == c.rb.Stack)
                            c.rb.EmitPopStack();

                        Errors.CompWarning(c.cc.Context, c.form, "RETURN value ignored: enclosing block is in void context");
                    }

                    c.cc.ReturnState |= BlockReturnState.Returned;
                    c.rb.Branch(c.cc.ReturnLabel);
                }
            }

            [Builtin("AGAIN", HasSideEffect = true)]
            public static void AgainOp(VoidCall c)
            {
                if (c.cc.AgainLabel != null)
                {
                    c.rb.Branch(c.cc.AgainLabel);
                }
                else
                {
                    Errors.CompError(c.cc.Context, c.form, "AGAIN requires an enclosing PROG/REPEAT");
                }
            }

            [Builtin("APPLY", "CALL", HasSideEffect = true)]
            public static IOperand CallValueOp(ValueCall c,
                [Routine] IOperand routine, params IOperand[] args)
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
                [Routine] IOperand routine, params IOperand[] args)
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

            [Builtin("INTBL?", MinVersion = 4, MaxVersion = 4)]
            [return: Table]
            public static IOperand IntblValueOp_V4(ValueCall c,
                IOperand value, [Table] IOperand table, IOperand length)
            {
                c.rb.EmitScanTable(value, table, length, null, c.resultStorage);
                return c.resultStorage;
            }

            [Builtin("INTBL?", MinVersion = 4, MaxVersion = 4)]
            public static void IntblPredOp_V4(PredCall c,
                IOperand value, [Table] IOperand table, IOperand length)
            {
                c.rb.EmitScanTable(value, table, length, null, c.label, c.polarity);
            }

            [Builtin("INTBL?", MinVersion = 5)]
            [return: Table]
            public static IOperand IntblValueOp_V5(ValueCall c,
                IOperand value, [Table] IOperand table, IOperand length, IOperand form = null)
            {
                c.rb.EmitScanTable(value, table, length, form, c.resultStorage);
                return c.resultStorage;
            }

            [Builtin("INTBL?", MinVersion = 5)]
            public static void IntblPredOp_V5(PredCall c,
                IOperand value, [Table] IOperand table, IOperand length, IOperand form = null)
            {
                c.rb.EmitScanTable(value, table, length, form, c.label, c.polarity);
            }
        }
    }
}
