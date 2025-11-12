/* Copyright 2010-2023 Tara McGrew
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

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Zilf.Diagnostics;
using Zilf.Language;

namespace Zilf.Interpreter.Values
{
    [BuiltinType(StdAtom.LIST, PrimType.LIST)]
    sealed class ZilList : ZilListBase
    {
        public ZilList(IEnumerable<ZilObject> sequence)
            : base(sequence)
        {
        }

        public ZilList([NotNullIfNotNull(nameof(rest))] ZilObject? first, [NotNullIfNotNull(nameof(first))] ZilListoidBase? rest)
            : base(first, rest) { }

        [ChtypeMethod]
        public static ZilList FromList(ZilListBase list) => new(list.First, list.Rest);

        public override StdAtom StdTypeAtom => StdAtom.LIST;

        protected override string OpenBracket => "(";

        protected override ZilResult EvalImpl(Context ctx, LocalEnvironment? environment, ZilAtom? originalType)
        {
            static ZilResult? AppendResults(IEnumerable<ZilResult> sequence, List<ZilObject> destination)
            {
                foreach (var item in sequence)
                {
                    if (item.ShouldPass())
                        return item;

                    destination.Add((ZilObject)item);
                }

                return null;
            }

            ZilListoidBase? TryGetListTail(ZilObject value)
            {
                if (value is ZilListoidBase listTail)
                    return listTail;

                if (value.PrimType != PrimType.LIST)
                    return null;

                if (value.GetPrimitive(ctx) is ZilListoidBase primitiveTail)
                    return primitiveTail;

                return null;
            }

            if (IsEmpty)
            {
                var emptyList = new ZilList(null, null) { SourceLine = SourceLine };
                return originalType != null ? ctx.ChangeType(emptyList, originalType) : emptyList;
            }

            var rawElements = new List<ZilObject>();
            for (ZilListoidBase? cell = this; cell.IsCons(out var element, out var rest); cell = rest)
            {
                rawElements.Add(element);
            }

            var values = new List<ZilObject>(rawElements.Count);
            ZilListoidBase? linkedTail = null;

            var lastIndex = rawElements.Count - 1;

            for (int i = 0; i < lastIndex; i++)
            {
                var passResult = AppendResults(ZilObject.ExpandOrEvalWithSplice(ctx, rawElements[i], environment), values);
                if (passResult.HasValue)
                    return passResult.Value;
            }

            if (rawElements.Count > 0)
            {
                var lastElement = rawElements[lastIndex];

                if (lastElement is ZilSegment segment)
                {
                    var segmentResult = segment.Form.Eval(ctx, environment);
                    if (segmentResult.ShouldPass())
                        return segmentResult;

                    var segmentValue = (ZilObject)segmentResult;

                    linkedTail = TryGetListTail(segmentValue);

                    if (linkedTail == null)
                    {
                        if (segmentValue is IEnumerable<ZilObject> segmentSequence)
                        {
                            foreach (var item in segmentSequence)
                            {
                                values.Add(item);
                            }
                        }
                        else
                        {
                            throw new InterpreterError(
                                InterpreterMessages._0_1_Must_Return_2,
                                InterpreterMessages.NoFunction,
                                "segment evaluation",
                                "a structure");
                        }
                    }
                }
                else
                {
                    var passResult = AppendResults(ZilObject.ExpandOrEvalWithSplice(ctx, lastElement, environment), values);
                    if (passResult.HasValue)
                        return passResult.Value;
                }
            }

            ZilObject resultObject;

            if (linkedTail != null)
            {
                ZilListoidBase resultList = linkedTail;

                if (values.Count > 0)
                {
                    for (int i = values.Count - 1; i >= 0; i--)
                    {
                        resultList = new ZilList(values[i], resultList);
                    }

                    if (values.Count > 0 && resultList is ZilList newHead)
                        newHead.SourceLine = SourceLine;
                }

                resultObject = resultList;
            }
            else
            {
                var newList = new ZilList(values) { SourceLine = SourceLine };
                resultObject = newList;
            }

            return originalType != null ? ctx.ChangeType(resultObject, originalType) : resultObject;
        }
    }
}