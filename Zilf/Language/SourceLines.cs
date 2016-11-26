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
namespace Zilf.Language
{
    static class SourceLines
    {
        public static readonly ISourceLine Unknown = new StringSourceLine("<internally created FORM>");
        public static readonly ISourceLine Chtyped = new StringSourceLine("<result of CHTYPE>");
        public static readonly ISourceLine MakeGval = new StringSourceLine("<result of MAKE-GVAL>");
        public static readonly ISourceLine TopLevel = new StringSourceLine("<top level>");
    }
}