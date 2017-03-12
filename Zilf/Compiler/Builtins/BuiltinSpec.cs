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
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Zilf.Emit;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.ZModel;

namespace Zilf.Compiler.Builtins
{
    class BuiltinSpec
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
                    throw new ArgumentException("Value call must return IOperand");

                if (CallType == typeof(VoidCall) && method.ReturnType != typeof(void))
                    throw new ArgumentException("Void call must return void");

                if (CallType == typeof(PredCall) && method.ReturnType != typeof(void))
                    throw new ArgumentException("Predicate call must return void");

                if (CallType == typeof(ValuePredCall) && method.ReturnType != typeof(void))
                    throw new ArgumentException("Value+predicate call must return void");
            }
            catch (ArgumentException ex)
            {
                throw new ArgumentException(string.Format(
                    CultureInfo.CurrentCulture,
                    "Bad attribute {0} on method {1}",
                    attr.Names.First(), method.Name),
                    ex);
            }
        }

        public bool AppliesTo(int zversion, int argCount, Type callType = null)
        {
            if (!ZEnvironment.VersionMatches(zversion, Attr.MinVersion, Attr.MaxVersion))
                return false;

            if (argCount < MinArgs || (MaxArgs != null && argCount > MaxArgs))
                return false;

            if (callType != null && this.CallType != callType)
                return false;

            return true;
        }
    }
}