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

namespace Zapf.Parsing.Instructions
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class ZOpAttribute(string classicName, string informName, int minVer, int maxVer, ZOpFlags flags) : Attribute
    {
        public string ClassicName { get; } = classicName;

        public string InformName { get; } = informName;

        public int MinVer { get; } = minVer;

        public int MaxVer { get; } = maxVer;

        public ZOpFlags Flags { get; } = flags;

        public string? WhenExtra { get; set; }
    }
}