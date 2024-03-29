/* Copyright 2010-2024 Tara McGrew
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

using System;
using System.Collections.Generic;

namespace ZilfSourceGenerators
{
    internal sealed class IndentedWriter
    {
        private readonly List<string> lines = [];

        public int Indent { get; set; } = 0;

        public IndentedWriter WriteLine(string line)
        {
            lines.Add(new string(' ', Indent * 4) + line);
            return this;
        }

        public IndentedWriter WriteLine()
        {
            lines.Add(string.Empty);
            return this;
        }

        public BlockCloser Block(string line)
        {
            if (!string.IsNullOrEmpty(line))
            {
                WriteLine(line);
            }

            WriteLine("{");
            Indent++;
            return new BlockCloser(this);
        }

        public EquatableArray<string> GetLines() => lines.ToEquatableArray();

        public readonly struct BlockCloser(IndentedWriter writer) : IDisposable
        {
            public readonly void Dispose()
            {
                writer.Indent--;
                writer.WriteLine("}");
            }
        }
    }
}
