using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Text;
using Zilf.Emit;

namespace Zilf
{
    partial class Compiler
    {
        #region ZBuiltins Infrastructure

        struct VoidCall
        {
            public CompileCtx cc { get; private set; }
            public IRoutineBuilder rb { get; private set; }
            public ZilForm form { get; private set; }

            public VoidCall(CompileCtx cc, IRoutineBuilder rb, ZilForm form)
                : this()
            {
                Contract.Requires(cc != null);
                Contract.Requires(rb != null);
                Contract.Requires(form != null);

                this.cc = cc;
                this.rb = rb;
                this.form = form;
            }

            [ContractInvariantMethod]
            private void ObjectInvariant()
            {
                Contract.Invariant(cc != null);
                Contract.Invariant(rb != null);
                Contract.Invariant(form != null);
            }
        }

        struct ValueCall
        {
            public CompileCtx cc { get; private set; }
            public IRoutineBuilder rb { get; private set; }
            public ZilForm form { get; private set; }

            public IVariable resultStorage { get; private set; }

            public ValueCall(CompileCtx cc, IRoutineBuilder rb, ZilForm form, IVariable resultStorage)
                : this()
            {
                Contract.Requires(cc != null);
                Contract.Requires(rb != null);
                Contract.Requires(form != null);
                Contract.Requires(resultStorage != null);

                this.cc = cc;
                this.rb = rb;
                this.form = form;
                this.resultStorage = resultStorage;
            }

            [ContractInvariantMethod]
            private void ObjectInvariant()
            {
                Contract.Invariant(cc != null);
                Contract.Invariant(rb != null);
                Contract.Invariant(form != null);
                Contract.Invariant(resultStorage != null);
            }
        }

        struct PredCall
        {
            public CompileCtx cc { get; private set; }
            public IRoutineBuilder rb { get; private set; }
            public ZilForm form { get; private set; }

            public ILabel label { get; private set; }
            public bool polarity { get; private set; }

            public PredCall(CompileCtx cc, IRoutineBuilder rb, ZilForm form, ILabel label, bool polarity)
                : this()
            {
                Contract.Requires(cc != null);
                Contract.Requires(rb != null);
                Contract.Requires(form != null);
                Contract.Requires(label != null);

                this.cc = cc;
                this.rb = rb;
                this.form = form;
                this.label = label;
                this.polarity = polarity;
            }

            [ContractInvariantMethod]
            private void ObjectInvariant()
            {
                Contract.Invariant(cc != null);
                Contract.Invariant(rb != null);
                Contract.Invariant(form != null);
                Contract.Invariant(label != null);
            }
        }

        struct ValuePredCall
        {
            public CompileCtx cc { get; private set; }
            public IRoutineBuilder rb { get; private set; }
            public ZilForm form { get; private set; }

            public IVariable resultStorage { get; private set; }
            public ILabel label { get; private set; }
            public bool polarity { get; private set; }

            public ValuePredCall(CompileCtx cc, IRoutineBuilder rb, ZilForm form, IVariable resultStorage, ILabel label, bool polarity)
                : this()
            {
                Contract.Requires(cc != null);
                Contract.Requires(rb != null);
                Contract.Requires(form != null);
                Contract.Requires(resultStorage != null);
                Contract.Requires(label != null);

                this.cc = cc;
                this.rb = rb;
                this.form = form;
                this.resultStorage = resultStorage;
                this.label = label;
                this.polarity = polarity;
            }

            [ContractInvariantMethod]
            private void ObjectInvariant()
            {
                Contract.Invariant(cc != null);
                Contract.Invariant(rb != null);
                Contract.Invariant(resultStorage != null);
                Contract.Invariant(form != null);
                Contract.Invariant(label != null);
            }
        }

        internal struct ArgCountRange
        {
            public readonly int MinArgs;
            public readonly int? MaxArgs;

            public ArgCountRange(int min, int? max)
            {
                this.MinArgs = min;
                this.MaxArgs = max;
            }
        }

        private static IEnumerable<T> Collapse<T>(IEnumerable<T> sequence,
            Func<T, T, bool> match, Func<T, T, T> combine)
        {
            Contract.Requires(sequence != null);
            Contract.Requires(match != null);
            Contract.Requires(combine != null);
            //Contract.Ensures(Contract.Result<IEnumerable<T>>() != null);

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
            Contract.Requires(sequence != null);
            Contract.Requires(conjunction != null);
            Contract.Ensures(Contract.Result<string>() != null);

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
            Contract.Requires(ranges != null);

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
            public class BuiltinAttribute : Attribute
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

                    Priority = 1;
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

                /// <summary>
                /// Gets or sets a priority value used to disambiguate when more than one method matches the call.
                /// Lower values indicate a better match. Defaults to 1.
                /// </summary>
                public int Priority { get; set; }
            }

            /// <summary>
            /// Indicates the parameter where the value of <see cref="BuiltinAttribute.Data"/>
            /// should be passed in. This does not correspond to a ZIL parameter.
            /// </summary>
            [AttributeUsage(AttributeTargets.Parameter)]
            public class DataAttribute : Attribute
            {
            }

