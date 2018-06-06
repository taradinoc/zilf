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

using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace Zilf.Interpreter
{
    static class Recursion
    {
        static readonly ConditionalWeakTable<object, object> table = new ConditionalWeakTable<object, object>();

        public static bool TryLock(object obj)
        {
            if (table.TryGetValue(obj, out _))
            {
                return false;
            }

            table.Add(obj, null);
            return true;
        }

        public static void Unlock([NotNull] object obj)
        {
            table.Remove(obj);
        }
    }
}
