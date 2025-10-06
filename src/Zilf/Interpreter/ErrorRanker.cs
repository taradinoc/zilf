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

using System;
using System.Diagnostics.CodeAnalysis;

namespace Zilf.Interpreter
{
    /// <summary>
    /// Helper struct for tracking the most promising errors during parameter parsing
    /// without using exceptions for flow control.
    /// </summary>
    internal struct ErrorRanker
    {
        private enum ErrorType
        {
            None,
            WrongType,
            WrongCount,
            TooMany,
        }

        private int bestProgress = -1;
        private ErrorType bestError = ErrorType.None;
        private CallSite? bestSite = null;
        private int bestInt = 0;   // WrongCount: lowerBound, TooMany: firstUnexpectedIndex, WrongType: index
        private int? bestNullInt = null; // WrongCount: upperBound (null = unbounded), TooMany: suspiciousTypeIndex (null = no suspicious earlier argument)
        private bool bestBool = false; // WrongCount: morePrefix
        private string? bestStr = null; // WrongType: constraintDesc

        public ErrorRanker()
        {
        }

        /// <summary>
        /// Indicates that an argument's type was not one of the expected types.
        /// </summary>
        /// <param name="progress">The total number of arguments that have been parsed successfully so far.</param>
        /// <param name="site">The call site descriptor.</param>
        /// <param name="index">The index of the argument within the call site where the type error was located.</param>
        /// <param name="constraintDesc">A string describing the type(s) that were expected.</param>
        public void WrongType(int progress, CallSite site, int index, string constraintDesc)
        {
            // Prefer errors that consumed more progress. If progress is equal,
            // prefer WrongType over WrongCount/TooMany so type-mismatch messages
            // are shown when equally-progressing candidates compete.
            if (progress > bestProgress)
            {
                bestProgress = progress;
                bestError = ErrorType.WrongType;
                bestSite = site;
                bestInt = index;
                bestStr = constraintDesc;
            }
            else if (progress == bestProgress)
            {
                // When progress ties, only override an existing WrongType candidate.
                // This lets WrongCount/TooMany results from inner helpers take precedence,
                // while still allowing later WrongType candidates to refine earlier ones.
                if (bestError != ErrorType.WrongType)
                {
                    return;
                }

                bestError = ErrorType.WrongType;
                bestSite = site;
                bestInt = index;
                bestStr = constraintDesc;
            }
        }

        /// <summary>
        /// Indicates that the nunber of arguments passed was outside the required range.
        /// </summary>
        /// <param name="progress">The total number of arguments that have been parsed successfully so far.</param>
        /// <param name="site">The call site descriptor.</param>
        /// <param name="lowerBound">The minimum number of arguments (or additional arguments) required.</param>
        /// <param name="upperBound">The maximum number of arguments (or additional arguments) allowed, or <c>null</c> if there is no upper bound.</param>
        /// <param name="morePrefix"><c>true</c> if <see paramref="lowerBound"/> and <see paramref="upperBound"/> indicate the number of
        /// <b>additional</b> arguments expected./param>
        /// <remarks>
        /// <see paramref="morePrefix"/> can be used in situations such as when the number of arguments allowed depends on the values
        /// or types of some arguments.
        /// </remarks>
        public void WrongCount(int progress, CallSite site, int lowerBound, int? upperBound, bool morePrefix)
        {
            // Prefer higher progress. When progress ties, prefer WrongType over
            // WrongCount, but prefer WrongCount over TooMany.
            if (progress > bestProgress || (progress == bestProgress && bestError != ErrorType.WrongCount))
            {
                bestProgress = progress;
                bestError = ErrorType.WrongCount;
                bestSite = site;
                bestInt = lowerBound;
                bestNullInt = upperBound;
                bestBool = morePrefix;
            }
        }

        /// <summary>
        /// Indicates that at least one argument was still left over after parsing the maximum number of expected arguments.
        /// </summary>
        /// <param name="progress">The total number of arguments that have been parsed successfully so far.</param>
        /// <param name="site">The call site descriptor.</param>
        /// <param name="firstUnexpectedIndex">The index of the first unexpected argument.</param>
        /// <param name="suspiciousTypeIndex">The index of a previous argument where a mismatched type could have caused this error,
        /// or <c>null</c> if there is none.</param>
        public void TooMany(int progress, CallSite site, int firstUnexpectedIndex, int? suspiciousTypeIndex)
        {
            if (progress > bestProgress)
            {
                bestProgress = progress;
                bestError = ErrorType.TooMany;
                bestSite = site;
                bestInt = firstUnexpectedIndex;
                bestNullInt = suspiciousTypeIndex;
            }
        }

        /*private bool IsBetterError(ErrorType newErrorType)
        {
            // When progress is equal, prefer more specific errors:
            // Within WrongType: prefer string > ATOM for better error messages
            // Across types: WrongType (more specific) > WrongCount > TooMany
            // But within same type, prefer better constraints
            if (bestError == ErrorType.None) return true;
            if (bestError == newErrorType)
            {
                    // For same error type, prefer better constraint descriptions
                    if (newErrorType == ErrorType.WrongType)
                    {
                        // Prefer string constraints over ATOM constraints
                        return bestStr == "ATOM"; // Replace ATOM with more specific constraint
                    }
                    return false; // Keep first of same type for other cases
            }
            if (newErrorType == ErrorType.WrongType && bestError != ErrorType.WrongType) return true;
            if (newErrorType == ErrorType.WrongCount && bestError == ErrorType.TooMany) return true;
            return false;
        }*/

