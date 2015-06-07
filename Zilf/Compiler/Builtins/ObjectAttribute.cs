using System;

namespace Zilf.Compiler.Builtins
{
    /// <summary>
    /// Indicates that the parameter will be used as an object.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    public class ObjectAttribute : Attribute
    {
    }
}