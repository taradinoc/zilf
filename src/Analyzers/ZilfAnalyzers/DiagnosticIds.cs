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

namespace ZilfAnalyzers
{
    public static class DiagnosticIds
    {
        public const string ExceptionShouldUseDiagnosticCode = "ZILF0001";
        public const string DuplicateMessageCode = "ZILF0002";
        public const string DuplicateMessageFormat = "ZILF0003";
        public const string PrefixedMessageFormat = "ZILF0004";
        public const string ComparingZilObjectsWithEquals = "ZILF0005";
        public const string PartiallyOverriddenZilObjectComparison = "ZILF0006";
        public const string OverridingExactlyEqualsWithoutGetHashCode = "ZILF0007";
    }
}