/* Copyright 2010-2025 Tara McGrew
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

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace ZilfSourceGenerators
{
    // Data structures for method information
    public class BuiltinMethodInfo
    {
        public MethodDeclarationSyntax Method { get; set; } = null!;
        public IMethodSymbol MethodSymbol { get; set; } = null!;
        public BuiltinAttributeInfo[] AttributeInfos { get; set; } = null!;
        public ImmutableArray<IParameterSymbol> ArgumentParameters { get; set; } = ImmutableArray<IParameterSymbol>.Empty;
        public ImmutableArray<IParameterSymbol> DataParameters { get; set; } = ImmutableArray<IParameterSymbol>.Empty;
    }

    public class BuiltinAttributeInfo
    {
        public string Name { get; set; } = "";
        public string? Data { get; set; }
        public int? MinVersion { get; set; }
        public int? MaxVersion { get; set; }
        public bool HasSideEffect { get; set; }
        public int Priority { get; set; } = 1;
        public string? Summary { get; set; }
        public BuiltinPlatformSetting Platform { get; set; } = BuiltinPlatformSetting.Any;
    }

    public enum BuiltinPlatformSetting
    {
        Any = 0,
        ZMachine = 1,
        Glulx = 2,
        Cornerstone = 3,
    }

    public class ParameterInfo
    {
        public List<IParameterSymbol> DataParameters { get; set; } = [];
        public List<IParameterSymbol> ArgumentParameters { get; set; } = [];
        public int RequiredArgumentCount { get; set; } = 0;
        public int OptionalArgumentCount { get; set; } = 0;
        public bool HasParamsArray { get; set; } = false;
    }

    public class ArgumentCountGroup
    {
        public int Key { get; set; }
        public List<OverloadInfo> Overloads { get; set; } = [];
    }

    public class OverloadGroup
    {
        public string BuiltinName { get; set; } = "";
        public string CallType { get; set; } = "";
        public List<OverloadInfo> Overloads { get; set; } = [];
    }

    public class OverloadInfo
    {
        public BuiltinMethodInfo Method { get; set; } = null!;
        public BuiltinAttributeInfo Attribute { get; set; } = null!;
    }
}
