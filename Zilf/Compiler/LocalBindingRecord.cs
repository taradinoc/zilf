/* Copyright 2010-2019 Jesse McGrew
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
using JetBrains.Annotations;
using Zilf.Emit;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;

namespace Zilf.Compiler
{
    enum LocalBindingType
    {
        CompilerTemporary,
        LoopState,
        ProgAuxiliary,
        RoutineAuxiliary,
        RoutineOptional,
        RoutineRequired,
    }

    static class LocalBindingTypeExtensions
    {
        public static LocalBindingType ToLocalBindingType(this ArgItem.ArgType argType)
        {
            switch (argType)
            {
                case ArgItem.ArgType.Required:
                    return LocalBindingType.RoutineRequired;
                case ArgItem.ArgType.Optional:
                    return LocalBindingType.RoutineOptional;
                case ArgItem.ArgType.Auxiliary:
                    return LocalBindingType.RoutineAuxiliary;
                default:
                    throw new ArgumentOutOfRangeException(nameof(argType), argType, null);
            }
        }
    }
    
    /// <summary>
    /// Tracks the usage of a local variable storage slot during the part of its lifespan
    /// corresponding to a particular ZIL binding.
    /// </summary>
    sealed class LocalBindingRecord
    {
        public LocalBindingType Type { get; }
        [NotNull]
        public ILocalBuilder LocalBuilder { get; }
        [NotNull]
        public string BoundName { get; }
        [NotNull]
        public ISourceLine Definition { get; }

        /// <summary>
        /// Gets or sets a flag indicating whether the local variable was used as an operand.
        /// </summary>
        public bool IsEverRead { get; set; }
        /// <summary>
        /// Gets or sets a flag indicating whether a value was assigned to the local variable.
        /// </summary>
        public bool IsEverWritten { get; set; }

        public LocalBindingRecord(LocalBindingType type, [NotNull] ISourceLine definition, [NotNull] string boundName,
            [NotNull] ILocalBuilder storage)
        {
            Type = type;
            Definition = definition;
            LocalBuilder = storage;
            BoundName = boundName;
        }
    }
}
