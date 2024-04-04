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

namespace Zilf.Emit
{
    public interface IRoutineBuilder : IConstantOperand
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

        /// <exception cref="ArgumentException">
        /// A local variable already exists by that paramName.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The routine is the entry point and thus not allowed to have local variables.
        /// </exception>
        ILocalBuilder DefineRequiredParameter(string name);

        /// <exception cref="ArgumentException">
        /// A local variable already exists by that paramName.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The routine is the entry point and thus not allowed to have local variables.
        /// </exception>
        ILocalBuilder DefineOptionalParameter(string paramName);

        /// <exception cref="ArgumentException">
        /// A local variable already exists by that localName.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The routine is the entry point and thus not allowed to have local variables.
        /// </exception>
        ILocalBuilder DefineLocal(string localName);

        ILabel RoutineStart { get; }

        ILabel DefineLabel();

        void MarkLabel(ILabel label);

        /// <summary>
        /// Gets a value indicating whether <see cref="Condition.ArgProvided"/> is supported.
        /// </summary>
        bool HasArgCount { get; }

        void Branch(ILabel label);
        void Branch(Condition cond, IOperand? left, IOperand? right, ILabel label, bool polarity);
        void BranchIfZero(IOperand operand, ILabel label, bool polarity);
        void BranchIfEqual(IOperand value, IOperand option1, ILabel label, bool polarity);

        void BranchIfEqual(IOperand value, IOperand option1, IOperand option2,
            ILabel label, bool polarity);

        void BranchIfEqual(IOperand value, IOperand option1, IOperand option2,
            IOperand option3, ILabel label, bool polarity);

        void Return(IOperand result);
        void EmitRestart();
        void EmitQuit();

        /// <summary>
        /// Gets a value indicating whether the forms of <see cref="EmitSave(ILabel,bool)"/> and <see cref="EmitRestore(ILabel,bool)"/>
        /// that take label and polarity parameters are supported.
        /// </summary>
        bool HasBranchSave { get; }

        void EmitSave(ILabel label, bool polarity);
        void EmitRestore(ILabel label, bool polarity);

        /// <summary>
        /// Gets a value indicating whether the forms of <see cref="EmitSave(IVariable)"/> and <see cref="EmitRestore(IVariable)"/>
        /// that take a result parameter are supported.
        /// </summary>
        bool HasStoreSave { get; }

        void EmitSave(IVariable result);
        void EmitRestore(IVariable result);

        /// <summary>
        /// Gets a value indicating whether the forms of <see cref="EmitSave(IOperand,IOperand,IOperand,IVariable)"/> and <see cref="EmitRestore(IOperand,IOperand,IOperand,IVariable)"/>
        /// that take table, size, name, and result parameters are supported.
        /// </summary>
        bool HasExtendedSave { get; }

        void EmitSave(IOperand table, IOperand size, IOperand name,
            IVariable result);

        void EmitRestore(IOperand table, IOperand size, IOperand name,
            IVariable result);

        // form may be null
        void EmitScanTable(IOperand value, IOperand table, IOperand length, IOperand? form,
            IVariable result, ILabel label, bool polarity);

        void EmitGetChild(IOperand value, IVariable result, ILabel label, bool polarity);

        void EmitGetSibling(IOperand value, IVariable result, ILabel label,
            bool polarity);

        /// <summary>
        /// Gets a value indicating whether the nullary operations <see cref="NullaryOp.SaveUndo"/> and
        /// <see cref="NullaryOp.RestoreUndo"/> are available.
        /// </summary>
        bool HasUndo { get; }

        void EmitNullary(NullaryOp op, IVariable? result);
        void EmitUnary(UnaryOp op, IOperand value, IVariable? result);
        void EmitBinary(BinaryOp op, IOperand left, IOperand right, IVariable? result);

        void EmitTernary(TernaryOp op, IOperand left, IOperand center, IOperand right,
            IVariable? result);

        void EmitPrint(string text, bool crlfRtrue);

        void EmitPrint(PrintOp op, IOperand value);