        /// <summary>
        /// Throws an exception based on the highest ranked error.
        /// </summary>
        /// <exception cref="ArgumentCountError"><see cref="WrongCount(int, CallSite, int, int?, bool)"/> was called and is the highest ranked error.</exception>
        /// <exception cref="ArgumentTypeError"><see cref="WrongType(int, CallSite, int, string)"/> was called and is the highest ranked error.</exception>
        /// <exception cref="InvalidOperationException">No error has been ranked.</exception>
        /// <remarks>
        /// This method never returns.
        /// </remarks>
        [DoesNotReturn]
        public readonly void Throw()
        {
            switch (bestError)
            {
                case ErrorType.None:
                    throw new InvalidOperationException("Throw called with no pending error");
                case ErrorType.WrongType:
                    var site = bestSite!;
                    if (site.ChildName == "element")
                    {
                        var suffix = $": {site.ChildName} {bestInt + 1}";
                        var name = site.ToString();
                        var temp = name;
                        var repeatedCount = 0;
                        while (temp.EndsWith(suffix, StringComparison.Ordinal))
                        {
                            temp = temp.Substring(0, temp.Length - suffix.Length);
                            repeatedCount++;
                        }

                        if (repeatedCount >= 2)
                        {
                            var desiredLength = name.Length - suffix.Length * (repeatedCount - 1);
                            if (desiredLength < 0)
                            {
                                desiredLength = 0;
                            }
                            var desiredName = name.Substring(0, desiredLength);
                            site = new SyntheticCallSite(desiredName, site.ChildName);
                        }
                    }

                    throw new ArgumentTypeError(site, bestInt, bestStr!);
                case ErrorType.WrongCount:
                    throw ArgumentCountError.WrongCount(bestSite!, bestInt, bestNullInt, bestBool);
                case ErrorType.TooMany:
                    throw ArgumentCountError.TooMany(bestSite!, bestInt, bestNullInt);
                default:
                    throw new InvalidOperationException("Unknown error type");
            }
        }

        /// <summary>
        /// Throws an exception based on the highest ranked error, if there is one.
        /// </summary>
        /// <exception cref="ArgumentCountError"><see cref="WrongCount(int, CallSite, int, int?, bool)"/> was called and is the highest ranked error.</exception>
        /// <exception cref="ArgumentTypeError"><see cref="WrongType(int, CallSite, int, string)"/> was called and is the highest ranked error.</exception>
        /// <remarks>
        /// Convenience: throw the ranked error only if one exists. Some generated parsing sites call this when they want to prefer
        /// any recorded ranked error but still throw a more specific exception when none was recorded.
        /// </remarks>
        public readonly void ThrowIfError()
        {
            if (bestError != ErrorType.None)
                Throw();
        }

        /// <summary>
        /// Gets a value indicating whether any error has been recorded.
        /// </summary>
        public readonly bool HasError => bestError != ErrorType.None;

        /// <summary>
        /// Gets a value indicating whether the current ranked error is a type mismatch.
        /// </summary>
        public readonly bool IsWrongType => bestError == ErrorType.WrongType;

        /// <summary>
        /// Gets a value indicating whether the current ranked error is a wrong-count error.
        /// </summary>
        public readonly bool IsWrongCount => bestError == ErrorType.WrongCount;

        /// <summary>
        /// Gets a value indicating whether the current ranked error is a too-many-arguments error.
        /// </summary>
        public readonly bool IsTooMany => bestError == ErrorType.TooMany;

        /// <summary>
        /// Gets the currently ranked type constraint description if the best error is a type mismatch.
        /// </summary>
        public readonly string? CurrentConstraint => bestError == ErrorType.WrongType ? bestStr : null;

        /// <summary>
        /// Gets the call site associated with the current ranked error, if any.
        /// </summary>
        public readonly CallSite? CurrentSite => bestSite;

        /// <summary>
        /// Gets the argument index associated with the current ranked error.
        /// </summary>
        public readonly int CurrentIndex => bestInt;

        /// <summary>
        /// Gets the amount of progress recorded for the current ranked error.
        /// </summary>
        public readonly int CurrentProgress => bestProgress;

        internal readonly void UpdateBestCandidate(ref bool hasBest, ref ErrorRanker best)
        {
            if (!HasError)
            {
                return;
            }

            if (!hasBest || CurrentProgress > best.CurrentProgress)
            {
                best = this;
                hasBest = true;
                return;
            }

            if (CurrentProgress == best.CurrentProgress)
            {
                var candidatePriority = GetPriority(this);
                var currentPriority = GetPriority(best);
                if (candidatePriority >= currentPriority)
                {
                    best = this;
                    hasBest = true;
                }
            }
        }

        private static int GetPriority(in ErrorRanker ranker) => ranker.bestError switch
        {
            ErrorType.WrongCount => 3,
            ErrorType.WrongType => 2,
            ErrorType.TooMany => 1,
            _ => 0,
        };

        private sealed class SyntheticCallSite : CallSite
        {
            private readonly string childName;

            public SyntheticCallSite(string name, string childName)
                : base(name)
            {
                this.childName = childName;
            }

            public override string ChildName => childName;
        }

        //public readonly void ApplyTo(ref ErrorRanker other)
        //{
        //    switch (bestError)
        //    {
        //        case ErrorType.WrongType:
        //            other.WrongType(bestProgress, bestSite!, bestInt, bestStr!);
        //            break;
        //        case ErrorType.WrongCount:
        //            other.WrongCount(bestProgress, bestSite!, bestInt, bestNullInt, bestBool);
        //            break;
        //        case ErrorType.TooMany:
        //            other.TooMany(bestProgress, bestSite!, bestInt, bestNullInt);
        //            break;
        //    }
        //}
    }
}
