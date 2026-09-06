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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Zilf.Emit
{
    public interface IGameBuilder : IDisposable
    {
        /// <summary>
        /// Gets a target-specific options object.
        /// </summary>
        IGameOptions Options { get; }

        /// <summary>
        /// Gets the debug file builder, if one exists.
        /// </summary>
        IDebugFileBuilder? DebugFile { get; }

        /// <summary>
        /// Defines a new global variable.
        /// </summary>
        /// <param name="name">The name of the variable.</param>
        /// <returns>A helper object which may be used to set the variable's default value
        /// or refer to the variable as an operand.</returns>
        IGlobalBuilder DefineGlobal(string name);
        /// <summary>
        /// Defines a new table.
        /// </summary>
        /// <param name="name">The name of the table, or null to generate a new name.</param>
        /// <param name="pure">true if the table should be stored in read-only memory.</param>
        /// <returns>A helper object which may be used to add values to the table or refer
        /// to the table as an operand.</returns>
        ITableBuilder DefineTable(string? name, bool pure);
        /// <summary>
        /// Defines a Unicode translation table and returns an operand for it.
        /// </summary>
        /// <param name="characters">The Unicode characters to include, in order.</param>
        /// <returns>An operand representing the table's address.</returns>
        IOperand DefineUnicodeTranslationTable(IEnumerable<char> characters);
        /// <summary>
        /// Defines a new routine.
        /// </summary>
        /// <param name="name">The name of the routine.</param>
        /// <param name="entryPoint">true if the routine is the entry point of the
        /// game. Only one entry point may be defined.</param>
        /// <param name="cleanStack">true if the code generated for this routine should
        /// sacrifice space to ensure the stack is kept clean.</param>
        /// <returns>A helper object which may be used to add code to the routine or
        /// refer to the routine as an operand.</returns>
        IRoutineBuilder DefineRoutine(string name, bool entryPoint, bool cleanStack);
        /// <summary>
        /// Defines a new object.
        /// </summary>
        /// <param name="name">The name of the object.</param>
        /// <returns>A helper object which may be used to add properties to the object
        /// or refer to the object as an operand.</returns>
        IObjectBuilder DefineObject(string name);
        /// <summary>
        /// Defines a new object property.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <returns>A helper object which may be used to set the property's default
        /// value or refer to the property as an operand.</returns>
        IPropertyBuilder DefineProperty(string name);
        /// <summary>
        /// Defines a new object flag.
        /// </summary>
        /// <param name="name">The name of the flag.</param>
        /// <returns>A helper object which may be used to refer to the flag as an
        /// operand.</returns>
        IFlagBuilder DefineFlag(string name);

        /// <summary>
        /// Gets the maximum allowable length of a property, in bytes.
        /// </summary>
        int MaxPropertyLength { get; }

        /// <summary>
        /// Gets the maximum allowable number of properties.
        /// </summary>
        int MaxProperties { get; }

        /// <summary>
        /// Gets the maximum allowable number of flags.
        /// </summary>
        int MaxFlags { get; }

        /// <summary>
        /// Gets the maximum allowable number of arguments to a routine call.
        /// </summary>
        int MaxCallArguments { get; }

        /// <summary>
        /// Gets a predefined operand representing the constant 0.
        /// </summary>
        INumericOperand Zero { get; }
        /// <summary>
        /// Gets a predefined operand representing the constant 1.
        /// </summary>
        INumericOperand One { get; }
        /// <summary>
        /// Gets an operand representing a numeric constant.
        /// </summary>
        /// <param name="value">The numeric constant.</param>
        /// <returns>The operand.</returns>
        INumericOperand MakeOperand(int value);
        /// <summary>
        /// Gets an operand representing a character constant.
        /// </summary>
        /// <param name="value">The character constant.</param>
        /// <returns>The operand.</returns>
        /// <remarks>
        /// This may or may not be equal to the Unicode value of the character.
        /// </remarks>
        IOperand MakeOperand(char value);
        /// <summary>
        /// Gets an operand representing a string constant.
        /// </summary>
        /// <param name="value">The string constant.</param>
        /// <returns>The operand.</returns>
        IOperand MakeOperand(string value);
        /// <summary>
        /// Defines a new constant to represent an existing operand, or redefines
        /// an existing constant.
        /// </summary>
        /// <param name="name">The name of the constant.</param>
        /// <param name="value">The operand which is the constant's value.</param>
        /// <returns>An operand representing the constant, which is equivalent
        /// to the operand specified as the value.</returns>
        /// <remarks>
        /// <para>This may be used to make the output code more readable, or to
        /// define values which are important to the backend (such as the release
        /// number).</para>
        /// <para>The effect of redefining an existing constant when the constant
        /// has already been emitted in routine code is undefined.</para>
        /// </remarks>
        IOperand DefineConstant(string name, IOperand value);

        /// <summary>
        /// Defines a new vocabulary word.
        /// </summary>
        /// <param name="word">The vocabulary word.</param>
        /// <returns>A helper object which may be used to add extra data to the
        /// word or refer to the word as an operand.</returns>
        /// <remarks>
        /// If extra data is used, the extra data of all vocabulary words must
        /// be the same size.
        /// </remarks>
        IWordBuilder DefineVocabularyWord(string word);
        /// <summary>
        /// Deletes a previously defined vocabulary word.
        /// </summary>
        /// <param name="word">The vocabulary word.</param>
        void RemoveVocabularyWord(string word);
        /// <summary>
        /// Gets the collection of self-inserting word break characters.
        /// </summary>
        /// <remarks>
        /// These characters are split off during lexical analysis by the LEX
        /// and READ instructions, such that if '.' is in the set, "Mrs. Smith"
        /// will be lexed as three words: {"mrs", ".", "smith"}.
        /// </remarks>
        ICollection<char> SelfInsertingBreaks { get; }
        /// <summary>
        /// Gets a predefined operand representing the vocabulary table.
        /// </summary>
        IConstantOperand VocabularyTable { get; }

        /// <summary>
        /// Checks whether a name is in use as a global symbol.
        /// </summary>
        /// <param name="name">The name to check.</param>
        /// <param name="type">The type of the existing symbol definition, or <see langword="null"/>
        /// if the name is not in use.</param>
        /// <returns><see langword="true"/> if a global symbol is defined with that name, or
        /// <see langword="false"/> otherwise.</returns>
        bool IsGloballyDefined(string name, [NotNullWhen(true)] out string? type);

        /// <summary>
        /// Records a compiler-level optimization statistic for inclusion in debug assembly output.
        /// </summary>
        /// <param name="name">The statistic name.</param>
        /// <param name="count">The amount to add.</param>
        void RecordCompilerOptimizationStatistic(string name, int count);

        /// <summary>
        /// Writes the final output and closes the game builder.
        /// </summary>
        void Finish();
    }
}
