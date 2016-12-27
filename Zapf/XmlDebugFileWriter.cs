/* Copyright 2010-2016 Jesse McGrew
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

using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Zapf
{
    class XmlDebugFileWriter : IDebugFileWriter
    {
        readonly XmlWriter xml;
        bool inRoutine;

        public XmlDebugFileWriter(Stream debugStream)
        {
            var settings = new XmlWriterSettings()
            {
                Indent = true
            };

            xml = XmlWriter.Create(debugStream, settings);
            xml.WriteStartDocument();

            xml.WriteStartElement("inform-story-file");
            xml.WriteAttributeString("content-creator", "ZAPF");

            xml.WriteAttributeString("content-creator-version", Program.VERSION);
        }

        public void Close()
        {
            xml.WriteEndElement();    // inform-story-file
            xml.WriteEndDocument();
            xml.Close();
        }

        public void StartRoutine(LineRef start, int address, string name, IEnumerable<string> locals)
        {
            xml.WriteStartElement("routine");

            xml.WriteElementString("identifier", name);
            xml.WriteElementString("address", address.ToString());
            // TODO: <value> for packed address?

            int index = 1;
            foreach (var local in locals)
            {
                xml.WriteStartElement("local-variable");

                xml.WriteElementString("identifier", local);
                xml.WriteElementString("index", index.ToString());

                xml.WriteEndElement();
                index++;
            }

            // leave element open...
            inRoutine = true;
        }

        public bool InRoutine
        {
            get { return inRoutine; }
        }

        public void WriteLine(LineRef loc, int address)
        {
            xml.WriteStartElement("sequence-point");

            xml.WriteElementString("address", address.ToString());
            WriteSourceCodeLocation(loc, loc);

            xml.WriteEndElement();
        }

        public void EndRoutine(LineRef end, int address)
        {
            // close <routine>
            xml.WriteEndElement();

            inRoutine = false;
        }

        public void WriteAction(ushort number, string name)
        {
            xml.WriteStartElement("action");

            xml.WriteElementString("identifier", name);
            xml.WriteElementString("value", number.ToString());

            xml.WriteEndElement();
        }

        public void WriteArray(ushort offsetFromGlobal, string name)
        {
            xml.WriteStartElement("array");

            xml.WriteElementString("identifier", name);
            //XXX value is (probably) supposed to be an actual address, not offset
            xml.WriteElementString("value", offsetFromGlobal.ToString());
            // TODO: <byte-count>
            // TODO: <bytes-per-element>
            // TODO: <zeroth-element-holds-length>
            // TODO: <source-code-location>

            xml.WriteEndElement();
        }

        public void WriteAttr(ushort number, string name)
        {
            xml.WriteStartElement("attribute");

            xml.WriteElementString("identifier", name);
            xml.WriteElementString("value", number.ToString());

            xml.WriteEndElement();
        }

        public void WriteClass(string name, LineRef start, LineRef end)
        {
            xml.WriteStartElement("object");

            xml.WriteElementString("identifier", name);
            // TODO: <value>

            WriteSourceCodeLocation(start, end);

            xml.WriteEndElement();
        }

        public void WriteFakeAction(ushort number, string name)
        {
            xml.WriteStartElement("fake-action");

            xml.WriteElementString("identifier", name);
            xml.WriteElementString("value", number.ToString());

            xml.WriteEndElement();
        }

        public void WriteFile(byte number, string includeName, string actualName)
        {
            xml.WriteStartElement("source");
            xml.WriteAttributeString("index", number.ToString());

            xml.WriteAttributeString("given-path", includeName);
            xml.WriteAttributeString("resolved-path", actualName);
            // TODO: <language>... it's probably ZIL, but not necessarily

            xml.WriteEndElement();
        }

        public void WriteGlobal(byte number, string name)
        {
            xml.WriteStartElement("global-variable");

            xml.WriteElementString("identifier", name);
            xml.WriteElementString("value", number.ToString());
            // TODO: <address>

            xml.WriteEndElement();
        }

        public void WriteHeader(byte[] header)
        {
            xml.WriteStartElement("story-file-prefix");
            xml.WriteBase64(header, 0, header.Length);
            xml.WriteEndElement();
        }

        public void WriteMap(IEnumerable<KeyValuePair<string, int>> map)
        {
            foreach (var entry in map)
            {
                xml.WriteStartElement("story-file-section");

                xml.WriteElementString("type", entry.Key);
                xml.WriteElementString("address", entry.Value.ToString());
                // TODO: <end-address>

                xml.WriteEndElement();
            }
        }

        public void WriteObject(ushort number, string name, LineRef start, LineRef end)
        {
            xml.WriteStartElement("object");

            xml.WriteElementString("identifier", name);
            xml.WriteElementString("value", number.ToString());

            WriteSourceCodeLocation(start, end);

            xml.WriteEndElement();
        }

        public void WriteProp(ushort number, string name)
        {
            xml.WriteStartElement("property");

            xml.WriteElementString("identifier", name);
            xml.WriteElementString("value", number.ToString());

            xml.WriteEndElement();
        }

        void WriteSourceCodeLocation(LineRef start, LineRef end)
        {
            xml.WriteStartElement("source-code-location");

            xml.WriteElementString("file-index", start.File.ToString());
            xml.WriteElementString("line", start.Line.ToString());
            xml.WriteElementString("character", start.Col.ToString());
            // TODO: <file-position>

            if (end != start)
            {
                // TODO: handle end.File != start.File

                xml.WriteElementString("end-line", end.Line.ToString());
                xml.WriteElementString("end-character", end.Col.ToString());
                // TODO: <end-file-position>
            }

            xml.WriteEndElement();
        }
    }
}