            /// <summary>
            /// Indicates that the parameter will be used as an object.
            /// </summary>
            [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
            public class ObjectAttribute : Attribute
            {
            }

            /// <summary>
            /// Indicates that the parameter will be used as a table address.
            /// </summary>
            [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
            public class TableAttribute : Attribute
            {
            }

            /// <summary>
            /// Indicates that the parameter will be used as a routine address.
            /// </summary>
            [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
            public class RoutineAttribute : Attribute
            {
            }

            [Flags]
            public enum QuirksMode
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
            public class VariableAttribute : Attribute
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
                    Contract.Requires(attr != null);
                    Contract.Requires(method != null);

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

                                if (CallType != typeof(VoidCall) && CallType != typeof(ValueCall) && CallType != typeof(PredCall) && CallType != typeof(ValuePredCall))
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

                            if (pi.ParameterType == typeof(IOperand) || pi.ParameterType == typeof(string) || pi.ParameterType == typeof(ZilObject) ||
                                pi.ParameterType == typeof(ZilAtom) || pi.ParameterType == typeof(int) || pi.ParameterType == typeof(Block))
                            {
                                // regular operand: may be optional
                                max++;
                                if (!pi.IsOptional)
                                    min++;
                                continue;
                            }

                            if (pi.ParameterType == typeof(IVariable) || pi.ParameterType == typeof(SoftGlobal))
                            {
                                // indirect variable operand: must have [Variable]
                                if (!pattrs.Any(a => a is VariableAttribute))
                                    throw new ArgumentException("IVariable/SoftGlobal parameter must be marked [Variable]");

                                max++;
                                min++;
                                continue;
                            }

                            if (pi.ParameterType == typeof(IOperand[]) || pi.ParameterType == typeof(ZilObject[]))
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
                        else if (CallType == typeof(ValuePredCall) && method.ReturnType != typeof(void))
                        {
                            throw new ArgumentException("Value+predicate call must return void");
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

            public static bool IsBuiltinValuePredCall(string name, int zversion, int argCount)
            {
                return builtins[name].Any(s => s.AppliesTo(zversion, argCount, typeof(ValuePredCall)));
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
                public readonly BuiltinArgType Type;
                public readonly object Value;

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
                Contract.Requires(cc != null);
                Contract.Requires(spec != null);
                Contract.Requires(builtinParamInfos != null);
                Contract.Requires(builtinParamInfos.Length >= 1);
                Contract.Requires(args != null);
                Contract.Requires(error != null);
                Contract.Ensures(Contract.Result<IList<BuiltinArg>>() != null);
                Contract.Ensures(Contract.Result<IList<BuiltinArg>>().Count == args.Length);

                // args may be short (for optional params)

                var result = new List<BuiltinArg>(args.Length);

                for (int i = 0, j = spec.Attr.Data == null ? 1 : 2; i < args.Length; i++, j++)
                {
                    Contract.Assume(j < builtinParamInfos.Length);

                    var arg = args[i];
                    var pi = builtinParamInfos[j];

                    if (pi.ParameterType == typeof(IVariable) || pi.ParameterType == typeof(SoftGlobal))
                    {
                        // arg must be an atom, or <GVAL atom> or <LVAL atom> in quirks mode
                        ZilAtom atom = arg as ZilAtom;
                        QuirksMode quirks = QuirksMode.None;
                        if (atom == null)
                        {
                            var attr = pi.GetCustomAttributes(typeof(VariableAttribute), false).Cast<VariableAttribute>().Single();
                            quirks = attr.QuirksMode;
                            if (quirks != QuirksMode.None && arg is ZilForm)
                            {
                                var form = (ZilForm)arg;
                                var fatom = form.First as ZilAtom;
                                if (fatom != null &&
                                    (((quirks & QuirksMode.Global) != 0 && fatom.StdAtom == StdAtom.GVAL) ||
                                     ((quirks & QuirksMode.Local) != 0 && fatom.StdAtom == StdAtom.LVAL)) &&
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
                            result.Add(new BuiltinArg(BuiltinArgType.Operand, null));
                        }
                        else if (pi.ParameterType == typeof(IVariable))
                        {
                            if (!cc.Locals.ContainsKey(atom) && !cc.Globals.ContainsKey(atom))
                                error(i, "no such variable: " + atom.ToString());

                            var variableRef = GetVariable(cc, arg, quirks);
                            result.Add(new BuiltinArg(BuiltinArgType.Operand, variableRef == null ? null : variableRef.Value.Hard));
                        }
                        else // if (pi.ParameterType == typeof(SoftGlobal))
                        {
                            if (!cc.SoftGlobals.ContainsKey(atom))
                                error(i, "no such variable: " + atom.ToString());

                            var variableRef = GetVariable(cc, arg, quirks);
                            result.Add(new BuiltinArg(BuiltinArgType.Operand, variableRef == null ? null : variableRef.Value.Soft));
                        }
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
                            result.Add(new BuiltinArg(BuiltinArgType.Operand, Compiler.TranslateString(zstr.Text, cc.Context)));
                        }
                    }
                    else if (pi.ParameterType == typeof(IOperand))
                    {
                        // if marked with [Variable], allow a variable reference and forbid a non-variable bare atom
                        var varAttr = (VariableAttribute)pi.GetCustomAttributes(typeof(VariableAttribute), false).SingleOrDefault();
                        VariableRef? variable;
                        if (varAttr != null)
                        {
                            if ((variable = GetVariable(cc, arg, varAttr.QuirksMode)) != null)
                            {
                                if (!variable.Value.IsHard)
                                {
                                    error(i, "soft variable may not be used here");
                                    result.Add(new BuiltinArg(BuiltinArgType.Operand, null));
                                }
                                else
                                {
                                    result.Add(new BuiltinArg(BuiltinArgType.Operand, variable.Value.Hard.Indirect));
                                }
                            }
                            else if (arg is ZilAtom)
                            {
                                error(i, "bare atom argument must be a variable name");
                                result.Add(new BuiltinArg(BuiltinArgType.Operand, null));
                            }
                            else
                            {
                                result.Add(new BuiltinArg(BuiltinArgType.NeedsEval, arg));
                            }
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
                    else if (pi.ParameterType == typeof(ZilObject))
                    {
                        result.Add(new BuiltinArg(BuiltinArgType.Operand, arg));
                    }
                    else if (pi.ParameterType == typeof(ZilAtom))
                    {
                        if (arg.GetTypeAtom(cc.Context).StdAtom != StdAtom.ATOM)
                            error(i, "argument must be an atom");

                        result.Add(new BuiltinArg(BuiltinArgType.Operand, arg));
                    }
                    else if (pi.ParameterType == typeof(int))
                    {
                        if (arg.GetTypeAtom(cc.Context).StdAtom != StdAtom.FIX)
                            error(i, "argument must be a FIX");

                        result.Add(new BuiltinArg(BuiltinArgType.Operand, ((ZilFix)arg).Value));
                    }
                    else if (pi.ParameterType == typeof(ZilObject[]))
                    {
                        // this absorbs the rest of the args
                        while (i < args.Length)
                        {
                            result.Add(new BuiltinArg(BuiltinArgType.Operand, args[i++]));
                        }
                        break;
                    }
                    else if (pi.ParameterType == typeof(Block))
                    {
                        // arg must be an LVAL reference
                        if (arg.IsLVAL())
                        {
                            var atom = (ZilAtom)((ZilForm)arg).Rest.First;
                            var block = cc.Blocks.First(b => b.Name == atom);
                            if (block == null)
                            {
                                error(i, "argument must be bound to a block");
                            }

                            result.Add(new BuiltinArg(BuiltinArgType.Operand, block));
                        }
                        else
                        {
                            error(i, "argument must be a local variable reference");
                            result.Add(new BuiltinArg(BuiltinArgType.Operand, null));
                        }
                    }
                    else
                    {
                        throw new NotImplementedException("Unexpected parameter type");
                    }
                }

                return result;
            }

            private struct VariableRef
            {
                public readonly IVariable Hard;
                public readonly SoftGlobal Soft;

                public VariableRef(IVariable hard)
                {
                    this.Hard = hard;
                    this.Soft = null;
                }

                public VariableRef(SoftGlobal soft)
                {
                    this.Soft = soft;
                    this.Hard = null;
                }

                public bool IsHard
                {
                    get { return Hard != null; }
                }
            }

            private static VariableRef? GetVariable(CompileCtx cc, ZilObject expr, QuirksMode quirks = QuirksMode.None)
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
                SoftGlobal sg;

                if (cc.Locals.TryGetValue(atom, out lb))
                    return new VariableRef(lb);
                if (cc.Globals.TryGetValue(atom, out gb))
                    return new VariableRef(gb);
                if (cc.SoftGlobals.TryGetValue(atom, out sg))
                    return new VariableRef(sg);

                return null;
            }

            private static List<object> MakeBuiltinMethodParams(
                CompileCtx cc, BuiltinSpec spec,
                ParameterInfo[] builtinParamInfos, object call,
                IList<BuiltinArg> args)
            {
                Contract.Requires(cc != null);
                Contract.Requires(spec != null);
                Contract.Requires(builtinParamInfos != null);
                Contract.Requires(builtinParamInfos.Length >= 1);
                Contract.Requires(spec.Attr.Data == null || builtinParamInfos.Length >= 2);
                Contract.Requires(call != null);
                Contract.Requires(args != null);
                Contract.Requires(Contract.ForAll(args, a => a.Type == BuiltinArgType.Operand));
                Contract.Ensures(Contract.Result<List<object>>().Count == builtinParamInfos.Length);

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
                    Contract.Assume(i < builtinParamInfos.Length);

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
                    else if (pi.ParameterType == typeof(ZilObject[]))
                    {
                        // add all remaining values as a param array
                        if (j >= args.Count)
                        {
                            result.Add(new ZilObject[0]);
                        }
                        else
                        {
                            result.Add(args.Skip(j).Select(a => (ZilObject)a.Value).ToArray());
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
                Contract.Requires(name != null);
                Contract.Requires(cc != null);
                Contract.Requires(rb != null);
                Contract.Requires(form != null);
                Contract.Requires(call != null);

                int zversion = cc.Context.ZEnvironment.ZVersion;
                var argList = form.Rest;
                var args = argList.ToArray();
                var candidateSpecs = builtins[name].Where(s => s.AppliesTo(zversion, args.Length, typeof(TCall))).ToArray();
                Contract.Assume(candidateSpecs.Length >= 1);

                // find the best matching spec, if there's more than one
                BuiltinSpec spec;
                if (candidateSpecs.Length > 1)
                {
                    // choose the one with the fewest validation errors
                    spec = candidateSpecs.OrderBy(s =>
                    {
                        int errors = 0;
                        var pis = s.Method.GetParameters();
                        ValidateArguments(cc, s, pis, args, delegate { errors++; });
                        return errors;
                    }).ThenBy(s => s.Attr.Priority).First();
                }
                else
                {
                    spec = candidateSpecs[0];
                }
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
                using (var operands = Operands.Compile(cc, rb, form.SourceLine, needEvalExprs))
                {
                    // update validatedArgs with the evaluated operands
                    for (int i = 0; i < operands.Count; i++)
                    {
                        var oidx = needEval[i].oidx;
                        validatedArgs[oidx] = new BuiltinArg(BuiltinArgType.Operand, operands[i]);
                    }

                    // call the spec method to generate code for the builtin
                    var builtinParams = MakeBuiltinMethodParams(cc, spec, builtinParamInfos, call, validatedArgs);
                    return spec.Method.Invoke(null, builtinParams.ToArray());
                }
            }

            public static IOperand CompileValueCall(string name, CompileCtx cc, IRoutineBuilder rb, ZilForm form, IVariable resultStorage)
            {
                Contract.Requires(name != null);
                Contract.Requires(cc != null);
                Contract.Requires(rb != null);
                Contract.Requires(form != null);
                Contract.Ensures(Contract.Result<IOperand>() != null);

                // TODO: allow resultStorage to be passed as null to handlers that want it? are there any?
                return (IOperand)CompileBuiltinCall(name, cc, rb, form,
                    new ValueCall(cc, rb, form, resultStorage ?? rb.Stack));
            }

            public static void CompileVoidCall(string name, CompileCtx cc, IRoutineBuilder rb, ZilForm form)
            {
                Contract.Requires(name != null);
                Contract.Requires(cc != null);
                Contract.Requires(rb != null);
                Contract.Requires(form != null);

                CompileBuiltinCall(name, cc, rb, form, new VoidCall(cc, rb, form));
            }

            public static void CompilePredCall(string name, CompileCtx cc, IRoutineBuilder rb, ZilForm form, ILabel label, bool polarity)
            {
                Contract.Requires(name != null);
                Contract.Requires(cc != null);
                Contract.Requires(rb != null);
                Contract.Requires(form != null);
                Contract.Requires(label != null);

                CompileBuiltinCall(name, cc, rb, form, new PredCall(cc, rb, form, label, polarity));
            }

            public static void CompileValuePredCall(string name, CompileCtx cc, IRoutineBuilder rb, ZilForm form, IVariable resultStorage, ILabel label, bool polarity)
            {
                Contract.Requires(name != null);
                Contract.Requires(cc != null);
                Contract.Requires(rb != null);
                Contract.Requires(form != null);
                Contract.Requires(label != null);

                CompileBuiltinCall(name, cc, rb, form,
                    new ValuePredCall(cc, rb, form, resultStorage ?? rb.Stack, label, polarity));
            }

        #endregion

            #region Equality Opcodes

            [Builtin("EQUAL?", "=?", "==?")]
            public static void VarargsEqualityOp(
                PredCall c, IOperand arg1, IOperand arg2,
                params IOperand[] restOfArgs)
            {
                Contract.Requires(arg1 != null);
                Contract.Requires(arg2 != null);
                Contract.Requires(restOfArgs != null);

                if (restOfArgs.Length <= 2)
                {
                    // TODO: there should really just be one BranchIfEqual with optional params
                    switch (restOfArgs.Length)
                    {
                        case 2:
                            c.rb.BranchIfEqual(arg1, arg2, restOfArgs[0], restOfArgs[1], c.label, c.polarity);
                            break;
                        case 1:
                            c.rb.BranchIfEqual(arg1, arg2, restOfArgs[0], c.label, c.polarity);
                            break;
                        default:
                            c.rb.BranchIfEqual(arg1, arg2, c.label, c.polarity);
                            break;
                    }
                }
                else
                {
                    ILocalBuilder tempLocal = null;
                    ZilAtom tempAtom = null;
                    if (arg1 == c.rb.Stack)
                    {
                        tempAtom = ZilAtom.Parse("?TMP", c.cc.Context);
                        tempLocal = PushInnerLocal(c.cc, c.rb, tempAtom);
                        c.rb.EmitStore(tempLocal, arg1);
                        arg1 = tempLocal;
                    }

                    var queue = new Queue<IOperand>(1 + restOfArgs.Length);
                    queue.Enqueue(arg2);
                    foreach (var arg in restOfArgs)
                        queue.Enqueue(arg);

                    if (c.polarity)
                    {
                        while (queue.Count > 0)
                        {
                            switch (queue.Count)
                            {
                                case 3:
                                default:
                                    c.rb.BranchIfEqual(arg1, queue.Dequeue(), queue.Dequeue(), queue.Dequeue(), c.label, true);
                                    break;
                                case 2:
                                    c.rb.BranchIfEqual(arg1, queue.Dequeue(), queue.Dequeue(), c.label, true);
                                    break;
                                case 1:
                                    c.rb.BranchIfEqual(arg1, queue.Dequeue(), c.label, true);
                                    break;
                            }
                        }
                    }
                    else
                    {
                        var skip = c.rb.DefineLabel();

                        while (queue.Count > 0)
                        {
                            // the last test (count <= 3) has false polarity and branches to the target label
                            // the earlier ones (count > 3) have true polarity and just skip the rest of the tests
                            switch (queue.Count)
                            {
                                case 3:
                                    c.rb.BranchIfEqual(arg1, queue.Dequeue(), queue.Dequeue(), queue.Dequeue(), c.label, false);
                                    break;
                                case 2:
                                    c.rb.BranchIfEqual(arg1, queue.Dequeue(), queue.Dequeue(), c.label, false);
                                    break;
                                case 1:
                                    c.rb.BranchIfEqual(arg1, queue.Dequeue(), c.label, false);
                                    break;
                                default:
                                    c.rb.BranchIfEqual(arg1, queue.Dequeue(), queue.Dequeue(), queue.Dequeue(), skip, true);
                                    break;
                            }
                        }

                        c.rb.MarkLabel(skip);
                    }

                    if (tempAtom != null)
                        PopInnerLocal(c.cc, tempAtom);
                }
            }

            [Builtin("N=?", "N==?")]
            public static void NegatedVarargsEqualityOp(
                PredCall c, IOperand arg1, IOperand arg2,
                params IOperand[] restOfArgs)
            {
                Contract.Requires(arg1 != null);
                Contract.Requires(arg2 != null);
                Contract.Requires(restOfArgs != null);

                var innerCall = new PredCall(c.cc, c.rb, c.form, c.label, !c.polarity);
                VarargsEqualityOp(innerCall, arg1, arg2, restOfArgs);
            }

            #endregion

            #region Ternary Opcodes

            [Builtin("PUT", "ZPUT", Data = TernaryOp.PutWord, HasSideEffect = true)]
            [Builtin("PUTB", Data = TernaryOp.PutByte, HasSideEffect = true)]
            public static void TernaryTableVoidOp(
                VoidCall c, [Data] TernaryOp op,
                [Table] IOperand left, IOperand center, IOperand right)
            {
                Contract.Requires(left != null);
                Contract.Requires(center != null);
                Contract.Requires(right != null);

                c.rb.EmitTernary(op, left, center, right, null);
            }

            [Builtin("PUTP", Data = TernaryOp.PutProperty, HasSideEffect = true)]
            public static void TernaryObjectVoidOp(
                VoidCall c, [Data] TernaryOp op,
                [Object] IOperand left, IOperand center, IOperand right)
            {
                Contract.Requires(left != null);
                Contract.Requires(center != null);
                Contract.Requires(right != null);

                c.rb.EmitTernary(op, left, center, right, null);
            }

            [Builtin("COPYT", Data = TernaryOp.CopyTable, HasSideEffect = true, MinVersion = 5)]
            public static void TernaryTableTableVoidOp(
                VoidCall c, [Data] TernaryOp op,
                [Table] IOperand left, [Table] IOperand center, IOperand right)
            {
                Contract.Requires(left != null);
                Contract.Requires(center != null);
                Contract.Requires(right != null);

                c.rb.EmitTernary(op, left, center, right, null);
            }

            #endregion

            #region Binary Opcodes

            [Builtin("MOD", Data = BinaryOp.Mod)]
            [Builtin("BAND", "ANDB", Data = BinaryOp.And)]
            [Builtin("BOR", "ORB", Data = BinaryOp.Or)]
            [Builtin("ASH", "ASHIFT", Data = BinaryOp.ArtShift, MinVersion = 5)]
            [Builtin("LSH", "SHIFT", Data = BinaryOp.LogShift, MinVersion = 5)]
            public static IOperand BinaryValueOp(
                ValueCall c, [Data] BinaryOp op, IOperand left, IOperand right)
            {
                Contract.Requires(left != null);
                Contract.Requires(right != null);
                Contract.Ensures(Contract.Result<IOperand>() != null);

                c.rb.EmitBinary(op, left, right, c.resultStorage);
                return c.resultStorage;
            }

            [Builtin("XORB")]
            public static IOperand BinaryXorOp(ValueCall c, ZilObject left, ZilObject right)
            {
                Contract.Requires(left != null);
                Contract.Requires(right != null);
                Contract.Ensures(Contract.Result<IOperand>() != null);

                ZilObject value;
                if (left is ZilFix && ((ZilFix)left).Value == -1)
                {
                    value = right;
                }
                else if (right is ZilFix && ((ZilFix)right).Value == -1)
                {
                    value = left;
                }
                else
                {
                    Errors.CompError(c.cc.Context, c.form, "XORB: one operand must be -1");
                    return c.cc.Game.Zero;
                }

                var storage = CompileAsOperand(c.cc, c.rb, value, c.form.SourceLine, c.resultStorage);
                c.rb.EmitUnary(UnaryOp.Not, storage, c.resultStorage);
                return c.resultStorage;
            }

            [Builtin("ADD", "+", Data = BinaryOp.Add)]
            [Builtin("SUB", "-", Data = BinaryOp.Sub)]
            [Builtin("MUL", "*", Data = BinaryOp.Mul)]
            [Builtin("DIV", "/", Data = BinaryOp.Div)]
            public static IOperand ArithmeticOp(
                ValueCall c, [Data] BinaryOp op, params IOperand[] args)
            {
                Contract.Requires(args != null);

                IOperand init;
                switch (op)
                {
                    case BinaryOp.Mul:
                    case BinaryOp.Div:
                        init = c.cc.Game.One;
                        break;

                    default:
                        init = c.cc.Game.Zero;
                        break;
                }

                switch (args.Length)
                {
                    case 0:
                        return init;

                    case 1:
                        switch (op)
                        {
                            case BinaryOp.Add:
                            case BinaryOp.Mul:
                                return args[0];

                            case BinaryOp.Sub:
                                c.rb.EmitUnary(UnaryOp.Neg, args[0], c.resultStorage);
                                return c.resultStorage;

                            default:
                                c.rb.EmitBinary(op, init, args[0], c.resultStorage);
                                return c.resultStorage;
                        }

                    case 2:
                        c.rb.EmitBinary(op, args[0], args[1], c.resultStorage);
                        return c.resultStorage;

                    default:
                        c.rb.EmitBinary(op, args[0], args[1], c.rb.Stack);
                        for (int i = 2; i + 1 < args.Length; i++)
                        {
                            c.rb.EmitBinary(op, c.rb.Stack, args[i], c.rb.Stack);
                        }
                        c.rb.EmitBinary(op, c.rb.Stack, args[args.Length - 1], c.resultStorage);
                        return c.resultStorage;
                }
            }

            // TODO: REST with a constant table argument should produce a constant operand (<REST MYTABLE 2> -> "MYTABLE+2")
            [Builtin("REST", "ZREST", Data = BinaryOp.Add)]
            [Builtin("BACK", "ZBACK", Data = BinaryOp.Sub)]
            public static IOperand RestOrBackOp(
                ValueCall c, [Data] BinaryOp op, IOperand left, IOperand right = null)
            {
                Contract.Requires(left != null);
                Contract.Ensures(Contract.Result<IOperand>() != null);

                return BinaryValueOp(c, op, left, right ?? c.cc.Game.One);
            }

            [Builtin("CURSET", Data = BinaryOp.SetCursor, MinVersion = 4, HasSideEffect = true)]
            [Builtin("COLOR", Data = BinaryOp.SetColor, MinVersion = 5, HasSideEffect = true)]
            [Builtin("DIROUT", Data = BinaryOp.DirectOutput, HasSideEffect = true)]
            [Builtin("THROW", Data = BinaryOp.Throw, MinVersion = 5, HasSideEffect = true)]
            public static void BinaryVoidOp(
                VoidCall c, [Data] BinaryOp op, IOperand left, IOperand right)
            {
                Contract.Requires(left != null);
                Contract.Requires(right != null);

                c.rb.EmitBinary(op, left, right, null);
            }

            [Builtin("GRTR?", "G?", Data = Condition.Greater)]
            [Builtin("LESS?", "L?", Data = Condition.Less)]
            [Builtin("BTST", Data = Condition.TestBits)]
            public static void BinaryPredOp(
                PredCall c, [Data] Condition cond, IOperand left, IOperand right)
            {
                Contract.Requires(left != null);
                Contract.Requires(right != null);
                
                c.rb.Branch(cond, left, right, c.label, c.polarity);
            }

            [Builtin("L=?", Data = Condition.Greater)]
            [Builtin("G=?", Data = Condition.Less)]
            public static void NegatedBinaryPredOp(
                PredCall c, [Data] Condition cond, IOperand left, IOperand right)
            {
                Contract.Requires(left != null);
                Contract.Requires(right != null);

                c.rb.Branch(cond, left, right, c.label, !c.polarity);
            }

            [Builtin("DLESS?", Data = Condition.DecCheck, HasSideEffect = true)]
            [Builtin("IGRTR?", Data = Condition.IncCheck, HasSideEffect = true)]
            public static void BinaryVariablePredOp(
                PredCall c, [Data] Condition cond, [Variable] IVariable left, IOperand right)
            {
                Contract.Requires(left != null);
                Contract.Requires(right != null);

                c.rb.Branch(cond, left, right, c.label, c.polarity);
            }

            [Builtin("DLESS?", Data = Condition.Less, HasSideEffect = true)]
            [Builtin("IGRTR?", Data = Condition.Greater, HasSideEffect = true)]
            public static void BinaryVariablePredOp(
                PredCall c, [Data] Condition cond, [Variable] SoftGlobal left, IOperand right)
            {
                Contract.Requires(left != null);
                Contract.Requires(right != null);

                var offset = c.cc.Game.MakeOperand(left.Offset);

                // increment and leave the new value on the stack
                c.rb.EmitBinary(
                    left.IsWord ? BinaryOp.GetWord : BinaryOp.GetByte,
                    c.cc.SoftGlobalsTable,
                    offset,
                    c.rb.Stack);
                c.rb.EmitBinary(
                    cond == Condition.Greater ? BinaryOp.Add : BinaryOp.Sub,
                    c.rb.Stack,
                    c.cc.Game.One,
                    c.rb.Stack);
                c.rb.EmitUnary(
                    UnaryOp.LoadIndirect,
                    c.rb.Stack.Indirect,
                    c.rb.Stack);
                c.rb.EmitTernary(
                    left.IsWord ? TernaryOp.PutWord : TernaryOp.PutByte,
                    c.cc.SoftGlobalsTable,
                    offset,
                    c.rb.Stack,
                    null);

                // this works even if right == Stack, since the new value will be popped first
                c.rb.Branch(cond, c.rb.Stack, right, c.label, c.polarity);
            }

            [Builtin("GETP", Data = BinaryOp.GetProperty)]
            [Builtin("NEXTP", Data = BinaryOp.GetNextProp)]
            public static IOperand BinaryObjectValueOp(
                ValueCall c, [Data] BinaryOp op, [Object] IOperand left, IOperand right)
            {
                Contract.Requires(left != null);
                Contract.Requires(right != null);
                Contract.Ensures(Contract.Result<IOperand>() != null);

                c.rb.EmitBinary(op, left, right, c.resultStorage);
                return c.resultStorage;
            }

            [Builtin("FSET", Data = BinaryOp.SetFlag, HasSideEffect = true)]
            [Builtin("FCLEAR", Data = BinaryOp.ClearFlag, HasSideEffect = true)]
            public static void BinaryObjectVoidOp(
                VoidCall c, [Data] BinaryOp op, [Object] IOperand left, IOperand right)
            {
                Contract.Requires(left != null);
                Contract.Requires(right != null);

                c.rb.EmitBinary(op, left, right, null);
            }

            [Builtin("FSET?", Data = Condition.TestAttr)]
            public static void BinaryObjectPredOp(
                PredCall c, [Data] Condition cond, [Object] IOperand left, IOperand right)
            {
                Contract.Requires(left != null);
                Contract.Requires(right != null);

                c.rb.Branch(cond, left, right, c.label, c.polarity);
            }

            [Builtin("IN?", Data = Condition.Inside)]
            public static void BinaryObjectObjectPredOp(
                PredCall c, [Data] Condition cond, [Object] IOperand left, [Object] IOperand right)
            {
                Contract.Requires(left != null);
                Contract.Requires(right != null);

                c.rb.Branch(cond, left, right, c.label, c.polarity);
            }

            [Builtin("MOVE", Data = BinaryOp.MoveObject, HasSideEffect = true)]
            public static void BinaryObjectObjectVoidOp(
                VoidCall c, [Data] BinaryOp op, [Object] IOperand left, [Object] IOperand right)
            {
                Contract.Requires(left != null);
                Contract.Requires(right != null);

                c.rb.EmitBinary(op, left, right, null);
            }

            [Builtin("GETPT", Data = BinaryOp.GetPropAddress)]
            [return: Table]
            public static IOperand BinaryObjectToTableValueOp(
                ValueCall c, [Data] BinaryOp op, [Object] IOperand left, IOperand right)
            {
                Contract.Requires(left != null);
                Contract.Requires(right != null);
                Contract.Ensures(Contract.Result<IOperand>() != null);

                c.rb.EmitBinary(op, left, right, c.resultStorage);
                return c.resultStorage;
            }

            [Builtin("GET", "NTH", "ZGET", Data = BinaryOp.GetWord)]
            [Builtin("GETB", Data = BinaryOp.GetByte)]
            public static IOperand BinaryTableValueOp(
                ValueCall c, [Data] BinaryOp op, [Table] IOperand left, IOperand right)
            {
                Contract.Requires(left != null);
                Contract.Requires(right != null);
                Contract.Ensures(Contract.Result<IOperand>() != null);

                c.rb.EmitBinary(op, left, right, c.resultStorage);
                return c.resultStorage;
            }

            #endregion

            #region Unary Opcodes

            [Builtin("BCOM", Data = UnaryOp.Not)]
            [Builtin("RANDOM", "ZRANDOM", Data = UnaryOp.Random, HasSideEffect = true)]
            [Builtin("FONT", Data = UnaryOp.SetFont, MinVersion = 5, HasSideEffect = true)]
            [Builtin("CHECKU", Data = UnaryOp.CheckUnicode, MinVersion = 5)]
            public static IOperand UnaryValueOp(
                ValueCall c, [Data] UnaryOp op, IOperand value)
            {
                Contract.Requires(value != null);
                Contract.Ensures(Contract.Result<IOperand>() != null);

                c.rb.EmitUnary(op, value, c.resultStorage);
                return c.resultStorage;
            }

            [Builtin("DIRIN", Data = UnaryOp.DirectInput, HasSideEffect = true)]
            [Builtin("DIROUT", Data = UnaryOp.DirectOutput, HasSideEffect = true)]
            [Builtin("BUFOUT", "ZBUFOUT", Data = UnaryOp.OutputBuffer, MinVersion = 4, HasSideEffect = true)]
            [Builtin("HLIGHT", Data = UnaryOp.OutputStyle, HasSideEffect = true)]
            [Builtin("CLEAR", Data = UnaryOp.ClearWindow, MinVersion = 4, HasSideEffect = true)]
            [Builtin("SCREEN", Data = UnaryOp.SelectWindow, HasSideEffect = true)]
            [Builtin("SPLIT", Data = UnaryOp.SplitWindow, HasSideEffect = true)]
            [Builtin("ERASE", Data = UnaryOp.EraseLine, MinVersion = 4, HasSideEffect = true)]
            public static void UnaryVoidOp(
                VoidCall c, [Data] UnaryOp op, IOperand value)
            {
                Contract.Requires(value != null);

                c.rb.EmitUnary(op, value, null);
            }

            [Builtin("ZERO?", "0?")]
            public static void ZeroPredOp(PredCall c, IOperand value)
            {
                Contract.Requires(value != null);

                c.rb.BranchIfZero(value, c.label, c.polarity);
            }

            [Builtin("1?")]
            public static void OnePredOp(PredCall c, IOperand value)
            {
                Contract.Requires(value != null);

                c.rb.BranchIfEqual(value, c.cc.Game.One, c.label, c.polarity);
            }

            [Builtin("LOC", Data = UnaryOp.GetParent)]
            public static IOperand UnaryObjectValueOp(
                ValueCall c, [Data] UnaryOp op, [Object] IOperand obj)
            {
                Contract.Requires(obj != null);
                Contract.Ensures(Contract.Result<IOperand>() != null);

                c.rb.EmitUnary(op, obj, c.resultStorage);
                return c.resultStorage;
            }

            [Builtin("FIRST?", Data = false)]
            [Builtin("NEXT?", Data = true)]
            public static void UnaryObjectValuePredOp(
                ValuePredCall c, [Data] bool sibling, [Object] IOperand obj)
            {
                Contract.Requires(obj != null);
                
                if (sibling)
                    c.rb.EmitGetSibling(obj, c.resultStorage, c.label, c.polarity);
                else
                    c.rb.EmitGetChild(obj, c.resultStorage, c.label, c.polarity);
            }

            [Builtin("PTSIZE", Data = UnaryOp.GetPropSize)]
            public static IOperand UnaryTableValueOp(
                ValueCall c, [Data] UnaryOp op, [Table] IOperand value)
            {
                Contract.Requires(value != null);
                Contract.Ensures(Contract.Result<IOperand>() != null);

                c.rb.EmitUnary(op, value, c.resultStorage);
                return c.resultStorage;
            }

            [Builtin("REMOVE", "ZREMOVE", Data = UnaryOp.RemoveObject, HasSideEffect = true)]
            public static void UnaryObjectVoidOp(
                VoidCall c, [Data] UnaryOp op, [Object] IOperand value)
            {
                Contract.Requires(value != null);
                
                c.rb.EmitUnary(op, value, null);
            }

            [Builtin("ASSIGNED?", Data = Condition.ArgProvided, MinVersion = 5)]
            public static void UnaryVariablePredOp(
                PredCall c, [Data] Condition cond, [Variable] IVariable var)
            {
                Contract.Requires(var != null);
                
                c.rb.Branch(cond, var, null, c.label, c.polarity);
            }

            [Builtin("ASSIGNED?", MinVersion = 5)]
            public static void SoftGlobalAssignedOp(PredCall c, [Variable] SoftGlobal var)
            {
                Contract.Requires(var != null);
                
                // globals are never "assigned" in this sense
                if (!c.polarity)
                    c.rb.Branch(c.label);
            }

            [Builtin("CURGET", Data = UnaryOp.GetCursor, MinVersion = 4, HasSideEffect = true)]
            public static void UnaryTableVoidOp(
                VoidCall c, [Data] UnaryOp op, [Table] IOperand value)
            {
                Contract.Requires(value != null);
                
                c.rb.EmitUnary(op, value, null);
            }

            #endregion

            #region Print Opcodes

            [Builtin("PRINT", "ZPRINT", Data = PrintOp.PackedAddr, HasSideEffect = true)]
            [Builtin("PRINTB", "ZPRINTB", Data = PrintOp.Address, HasSideEffect = true)]
            [Builtin("PRINTC", Data = PrintOp.Character, HasSideEffect = true)]
            [Builtin("PRINTD", Data = PrintOp.Object, HasSideEffect = true)]
            [Builtin("PRINTN", Data = PrintOp.Number, HasSideEffect = true)]
            [Builtin("PRINTU", Data = PrintOp.Unicode, HasSideEffect = true)]
            public static void UnaryPrintVoidOp(
                VoidCall c, [Data] PrintOp op, IOperand value)
            {
                Contract.Requires(value != null);
                                
                c.rb.EmitPrint(op, value);
            }

            [Builtin("PRINTT", HasSideEffect = true)]
            public static void PrintTableOp(
                VoidCall c, [Table] IOperand table, IOperand width,
                IOperand height = null, IOperand skip = null)
            {
                Contract.Requires(table != null);
                Contract.Requires(width != null);
                Contract.Requires(height != null || skip == null);
                                
                c.rb.EmitPrintTable(table, width, height, skip);
            }

            [Builtin("PRINTI", Data = false, HasSideEffect = true)]
            [Builtin("PRINTR", Data = true, HasSideEffect = true)]
            public static void UnaryPrintStringOp(
                VoidCall c, [Data] bool crlfRtrue, string text)
            {
                Contract.Requires(text != null);

                c.rb.EmitPrint(text, crlfRtrue);
            }

            [Builtin("CRLF", "ZCRLF", HasSideEffect = true)]
            public static void CrlfVoidOp(VoidCall c)
            {
                c.rb.EmitPrintNewLine();
            }

            #endregion

            #region Variable Opcodes

            [Builtin("SET", HasSideEffect = true)]
            public static IOperand SetValueOp(
                ValueCall c, [Variable(QuirksMode = QuirksMode.Local)] IVariable dest, ZilObject value)
            {
                Contract.Requires(dest != null);
                Contract.Requires(value != null);
                Contract.Ensures(Contract.Result<IOperand>() != null);

                // in value context, we need to be able to return the newly set value,
                // so dest is IVariable. this means <SET <fancy-expression> value> isn't
                // supported in value context.

                /* TODO: it could be supported if we wanted to get complicated:
                 * <SET expr value> ->
                 *   evaluate expr >TMP
                 *   evaluate value >STACK
                 *   PUSH 'STACK        ;duplicates top value
                 *   SET TMP,STACK      ;performs indirect store
                 *   return STACK       ;value is left on stack
                 */

                var storage = Compiler.CompileAsOperand(c.cc, c.rb, value, c.form.SourceLine, dest);
                if (storage != dest)
                    c.rb.EmitStore(dest, storage);
                return dest;
            }

            [Builtin("SET", HasSideEffect = true)]
            public static IOperand SetValueOp(
                ValueCall c, [Variable(QuirksMode = QuirksMode.Local)] SoftGlobal dest, ZilObject value)
            {
                Contract.Requires(dest != null);
                Contract.Requires(value != null);
                Contract.Ensures(Contract.Result<IOperand>() != null);

                var storage = Compiler.CompileAsOperand(c.cc, c.rb, value, c.form.SourceLine, c.rb.Stack);

                if (storage == c.rb.Stack)
                {
                    // duplicate the value
                    c.rb.EmitUnary(UnaryOp.LoadIndirect, c.rb.Stack, c.rb.Stack);
                }

                c.rb.EmitTernary(
                    dest.IsWord ? TernaryOp.PutWord : TernaryOp.PutByte,
                    c.cc.SoftGlobalsTable,
                    c.cc.Game.MakeOperand(dest.Offset),
                    storage,
                    null);

                return storage;
            }

            [Builtin("SETG", HasSideEffect = true)]
            public static IOperand SetgValueOp(
                ValueCall c, [Variable(QuirksMode = QuirksMode.Global)] IVariable dest, ZilObject value)
            {
                Contract.Requires(dest != null);
                Contract.Requires(value != null);
                Contract.Ensures(Contract.Result<IOperand>() != null);

                return SetValueOp(c, dest, value);
            }

            [Builtin("SETG", HasSideEffect = true)]
            public static IOperand SetgValueOp(
                ValueCall c, [Variable(QuirksMode = QuirksMode.Global)] SoftGlobal dest, ZilObject value)
            {
                Contract.Requires(dest != null);
                Contract.Requires(value != null);
                Contract.Ensures(Contract.Result<IOperand>() != null);

                return SetValueOp(c, dest, value);
            }

            [Builtin("SET")]
            public static void SetVoidOp(
                VoidCall c, [Variable(QuirksMode = QuirksMode.Local)] IOperand dest, ZilObject value)
            {
                Contract.Requires(dest != null);
                Contract.Requires(value != null);
                
                // in void context, we don't need to return the newly set value, so we
                // can support <SET <fancy-expression> value>.

                if (dest is IIndirectOperand)
                {
                    var destVar = ((IIndirectOperand)dest).Variable;
                    var storage = Compiler.CompileAsOperand(c.cc, c.rb, value, c.form.SourceLine, destVar);
                    if (storage != destVar)
                        c.rb.EmitStore(destVar, storage);
                }
                else
                {
                    using (var operands = Operands.Compile(c.cc, c.rb, c.form.SourceLine, value))
                    {
                        if (dest == c.rb.Stack && operands[0] == c.rb.Stack)
                        {
                            var tempAtom = ZilAtom.Parse("?TMP", c.cc.Context);
                            Compiler.PushInnerLocal(c.cc, c.rb, tempAtom);
                            try
                            {
                                var tempLocal = c.cc.Locals[tempAtom];
                                c.rb.EmitStore(tempLocal, operands[0]);
                                c.rb.EmitBinary(BinaryOp.StoreIndirect, dest, tempLocal, null);
                            }
                            finally
                            {
                                Compiler.PopInnerLocal(c.cc, tempAtom);
                            }
                        }
                        else
                        {
                            c.rb.EmitBinary(BinaryOp.StoreIndirect, dest, operands[0], null);
                        }
                    }
                }
            }

            [Builtin("SET")]
            public static void SetVoidOp(
                VoidCall c, [Variable(QuirksMode = QuirksMode.Local)] SoftGlobal dest, ZilObject value)
            {
                Contract.Requires(dest != null);
                Contract.Requires(value != null);

                Contract.Assume(c.cc.SoftGlobalsTable != null);

                c.rb.EmitTernary(
                    dest.IsWord ? TernaryOp.PutWord : TernaryOp.PutByte,
                    c.cc.SoftGlobalsTable,
                    c.cc.Game.MakeOperand(dest.Offset),
                    CompileAsOperand(c.cc, c.rb, value, c.form.SourceLine),
                    null);
            }

            [Builtin("SETG")]
            public static void SetgVoidOp(
                VoidCall c, [Variable(QuirksMode = QuirksMode.Global)] IOperand dest, ZilObject value)
            {
                Contract.Requires(dest != null);
                Contract.Requires(value != null);
                
                SetVoidOp(c, dest, value);
            }
            
            [Builtin("SETG")]
            public static void SetgVoidOp(
                VoidCall c, [Variable(QuirksMode = QuirksMode.Global)] SoftGlobal dest, ZilObject value)
            {
                Contract.Requires(dest != null);
                Contract.Requires(value != null);
                
                SetVoidOp(c, dest, value);
            }

            [Builtin("SET", HasSideEffect = true)]
            public static void SetPredOp(
                PredCall c, [Variable(QuirksMode = QuirksMode.Local)] IVariable dest, ZilObject value)
            {
                Contract.Requires(dest != null);
                Contract.Requires(value != null);
                
                // see note in SetValueOp regarding dest being IVariable
                Compiler.CompileAsOperandWithBranch(c.cc, c.rb, value, dest, c.label, c.polarity);
            }

            [Builtin("SETG", HasSideEffect = true)]
            public static void SetgPredOp(
                PredCall c, [Variable(QuirksMode = QuirksMode.Global)] IVariable dest, ZilObject value)
            {
                Contract.Requires(dest != null);
                Contract.Requires(value != null);
                
                SetPredOp(c, dest, value);
            }

            [Builtin("INC", Data = BinaryOp.Add, HasSideEffect = true)]
            [Builtin("DEC", Data = BinaryOp.Sub, HasSideEffect = true)]
            public static IOperand IncValueOp(ValueCall c, [Data] BinaryOp op,
                [Variable(QuirksMode = QuirksMode.Both)] IVariable victim)
            {
                Contract.Requires(victim != null);
                Contract.Ensures(Contract.Result<IOperand>() != null);

                c.rb.EmitBinary(op, victim, c.cc.Game.One, victim);
                return victim;
            }

            [Builtin("INC", Data = BinaryOp.Add, HasSideEffect = true)]
            [Builtin("DEC", Data = BinaryOp.Sub, HasSideEffect = true)]
            public static IOperand IncValueOp(ValueCall c, [Data] BinaryOp op,
                [Variable(QuirksMode = QuirksMode.Both)] SoftGlobal victim)
            {
                Contract.Requires(victim != null);
                Contract.Ensures(Contract.Result<IOperand>() != null);
                
                var offset = c.cc.Game.MakeOperand(victim.Offset);
                c.rb.EmitBinary(
                    victim.IsWord ? BinaryOp.GetWord : BinaryOp.GetByte,
                    c.cc.SoftGlobalsTable,
                    offset,
                    c.rb.Stack);
                c.rb.EmitBinary(op, c.rb.Stack, c.cc.Game.One, c.rb.Stack);
                c.rb.EmitUnary(UnaryOp.LoadIndirect, c.rb.Stack.Indirect, c.rb.Stack);
                c.rb.EmitTernary(
                    victim.IsWord ? TernaryOp.PutWord : TernaryOp.PutByte,
                    c.cc.SoftGlobalsTable,
                    offset,
                    c.rb.Stack,
                    null);
                return c.rb.Stack;
            }

            [Builtin("INC", Data = BinaryOp.Add, HasSideEffect = true)]
            [Builtin("DEC", Data = BinaryOp.Sub, HasSideEffect = true)]
            public static void IncVoidOp(VoidCall c, [Data] BinaryOp op,
                [Variable(QuirksMode = QuirksMode.Both)] IVariable victim)
            {
                Contract.Requires(victim != null);

                c.rb.EmitBinary(op, victim, c.cc.Game.One, victim);
            }

            [Builtin("INC", Data = BinaryOp.Add, HasSideEffect = true)]
            [Builtin("DEC", Data = BinaryOp.Sub, HasSideEffect = true)]
            public static void IncVoidOp(VoidCall c, [Data] BinaryOp op,
                [Variable(QuirksMode = QuirksMode.Both)] SoftGlobal victim)
            {
                Contract.Requires(victim != null);

                var offset = c.cc.Game.MakeOperand(victim.Offset);
                c.rb.EmitBinary(
                    victim.IsWord ? BinaryOp.GetWord : BinaryOp.GetByte,
                    c.cc.SoftGlobalsTable,
                    offset,
                    c.rb.Stack);
                c.rb.EmitBinary(op, c.rb.Stack, c.cc.Game.One, c.rb.Stack);
                c.rb.EmitTernary(
                    victim.IsWord ? TernaryOp.PutWord : TernaryOp.PutByte,
                    c.cc.SoftGlobalsTable,
                    offset,
                    c.rb.Stack,
                    null);
            }

            [Builtin("PUSH", HasSideEffect = true)]
            public static void PushVoidOp(VoidCall c, IOperand value)
            {
                Contract.Requires(value != null);

                c.rb.EmitStore(c.rb.Stack, value);
            }

            // TODO: support the IVariable, SoftGlobal, and IOperand versions side by side? that way we can skip emitting an instruction for <VALUE VARNAME>
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
                Contract.Requires(value != null);
                Contract.Ensures(Contract.Result<IOperand>() != null);

                c.rb.EmitUnary(UnaryOp.LoadIndirect, value, c.resultStorage);
                return c.resultStorage;
            }

            #endregion

            #region Nullary Opcodes

            [Builtin("CATCH", Data = NullaryOp.Catch, MinVersion = 5)]
            [Builtin("ISAVE", Data = NullaryOp.SaveUndo, HasSideEffect = true, MinVersion = 5)]
            [Builtin("IRESTORE", Data = NullaryOp.RestoreUndo, HasSideEffect = true, MinVersion = 5)]
            public static IOperand NullaryValueOp(ValueCall c, [Data] NullaryOp op)
            {
                Contract.Ensures(Contract.Result<IOperand>() != null);

                c.rb.EmitNullary(op, c.resultStorage);
                return c.resultStorage;
            }

            [Builtin("ORIGINAL?", Data = Condition.Original, MinVersion = 5)]
            [Builtin("VERIFY", Data = Condition.Verify)]
            public static void NullaryPredOp(PredCall c, [Data] Condition cond)
            {
                c.rb.Branch(cond, null, null, c.label, c.polarity);
            }

            [Builtin("USL", Data = NullaryOp.ShowStatus, MinVersion = 3, MaxVersion = 3, HasSideEffect = true)]
            public static void NullaryVoidOp(VoidCall c, [Data] NullaryOp op)
            {
                c.rb.EmitNullary(op, null);
            }

            [Builtin("RTRUE", Data = 1, HasSideEffect = true)]
            [Builtin("RFALSE", Data = 0, HasSideEffect = true)]
            [Builtin("RFATAL", Data = 2, HasSideEffect = true)]
            [Builtin("RSTACK", Data = -1, HasSideEffect = true)]
            public static void NullaryReturnOp(VoidCall c, [Data] int what)
            {
                var operand =
                    (what == 1) ? c.cc.Game.One :
                    (what == 0) ? c.cc.Game.Zero :
                    (what == 2) ? c.cc.Game.MakeOperand(2) :
                    c.rb.Stack;

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

            #endregion

            #region Input Opcodes

            [Builtin("READ", "ZREAD", MaxVersion = 3, HasSideEffect = true)]
            public static void ReadOp_V3(VoidCall c, IOperand text, IOperand parse)
            {
                Contract.Requires(text != null);
                
                c.rb.EmitRead(text, parse, null, null, null);
            }

            [Builtin("READ", "ZREAD", MinVersion = 4, MaxVersion = 4, HasSideEffect = true)]
            public static void ReadOp_V4(VoidCall c, IOperand text, IOperand parse,
                IOperand time = null, [Routine] IOperand routine = null)
            {
                Contract.Requires(text != null);
                Contract.Requires(parse != null);
                Contract.Requires(time != null || routine == null);

                c.rb.EmitRead(text, parse, time, routine, null);
            }

            [Builtin("READ", "ZREAD", MinVersion = 5, HasSideEffect = true)]
            public static IOperand ReadOp_V5(ValueCall c, IOperand text,
                IOperand parse = null, IOperand time = null,
                [Routine] IOperand routine = null)
            {
                Contract.Requires(text != null);
                Contract.Requires(parse != null || time == null);
                Contract.Requires(time != null || routine == null);
                Contract.Ensures(Contract.Result<IOperand>() != null);

                c.rb.EmitRead(text, parse, time, routine, c.resultStorage);
                return c.resultStorage;
            }

            [Builtin("INPUT", MinVersion = 4, HasSideEffect = true)]
            public static IOperand InputOp(ValueCall c, IOperand dummy,
                IOperand interval = null, [Routine] IOperand routine = null)
            {
                Contract.Requires(dummy != null);
                
                if (!(c.form.Rest.First is ZilFix && ((ZilFix)c.form.Rest.First).Value == 1))
                {
                    Errors.CompError(c.cc.Context, c.form, "INPUT: argument 1 must be 1");
                    return c.cc.Game.Zero;
                }

                c.rb.EmitReadChar(interval, routine, c.resultStorage);
                return c.resultStorage;
            }

            #endregion

            #region Sound Opcodes

            [Builtin("SOUND", MaxVersion = 4, HasSideEffect = true)]
            public static void SoundOp_V3(VoidCall c, IOperand number,
                IOperand effect = null, IOperand volume = null)
            {
                Contract.Requires(number != null);
                Contract.Requires(effect != null || volume == null);

                c.rb.EmitPlaySound(number, effect, volume, null);
            }

            [Builtin("SOUND", MinVersion = 5, HasSideEffect = true)]
            public static void SoundOp_V5(VoidCall c, IOperand number,
                IOperand effect = null, IOperand volume = null,
                [Routine] IOperand routine = null)
            {
                Contract.Requires(number != null);
                Contract.Requires(effect != null || volume == null);
                Contract.Requires(volume != null || routine == null);

                c.rb.EmitPlaySound(number, effect, volume, null);
            }

            #endregion

            #region Vocab Opcodes

            [Builtin("ZWSTR", MinVersion = 5, HasSideEffect = true)]
            public static void EncodeTextOp(VoidCall c,
                [Table] IOperand src, IOperand length,
                IOperand srcOffset, [Table] IOperand dest)
            {
                Contract.Requires(src != null);
                Contract.Requires(length != null);
                Contract.Requires(srcOffset != null);
                Contract.Requires(dest != null);

                c.rb.EmitEncodeText(src, length, srcOffset, dest);
            }

            [Builtin("LEX", MinVersion = 5, HasSideEffect = true)]
            public static void LexOp(VoidCall c,
                [Table] IOperand text, [Table] IOperand parse,
                [Table] IOperand dictionary = null, IOperand flag = null)
            {
                Contract.Requires(text != null);
                Contract.Requires(parse != null);
                Contract.Requires(dictionary != null || flag == null);

                c.rb.EmitTokenize(text, parse, dictionary, flag);
            }

            #endregion

            #region Save/Restore Opcodes

            [Builtin("RESTORE", "ZRESTORE", MaxVersion = 3, HasSideEffect = true)]
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

            [Builtin("RESTORE", "ZRESTORE", MinVersion = 4, HasSideEffect = true)]
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

            [Builtin("RESTORE", "ZRESTORE", MinVersion = 5, HasSideEffect = true)]
            public static IOperand RestoreOp_V5(ValueCall c, [Table] IOperand table,
                IOperand bytes, [Table] IOperand name)
            {
                Contract.Requires(table != null);
                Contract.Requires(bytes != null);
                Contract.Requires(name != null);
                Contract.Ensures(Contract.Result<IOperand>() != null);

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

            [Builtin("SAVE", "ZSAVE", MaxVersion = 3, HasSideEffect = true)]
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

            [Builtin("SAVE", "ZSAVE", MinVersion = 4, HasSideEffect = true)]
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

            [Builtin("SAVE", "ZSAVE", MinVersion = 5, HasSideEffect = true)]
            public static IOperand SaveOp_V5(ValueCall c, [Table] IOperand table,
                IOperand bytes, [Table] IOperand name)
            {
                Contract.Requires(table != null);
                Contract.Requires(bytes != null);
                Contract.Requires(name != null);
                Contract.Ensures(Contract.Result<IOperand>() != null);

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

            #endregion

            #region Routine Opcodes/Builtins

            [Builtin("RETURN", HasSideEffect = true)]
            public static void ReturnOp(VoidCall c, IOperand value = null, Block block = null)
            {
                if (block == null)
                {
                    block = c.cc.Blocks.First(b => (b.Flags & BlockFlags.ExplicitOnly) == 0);
                }

                if (block.ReturnLabel == null)
                {
                    // return from routine
                    c.rb.Return(value ?? c.cc.Game.One);
                }
                else
                {
                    // return from enclosing PROG/REPEAT
                    if ((block.Flags & BlockFlags.WantResult) != 0)
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

                        Errors.CompWarning(c.cc.Context, c.form, "RETURN value ignored: block is in void context");
                    }

                    block.Flags |= BlockFlags.Returned;
                    c.rb.Branch(block.ReturnLabel);
                }
            }

            [Builtin("AGAIN", HasSideEffect = true)]
            public static void AgainOp(VoidCall c, Block block = null)
            {
                if (block == null)
                {
                    block = c.cc.Blocks.First(b => (b.Flags & BlockFlags.ExplicitOnly) == 0);
                }

                if (block.AgainLabel != null)
                {
                    c.rb.Branch(block.AgainLabel);
                }
                else
                {
                    Errors.CompError(c.cc.Context, c.form, "AGAIN requires a PROG/REPEAT block or routine");
                }
            }

            [Builtin("APPLY", "CALL", "ZAPPLY", HasSideEffect = true)]
            public static IOperand CallValueOp(ValueCall c,
                [Routine] IOperand routine, params IOperand[] args)
            {
                Contract.Requires(routine != null);
                Contract.Requires(args != null);
                Contract.Ensures(Contract.Result<IOperand>() != null);

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
                Contract.Requires(routine != null);
                Contract.Requires(args != null);

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

            #endregion

            #region Table Opcodes/Builtins

            [Builtin("INTBL?", MinVersion = 4, MaxVersion = 4)]
            [return: Table]
            public static void IntblValuePredOp_V4(ValuePredCall c,
                IOperand value, [Table] IOperand table, IOperand length)
            {
                Contract.Requires(value != null);
                Contract.Requires(table != null);
                Contract.Requires(length != null);

                c.rb.EmitScanTable(value, table, length, null, c.resultStorage, c.label, c.polarity);
            }

            [Builtin("INTBL?", MinVersion = 5)]
            [return: Table]
            public static void IntblValuePredOp_V5(ValuePredCall c,
                IOperand value, [Table] IOperand table, IOperand length, IOperand form = null)
            {
                Contract.Requires(value != null);
                Contract.Requires(table != null);
                Contract.Requires(length != null);

                c.rb.EmitScanTable(value, table, length, form, c.resultStorage, c.label, c.polarity);
            }

            private static bool GetLowCoreField(string name, Context ctx, ISourceLine src, ZilObject fieldSpec, bool writing,
                out int offset, out LowCoreFlags flags, out int minVersion)
            {
                Contract.Requires(name != null);
                Contract.Requires(ctx != null);
                Contract.Requires(src != null);
                Contract.Requires(fieldSpec != null);
                Contract.Ensures(Contract.ValueAtReturn(out offset) >= 0);
                Contract.Ensures(Contract.ValueAtReturn(out minVersion) >= 0);
                Contract.Ensures(Contract.ValueAtReturn(out minVersion) >= 1 || Contract.Result<bool>() == false);

                offset = 0;
                flags = LowCoreFlags.None;
                minVersion = 0;

                var atom = fieldSpec as ZilAtom;
                if (atom != null)
                {
                    var field = LowCoreField.Get(atom);
                    if (field == null)
                    {
                        Errors.CompError(ctx, src, name + ": unrecognized header field " + atom);
                        return false;
                    }
                    else if (field.MinVersion > ctx.ZEnvironment.ZVersion)
                    {
                        Errors.CompError(ctx, src, name + ": field not supported in this Z-machine version: " + atom);
                        return false;
                    }
                    else if (writing && (field.Flags & LowCoreFlags.Writable) == 0)
                    {
                        Errors.CompError(ctx, src, name + ": field is not writable: " + atom);
                        return false;
                    }

                    offset = field.Offset;
                    flags = field.Flags;
                    minVersion = field.MinVersion;
                    return true;
                }

                var list = fieldSpec as ZilList;
                if (list != null && list.GetTypeAtom(ctx).StdAtom == StdAtom.LIST)
                {
                    if (((IStructure)list).GetLength(2) != 2)
                    {
                        Errors.CompError(ctx, src, name + ": list must have 2 elements");
                        return false;
                    }

                    atom = list.First as ZilAtom;
                    if (atom == null)
                    {
                        Errors.CompError(ctx, src, name + ": first list element must be an atom");
                        return false;
                    }

                    var fix = list.Rest.First as ZilFix;
                    if (fix == null || fix.Value < 0 || fix.Value > 1)
                    {
                        Errors.CompError(ctx, src, name + ": second list element must be 0 or 1");
                        return false;
                    }

                    var field = LowCoreField.Get(atom);
                    if (field == null)
                    {
                        Errors.CompError(ctx, src, name + ": unrecognized header field " + atom);
                        return false;
                    }
                    else if (field.MinVersion > ctx.ZEnvironment.ZVersion)
                    {
                        Errors.CompError(ctx, src, name + ": field not supported in this Z-machine version: " + atom);
                        return false;
                    }
                    else if ((field.Flags & LowCoreFlags.Byte) != 0)
                    {
                        Errors.CompError(ctx, src, name + ": not a word field: " + atom);
                        return false;
                    }
                    else if (writing && (field.Flags & LowCoreFlags.Writable) == 0)
                    {
                        Errors.CompError(ctx, src, name + ": field is not writable: " + atom);
                        return false;
                    }

                    offset = field.Offset * 2 + fix.Value;
                    flags = field.Flags | LowCoreFlags.Byte;
                    minVersion = field.MinVersion;
                    return true;
                }

                Errors.CompError(ctx, src, name + ": first arg must be an atom or list");
                return false;
            }

            [Builtin("LOWCORE")]
            public static IOperand LowCoreReadOp(ValueCall c, ZilObject fieldSpec)
            {
                Contract.Requires(fieldSpec != null);
                Contract.Ensures(Contract.Result<IOperand>() != null);

                int offset, minVersion;
                LowCoreFlags flags;

                if (!GetLowCoreField("LOWCORE", c.cc.Context, c.form.SourceLine, fieldSpec, false, out offset, out flags, out minVersion))
                    return c.cc.Game.Zero;

                var binaryOp = ((flags & LowCoreFlags.Byte) != 0) ? BinaryOp.GetByte : BinaryOp.GetWord;
                if ((flags & LowCoreFlags.Extended) != 0)
                {
                    var wordOffset = ((flags & LowCoreFlags.Byte) != 0) ? (offset + 1) / 2 : offset;
                    c.cc.Context.ZEnvironment.EnsureMinimumHeaderExtension(wordOffset);
                    c.rb.EmitBinary(BinaryOp.GetWord, c.cc.Game.Zero, c.cc.Game.MakeOperand(27 /* EXTAB */), c.rb.Stack);
                    c.rb.EmitBinary(binaryOp, c.rb.Stack, c.cc.Game.MakeOperand(offset), c.resultStorage);
                }
                else
                {
                    c.rb.EmitBinary(binaryOp, c.cc.Game.Zero, c.cc.Game.MakeOperand(offset), c.resultStorage);
                }

                return c.resultStorage;
            }

            [Builtin("LOWCORE", HasSideEffect = true)]
            public static void LowCoreWriteOp(VoidCall c, ZilObject fieldSpec, IOperand newValue)
            {
                Contract.Requires(fieldSpec != null);
                Contract.Requires(newValue != null);

                int offset, minVersion;
                LowCoreFlags flags;

                if (!GetLowCoreField("LOWCORE", c.cc.Context, c.form.SourceLine, fieldSpec, true, out offset, out flags, out minVersion))
                    return;

                var ternaryOp = ((flags & LowCoreFlags.Byte) != 0) ? TernaryOp.PutByte : TernaryOp.PutWord;
                if ((flags & LowCoreFlags.Extended) != 0)
                {
                    var wordOffset = ((flags & LowCoreFlags.Byte) != 0) ? (offset + 1) / 2 : offset;
                    c.cc.Context.ZEnvironment.EnsureMinimumHeaderExtension(wordOffset);
                    c.rb.EmitBinary(BinaryOp.GetWord, c.cc.Game.Zero, c.cc.Game.MakeOperand(27 /* EXTAB */), c.rb.Stack);
                    c.rb.EmitTernary(ternaryOp, c.rb.Stack, c.cc.Game.MakeOperand(offset), newValue, null);
                }
                else
                {
                    c.rb.EmitTernary(ternaryOp, c.cc.Game.Zero, c.cc.Game.MakeOperand(offset), newValue, null);
                }
            }

            [Builtin("LOWCORE-TABLE", HasSideEffect = true)]
            public static void LowCoreTableOp(VoidCall c, ZilObject fieldSpec, int length, ZilAtom handler)
            {
                Contract.Requires(fieldSpec != null);
                Contract.Requires(handler != null);

                int offset, minVersion;
                LowCoreFlags flags;

                if (!GetLowCoreField("LOWCORE-TABLE", c.cc.Context, c.form.SourceLine, fieldSpec, false, out offset, out flags, out minVersion))
                    return;

                if ((flags & LowCoreFlags.Byte) == 0)
                {
                    offset *= 2;
                }

                var tmpAtom = ZilAtom.Parse("?TMP", c.cc.Context);
                var lb = PushInnerLocal(c.cc, c.rb, tmpAtom);
                try
                {
                    c.rb.EmitStore(lb, c.cc.Game.MakeOperand((int)offset));

                    var label = c.rb.DefineLabel();
                    c.rb.MarkLabel(label);

                    var form = new ZilForm(new ZilObject[] {
                        handler,
                        new ZilForm(new ZilObject[] {
                            c.cc.Context.GetStdAtom(StdAtom.GETB),
                            new ZilFix(0),
                            new ZilForm(new ZilObject[] {
                                c.cc.Context.GetStdAtom(StdAtom.LVAL),
                                tmpAtom
                            }),
                        }),
                    });
                    form.SourceLine = c.form.SourceLine;
                    CompileForm(c.cc, c.rb, form, false, null);

                    c.rb.Branch(Condition.IncCheck, lb, c.cc.Game.MakeOperand((int)offset + length - 1), label, false);
                }
                finally
                {
                    PopInnerLocal(c.cc, tmpAtom);
                }
            }

            #endregion

            [Builtin("CHTYPE")]
            public static IOperand ChtypeValueOp(ValueCall c, IOperand value, ZilAtom type)
            {
                Contract.Requires(value != null);
                Contract.Requires(type != null);
                Contract.Ensures(Contract.Result<IOperand>() != null);

                // TODO: check type?
                return value;
            }
        }
    }
}
