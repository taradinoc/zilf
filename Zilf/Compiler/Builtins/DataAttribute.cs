using System;

namespace Zilf.Compiler.Builtins
{
    /// <summary>
    /// Indicates the parameter where the value of <see cref="BuiltinAttribute.Data"/>
    /// should be passed in. This does not correspond to a ZIL parameter.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class DataAttribute : Attribute
    {
    }
}