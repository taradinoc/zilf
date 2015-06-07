using System;
using System.Collections.Generic;

namespace Zilf.Compiler.Builtins
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class BuiltinAttribute : Attribute
    {
        public BuiltinAttribute(string name)
            : this(name, null)
        {
        }

        public BuiltinAttribute(string name, params string[] aliases)
        {
            this.name = name;
            this.aliases = aliases;

            MinVersion = 1;
            MaxVersion = 6;

            Priority = 1;
        }

        private readonly string name;
        private readonly string[] aliases;

        public IEnumerable<string> Names
        {
            get
            {
                yield return name;

                if (aliases != null)
                {
                    foreach (var a in aliases)
                        yield return a;
                }
            }
        }

        public object Data { get; set; }
        public int MinVersion { get; set; }
        public int MaxVersion { get; set; }
        public bool HasSideEffect { get; set; }

        /// <summary>
        /// Gets or sets a priority value used to disambiguate when more than one method matches the call.
        /// Lower values indicate a better match. Defaults to 1.
        /// </summary>
        public int Priority { get; set; }
    }
}