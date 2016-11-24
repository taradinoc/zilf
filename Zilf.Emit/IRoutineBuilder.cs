using System;
using System.Diagnostics.Contracts;

namespace Zilf.Emit
{
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
        void EmitPushUserStack(IOperand value, IOperand stack, ILabel label, bool polarity);

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

        public void EmitPushUserStack(IOperand value, IOperand stack, ILabel label, bool polarity)
        {
            Contract.Requires(value != null);
            Contract.Requires(stack != null);
            Contract.Requires(label != null);
        }

        public void Finish()
        {
            throw new System.NotImplementedException();
        }
    }
}