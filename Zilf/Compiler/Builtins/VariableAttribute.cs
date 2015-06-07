using System;

namespace Zilf.Compiler.Builtins
{
    /// <summary>
    /// Indicates that the parameter will be used as a variable index.
    /// The caller may pass an atom, which will be interpreted as the name of
    /// a variable, and its index will be used instead of its value.
    /// </summary>
    /// <seealso cref="IVariable.Indirect"/>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class VariableAttribute : Attribute
    {
        /// <summary>
        /// If true, then even a reference to a variable's value via
        /// &lt;LVAL X&gt; or &lt;GVAL X&gt; (or .X or ,X) will be interpreted
        /// as referring to its index. Use &lt;VALUE X&gt; to force the
        /// value to be used.
        /// </summary>
        public QuirksMode QuirksMode { get; set; }
    }
}