/* Copyright 2010-2023 Tara McGrew
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
    /// <summary>
    /// Represents an edge in a suffix tree, connecting one node to another with a labeled substring.
    /// </summary>
    /// <typeparam name="T">The type of data associated with each string in the suffix tree.</typeparam>
    internal sealed class Edge<T> : IEdge<T>
    {
        /// <summary>
        /// Gets or sets the substring label for this edge.
        /// </summary>
        public ReadOnlyMemory<char> Label { get; set; }

        /// <summary>
        /// Gets or sets the destination node that this edge points to.
        /// </summary>
        public Node<T> Dest { get; set; }

        INode<T> IEdge<T>.Dest => Dest;

        /// <summary>
        /// Initializes a new instance of the <see cref="Edge{T}"/> class.
        /// </summary>
        /// <param name="label">The substring label for this edge.</param>
        /// <param name="dest">The destination node that this edge points to.</param>
        public Edge(ReadOnlyMemory<char> label, Node<T> dest)
        {
            this.Label = label;
            this.Dest = dest;
        }
    }

    /// <summary>
    /// Represents a read-only view of an edge in a suffix tree.
    /// </summary>
    /// <typeparam name="T">The type of data associated with each string in the suffix tree.</typeparam>
    public interface IEdge<T>
    {
        /// <summary>
        /// Gets the substring label for this edge.
        /// </summary>
        ReadOnlyMemory<char> Label { get; }

        /// <summary>
        /// Gets the destination node that this edge points to.
        /// </summary>
        INode<T> Dest { get; }
    }
}
