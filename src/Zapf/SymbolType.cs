/* Copyright 2010-2018 Tara McGrew
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

namespace Zapf
{
    public enum SymbolType
    {
        /// <summary>
        /// The symbol has not been defined.
        /// </summary>
        Unknown,
        /// <summary>
        /// The symbol is a numeric constant.
        /// </summary>
        Constant,
        /// <summary>
        /// The symbol is a local or global variable.
        /// </summary>
        Variable,
        /// <summary>
        /// The symbol is a local or global label (byte address).
        /// </summary>
        Label,
        /// <summary>
        /// The symbol is a packed function address.
        /// </summary>
        Function,
        /// <summary>
        /// The symbol is a packed string address.
        /// </summary>
        String,
        /// <summary>
        /// The symbol is an object number.
        /// </summary>
        Object,
    }
}
