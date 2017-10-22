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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using Zilf.Diagnostics;
using Zilf.Interpreter.Values;
using Zilf.Language;

namespace Zilf.Interpreter
{
    struct ZilResult
    {
        private readonly Outcome outcome;
        private readonly ZilObject value;
        private readonly ZilActivation activation;

        public enum Outcome : byte
        {
            Value = 0,
            Return = 1,
            Again = 2,
            MapRet = 3,
            MapLeave = 4,
            MapStop = 5,
        }

        private ZilResult(Outcome outcome, ZilObject value, ZilActivation activation)
        {
            this.outcome = outcome;
            this.value = value;
            this.activation = activation;
        }

        public override string ToString()
        {
            var sb = new StringBuilder(outcome.ToString());

            if (activation != null)
            {
                sb.Append(' ');
                sb.Append("(@ ");
                sb.Append(activation);
                sb.Append(')');
            }

            if (value != null)
            {
                sb.Append(' ');
                sb.Append(value);
            }

            return sb.ToString();
        }

        public static implicit operator ZilResult(ZilObject value)
        {
            return new ZilResult(Outcome.Value, value, null);
        }

        public static ZilResult MapRet([NotNull] ZilObject[] values)
        {
            return new ZilResult(Outcome.MapRet, new ZilVector(values), null);
        }

        public static ZilResult MapStop([NotNull] ZilObject[] values)
        {
            return new ZilResult(Outcome.MapStop, new ZilVector(values), null);
        }

        public static ZilResult MapLeave(ZilObject value)
        {
            return new ZilResult(Outcome.MapLeave, value, null);
        }

        public static ZilResult Return(ZilActivation activation, ZilObject value)
        {
            return new ZilResult(Outcome.Return, value, activation);
        }

        public static ZilResult Again(ZilActivation activation)
        {
            return new ZilResult(Outcome.Again, null, activation);
        }

        /// <summary>
        /// Extracts the value, if this result is a simple value, or throws an exception.
        /// </summary>
        /// <param name="result"></param>
        /// <exception cref="InterpreterError">The result is not a simple value.</exception>
        public static explicit operator ZilObject(ZilResult result)
        {
            if (result.outcome == Outcome.Value)
                return result.value;

            throw new InterpreterError(InterpreterMessages.Misplaced_0, result.outcome.ToString().ToUpperInvariant());
        }

        public bool ShouldPass()
        {
            ZilResult dummy = default(ZilResult);
            return ShouldPass(null, ref dummy);
        }

        public bool ShouldPass([CanBeNull] ZilActivation currentActivation, ref ZilResult resultToPass)
        {
            switch (outcome)
            {
                case Outcome.Value:
                    return false;

                case Outcome.Return when currentActivation != null && activation == currentActivation:
                    resultToPass = value;
                    return true;

                default:
                    resultToPass = this;
                    return true;
            }
        }

        [SuppressMessage("ReSharper", "ParameterHidesMember")]
        public bool IsMapControl(out Outcome outcome, out ZilObject value)
        {
            outcome = this.outcome;
            value = this.value;

            switch (this.outcome)
            {
                case Outcome.MapLeave:
                case Outcome.MapRet:
                case Outcome.MapStop:
                    return true;

                default:
                    return false;
            }
        }

        [SuppressMessage("ReSharper", "ParameterHidesMember")]
        public bool IsReturn(ZilActivation currentActivation, out ZilObject value)
        {
            value = this.value;
            return outcome == Outcome.Return && activation == currentActivation;
        }

        public bool IsAgain(ZilActivation currentActivation) =>
            outcome == Outcome.Again && activation == currentActivation;

        public bool IsNull => outcome == Outcome.Value && value == null;
    }

    static class ZilResultSequenceExtensions
    {
        public static IEnumerable<ZilResult> Trim([NotNull] this IEnumerable<ZilResult> inputs)
        {
            foreach (var i in inputs)
            {
                yield return i;

                if (i.ShouldPass())
                    yield break;
            }
        }

        [NotNull]
        public static IEnumerable<ZilResult> AsResultSequence([NotNull] this IEnumerable<ZilObject> inputs)
        {
            return inputs.Select<ZilObject, ZilResult>(i => i);
        }

        public static ZilResult ToZilVectorResult([NotNull] this IEnumerable<ZilResult> inputs, ISourceLine sourceLine)
        {
            var array = inputs.Trim().ToArray();
            if (array.Length > 0 && array[array.Length - 1].ShouldPass())
                return array[array.Length - 1];

            return new ZilVector(Array.ConvertAll(array, i => (ZilObject)i)) { SourceLine = sourceLine };
        }

        public static ZilResult ToZilListResult([NotNull] this IEnumerable<ZilResult> inputs, ISourceLine sourceLine)
        {
            var array = inputs.Trim().ToArray();
            if (array.Length > 0 && array[array.Length - 1].ShouldPass())
                return array[array.Length - 1];

            return new ZilList(array.Select(i => (ZilObject)i)) { SourceLine = sourceLine };
        }

        [ContractAnnotation("=> false, array: null; => true, array: notnull")]
        public static bool TryToZilObjectArray([NotNull] this IEnumerable<ZilResult> inputs, [CanBeNull] out ZilObject[] array, out ZilResult result)
        {
            List<ZilObject> list;
            if (inputs is ICollection<ZilResult> coll)
            {
                list = new List<ZilObject>(coll.Count);
            }
            else
            {
                list = new List<ZilObject>();
            }

            foreach (var zr in inputs)
            {
                if (zr.ShouldPass())
                {
                    array = null;
                    result = zr;
                    return false;
                }

                list.Add((ZilObject)zr);
            }

            array = list.ToArray();
            result = default(ZilResult);
            return true;
        }

        public static bool SequenceStructurallyEqual([NotNull] this IEnumerable<ZilObject> first, [NotNull] IEnumerable<ZilObject> second)
        {
#pragma warning disable ZILF0005 // Comparing ZilObjects with Equals
            return first.SequenceEqual(second, StructuralEqualityComparer.Instance);
#pragma warning restore ZILF0005 // Comparing ZilObjects with Equals
        }
    }
}
