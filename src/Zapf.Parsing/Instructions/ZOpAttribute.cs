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
    public class ZOpAttribute : Attribute
    {
        public ZOpAttribute(string classicName, string informName, int minVer, int maxVer, ZOpFlags flags)
        {
            ClassicName = classicName;
            InformName = informName;
            MinVer = minVer;
            MaxVer = maxVer;
            Flags = flags;
        }

        public string ClassicName { get; }

        public string InformName { get; }

        public int MinVer { get; }

        public int MaxVer { get; }

        public ZOpFlags Flags { get; }

        public string? WhenExtra { get; set; }
    }
}