        // height and skip may be null
        void EmitPrintTable(IOperand table, IOperand width, IOperand? height, IOperand? skip);

        void EmitPrintNewLine();

        // V3: interval, routine, and result must be null
        // V4: interval and routine may be null, result must be null
        // V5+: lexbuf, interval, and routine may be null
        void EmitRead(IOperand chrbuf, IOperand? lexbuf, IOperand? interval, IOperand? routine,
            IVariable? result);

        // interval and routine may be null
        void EmitReadChar(IOperand? interval, IOperand? routine, IVariable result);

        // V3: routine must be null
        // effect, volume, and routine may always be null
        void EmitPlaySound(IOperand number, IOperand? effect, IOperand? volume, IOperand? routine);

        // TODO: make EmitQuaternary for EncodeText, PlaySound, Read, and Tokenize?
        void EmitEncodeText(IOperand src, IOperand length, IOperand srcOffset,
            IOperand dest);

        void EmitTokenize(IOperand text, IOperand parse, IOperand? dictionary, IOperand? flag);

        // result may be null
        void EmitCall(IOperand routine, ReadOnlySpan<IOperand> args, IVariable? result);

        void EmitStore(IVariable dest, IOperand src);
        void EmitPopStack();

        void EmitPushUserStack(IOperand value, IOperand stack, ILabel label,
            bool polarity);

        void Finish();
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
        /// Write height/width of picture left into array right and branch if the
        /// picture number is valid. (Or if left is 0, write number of available
        /// pictures and release number of picture file, and branch if any pictures
        /// are available.)
        /// </summary>
        PictureData,

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

        /// <summary>
        /// Add a menu, with ID left and item names right (or remove the menu
        /// if right is 0), and branch if successful.
        /// </summary>
        MakeMenu,
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

        /// <summary>
        /// Changes the property center of window left to the value given by right.
        /// </summary>
        PutWindowProperty,

        /// <summary>
        /// Draws picture number left at Y-position center, X-position right (or
        /// the cursor's Y or X position if either are zero).
        /// </summary>
        DrawPicture,

        /// <summary>
        /// Sets or changes the style attributes of window left, according to the
        /// attributes given by center and the operation given by right.
        /// </summary>
        WindowStyle,

        /// <summary>
        /// Moves window left to Y-position center, X-position right.
        /// </summary>
        MoveWindow,

        /// <summary>
        /// Changes the size of window left to height center, width right.
        /// </summary>
        WindowSize,

        /// <summary>
        /// Sets the margins for the window given by right: the new left margin is
        /// given by left, and the new right margin is given by center.
        /// </summary>
        SetMargins,

        /// <summary>
        /// Sets the cursor position in window right to Y-position left, X-position center.
        /// </summary>
        SetCursor,

        /// <summary>
        /// Changes the output stream setting (with two parameters, for V6 only).
        /// </summary>
        DirectOutput,

        /// <summary>
        /// Erases picture number left at Y-position center, X-position right.
        /// </summary>
        ErasePicture,
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

        /// <summary>
        /// Throws away left items from the top of user stack right.
        /// </summary>
        FlushUserStack,

        /// <summary>
        /// Reads the property right from window left.
        /// </summary>
        GetWindowProperty,

        /// <summary>
        /// Scrolls window left by the number of pixels given by right.
        /// </summary>
        ScrollWindow,
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

        /// <summary>
        /// Returns a value popped from the given user stack.
        /// </summary>
        PopUserStack,

        /// <summary>
        /// Throws away the given number of values from the top of the main stack.
        /// </summary>
        FlushStack,

        /// <summary>
        /// Caches the pictures listed in the given table.
        /// </summary>
        PictureTable,

        /// <summary>
        /// Constrains the mouse pointer to the bounds of the given window.
        /// </summary>
        MouseWindow,

        /// <summary>
        /// Writes the mouse coordinates, button state, and menu state into the given table.
        /// </summary>
        ReadMouse,

        /// <summary>
        /// Prints formatted text from the given table.
        /// </summary>
        PrintForm,
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
}