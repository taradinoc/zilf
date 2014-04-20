/* Copyright 2010, 2012 Jesse McGrew
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
using System.Linq;
using System.Text;

namespace Zilf
{
    [AttributeUsage(AttributeTargets.Field)]
    class AtomAttribute : Attribute
    {
        private readonly string name;

        public AtomAttribute(string name)
        {
            this.name = name;
        }

        public string Name
        {
            get { return name; }
        }
    }

    /// <summary>
    /// Contains values for atoms used internally.
    /// </summary>
    enum StdAtom
    {
        None,

        [Atom("+")]
        Plus,
        [Atom("-")]
        Minus,
        [Atom("*")]
        Times,
        [Atom("/")]
        Divide,
        [Atom("=")]
        Eq,
        [Atom("=?")]
        Eq_P,
        [Atom("==?")]
        Eeq_P,
        [Atom("N=?")]
        Neq_P,
        [Atom("N==?")]
        Neeq_P,
        [Atom("0?")]
        Zero_P,
        [Atom("1?")]
        One_P,

        ACTIONS,
        ADD,
        ADJECTIVE,
        AGAIN,
        AND,
        ANDB,
        APPLY,
        ATOM,
        BACK,
        BAND,
        BCOM,
        BIND,
        BOR,
        BTST,
        BUFOUT,
        BYTE,
        BYTELENGTH,
        CARRIED,
        CHARACTER,
        CLEAR,
        [Atom("COMPACT-SYNTAXES?")]
        COMPACT_SYNTAXES_P,
        COND,
        CONSTANT,
        COPYT,
        CRLF,
        CURSET,
        DEC,
        DEFAULT,
        DEFINED,
        DESC,
        DIRIN,
        DIROUT,
        DIV,
        [Atom("DLESS?")]
        DLESS_P,
        ELSE,
        [Atom("EQUAL?")]
        EQUAL_P,
        EZIP,
        FALSE,
        [Atom("FALSE-VALUE")]
        FALSE_VALUE,
        FCLEAR,
        FIND,
        FIRST,
        [Atom("FIRST?")]
        FIRST_P,
        FIX,
        FLAGS,
        FORM,
        FSET,
        [Atom("FSET?")]
        FSET_P,
        FSUBR,
        FUNCTION,
        [Atom("G?")]
        G_P,
        [Atom("G=?")]
        GEq_P,
        GET,
        GETB,
        GETP,
        GETPT,
        GLOBAL,
        GO,
        GVAL,
        HAVE,
        HELD,
        HLIGHT,
        IF,
        [Atom("IGRTR?")]
        IGRTR_P,
        IN,
        [Atom("IN?")]
        IN_P,
        [Atom("IN-ROOM")]
        IN_ROOM,
        INC,
        INPUT,
        [Atom("INTBL?")]
        INTBL_P,
        IRESTORE,
        IS,
        ISAVE,
        KERNEL,
        [Atom("L?")]
        L_P,
        LAST,
        LENGTH,
        [Atom("L=?")]
        LEq_P,
        LEXV,
        LIST,
        LOC,
        [Atom("LOW-DIRECTION")]
        LOW_DIRECTION,
        LVAL,
        MACRO,
        MANY,
        MOD,
        MOVE,
        MUL,
        [Atom("NEW-VOC?")]
        NEW_VOC_P,
        [Atom("NEXT?")]
        NEXT_P,
        NEXTP,
        NONE,
        NOT,
        NTH,
        OBJECT,
        OBLIST,
        [Atom("ON-GROUND")]
        ON_GROUND,
        OPEN,
        OR,
        ORB,
        PER,
        [Atom("PLUS-MODE")]
        PLUS_MODE,
        PREACTIONS,
        PREDGEN,
        PREPOSITIONS,
        PRINT,
        PRINTB,
        PRINTC,
        PRINTD,
        PRINTI,
        PRINTN,
        PRINTR,
        PROG,
        PROPSPEC,
        PTSIZE,
        PURE,
        PUSH,
        PUT,
        PUTB,
        PUTP,
        QUIT,
        QUOTE,
        RANDOM,
        READ,
        REDEFINE,
        REMOVE,
        REPEAT,
        REST,
        RESTART,
        RESTORE,
        RETURN,
        RFALSE,
        [Atom("ROOMS-FIRST")]
        ROOMS_FIRST,
        [Atom("ROOMS-LAST")]
        ROOMS_LAST,
        ROUTINE,
        RSTACK,
        RTRUE,
        SAVE,
        SCREEN,
        SEGMENT,
        SERIAL,
        SET,
        SETG,
        SORRY,
        SOUND,
        SPLIT,
        STRING,
        SUB,
        SUBR,
        SYNONYM,
        T,
        TABLE,
        TAKE,
        TO,
        [Atom("TRUE-VALUE")]
        TRUE_VALUE,
        USL,
        VALUE,
        VERBS,
        VERIFY,
        [Atom("VERSION?")]
        VERSION_P,
        WORD,
        WORDLENGTH,
        XZIP,
        YZIP,
        [Atom("ZERO?")]
        ZERO_P,
        ZILCH,
        ZILF,
        [Atom("ZIL-VERSION")]
        ZIL_VERSION,
        ZIP,
    }
}
