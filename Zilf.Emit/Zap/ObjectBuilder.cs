using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Zilf.Emit.Zap
{
    class ObjectBuilder : IObjectBuilder
    {
        const string INDENT = "\t";

        struct PropertyEntry
        {
            public const byte BYTE = 0;
            public const byte WORD = 1;
            public const byte TABLE = 2;

            public readonly PropertyBuilder Property;
            public readonly IOperand Value;
            public readonly byte Kind;

            public PropertyEntry(PropertyBuilder prop, IOperand value, byte kind)
            {
                this.Property = prop;
                this.Value = value;
                this.Kind = kind;
            }
        }

        private readonly GameBuilder game;
        readonly int number;
        readonly string name;
        readonly List<PropertyEntry> props = new List<PropertyEntry>();
        readonly List<FlagBuilder> flags = new List<FlagBuilder>();

        string descriptiveName = "";
        IObjectBuilder parent, child, sibling;

        public ObjectBuilder(GameBuilder game, int number, string name)
        {
            this.game = game;
            this.number = number;
            this.name = name;
        }

        public string SymbolicName
        {
            get { return name; }
        }

        public int Number
        {
            get { return number; }
        }

        public string DescriptiveName
        {
            get { return descriptiveName; }
            set { descriptiveName = value; }
        }

        public IObjectBuilder Parent
        {
            get { return parent; }
            set { parent = value; }
        }

        public IObjectBuilder Child
        {
            get { return child; }
            set { child = value; }
        }

        public IObjectBuilder Sibling
        {
            get { return sibling; }
            set { sibling = value; }
        }

        public string Flags1
        {
            get { return GetFlagsString(0); }
        }

        public string Flags2
        {
            get { return GetFlagsString(16); }
        }

        public string Flags3
        {
            get { return GetFlagsString(32); }
        }

        string GetFlagsString(int start)
        {
            StringBuilder sb = new StringBuilder();

            foreach (FlagBuilder flag in flags)
            {
                int num = flag.Number;
                if (num >= start && num < start + 16)
                {
                    if (sb.Length > 0)
                        sb.Append('+');

                    sb.Append("FX?");
                    sb.Append(flag.ToString());
                }
            }

            if (sb.Length == 0)
                return "0";

            return sb.ToString();
        }

        public void AddByteProperty(IPropertyBuilder prop, IOperand value)
        {
            PropertyEntry pe = new PropertyEntry((PropertyBuilder)prop, value, PropertyEntry.BYTE);
            props.Add(pe);
        }

        public void AddWordProperty(IPropertyBuilder prop, IOperand value)
        {
            PropertyEntry pe = new PropertyEntry((PropertyBuilder)prop, value, PropertyEntry.WORD);
            props.Add(pe);
        }

        public ITableBuilder AddComplexProperty(IPropertyBuilder prop)
        {
            TableBuilder data = new TableBuilder(string.Format("?{0}?CP?{1}", this, prop));
            PropertyEntry pe = new PropertyEntry((PropertyBuilder)prop, data, PropertyEntry.TABLE);
            props.Add(pe);
            return data;
        }

        public void AddFlag(IFlagBuilder flag)
        {
            FlagBuilder fb = (FlagBuilder)flag;
            if (!flags.Contains(fb))
                flags.Add(fb);
        }

        internal void WriteProperties(TextWriter writer)
        {
            writer.WriteLine(INDENT + ".STRL \"{0}\"", GameBuilder.SanitizeString(descriptiveName));

            props.Sort((a, b) => b.Property.Number.CompareTo(a.Property.Number));

            for (int i = 0; i < props.Count; i++)
            {
                PropertyEntry pe = props[i];
                if (pe.Kind == PropertyEntry.BYTE)
                {
                    writer.WriteLine(INDENT + ".PROP 1,{0}", pe.Property);
                    writer.WriteLine(INDENT + ".BYTE {0}", pe.Value);
                }
                else if (pe.Kind == PropertyEntry.WORD)
                {
                    writer.WriteLine(INDENT + ".PROP 2,{0}", pe.Property);
                    writer.WriteLine(INDENT + ".WORD {0}", pe.Value);
                }
                else // TABLE
                {
                    TableBuilder tb = (TableBuilder)pe.Value;
                    writer.WriteLine(INDENT + ".PROP {0},{1}", tb.Size, pe.Property);
                    tb.WriteTo(writer);
                }
            }

            writer.WriteLine(INDENT + ".BYTE 0");
        }

        public override string ToString()
        {
            return name;
        }
    }
}