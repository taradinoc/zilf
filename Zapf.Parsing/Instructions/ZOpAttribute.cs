/* Copyright 2010-2017 Jesse McGrew
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
using JetBrains.Annotations;

namespace Zapf.Parsing.Instructions
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    [MeansImplicitUse]
    public class ZOpAttribute : Attribute
    {
        public ZOpAttribute([NotNull] string classicName, [NotNull] string informName, int minVer, int maxVer, ZOpFlags flags)
        {
            ClassicName = classicName;
            InformName = informName;
            MinVer = minVer;
            MaxVer = maxVer;
            Flags = flags;
        }

        [NotNull]
        public string ClassicName { get; }

        [NotNull]
        public string InformName { get; }

        public int MinVer { get; }

        public int MaxVer { get; }

        public ZOpFlags Flags { get; }

        [CanBeNull]
        public string WhenExtra { get; set; }
    }
}