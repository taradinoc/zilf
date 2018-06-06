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
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;

namespace Zilf.Interpreter.Values.Tied
{
    sealed class TiedLayout
    {
        internal static readonly Dictionary<Type, TiedLayout> Layouts = new Dictionary<Type, TiedLayout>();

        [NotNull]
        public static TiedLayout Create<T>(params Expression<Func<T, ZilObject>>[] elements)
            where T : ZilObject, IStructure
        {
            var properties = from e in elements
                             let m = (MemberExpression)e.Body
                             let pi = (PropertyInfo)m.Member
                             select pi;

            return new TiedLayout(properties.ToArray());
        }

        public TiedLayout(PropertyInfo[] properties, [CanBeNull] PropertyInfo catchAll = null)
        {
            PropertyInfos = properties;
            CatchAllPropertyInfo = catchAll;
        }

        public IReadOnlyList<PropertyInfo> PropertyInfos { get; }
        public PropertyInfo CatchAllPropertyInfo { get; }
        public int MinLength => PropertyInfos.Count;

        [NotNull]
        public TiedLayout WithCatchAll<T>([NotNull] Expression<Func<T, IStructure>> catchAll)
            where T : ZilObject, IStructure
        {
            if (CatchAllPropertyInfo != null)
                throw new InvalidOperationException($"{nameof(CatchAllPropertyInfo)} already set");

            var m = (MemberExpression)catchAll.Body;
            var pi = (PropertyInfo)m.Member;

            return new TiedLayout((PropertyInfo[])PropertyInfos, pi);
        }
    }
}
