/* Copyright 2010-2020 Jesse McGrew
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

namespace Zilf.Common.StringEncoding.SuffixTrees
{
    internal sealed class Edge<T> : IEdge<T>
    {
        public ReadOnlyMemory<char> Label { get; set; }
        public Node<T> Dest { get; set; }

        INode<T> IEdge<T>.Dest => Dest;

        public Edge(ReadOnlyMemory<char> label, Node<T> dest)
        {
            this.Label = label;
            this.Dest = dest;
        }
    }

    public interface IEdge<T>
    {
        ReadOnlyMemory<char> Label { get; }
        INode<T> Dest { get; }
    }
}
