using System;

namespace Zilf.Compiler.Builtins
{
    /// <summary>
    /// Indicates that the parameter will be used as a table address.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    public class TableAttribute : Attribute
    {
    }
}