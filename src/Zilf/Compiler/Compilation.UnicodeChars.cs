/* Copyright 2010-2026 Tara McGrew
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

using System.Diagnostics;
using Zilf.Common.StringEncoding;
using Zilf.Diagnostics;
using Zilf.Interpreter.Values;
using Zilf.Language;

namespace Zilf.Compiler
{
    partial class Compilation
    {
        void NoteZMachineCharUsage(ZilChar zch, ISourceLine? source)
        {
            if (Context.IsGlulx)
                return;

            var c = zch.Char;
            if (c == '\0' || c == '\r' || c == '\n' || (c >= 32 && c <= 126))
                return;

            var usage = Context.ZEnvironment.UnicodeUsage;
            var isDefaultExtra = UnicodeTranslation.Table.ContainsKey(c);

            if (isDefaultExtra)
            {
                if (!usage.TryNoteDefaultChar(c, out var rejected))
                {
                    Debug.Assert(rejected.HasValue);
                    Context.HandleError(new CompilerError(
                        source,
                        CompilerMessages.No_Room_Left_In_Unicode_Translation_Table_For_Character_0,
                        rejected!.Value));
                }

                return;
            }

            if (Context.ZEnvironment.ZVersion <= 4)
            {
                Context.HandleError(new CompilerError(
                    source,
                    CompilerMessages.Character_0_Is_Not_Part_Of_Standard_ZSCII_And_Cannot_Be_Printed_In_Zmachine_Version_1,
                    c,
                    Context.ZEnvironment.ZVersion));
                return;
            }

            if (!usage.TryRequireCustomChar(c, out var customRejected))
            {
                Debug.Assert(customRejected.HasValue);
                Context.HandleError(new CompilerError(
                    source,
                    CompilerMessages.No_Room_Left_In_Unicode_Translation_Table_For_Character_0,
                    customRejected!.Value));
            }
        }
    }
}
