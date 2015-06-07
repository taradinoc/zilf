using System;

namespace Zilf.Language
{
    [AttributeUsage(AttributeTargets.Field)]
    class AtomAttribute : Attribute
    {
        private readonly string name;

        public AtomAttribute(string name)
        {
            this.name = name;
        }

        public string Name
        {
            get { return name; }
        }
    }
}