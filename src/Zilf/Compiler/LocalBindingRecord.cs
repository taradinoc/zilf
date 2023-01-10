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

using System;
using Zilf.Emit;
using Zilf.Interpreter;
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
            return argType switch
            {
                ArgItem.ArgType.Required => LocalBindingType.RoutineRequired,
                ArgItem.ArgType.Optional => LocalBindingType.RoutineOptional,
                ArgItem.ArgType.Auxiliary => LocalBindingType.RoutineAuxiliary,
                _ => throw new ArgumentOutOfRangeException(nameof(argType), argType, null)
            };
        }

        public static bool ShouldWarnIfUnused(this LocalBindingType type)
        {
            return type switch
            {
                LocalBindingType.ProgAuxiliary => true,
                LocalBindingType.RoutineAuxiliary => true,
                LocalBindingType.RoutineOptional => true,
                _ => false
            };
        }
    }
    
    /// <summary>
    /// Tracks the usage of a local variable storage slot during the part of its lifespan
    /// corresponding to a particular ZIL binding.
    /// </summary>
    sealed class LocalBindingRecord
    {
        public LocalBindingType Type { get; }
        public ILocalBuilder LocalBuilder { get; }
        public string BoundName { get; }
        public ISourceLine? Definition { get; }

        /// <summary>
        /// Gets or sets a flag indicating whether the local variable was used as an operand.
        /// </summary>
        public bool IsEverRead { get; set; }

        /// <summary>
        /// Gets or sets a flag indicating whether a value was assigned to the local variable.
        /// </summary>
        public bool IsEverWritten { get; set; }

        public LocalBindingRecord(LocalBindingType type, ISourceLine? definition, string boundName,
            ILocalBuilder storage)
        {
            Type = type;
            Definition = definition;
            LocalBuilder = storage;
            BoundName = boundName;
        }
    }
}
