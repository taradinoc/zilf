/* Copyright 2010, 2016 Jesse McGrew
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
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Zilf.Interpreter.Values;
using Zilf.Language;

namespace Zilf.Interpreter
{
    abstract class ArgumentDecodingError : InterpreterError
    {
        public ArgumentDecodingError(string message)
            : base(message) { }
    }

    abstract class CallSite
    {
        public abstract override string ToString();
        public abstract string DescribeArgument(int childIndex);
    }

    sealed class FunctionCallSite : CallSite
    {
        public FunctionCallSite(string name)
        {
            this.Name = name;
        }

        public string Name { get; }

        public override string ToString()
        {
            return Name;
        }

        public override string DescribeArgument(int childIndex)
        {
            return $"{Name}: arg {childIndex + 1}";
        }
    }

    sealed class StructuredArgumentCallSite : CallSite
    {
        public StructuredArgumentCallSite(CallSite parent, int argIndex)
        {
            this.Parent = parent;
            this.ArgIndex = argIndex;
        }

        public CallSite Parent { get; }
        public int ArgIndex { get; }

        public override string ToString()
        {
            return Parent.DescribeArgument(ArgIndex);
        }

        public override string DescribeArgument(int childIndex)
        {
            return $"{Parent.DescribeArgument(ArgIndex)}: element {childIndex + 1}";
        }
    }

    sealed class ArgumentCountError : ArgumentDecodingError
    {
        public ArgumentCountError(CallSite site, int minExpected, int maxExpected, int actual)
            : base(ZilError.ArgCountMsg(site.ToString(), minExpected, maxExpected))
        {
        }
    }

    sealed class ArgumentTypeError : ArgumentDecodingError
    {
        public ArgumentTypeError(CallSite site, int index, string constraintDesc)
            : base($"{site.DescribeArgument(index)}: expected {constraintDesc}")
        {
        }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter)]
    sealed class DeclAttribute : Attribute
    {
        public DeclAttribute(string pattern)
        {
            this.Pattern = pattern;
        }

        public string Pattern { get; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter)]
    sealed class RequiredAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Struct)]
    sealed class ZilStructuredParamAttribute : Attribute
    {
        public ZilStructuredParamAttribute(StdAtom typeAtom)
        {
            this.TypeAtom = typeAtom;
        }

        public StdAtom TypeAtom { get; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter)]
    sealed class ZilOptionalAttribute : Attribute
    {
        public object Default { get; set; }
    }

    class ArgDecoder
    {
        // calls ready for each decoded value, returns number of arguments consumed
        private delegate int DecodingStep(ZilObject[] arguments, int index, DecodingStepCallbacks cb);

        private struct DecodingStepCallbacks
        {
            public Context Context;
            public CallSite Site;
            public Action<object> Ready;
            public Action<string> Error;
        }

        private struct DecodingStepInfo
        {
            public DecodingStep Step;

            /// <summary>
            /// The minimum number of arguments this step will consume.
            /// </summary>
            public int LowerBound;
            /// <summary>
            /// The maximum number of arguments this step will consume.
            /// </summary>
            public int? UpperBound;
        }

        private class ConstraintsBuilder
        {
            private readonly List<string> parts = new List<string>();
            private readonly List<Func<ZilObject, Context, bool>> preds = new List<Func<ZilObject, Context, bool>>();

            public void AddTypeConstraint(StdAtom typeAtom)
            {
                switch (typeAtom)
                {
                    case StdAtom.APPLICABLE:
                        parts.Add("applicable object");
                        preds.Add((zo, ctx) => zo is IApplicable);
                        break;

                    case StdAtom.STRUCTURED:
                        parts.Add("structured object");
                        preds.Add((zo, ctx) => zo is IStructure);
                        break;

                    default:
                        //XXX probably good enough for builtin types, but should get the real name from the attribute
                        parts.Add("TYPE " + typeAtom.ToString());
                        preds.Add((zo, ctx) => zo.GetTypeAtom(ctx).StdAtom == typeAtom);
                        break;
                }
            }

            public void AddPrimTypeConstraint(PrimType primtype)
            {
                parts.Add("PRIMTYPE " + primtype.ToString());
                preds.Add((zo, _) => zo.PrimType == primtype);
            }

            public void AddDeclConstraint(DeclWrapper decl)
            {
                parts.Add("matching pattern " + decl);
                preds.Add((zo, ctx) => Decl.Check(ctx, zo, decl.GetPattern(ctx)));
            }

            public Func<ZilObject, Context, bool> GetDelegate()
            {
                return (zo, ctx) => preds.All(p => p(zo, ctx));
            }

            public override string ToString()
            {
                switch (parts.Count)
                {
                    case 0:
                        return "???";

                    case 1:
                        return parts[0];

                    case 2:
                        return parts[0] + " and " + parts[1];

                    default:
                        var last = parts.Count - 1;
                        return string.Join(
                            ", ",
                            parts.Select((s, i) => i < last ? s : "and " + s));
                }
            }
        }

        private readonly DecodingStep[] steps;
        private readonly int lowerBound;
        private readonly int? upperBound;
        private readonly int lastRequiredStepIndex; // TODO: delete this

        private ArgDecoder(object[] methodAttributes, ParameterInfo[] parameters)
        {
            steps = new DecodingStep[parameters.Length - 1];
            lowerBound = 0;
            upperBound = 0;
            lastRequiredStepIndex = -1;

            // skip first arg (Context)
            for (int i = 1; i < parameters.Length; i++)
            {
                var stepInfo = PrepareOne(parameters[i]);
                steps[i - 1] = stepInfo.Step;

                lowerBound += stepInfo.LowerBound;

                if (lowerBound > 0)
                {
                    lastRequiredStepIndex = i - 1;
                }

                if (stepInfo.UpperBound == null)
                {
                    upperBound = null;
                }
                else
                {
                    upperBound += stepInfo.UpperBound;
                }
            }
        }

        private static DecodingStepInfo PrepareOne(ParameterInfo pi)
        {
            var zilOptAttr = pi.GetCustomAttribute<ZilOptionalAttribute>();

            bool isOptional;
            object defaultValue;

            if (zilOptAttr != null)
            {
                if (pi.IsOptional)
                    throw new InvalidOperationException($"Expected {nameof(ZilOptionalAttribute)} or {nameof(pi.IsOptional)}, not both");

                isOptional = true;
                defaultValue = zilOptAttr.Default;
            }
            else
            {
                isOptional = pi.IsOptional;
                defaultValue = pi.HasDefaultValue ? pi.DefaultValue : null;
            }

            return PrepareOne(
                pi.ParameterType,
                pi.GetCustomAttributes(false),
                isOptional,
                defaultValue);
        }

        private static DecodingStepInfo PrepareOne(FieldInfo fi)
        {
            var zilOptAttr = fi.GetCustomAttribute<ZilOptionalAttribute>();

            bool isOptional;
            object defaultValue;

            if (zilOptAttr != null)
            {
                isOptional = true;
                defaultValue = zilOptAttr.Default;
            }
            else
            {
                isOptional = false;
                defaultValue = null;
            }

            return PrepareOne(
                fi.FieldType,
                fi.GetCustomAttributes(false),
                isOptional,
                defaultValue);
        }

        private static DecodingStepInfo PrepareOne(Type paramType, object[] customAttributes, bool isOptional, object defaultValueWhenOptional)
        {
            DecodingStepInfo result;
            ConstraintsBuilder cb = new ConstraintsBuilder();
            string errmsg = null;
            object defaultValue = null;
            ZilStructuredParamAttribute zilParamAttr;

            bool isRequired = customAttributes.OfType<RequiredAttribute>().Any();
            if (isRequired && isOptional)
            {
                throw new InvalidOperationException("A parameter can't be both optional and required");
            }

            if (paramType.IsValueType &&
                (zilParamAttr = paramType.GetCustomAttribute<ZilStructuredParamAttribute>()) != null)
            {
                cb.AddTypeConstraint(zilParamAttr.TypeAtom);

                result = PrepareOneStructured(paramType);
            }
            else if (paramType.IsArray &&
                (zilParamAttr = paramType.GetElementType().GetCustomAttribute<ZilStructuredParamAttribute>()) != null)
            {
                cb.AddTypeConstraint(zilParamAttr.TypeAtom);

                var elemType = paramType.GetElementType();
                var innerStep = PrepareOneStructured(elemType).Step;
                defaultValue = Array.CreateInstance(elemType, 0);

                result = new DecodingStepInfo
                {
                    Step = (a, i, c) =>
                    {
                        var array = Array.CreateInstance(elemType, a.Length - i);

                        if (isRequired && array.Length == 0)
                        {
                            c.Error(errmsg);
                        }

                        c.Ready(array);

                        int elemIndex = 0;

                        for (; elemIndex < array.Length; elemIndex++)
                        {
                            c.Ready = obj => array.SetValue(obj, elemIndex);
                            innerStep(a, i + elemIndex, c);
                        }

                        return a.Length;
                    },
                    LowerBound = 0,
                    UpperBound = null,
                };

                if (isRequired)
                    result.LowerBound = 1;
            }
            else if (paramType == typeof(ZilObject))
            {
                // decode to ZilObject as-is
                result = new DecodingStepInfo
                {
                    Step = (a, i, c) => { c.Ready(a[i]); return i + 1; },
                    LowerBound = 1,
                    UpperBound = 1,
                };
            }
            else if (paramType == typeof(int) || paramType == typeof(int?))
            {
                cb.AddTypeConstraint(StdAtom.FIX);

                if (paramType == typeof(int))
                {
                    defaultValue = 0;
                }

                result = new DecodingStepInfo
                {
                    Step = (a, i, c) =>
                    {
                        var fix = a[i] as ZilFix;
                        if (fix == null)
                        {
                            c.Error(errmsg);
                        }
                        else
                        {
                            c.Ready(fix.Value);
                        }
                        return i + 1;
                    },
                    LowerBound = 1,
                    UpperBound = 1,
                };
            }
            else if (paramType == typeof(string))
            {
                cb.AddTypeConstraint(StdAtom.STRING);

                result = new DecodingStepInfo
                {
                    Step = (a, i, c) =>
                    {
                        var str = a[i] as ZilString;
                        if (str == null)
                        {
                            c.Error(errmsg);
                        }
                        else
                        {
                            c.Ready(str.Text);
                        }
                        return i + 1;
                    },
                    LowerBound = 1,
                    UpperBound = 1,
                };
            }
            else if (paramType == typeof(char) || paramType == typeof(char?))
            {
                cb.AddTypeConstraint(StdAtom.CHARACTER);

                if (paramType == typeof(char))
                {
                    defaultValue = '\0';
                }

                result = new DecodingStepInfo
                {
                    Step = (a, i, c) =>
                    {
                        var ch = a[i] as ZilChar;
                        if (ch == null)
                        {
                            c.Error(errmsg);
                        }
                        else
                        {
                            c.Ready(ch.Char);
                        }
                        return i + 1;
                    },
                    LowerBound = 1,
                    UpperBound = 1,
                };
            }
            else if (paramType == typeof(bool) || paramType == typeof(bool?))
            {
                if (paramType == typeof(bool))
                {
                    defaultValue = false;
                }

                result = new DecodingStepInfo
                {
                    Step = (a, i, c) =>
                    {
                        c.Ready(a[i].IsTrue);
                        return i + 1;
                    },
                    LowerBound = 1,
                    UpperBound = 1,
                };
            }
            else if (paramType.IsArray && IsZilObjectType(paramType.GetElementType()))
            {
                // decode as an array containing all remaining args
                var eltype = paramType.GetElementType();
                defaultValue = Array.CreateInstance(eltype, 0);

                if (eltype == typeof(IApplicable))
                {
                    cb.AddTypeConstraint(StdAtom.APPLICABLE);
                }
                else if (eltype == typeof(IStructure))
                {
                    cb.AddTypeConstraint(StdAtom.STRUCTURED);
                }
                else if (eltype != typeof(ZilObject))
                {
                    var builtinAttr = eltype.GetCustomAttribute<BuiltinTypeAttribute>();
                    if (builtinAttr == null)
                        throw new InvalidOperationException($"Type {eltype} is missing a BuiltinTypeAttribute");
                    cb.AddTypeConstraint(builtinAttr.Name);
                }

                result = new DecodingStepInfo
                {
                    Step = (a, i, c) =>
                    {
                        for (int j = i; j < a.Length; j++)
                        {
                            if (!eltype.IsInstanceOfType(a[j]))
                            {
                                c.Error(errmsg);
                            }
                        }

                        var array = Array.CreateInstance(eltype, a.Length - i);

                        if (isRequired && array.Length == 0)
                        {
                            c.Error(errmsg);
                        }

                        Array.Copy(a, i, array, 0, array.Length);
                        c.Ready(array);
                        return a.Length;
                    },
                    LowerBound = 0,
                    UpperBound = null,
                };

                if (isRequired)
                    result.LowerBound = 1;
            }
            else if (IsZilObjectType(paramType))
            {
                var zoType = paramType;

                if (paramType == typeof(IApplicable))
                {
                    cb.AddTypeConstraint(StdAtom.APPLICABLE);
                }
                else if (paramType == typeof(IStructure))
                {
                    cb.AddTypeConstraint(StdAtom.STRUCTURED);
                }
                else if (paramType != typeof(ZilObject))
                {
                    var builtinAttr = paramType.GetCustomAttribute<BuiltinTypeAttribute>();
                    if (builtinAttr == null)
                        throw new InvalidOperationException($"Type {paramType} is missing a BuiltinTypeAttribute");
                    cb.AddTypeConstraint(zoType.GetCustomAttribute<BuiltinTypeAttribute>().Name);
                }

                result = new DecodingStepInfo
                {
                    Step = (a, i, c) =>
                    {
                        if (!zoType.IsInstanceOfType(a[i]))
                        {
                            c.Error(errmsg);
                        }

                        c.Ready(a[i]);
                        return i + 1;
                    },
                    LowerBound = 1,
                    UpperBound = 1,
                };
            }
            else if (paramType == typeof(int[]))
            {
                cb.AddTypeConstraint(StdAtom.FIX);

                defaultValue = new int[0];

                result = new DecodingStepInfo
                {
                    Step = (a, i, c) =>
                    {
                        var array = new int[a.Length - i];

                        if (isRequired && array.Length == 0)
                        {
                            c.Error(errmsg);
                        }

                        for (int j = 0; j < array.Length; j++)
                        {
                            var fix = a[i + j] as ZilFix;
                            if (fix == null)
                            {
                                c.Error(errmsg);
                            }
                            else
                            {
                                array[j] = fix.Value;
                            }
                        }
                        c.Ready(array);
                        return a.Length;
                    },
                    LowerBound = 0,
                    UpperBound = null,
                };

                if (isRequired)
                    result.LowerBound = 1;
            }
            else if (paramType == typeof(string[]))
            {
                cb.AddTypeConstraint(StdAtom.STRING);

                defaultValue = new string[0];

                result = new DecodingStepInfo
                {
                    Step = (a, i, c) =>
                    {
                        var array = new string[a.Length - i];

                        if (isRequired && array.Length == 0)
                        {
                            c.Error(errmsg);
                        }

                        for (int j = 0; j < array.Length; j++)
                        {
                            var str = a[i + j] as ZilString;
                            if (str == null)
                            {
                                c.Error(errmsg);
                            }
                            else
                            {
                                array[j] = str.Text;
                            }
                        }
                        c.Ready(array);
                        return a.Length;
                    },
                    LowerBound = 0,
                    UpperBound = null,
                };

                if (isRequired)
                    result.LowerBound = 1;
            }
            else
            {
                throw new NotImplementedException($"Unhandled parameter type: {paramType}");
            }

            // modifiers
            var declAttr = customAttributes.OfType<DeclAttribute>().FirstOrDefault();
            if (declAttr != null)
            {
                var prevStep = result.Step;
                var declWrapper = new DeclWrapper(declAttr.Pattern);

                cb.AddDeclConstraint(declWrapper);

                // for array (varargs) parameters, the decl is checked against a LIST containing all the args
                if (paramType.IsArray)
                {
                    result.Step = (a, i, c) =>
                    {
                        var prevConsumed = prevStep(a, i, c);
                        var list = new ZilList(a.Skip(i).Take(prevConsumed));

                        if (!Decl.Check(c.Context, list, declWrapper.GetPattern(c.Context)))
                        {
                            c.Error(errmsg);
                        }

                        return prevConsumed;
                    };
                }
                else
                {
                    result.Step = (a, i, c) =>
                    {
                        if (!Decl.Check(c.Context, a[i], declWrapper.GetPattern(c.Context)))
                        {
                            c.Error(errmsg);
                        }

                        return prevStep(a, i, c);
                    };
                }
            }

            if (isOptional)
            {
                result.LowerBound = 0;

                var del = cb.GetDelegate();
                var prevStep = result.Step;

                defaultValueWhenOptional = defaultValueWhenOptional ?? defaultValue;

                result.Step = (a, i, c) =>
                {
                    if (i < a.Length && del(a[i], c.Context))
                    {
                        return prevStep(a, i, c);
                    }
                    else
                    {
                        c.Ready(defaultValueWhenOptional);
                        return i;
                    }
                };
            }

            errmsg = cb.ToString();
            return result;
        }

        // TODO: cache the result
        private static DecodingStepInfo PrepareOneStructured(Type structType)
        {
            Contract.Requires(structType != null);
            Contract.Requires(structType.IsValueType);
            Contract.Requires(structType.IsLayoutSequential);
            Contract.Requires(structType.GetCustomAttribute<ZilStructuredParamAttribute>() != null);

            var typeAtom = structType.GetCustomAttribute<ZilStructuredParamAttribute>().TypeAtom;

            var fields = structType.GetFields()
                .OrderBy(f => Marshal.OffsetOf(structType, f.Name).ToInt64())
                .ToArray();

            var steps = new DecodingStep[fields.Length];
            int lowerBound = 0;
            int? upperBound = null;

            for (int i = 0; i < fields.Length; i++)
            {
                DecodingStepInfo stepInfo = PrepareOne(fields[i]);
                steps[i] = stepInfo.Step;

                lowerBound += stepInfo.LowerBound;

                if (stepInfo.UpperBound == null)
                {
                    upperBound = null;
                }
                else
                {
                    upperBound += stepInfo.UpperBound;
                }
            }

            var result = new DecodingStepInfo
            {
                LowerBound = 1,
                UpperBound = 1,

                Step = (a, i, c) =>
                {
                    if (a[i].GetTypeAtom(c.Context).StdAtom != typeAtom)
                    {
                        c.Error($"TYPE {typeAtom}");
                    }

                    var input = (IStructure)a[i];

                    if (input.GetLength(lowerBound - 1) < lowerBound ||
                        (upperBound != null && !(input.GetLength(upperBound.Value) <= upperBound)))
                    {
                        // TODO: something better than GetLength(999)?
                        throw new ArgumentCountError(c.Site, lowerBound, upperBound ?? 0, input.GetLength(999) ?? 999);
                    }

                    var inputLength = input.GetLength();
                    var output = Activator.CreateInstance(structType);

                    var elements = new ZilObject[inputLength];
                    for (int j = 0; j < elements.Length; j++)
                        elements[j] = input[j];

                    var elemIndex = 0;
                    var stepIndex = 0;

                    var outerReady = c.Ready;
                    c.Site = new StructuredArgumentCallSite(c.Site, i);
                    c.Error = m => { throw new ArgumentTypeError(c.Site, elemIndex, m); };
                    c.Ready = obj => fields[stepIndex].SetValue(output, obj);

                    for (; stepIndex < steps.Length; stepIndex++)
                    {
                        var step = steps[stepIndex];
                        var next = step(elements, elemIndex, c);
                        Contract.Assert(next >= elemIndex);
                        elemIndex = next;
                    }

                    if (elemIndex < inputLength)
                    {
                        // TODO: clarify error message (argument count might be fine but types are wrong)
                        throw new ArgumentCountError(c.Site, lowerBound, upperBound ?? 0, elemIndex);
                    }

                    outerReady(output);
                    return i + 1;
                },
            };

            return result;
        }

        private static bool IsZilObjectType(Type t)
        {
            return typeof(ZilObject).IsAssignableFrom(t) ||
                t == typeof(IApplicable) || t == typeof(IStructure);
        }

        public static ArgDecoder FromMethodInfo(MethodInfo methodInfo)
        {
            if (methodInfo == null)
                throw new ArgumentNullException("methodInfo");

            if (!typeof(ZilObject).IsAssignableFrom(methodInfo.ReturnType))
                throw new ArgumentException("Method return type is not assignable to ZilObject");

            var methodAttrs = methodInfo.GetCustomAttributes(false);
            var parameters = methodInfo.GetParameters();
            var function = methodAttrs.OfType<Subrs.SubrAttribute>().FirstOrDefault()?.Name ?? methodInfo.Name;

            if (parameters.Length < 1 || parameters[0].ParameterType != typeof(Context))
                throw new ArgumentException("First parameter type must be Context");

            return new ArgDecoder(methodAttrs, parameters);
        }

        public static SubrDelegate WrapMethodAsSubrDelegate(MethodInfo methodInfo)
        {
            Contract.Requires(methodInfo.IsStatic);
            Contract.Ensures(Contract.Result<SubrDelegate>() != null);

            return WrapMethodAsSubrDelegate(methodInfo, null);
        }

        private static SubrDelegate WrapMethodAsSubrDelegate(MethodInfo methodInfo, Dictionary<MethodInfo, SubrDelegate> alreadyDone)
        {
            Contract.Requires(methodInfo.IsStatic);
            Contract.Ensures(Contract.Result<SubrDelegate>() != null);

            var parameters = methodInfo.GetParameters();

            if (parameters.Length == 3 &&
                parameters[0].ParameterType == typeof(string) &&
                parameters[1].ParameterType == typeof(Context) &&
                parameters[2].ParameterType == typeof(ZilObject[]))
            {
                return (SubrDelegate)Delegate.CreateDelegate(
                    typeof(SubrDelegate), methodInfo);
            }

            var decoder = ArgDecoder.FromMethodInfo(methodInfo);

            SubrDelegate del = (name, ctx, args) =>
            {
                try
                {
                    return (ZilObject)methodInfo.Invoke(null, decoder.Decode(name, ctx, args));
                }
                catch (TargetInvocationException ex)
                {
                    ExceptionDispatchInfo.Capture(ex.InnerException).Throw();

                    // shouldn't get here
                    throw new NotImplementedException();
                }
            };

            // handle MdlZilRedirectAttribute
            var redirectAttr = methodInfo.GetCustomAttribute<Subrs.MdlZilRedirectAttribute>();

            if (redirectAttr != null)
            {
                var targetMethodInfo = redirectAttr.Type.GetMethod(redirectAttr.Target, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                if (targetMethodInfo == null)
                    throw new InvalidOperationException("Can't find redirect target " + redirectAttr.Target);

                alreadyDone = alreadyDone ?? new Dictionary<MethodInfo, SubrDelegate>();
                alreadyDone.Add(methodInfo, del);

                SubrDelegate targetDel;
                if (alreadyDone.TryGetValue(targetMethodInfo, out targetDel) == false)
                {
                    targetDel = WrapMethodAsSubrDelegate(targetMethodInfo, alreadyDone);

                    if (!alreadyDone.ContainsKey(targetMethodInfo))
                        alreadyDone.Add(targetMethodInfo, targetDel);
                }

                var prevDel = del;
                var topLevelOnly = redirectAttr.TopLevelOnly;

                del = (name, ctx, args) =>
                {
                    if ((ctx.CurrentFileFlags & FileFlags.MdlZil) != 0 &&
                        (!topLevelOnly || ctx.AtTopLevel))
                    {
                        return targetDel(name, ctx, args);
                    }

                    return prevDel(name, ctx, args);
                };
            }

            return del;
        }

        public object[] Decode(string name, Context ctx, ZilObject[] args)
        {
            var site = new FunctionCallSite(name);

            if (args.Length < lowerBound || args.Length > upperBound)
            {
                throw new ArgumentCountError(site, lowerBound, upperBound ?? 0, args.Length);
            }

            var result = new List<object>(1 + args.Length) { ctx };

            var argIndex = 0;

            var callbacks = new DecodingStepCallbacks
            {
                Context = ctx,
                Site = site,

                Ready = o => result.Add(o),
                Error = m => { throw new ArgumentTypeError(site, argIndex, m); },
            };

            for (var stepIndex = 0; stepIndex < steps.Length; stepIndex++)
            {
                var step = steps[stepIndex];
                var next = step(args, argIndex, callbacks);
                Contract.Assert(next >= argIndex);
                argIndex = next;
            }

            if (argIndex < args.Length)
            {
                // TODO: clarify error message (argument count might be fine but types are wrong)
                throw new ArgumentCountError(site, lowerBound, upperBound ?? 0, argIndex);
            }

            return result.ToArray();
        }

        private class DeclWrapper
        {
            private string declText;
            private ZilObject decl;

            public DeclWrapper(string declText)
            {
                Contract.Requires(!string.IsNullOrWhiteSpace(declText));

                this.declText = declText;
            }

            [ContractInvariantMethod]
            private void ObjectInvariant()
            {
                Contract.Invariant((declText != null && decl == null) || (declText == null && decl != null));
            }

            public override string ToString()
            {
                if (declText != null)
                {
                    return declText;
                }

                return decl.ToString();
            }

            public ZilObject GetPattern(Context ctx)
            {
                Contract.Requires(ctx != null);
                Contract.Ensures(decl != null);

                if (decl == null)
                {
                    // TODO: parse decl without evaluating anything
                    decl = Program.Evaluate(ctx, $"<QUOTE {declText}>", true);
                    declText = null;
                }

                return decl;
            }
        }
    }
}
