/* Copyright 2010-2025 Tara McGrew
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

using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace ZilfSourceGenerators
{
    public partial class ZBuiltinParserGenerator
    {
        private static void GenerateOverloadDispatchBody(IndentedStringBuilder sb, OverloadGroup group)
        {
            var hasPlatformRestrictions = group.Overloads.Any(o => o.Attribute.Platform != BuiltinPlatformSetting.Any);
            if (!hasPlatformRestrictions)
            {
                GenerateOverloadDispatchBodyCore(sb, group);
                return;
            }

            var cornerstoneCandidates = group.Overloads
                .Where(o => o.Attribute.Platform is BuiltinPlatformSetting.Any or BuiltinPlatformSetting.Cornerstone)
                .ToList();
            var glulxCandidates = group.Overloads
                .Where(o => o.Attribute.Platform is BuiltinPlatformSetting.Any or BuiltinPlatformSetting.Glulx)
                .ToList();
            var zMachineCandidates = group.Overloads
                .Where(o => o.Attribute.Platform is BuiltinPlatformSetting.Any or BuiltinPlatformSetting.ZMachine)
                .ToList();

            var supportsCornerstone = cornerstoneCandidates.Count > 0;
            var supportsGlulx = glulxCandidates.Count > 0;
            var supportsZMachine = zMachineCandidates.Count > 0;

            // Detect when platforms share the same implementation methods
            bool csSameAsZM = supportsCornerstone && supportsZMachine &&
                OverloadListsSameMethods(cornerstoneCandidates, zMachineCandidates);
            bool csSameAsGlulx = supportsCornerstone && supportsGlulx &&
                OverloadListsSameMethods(cornerstoneCandidates, glulxCandidates);
            bool glulxSameAsZM = supportsGlulx && supportsZMachine &&
                OverloadListsSameMethods(glulxCandidates, zMachineCandidates);

            // Build platform branches, merging identical candidate sets
            if (csSameAsZM && !csSameAsGlulx)
            {
                // Cornerstone and ZMachine share code, Glulx is separate
                EmitPlatformBranch(sb, group, "!c.cc.Context.IsGlulx", cornerstoneCandidates, false,
                    unsupportedPlatform: BuiltinPlatformSetting.Cornerstone);
                EmitPlatformBranch(sb, group, null, glulxCandidates, true,
                    unsupportedPlatform: BuiltinPlatformSetting.Glulx);
            }
            else if (csSameAsGlulx && !csSameAsZM)
            {
                // Cornerstone and Glulx share code, ZMachine is separate
                EmitPlatformBranch(sb, group, "c.cc.Context.IsCornerstone || c.cc.Context.IsGlulx",
                    cornerstoneCandidates, false,
                    unsupportedPlatform: BuiltinPlatformSetting.Cornerstone);
                EmitPlatformBranch(sb, group, null, zMachineCandidates, true,
                    unsupportedPlatform: BuiltinPlatformSetting.ZMachine);
            }
            else if (glulxSameAsZM && !csSameAsGlulx)
            {
                // Glulx and ZMachine share code, Cornerstone is separate
                EmitPlatformBranch(sb, group, "c.cc.Context.IsCornerstone", cornerstoneCandidates, false,
                    unsupportedPlatform: BuiltinPlatformSetting.Cornerstone);
                EmitPlatformBranch(sb, group, null, glulxCandidates, true,
                    unsupportedPlatform: BuiltinPlatformSetting.Glulx);
            }
            else
            {
                // All three platforms have distinct implementations (or all share the same,
                // which means hasPlatformRestrictions should have been false)
                EmitPlatformBranch(sb, group, "c.cc.Context.IsCornerstone", cornerstoneCandidates, false,
                    unsupportedPlatform: BuiltinPlatformSetting.Cornerstone);
                EmitPlatformBranch(sb, group, "c.cc.Context.IsGlulx", glulxCandidates, false,
                    unsupportedPlatform: BuiltinPlatformSetting.Glulx);
                EmitPlatformBranch(sb, group, null, zMachineCandidates, true,
                    unsupportedPlatform: BuiltinPlatformSetting.ZMachine);
            }
        }

        /// <summary>
        /// Compares two overload lists to determine if they contain the same methods
        /// (i.e., the same implementation serves multiple platforms).
        /// </summary>
        private static bool OverloadListsSameMethods(List<OverloadInfo> a, List<OverloadInfo> b)
        {
            if (a.Count != b.Count)
                return false;

            for (int i = 0; i < a.Count; i++)
            {
                if (!SymbolEqualityComparer.Default.Equals(
                        a[i].Method.MethodSymbol, b[i].Method.MethodSymbol))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Emits a single platform branch: either an if-block or an else-block.
        /// </summary>
        /// <param name="condition">The if condition, or null for the final else block.</param>
        /// <param name="candidates">The candidate overloads for this branch.</param>
        /// <param name="isElse">True if this is the final else block.</param>
        /// <param name="unsupportedPlatform">If candidates is empty, which platform error to emit.</param>
        private static void EmitPlatformBranch(
            IndentedStringBuilder sb, OverloadGroup group, string? condition,
            List<OverloadInfo> candidates, bool isElse,
            BuiltinPlatformSetting unsupportedPlatform = BuiltinPlatformSetting.ZMachine)
        {
            if (isElse)
            {
                sb.AppendLine("else");
            }
            else
            {
                sb.AppendLine($"if ({condition})");
            }

            sb.AppendLine("{");
            sb.Indent();

            if (candidates.Count > 0)
            {
                GenerateOverloadDispatchBodyCore(sb, new OverloadGroup
                {
                    BuiltinName = group.BuiltinName ?? string.Empty,
                    CallType = group.CallType,
                    Overloads = candidates
                });
            }
            else
            {
                EmitPlatformError(sb, group.CallType, group.BuiltinName ?? string.Empty, unsupportedPlatform);
            }

            sb.Unindent();
            sb.AppendLine("}");
        }

        private static void GenerateOverloadDispatchBodyCore(IndentedStringBuilder sb, OverloadGroup group)
        {
            // If there's only one overload, generate simple dispatch
            if (group.Overloads.Count == 1)
            {
                GenerateSingleOverloadBody(sb, group.Overloads[0], group.CallType, group.BuiltinName ?? "", emitVersionGuard: true);
                return;
            }

            // Multi-overload dispatch: analyze parameter types to determine dispatch strategy
            GenerateMultiOverloadDispatchBody(sb, group);
        }

        private static void GenerateMultiOverloadDispatchBody(IndentedStringBuilder sb, OverloadGroup group)
        {
            // Group overloads by argument count to handle different arities
            // For overloads with optional parameters, expand to all valid argument counts
            var argumentCountToOverloads = new Dictionary<int, List<OverloadInfo>>();

            foreach (var overload in group.Overloads)
            {
                var validCounts = GetValidArgumentCounts(overload);
                foreach (var count in validCounts)
                {
                    if (!argumentCountToOverloads.ContainsKey(count))
                    {
                        argumentCountToOverloads[count] = [];
                    }
                    argumentCountToOverloads[count].Add(overload);
                }
            }

            var overloadsByArgCount = argumentCountToOverloads
                .Select(kvp => new ArgumentCountGroup { Key = kvp.Key, Overloads = kvp.Value })
                .OrderBy(x => x.Key)
                .ToList();


            if (overloadsByArgCount.Count == 1)
            {
                // All overloads have the same argument count - check if we need variable type dispatch
                var expectedArgCount = overloadsByArgCount[0].Key;
                var overloadsForThisCount = overloadsByArgCount[0].Overloads;

                // Generate argument count validation
                sb.AppendLine($"if (args.Length != {expectedArgCount})");
                sb.AppendLine("{");
                sb.Indent();
                GenerateErrorReturn(sb, group.BuiltinName, expectedArgCount.ToString(), GetReturnTypeForCallType(group.CallType), group.CallType);
                sb.Unindent();
                sb.AppendLine("}");
                sb.AppendLine();

                // Check if we have multiple overloads with variable parameters that need runtime dispatch
                if (overloadsForThisCount.Count > 1 && NeedsVariableTypeDispatch(overloadsForThisCount))
                {
                    var singleArgCountGroup = new OverloadGroup
                    {
                        BuiltinName = group.BuiltinName ?? "",
                        CallType = group.CallType,
                        Overloads = overloadsForThisCount
                    };
                    GenerateVariableTypeDispatch(sb, singleArgCountGroup);
                }
                else
                {
                    // Simple dispatch - just call the first overload
                    GenerateSingleOverloadBody(sb, overloadsForThisCount[0], group.CallType, group.BuiltinName ?? "", emitVersionGuard: true);
                }
            }
            else
            {
                // Multiple overloads with different argument counts - generate switch dispatch
                GenerateArgumentCountDispatch(sb, group, overloadsByArgCount);
            }
        }

        private static List<int> GetValidArgumentCounts(OverloadInfo overload)
        {
            var argsParams = overload.Method.ArgumentParameters;
            int requiredCount = 0;
            int optionalCount = 0;
            bool hasParamsArray = false;
            foreach (var p in argsParams)
            {
                if (p.IsParams)
                {
                    hasParamsArray = true;
                    continue;
                }
                if (p.IsOptional)
                    optionalCount++;
                else
                    requiredCount++;
            }

            var validCounts = new List<int>();

            if (hasParamsArray)
            {
                // With params array, accept required count or more
                for (int i = requiredCount; i <= requiredCount + optionalCount + 10; i++) // Limit to reasonable range
                {
                    validCounts.Add(i);
                }
            }
            else
            {
                // Without params array, accept required count through required + optional
                for (int i = requiredCount; i <= requiredCount + optionalCount; i++)
                {
                    validCounts.Add(i);
                }
            }

            return validCounts;
        }

        private static void EmitPlatformError(IndentedStringBuilder sb, string callType, string operationName, BuiltinPlatformSetting currentPlatform)
        {
            var messageId = currentPlatform switch
            {
                BuiltinPlatformSetting.Glulx => "CompilerMessages._0_Is_Not_Supported_When_Targeting_Glulx",
                BuiltinPlatformSetting.Cornerstone => "CompilerMessages._0_Is_Not_Supported_When_Targeting_Cornerstone",
                _ => "CompilerMessages._0_Is_Not_Supported_When_Targeting_The_Zmachine",
            };
            var safeName = operationName.Replace("\\", "\\\\").Replace("\"", "\\\"");

            switch (callType)
            {
                case "ValueCall":
                    sb.AppendLine($"return c.HandleMessage({messageId}, \"{safeName}\");");
                    break;
                case "ValuePredCall":
                    sb.AppendLine($"c.cc.Context.HandleError(new CompilerError(c.form, {messageId}, \"{safeName}\"));");
                    sb.AppendLine("return c.cc.Game.Zero;");
                    break;
                default:
                    sb.AppendLine($"c.cc.Context.HandleError(new CompilerError(c.form, {messageId}, \"{safeName}\"));");
                    sb.AppendLine("return;");
                    break;
            }
        }

        private static void GenerateArgumentCountDispatch(IndentedStringBuilder sb, OverloadGroup group, List<ArgumentCountGroup> overloadsByArgCount)
        {
            // Generate switch statement based on argument count
            sb.AppendLine("switch (args.Length)");
            sb.AppendLine("{");
            sb.Indent();

            foreach (var argCountGroup in overloadsByArgCount.OrderBy(g => g.Key))
            {
                var argCount = argCountGroup.Key;
                var overloadsForCount = argCountGroup.Overloads;

                sb.AppendLine($"case {argCount}:");
                sb.AppendLine("{");  // Add braces to create proper scope
                sb.Indent();

                if (overloadsForCount.Count == 1)
                {
                    // Single overload for this arg count
                    // Don't emit version guard in case statements - capability checker handles versions
                    GenerateSingleOverloadBody(sb, overloadsForCount[0], group.CallType, group.BuiltinName ?? "", emitVersionGuard: false);
                }
                else if (NeedsVariableTypeDispatch(overloadsForCount))
                {
                    // Multiple overloads that differ by variable types
                    var singleArgCountGroup = new OverloadGroup
                    {
                        BuiltinName = group.BuiltinName ?? "",
                        CallType = group.CallType,
                        Overloads = overloadsForCount
                    };
                    GenerateVariableTypeDispatch(sb, singleArgCountGroup);
                }
                else
                {
                    // Multiple overloads, use first one (may need more sophisticated dispatch in future)
                    // Don't emit version guard in case statements - capability checker handles versions
                    GenerateSingleOverloadBody(sb, overloadsForCount[0], group.CallType, group.BuiltinName ?? "", emitVersionGuard: false);
                }

                // Check if the method returns void to determine if we need a break
                var returnType = GetReturnTypeForCallType(group.CallType);
                if (returnType == "void")
                {
                    sb.AppendLine("return;");
                }

                sb.Unindent();
                sb.AppendLine("}");
                sb.AppendLine();
            }

            // Generate default case for invalid argument counts
            sb.AppendLine("default:");
            sb.Indent();
            var validArgCounts = overloadsByArgCount.Select(g => g.Key.ToString()).ToArray();
            var validCountsString = validArgCounts.Length == 1
                ? validArgCounts[0]
                : string.Join(" or ", validArgCounts);
            GenerateErrorReturn(sb, group.BuiltinName ?? "", validCountsString, GetReturnTypeForCallType(group.CallType), group.CallType);
            sb.Unindent();

            sb.Unindent();
            sb.AppendLine("}");
        }
    }
}
