using System;
using Zilf.Interpreter.Values;

namespace Zilf.Interpreter
{
    /// <summary>
    /// Specifies that a constructor or static method implements CHTYPE for a builtin type.
    /// </summary>
    /// <remarks>
    /// <para>If applied to a constructor, it must take a single value of the primitive type.</para>
    /// <para>If applied to a static method, it must take two parameters, <see cref="Context"/>
    /// and the primitive type, and return a type assignable to <see cref="ZilObject"/>.</para>
    /// </remarks>
    /// <seealso cref="BuiltinTypeAttribute"/>
    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method)]
    class ChtypeMethodAttribute : Attribute
    {
        public ChtypeMethodAttribute()
        {
        }
    }
}