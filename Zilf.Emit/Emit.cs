/* Copyright 2010, 2015 Jesse McGrew
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
using System.Diagnostics.Contracts;

namespace Zilf.Emit
{
    public struct DebugLineRef
    {
        public string File;
        public int Line;
        public int Column;

        public DebugLineRef(string file, int line, int column)
        {
            this.File = file;
            this.Line = line;
            this.Column = column;
        }
    }

    [ContractClass(typeof(IGameBuilderContracts))]
    public interface IGameBuilder
    {
        /// <summary>
        /// Gets a target-specific options object.
        /// </summary>
        IGameOptions Options { get; }

        /// <summary>
        /// Gets the debug file builder, if one exists.
        /// </summary>
        IDebugFileBuilder DebugFile { get; }

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
        ITableBuilder DefineTable(string name, bool pure);
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
        IOperand Zero { get; }
        /// <summary>
        /// Gets a predefined operand representing the constant 1.
        /// </summary>
        IOperand One { get; }
        /// <summary>
        /// Gets an operand representing a numeric constant.
        /// </summary>
        /// <param name="value">The numeric constant.</param>
        /// <returns>The operand.</returns>
        IOperand MakeOperand(int value);
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
        /// has already been emitted in routine code is undefined. 
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
        IOperand VocabularyTable { get; }

        /// <summary>
        /// Writes the final output and closes the game builder.
        /// </summary>
        void Finish();
    }

    [ContractClassFor(typeof(IGameBuilder))]
    abstract class IGameBuilderContracts : IGameBuilder
    {
        [ContractInvariantMethod]
        void ObjectInvariant()
        {
            Contract.Invariant(Options != null);
            Contract.Invariant(MaxPropertyLength > 0);
            Contract.Invariant(MaxProperties > 0);
            Contract.Invariant(MaxFlags > 0);
            Contract.Invariant(MaxCallArguments > 0);
            Contract.Invariant(Zero != null);
            Contract.Invariant(One != null);
            Contract.Invariant(SelfInsertingBreaks != null);
            Contract.Invariant(VocabularyTable != null);
        }

        public IGameOptions Options
        {
            get { throw new System.NotImplementedException(); }
        }

        public IDebugFileBuilder DebugFile
        {
            get { throw new System.NotImplementedException(); }
        }

        public IGlobalBuilder DefineGlobal(string name)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
            Contract.Ensures(Contract.Result<IGlobalBuilder>() != null);
            return default(IGlobalBuilder);
        }

        public ITableBuilder DefineTable(string name, bool pure)
        {
            Contract.Ensures(Contract.Result<ITableBuilder>() != null);
            return default(ITableBuilder);
        }

        public IRoutineBuilder DefineRoutine(string name, bool entryPoint, bool cleanStack)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
            Contract.Ensures(Contract.Result<IRoutineBuilder>() != null);
            return default(IRoutineBuilder);
        }

        public IObjectBuilder DefineObject(string name)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
            return default(IObjectBuilder);
        }

        public IPropertyBuilder DefineProperty(string name)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
            return default(IPropertyBuilder);
        }

        public IFlagBuilder DefineFlag(string name)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
            return default(IFlagBuilder);
        }

        public int MaxPropertyLength
        {
            get { throw new System.NotImplementedException(); }
        }

        public int MaxProperties
        {
            get { throw new System.NotImplementedException(); }
        }

        public int MaxFlags
        {
            get { throw new System.NotImplementedException(); }
        }

        public int MaxCallArguments
        {
            get { throw new System.NotImplementedException(); }
        }

        public IOperand Zero
        {
            get { throw new System.NotImplementedException(); }
        }

        public IOperand One
        {
            get { throw new System.NotImplementedException(); }
        }

        public IOperand MakeOperand(int value)
        {
            Contract.Ensures(Contract.Result<IOperand>() != null);
            return default(IOperand);
        }

        public IOperand MakeOperand(string value)
        {
            Contract.Requires(value != null);
            Contract.Ensures(Contract.Result<IOperand>() != null);
            return default(IOperand);
        }

        public IOperand DefineConstant(string name, IOperand value)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
            Contract.Requires(value != null);
            Contract.Ensures(Contract.Result<IOperand>() != null);
            return default(IOperand);
        }

        public IWordBuilder DefineVocabularyWord(string word)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(word));
            Contract.Ensures(Contract.Result<IWordBuilder>() != null);
            return default(IWordBuilder);
        }

        public ICollection<char> SelfInsertingBreaks
        {
            get { throw new System.NotImplementedException(); }
        }

        public IOperand VocabularyTable
        {
            get { throw new System.NotImplementedException(); }
        }

        public void Finish()
        {
            throw new System.NotImplementedException();
        }
    }

    public interface IGameOptions
    {
    }

    public interface IOperand
    {
    }

    [ContractClass(typeof(IVariableContracts))]
    public interface IVariable : IOperand
    {
        IIndirectOperand Indirect { get; }
    }

    [ContractClassFor(typeof(IVariable))]
    abstract class IVariableContracts : IVariable
    {
        [ContractInvariantMethod]
        void ObjectInvariant()
        {
            Contract.Invariant(Indirect != null);
        }

        public IIndirectOperand Indirect
        {
            get { throw new System.NotImplementedException(); }
        }
    }

    [ContractClass(typeof(IIndirectOperandContracts))]
    public interface IIndirectOperand : IOperand
    {
        IVariable Variable { get; }
    }

    [ContractClassFor(typeof(IIndirectOperand))]
    abstract class IIndirectOperandContracts : IIndirectOperand
    {
        [ContractInvariantMethod]
        void ObjectInvariant()
        {
            Contract.Invariant(Variable != null);
        }

        public IVariable Variable
        {
            get { throw new System.NotImplementedException(); }
        }
    }

    public enum Condition
    {
        /// <summary>
        /// Branch if left &lt; right.
        /// </summary>
        Less,
        /// <summary>
        /// Branch if left &gt; right.
        /// </summary>
        Greater,
        /// <summary>
        /// Branch if left object is inside right object.
        /// </summary>
        Inside,
        /// <summary>
        /// Branch if left & right == right.
        /// </summary>
        TestBits,
        /// <summary>
        /// Branch if left object has right attribute set.
        /// </summary>
        TestAttr,
        /// <summary>
        /// Increment left (which must be <see cref="IVariable"/> and branch if
        /// new value &gt; right.
        /// </summary>
        IncCheck,
        /// <summary>
        /// Decrement left (which must be <see cref="IVariable"/> and branch if
        /// new value &lt; right.
        /// </summary>
        DecCheck,

        /// <summary>
        /// Branch if at least this many arguments were passed in.
        /// </summary>
        ArgProvided,

        /// <summary>
        /// Branch if the story file's checksum is correct.
        /// </summary>
        Verify,
        /// <summary>
        /// Branch if the story file is genuine.
        /// </summary>
        Original,
    }

    public enum TernaryOp
    {
        /// <summary>
        /// Stores the byte given by right at the address left + center.
        /// </summary>
        PutByte,
        /// <summary>
        /// Stores the word given by right at the address left + (2 * center).
        /// </summary>
        PutWord,
        /// <summary>
        /// Changes the property center of object left to the value given by right.
        /// </summary>
        PutProperty,
        /// <summary>
        /// Copies the number of bytes given by right from the address given by left
        /// to the address given by center, or if center is zero, zeroes the bytes
        /// at left instead.
        /// </summary>
        /// <remarks>
        /// If right is negative, the actual number of bytes is its absolute value,
        /// and copying always proceeds forward (from low to high). Otherwise,
        /// copying proceeds either forward or backward as needed to avoid
        /// corrupting the source array.
        /// </remarks>
        CopyTable,
    }

    public enum BinaryOp
    {
        /// <summary>
        /// Adds left to right.
        /// </summary>
        Add,
        /// <summary>
        /// Subtracts right from left.
        /// </summary>
        Sub,
        /// <summary>
        /// Multiplies left by right.
        /// </summary>
        Mul,
        /// <summary>
        /// Divides left by right and returns the integer quotient.
        /// </summary>
        Div,
        /// <summary>
        /// Divides left by right and returns the remainder.
        /// </summary>
        Mod,
        /// <summary>
        /// Combines left and right with bitwise-AND.
        /// </summary>
        And,
        /// <summary>
        /// Combines left and right with bitwise-OR.
        /// </summary>
        Or,
        /// <summary>
        /// Shifts left by right bits (lshift if right is positive, rshift if negative),
        /// extending the sign for rshifts.
        /// </summary>
        ArtShift,
        /// <summary>
        /// Shifts left by right bits (lshift if right is positive, rshift if negative),
        /// zeroing the sign for rshifts.
        /// </summary>
        LogShift,
        /// <summary>
        /// Reads the byte at the address left + right.
        /// </summary>
        GetByte,
        /// <summary>
        /// Reads the word at the address left + (2 * right).
        /// </summary>
        GetWord,
        /// <summary>
        /// Reads the property right from object left.
        /// </summary>
        GetProperty,
        /// <summary>
        /// Returns the address of property right in the property table belonging to object left.
        /// </summary>
        GetPropAddress,
        /// <summary>
        /// Returns the number of the next property held by object left after
        /// property right.
        /// </summary>
        GetNextProp,
        /// <summary>
        /// Moves object left into object right.
        /// </summary>
        MoveObject,
        /// <summary>
        /// Stores the value right into the variable at indirect location left.
        /// </summary>
        StoreIndirect,
        /// <summary>
        /// Sets flag right on object left.
        /// </summary>
        SetFlag,
        /// <summary>
        /// Clears flag right on object left.
        /// </summary>
        ClearFlag,
        /// <summary>
        /// Changes the output stream setting (with a parameter, i.e. the table address for stream 3).
        /// </summary>
        DirectOutput,
        /// <summary>
        /// Moves the cursor to row left, column right.
        /// </summary>
        SetCursor,
        /// <summary>
        /// Sets the foreground and background color.
        /// </summary>
        SetColor,
        /// <summary>
        /// Returns from the routine that produced a catch token.
        /// </summary>
        Throw,
    }

    public enum UnaryOp
    {
        /// <summary>
        /// Returns the negative of the value.
        /// </summary>
        Neg,
        /// <summary>
        /// Returns the one's complement (bitwise-NOT) of the value.
        /// </summary>
        Not,
        /// <summary>
        /// Returns the child of the object.
        /// </summary>
        GetChild,
        /// <summary>
        /// Returns the sibling of the object.
        /// </summary>
        GetSibling,
        /// <summary>
        /// Returns the parent of the object.
        /// </summary>
        GetParent,
        /// <summary>
        /// Returns the size of the property at the given address.
        /// </summary>
        GetPropSize,
        /// <summary>
        /// Returns the value of the variable at the given indirect location.
        /// </summary>
        LoadIndirect,
        /// <summary>
        /// Returns a random number between 1 and the value (inclusive).
        /// </summary>
        Random,
        /// <summary>
        /// Unlinks the object from its parent and siblings.
        /// </summary>
        RemoveObject,
        /// <summary>
        /// Changes the input stream setting.
        /// </summary>
        DirectInput,
        /// <summary>
        /// Changes the output stream setting.
        /// </summary>
        DirectOutput,
        /// <summary>
        /// Changes the output text style setting.
        /// </summary>
        OutputStyle,
        /// <summary>
        /// Changes the output buffering setting.
        /// </summary>
        OutputBuffer,
        /// <summary>
        /// Changes the height of the upper window.
        /// </summary>
        SplitWindow,
        /// <summary>
        /// Changes the active display window.
        /// </summary>
        SelectWindow,
        /// <summary>
        /// Clears a display window.
        /// </summary>
        ClearWindow,
        /// <summary>
        /// Writes the cursor position into an array.
        /// </summary>
        GetCursor,
        /// <summary>
        /// Erases a line of the screen.
        /// </summary>
        EraseLine,
        /// <summary>
        /// Selects a new font and returns the previous one.
        /// </summary>
        SetFont,
        /// <summary>
        /// Checks whether a Unicode character can be input or output.
        /// </summary>
        CheckUnicode,
    }

    public enum NullaryOp
    {
        /// <summary>
        /// Updates the interpreter-drawn status line.
        /// </summary>
        ShowStatus,
        /// <summary>
        /// Saves the game state internally.
        /// </summary>
        SaveUndo,
        /// <summary>
        /// Restores the game state from an internal save.
        /// </summary>
        RestoreUndo,
        /// <summary>
        /// Obtains a catch token.
        /// </summary>
        Catch,
    }

    public enum PrintOp
    {
        /// <summary>
        /// Prints a number.
        /// </summary>
        Number,
        /// <summary>
        /// Prints a character given its ASCII code.
        /// </summary>
        Character,
        /// <summary>
        /// Prints the encoded string at a byte address.
        /// </summary>
        Address,
        /// <summary>
        /// Prints the encoded string at a packed address.
        /// </summary>
        PackedAddr,
        /// <summary>
        /// Prints the <see cref="IObjectBuilder.DescriptiveName"/> of an object.
        /// </summary>
        Object,
        /// <summary>
        /// Prints a character given its Unicode codepoint.
        /// </summary>
        Unicode,
    }

    [ContractClass(typeof(IRoutineBuilderContracts))]
    public interface IRoutineBuilder : IOperand
    {
        /// <summary>
        /// Gets a value indicating whether code generated for this routine is
        /// sacrificing space to ensure the stack is kept clean.
        /// </summary>
        /// <see cref="IGameBuilder.DefineRoutine"/>
        bool CleanStack { get; }

        ILabel RTrue { get; }
        ILabel RFalse { get; }
        IVariable Stack { get; }

        ILocalBuilder DefineRequiredParameter(string name);
        ILocalBuilder DefineOptionalParameter(string name);
        ILocalBuilder DefineLocal(string name);

        ILabel RoutineStart { get; }
        ILabel DefineLabel();
        void MarkLabel(ILabel label);

        /// <summary>
        /// Gets a value indicating whether <see cref="Condition.ArgProvided"/> is supported.
        /// </summary>
        bool HasArgCount { get; }

        void Branch(ILabel label);
        void Branch(Condition cond, IOperand left, IOperand right, ILabel label, bool polarity);
        void BranchIfZero(IOperand operand, ILabel label, bool polarity);
        void BranchIfEqual(IOperand value, IOperand option1, ILabel label, bool polarity);
        void BranchIfEqual(IOperand value, IOperand option1, IOperand option2, ILabel label, bool polarity);
        void BranchIfEqual(IOperand value, IOperand option1, IOperand option2, IOperand option3, ILabel label, bool polarity);

        void Return(IOperand result);
        void EmitRestart();
        void EmitQuit();

        /// <summary>
        /// Gets a value indicating whether the forms of <see cref="EmitSave"/> and <see cref="EmitRestore"/>
        /// that take label and polarity parameters are supported.
        /// </summary>
        bool HasBranchSave { get; }
        void EmitSave(ILabel label, bool polarity);
        void EmitRestore(ILabel label, bool polarity);

        /// <summary>
        /// Gets a value indicating whether the forms of <see cref="EmitSave"/> and <see cref="EmitRestore"/>
        /// that take a result parameter are supported.
        /// </summary>
        bool HasStoreSave { get; }
        void EmitSave(IVariable result);
        void EmitRestore(IVariable result);

        /// <summary>
        /// Gets a value indicating whether the forms of <see cref="EmitSave"/> and <see cref="EmitRestore"/>
        /// that take table, size, name, and result parameters are supported.
        /// </summary>
        bool HasExtendedSave { get; }
        void EmitSave(IOperand table, IOperand size, IOperand name, IVariable result);
        void EmitRestore(IOperand table, IOperand size, IOperand name, IVariable result);

        // form may be null
        void EmitScanTable(IOperand value, IOperand table, IOperand length, IOperand form, IVariable result, ILabel label, bool polarity);
        void EmitGetChild(IOperand value, IVariable result, ILabel label, bool polarity);
        void EmitGetSibling(IOperand value, IVariable result, ILabel label, bool polarity);

        /// <summary>
        /// Gets a value indicating whether the nullary operations <see cref="NullaryOp.SaveUndo"/> and
        /// <see cref="NullaryOp.RestoreUndo"/> are available.
        /// </summary>
        bool HasUndo { get; }

        void EmitNullary(NullaryOp op, IVariable result);
        void EmitUnary(UnaryOp op, IOperand value, IVariable result);
        void EmitBinary(BinaryOp op, IOperand left, IOperand right, IVariable result);
        void EmitTernary(TernaryOp op, IOperand left, IOperand center, IOperand right, IVariable result);

        void EmitPrint(string text, bool crlfRtrue);
        void EmitPrint(PrintOp op, IOperand value);
        // height and skip may be null
        void EmitPrintTable(IOperand table, IOperand width, IOperand height, IOperand skip);
        void EmitPrintNewLine();
        // V3: interval, routine, and result must be null
        // V4: interval and routine may be null, result must be null
        // V5+: lexbuf, interval, and routine may be null
        void EmitRead(IOperand chrbuf, IOperand lexbuf, IOperand interval, IOperand routine, IVariable result);
        // interval and routine may be null
        void EmitReadChar(IOperand interval, IOperand routine, IVariable result);
        // V3: routine must be null
        // effect, volume, and routine may always be null
        void EmitPlaySound(IOperand number, IOperand effect, IOperand volume, IOperand routine);

        // TODO: make EmitQuaternary for EncodeText, PlaySound, Read, and Tokenize?
        void EmitEncodeText(IOperand src, IOperand length, IOperand srcOffset, IOperand dest);
        void EmitTokenize(IOperand text, IOperand parse, IOperand dictionary, IOperand flag);

        // result may be null
        void EmitCall(IOperand routine, IOperand[] args, IVariable result);

        void EmitStore(IVariable dest, IOperand src);
        void EmitPopStack();

        void Finish();
    }

    [ContractClassFor(typeof(IRoutineBuilder))]
    abstract class IRoutineBuilderContracts : IRoutineBuilder
    {
        [ContractInvariantMethod]
        void ObjectInvariant()
        {
            Contract.Invariant(RTrue != null);
            Contract.Invariant(RFalse != null);
            Contract.Invariant(Stack != null);
            Contract.Invariant(RTrue != RFalse && RTrue != Stack && RFalse != Stack);
            Contract.Invariant(RoutineStart != null);
        }

        public bool CleanStack
        {
            get { throw new System.NotImplementedException(); }
        }

        public ILabel RTrue
        {
            get { throw new System.NotImplementedException(); }
        }

        public ILabel RFalse
        {
            get { throw new System.NotImplementedException(); }
        }

        public IVariable Stack
        {
            get { throw new System.NotImplementedException(); }
        }

        public ILocalBuilder DefineRequiredParameter(string name)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
            Contract.Ensures(Contract.Result<ILocalBuilder>() != null);
            return default(ILocalBuilder);
        }

        public ILocalBuilder DefineOptionalParameter(string name)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
            Contract.Ensures(Contract.Result<ILocalBuilder>() != null);
            return default(ILocalBuilder);
        }

        public ILocalBuilder DefineLocal(string name)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
            Contract.Ensures(Contract.Result<ILocalBuilder>() != null);
            return default(ILocalBuilder);
        }

        public ILabel RoutineStart
        {
            get { throw new System.NotImplementedException(); }
        }

        public ILabel DefineLabel()
        {
            Contract.Ensures(Contract.Result<ILabel>() != null);
            return default(ILabel);
        }

        public void MarkLabel(ILabel label)
        {
            Contract.Requires(label != null);
        }

        public bool HasArgCount
        {
            get { throw new System.NotImplementedException(); }
        }

        public void Branch(ILabel label)
        {
            Contract.Requires(label != null);
        }

        public void Branch(Condition cond, IOperand left, IOperand right, ILabel label, bool polarity)
        {
            Contract.Requires(left != null || (cond == Condition.Verify || cond == Condition.Original));
            Contract.Requires(right != null || (cond == Condition.Verify || cond == Condition.Original || cond == Condition.ArgProvided));
            Contract.Requires(label != null);
        }

        public void BranchIfZero(IOperand operand, ILabel label, bool polarity)
        {
            Contract.Requires(operand != null);
            Contract.Requires(label != null);
        }

        public void BranchIfEqual(IOperand value, IOperand option1, ILabel label, bool polarity)
        {
            Contract.Requires(value != null);
            Contract.Requires(option1 != null);
            Contract.Requires(label != null);
        }

        public void BranchIfEqual(IOperand value, IOperand option1, IOperand option2, ILabel label, bool polarity)
        {
            Contract.Requires(value != null);
            Contract.Requires(option1 != null);
            Contract.Requires(option2 != null);
            Contract.Requires(label != null);
        }

        public void BranchIfEqual(IOperand value, IOperand option1, IOperand option2, IOperand option3, ILabel label, bool polarity)
        {
            Contract.Requires(value != null);
            Contract.Requires(option1 != null);
            Contract.Requires(option2 != null);
            Contract.Requires(option3 != null);
            Contract.Requires(label != null);
        }

        public void Return(IOperand result)
        {
            Contract.Requires(result != null);
        }

        public void EmitRestart()
        {
            throw new System.NotImplementedException();
        }

        public void EmitQuit()
        {
            throw new System.NotImplementedException();
        }

        public bool HasBranchSave
        {
            get { throw new System.NotImplementedException(); }
        }

        public void EmitSave(ILabel label, bool polarity)
        {
            Contract.Requires(HasBranchSave);
            Contract.Requires(label != null);
        }

        public void EmitRestore(ILabel label, bool polarity)
        {
            Contract.Requires(HasBranchSave);
            Contract.Requires(label != null);
        }

        public bool HasStoreSave
        {
            get { throw new System.NotImplementedException(); }
        }

        public void EmitSave(IVariable result)
        {
            Contract.Requires(HasStoreSave);
            Contract.Requires(result != null);
        }

        public void EmitRestore(IVariable result)
        {
            Contract.Requires(HasStoreSave);
            Contract.Requires(result != null);
        }

        public bool HasExtendedSave
        {
            get { throw new System.NotImplementedException(); }
        }

        public void EmitSave(IOperand table, IOperand size, IOperand name, IVariable result)
        {
            Contract.Requires(HasExtendedSave);
            Contract.Requires(table != null);
            Contract.Requires(size != null);
            Contract.Requires(name != null);
            Contract.Requires(result != null);
        }

        public void EmitRestore(IOperand table, IOperand size, IOperand name, IVariable result)
        {
            Contract.Requires(HasExtendedSave);
            Contract.Requires(table != null);
            Contract.Requires(size != null);
            Contract.Requires(name != null);
            Contract.Requires(result != null);
        }

        public void EmitScanTable(IOperand value, IOperand table, IOperand length, IOperand form, IVariable result, ILabel label, bool polarity)
        {
            Contract.Requires(value != null);
            Contract.Requires(table != null);
            Contract.Requires(length != null);
            Contract.Requires(result != null);
            Contract.Requires(label != null);
        }

        public void EmitGetChild(IOperand value, IVariable result, ILabel label, bool polarity)
        {
            Contract.Requires(value != null);
            Contract.Requires(result != null);
            Contract.Requires(label != null);
        }

        public void EmitGetSibling(IOperand value, IVariable result, ILabel label, bool polarity)
        {
            Contract.Requires(value != null);
            Contract.Requires(result != null);
            Contract.Requires(label != null);
        }

        public bool HasUndo
        {
            get { throw new System.NotImplementedException(); }
        }

        public void EmitNullary(NullaryOp op, IVariable result)
        {
            Contract.Requires(HasUndo || (op != NullaryOp.RestoreUndo && op != NullaryOp.SaveUndo));
        }

        public void EmitUnary(UnaryOp op, IOperand value, IVariable result)
        {
            Contract.Requires(value != null);
        }

        public void EmitBinary(BinaryOp op, IOperand left, IOperand right, IVariable result)
        {
            Contract.Requires(left != null);
            Contract.Requires(right != null);
        }

        public void EmitTernary(TernaryOp op, IOperand left, IOperand center, IOperand right, IVariable result)
        {
            Contract.Requires(left != null);
            Contract.Requires(center != null);
            Contract.Requires(right != null);
        }

        public void EmitPrint(string text, bool crlfRtrue)
        {
            Contract.Requires(text != null);
        }

        public void EmitPrint(PrintOp op, IOperand value)
        {
            Contract.Requires(value != null);
        }

        public void EmitPrintTable(IOperand table, IOperand width, IOperand height, IOperand skip)
        {
            Contract.Requires(table != null);
            Contract.Requires(width != null);
            Contract.Requires(height != null || skip == null);
        }

        public void EmitPrintNewLine()
        {
            throw new System.NotImplementedException();
        }

        public void EmitRead(IOperand chrbuf, IOperand lexbuf, IOperand interval, IOperand routine, IVariable result)
        {
            Contract.Requires(chrbuf != null);
            Contract.Requires(lexbuf != null || interval == null);
            Contract.Requires(interval != null || routine == null);
        }

        public void EmitReadChar(IOperand interval, IOperand routine, IVariable result)
        {
            Contract.Requires(interval != null || routine == null);
            Contract.Requires(result != null);
        }

        public void EmitPlaySound(IOperand number, IOperand effect, IOperand volume, IOperand routine)
        {
            Contract.Requires(number != null);
            Contract.Requires(effect != null || volume == null);
            Contract.Requires(volume != null || routine == null);
        }

        public void EmitEncodeText(IOperand src, IOperand length, IOperand srcOffset, IOperand dest)
        {
            Contract.Requires(src != null);
            Contract.Requires(length != null);
            Contract.Requires(srcOffset != null);
            Contract.Requires(dest != null);
        }

        public void EmitTokenize(IOperand text, IOperand parse, IOperand dictionary, IOperand flag)
        {
            Contract.Requires(text != null);
            Contract.Requires(parse != null);
            Contract.Requires(dictionary != null || flag == null);
        }

        public void EmitCall(IOperand routine, IOperand[] args, IVariable result)
        {
            Contract.Requires(routine != null);
            Contract.Requires(args != null);
        }

        public void EmitStore(IVariable dest, IOperand src)
        {
            Contract.Requires(dest != null);
            Contract.Requires(src != null);
        }

        public void EmitPopStack()
        {
            throw new System.NotImplementedException();
        }

        public void Finish()
        {
            throw new System.NotImplementedException();
        }
    }

    public interface ILocalBuilder : IVariable
    {
        IOperand DefaultValue { get; set; }
    }

    public interface IGlobalBuilder : IVariable
    {
        IOperand DefaultValue { get; set; }
    }

    public interface IPropertyBuilder : IOperand
    {
        IOperand DefaultValue { get; set; }
    }

    public interface IFlagBuilder : IOperand
    {
    }

    [ContractClass(typeof(IObjectBuilderContracts))]
    public interface IObjectBuilder : IOperand
    {
        string DescriptiveName { get; set; }
        IObjectBuilder Parent { get; set; }
        IObjectBuilder Child { get; set; }
        IObjectBuilder Sibling { get; set; }

        void AddByteProperty(IPropertyBuilder prop, IOperand value);
        void AddWordProperty(IPropertyBuilder prop, IOperand value);
        ITableBuilder AddComplexProperty(IPropertyBuilder prop);

        void AddFlag(IFlagBuilder flag);
    }

    [ContractClassFor(typeof(IObjectBuilder))]
    abstract class IObjectBuilderContracts : IObjectBuilder
    {
        public string DescriptiveName { get; set; }
        public IObjectBuilder Parent { get; set; }
        public IObjectBuilder Child { get; set; }
        public IObjectBuilder Sibling { get; set; }

        public void AddByteProperty(IPropertyBuilder prop, IOperand value)
        {
            Contract.Requires(prop != null);
            Contract.Requires(value != null);
        }

        public void AddWordProperty(IPropertyBuilder prop, IOperand value)
        {
            Contract.Requires(prop != null);
            Contract.Requires(value != null);
        }

        public ITableBuilder AddComplexProperty(IPropertyBuilder prop)
        {
            Contract.Requires(prop != null);
            Contract.Ensures(Contract.Result<ITableBuilder>() != null);
            return default(ITableBuilder);
        }

        public void AddFlag(IFlagBuilder flag)
        {
            Contract.Requires(flag != null);
        }
    }

    [ContractClass(typeof(ITableBuilderContracts))]
    public interface ITableBuilder : IOperand
    {
        void AddByte(byte value);
        void AddByte(IOperand value);
        void AddShort(short value);
        void AddShort(IOperand value);
    }

    [ContractClassFor(typeof(ITableBuilder))]
    abstract class ITableBuilderContracts : ITableBuilder
    {
        public void AddByte(byte value)
        {
            throw new System.NotImplementedException();
        }

        public void AddByte(IOperand value)
        {
            Contract.Requires(value != null);
        }

        public void AddShort(short value)
        {
            throw new System.NotImplementedException();
        }

        public void AddShort(IOperand value)
        {
            Contract.Requires(value != null);
        }
    }

    public interface IWordBuilder : ITableBuilder
    {
    }

    public interface ILabel
    {
    }

    [ContractClass(typeof(IDebugFileBuilderContracts))]
    public interface IDebugFileBuilder
    {
        /// <summary>
        /// Marks an operand as an action constant.
        /// </summary>
        /// <param name="action">The operand whose value identifies an action.</param>
        /// <param name="name">The name of the action.</param>
        void MarkAction(IOperand action, string name);
        /// <summary>
        /// Marks the bounds of an object definition.
        /// </summary>
        /// <param name="obj">The object being defined.</param>
        /// <param name="start">The position where the object definition begins.</param>
        /// <param name="end">The position where the object definition ends.</param>
        void MarkObject(IObjectBuilder obj, DebugLineRef start, DebugLineRef end);
        /// <summary>
        /// Marks the bounds of a routine definition.
        /// </summary>
        /// <param name="routine">The routine being defined.</param>
        /// <param name="start">The position where the routine definition begins.</param>
        /// <param name="end">The position where the routine definition ends.</param>
        void MarkRoutine(IRoutineBuilder routine, DebugLineRef start, DebugLineRef end);
        /// <summary>
        /// Marks a sequence point at the current position in a routine.
        /// </summary>
        /// <param name="routine">The routine being defined.</param>
        /// <param name="point">The position corresponding to the next
        /// instruction emitted.</param>
        void MarkSequencePoint(IRoutineBuilder routine, DebugLineRef point);
    }

    [ContractClassFor(typeof(IDebugFileBuilder))]
    abstract class IDebugFileBuilderContracts : IDebugFileBuilder
    {
        public void MarkAction(IOperand action, string name)
        {
            Contract.Requires(action != null);
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
        }

        public void MarkObject(IObjectBuilder obj, DebugLineRef start, DebugLineRef end)
        {
            Contract.Requires(obj != null);
        }

        public void MarkRoutine(IRoutineBuilder routine, DebugLineRef start, DebugLineRef end)
        {
            Contract.Requires(routine != null);
        }

        public void MarkSequencePoint(IRoutineBuilder routine, DebugLineRef point)
        {
            Contract.Requires(routine != null);
        }
    }
}