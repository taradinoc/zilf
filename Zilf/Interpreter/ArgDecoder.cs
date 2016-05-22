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

    sealed class ArgumentCountError : ArgumentDecodingError
    {
        public ArgumentCountError(string function, int minExpected, int maxExpected, int actual)
            : base(ZilError.ArgCountMsg(function, minExpected, maxExpected))
        {
        }
    }

    sealed class ArgumentTypeError : ArgumentDecodingError
    {
        public ArgumentTypeError(string function, int index, string constraintDesc)
            : base($"{function}: arg {index + 1}: expected {constraintDesc}")
        {
        }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    class DeclAttribute : Attribute
    {
        public DeclAttribute(string pattern)
        {
            this.Pattern = pattern;
        }

        public string Pattern { get; }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    class RequiredAttribute : Attribute
    {
        public RequiredAttribute()
            : this(1)
        {
        }

        public RequiredAttribute(int count)
        {
            this.Count = count;
        }

        public int Count { get; }
    }

    class ArgDecoder
    {
        // calls ready for each decoded value, returns number of arguments consumed
        private delegate int DecodingStep(ZilObject[] arguments, int index, DecodingStepCallbacks cb);

        private struct DecodingStepCallbacks
        {
            public Context Context;
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
                //XXX probably good enough for builtin types, but should get the real name from the attribute
                parts.Add("TYPE " + typeAtom.ToString());
                preds.Add((zo, ctx) => zo.GetTypeAtom(ctx).StdAtom == typeAtom);
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
        private readonly int lastRequiredStepIndex;

        private static readonly ZilObject[] EmptyZilObjectArray = { };

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
            DecodingStepInfo result;
            ConstraintsBuilder cb = new ConstraintsBuilder();
            string errmsg = null;
            object defaultValue = null;

            if (pi.ParameterType == typeof(ZilObject))
            {
                // decode to ZilObject as-is
                result = new DecodingStepInfo
                {
                    Step = (a, i, c) => { c.Ready(a[i]); return i + 1; },
                    LowerBound = 1,
                    UpperBound = 1,
                };
            }
            else if (pi.ParameterType == typeof(int) || pi.ParameterType == typeof(int?))
            {
                cb.AddTypeConstraint(StdAtom.FIX);

                if (pi.ParameterType == typeof(int))
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
            else if (pi.ParameterType == typeof(string))
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
            else if (pi.ParameterType == typeof(char) || pi.ParameterType == typeof(char?))
            {
                cb.AddTypeConstraint(StdAtom.CHARACTER);

                if (pi.ParameterType == typeof(char))
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
            else if (pi.ParameterType == typeof(ZilObject[]))
            {
                // decode as a ZilObject[] containing all remaining args
                defaultValue = EmptyZilObjectArray;

                return new DecodingStepInfo
                {
                    Step = (a, i, c) =>
                    {
                        var array = new ZilObject[a.Length - i];
                        Array.Copy(a, i, array, 0, array.Length);
                        c.Ready(array);
                        return a.Length;
                    },
                    LowerBound = 0,
                    UpperBound = null,
                };
            }
            else if (typeof(ZilObject).IsAssignableFrom(pi.ParameterType))
            {
                var zoType = pi.ParameterType;

                cb.AddTypeConstraint(zoType.GetCustomAttribute<BuiltinTypeAttribute>().Name);

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
            else
            {
                throw new NotImplementedException();
            }

            // modifiers
            var declAttr = pi.GetCustomAttribute<DeclAttribute>();
            if (declAttr != null)
            {
                var prevStep = result.Step;
                var declWrapper = new DeclWrapper(declAttr.Pattern);

                cb.AddDeclConstraint(declWrapper);

                result.Step = (a, i, c) =>
                {
                    if (!Decl.Check(c.Context, a[i], declWrapper.GetPattern(c.Context)))
                    {
                        c.Error(errmsg);
                    }

                    return prevStep(a, i, c);
                };
            }

            if (pi.IsOptional)
            {
                result.LowerBound = 0;

                var del = cb.GetDelegate();
                var prevStep = result.Step;

                if (pi.HasDefaultValue)
                {
                    defaultValue = pi.DefaultValue;
                }

                result.Step = (a, i, c) =>
                {
                    if (i < a.Length && del(a[i], c.Context))
                    {
                        return prevStep(a, i, c);
                    }
                    else
                    {
                        c.Ready(defaultValue);
                        return i;
                    }
                };
            }

            errmsg = cb.ToString();
            return result;
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

            return del;
        }

        public object[] Decode(string name, Context ctx, ZilObject[] args)
        {
            if (args.Length < lowerBound || args.Length > upperBound)
            {
                throw new ArgumentCountError(name, lowerBound, upperBound ?? 0, args.Length);
            }

            var result = new List<object>(1 + args.Length) { ctx };

            var argIndex = 0;

            var callbacks = new DecodingStepCallbacks
            {
                Context = ctx,

                Ready = o => result.Add(o),

                Error = m =>
                {
                    throw new ArgumentTypeError(name, argIndex, m);
                },
            };

            for (var stepIndex = 0; stepIndex < steps.Length; stepIndex++)
            {
                var step = steps[stepIndex];
                var next = step(args, argIndex, callbacks);
                Contract.Assert(next >= argIndex);
                argIndex = next;
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
