/* Copyright 2010-2018 Jesse McGrew
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

namespace Zilf.Common
{
    [Serializable]
    public sealed class UnhandledCaseException : Exception
    {
        public static UnhandledCaseException FromEnum<T>(T enumValue, string usage = null)
            where T : struct
        {
            return new UnhandledCaseException(
                $"Unhandled {usage ?? "case"}: " +
                $"{typeof(T).Name}.{enumValue}");
        }

        public static UnhandledCaseException FromTypeOf<T>(T value, string usage = null)
        {
            return new UnhandledCaseException(
                $"Unhandled {(usage == null ? "type" : usage + " type")}: " +
                $"{(value == null ? "null" : value.GetType().Name)}");
        }

        public UnhandledCaseException(string message)
            : base(message)
        {
        }
    }
}
