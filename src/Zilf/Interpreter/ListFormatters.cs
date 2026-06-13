/* Copyright 2010-2026 Tara McGrew
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

namespace Zilf.Interpreter
{
    /// <summary>
    /// Zero-allocation formatter for up to two distinct type names, producing
    /// an English list like <c>"FIX"</c> or <c>"FIX or ATOM"</c>.
    /// </summary>
    internal struct ListFormatter2
    {
        private string? _first;
        private string? _second;
        public int Count { get; private set; }

        public void Add(string value)
        {
            if (Count >= 1 && _first == value)
                return;
            if (Count >= 2 && _second == value)
                return;

            switch (Count)
            {
                case 0:
                    _first = value;
                    break;
                case 1:
                    _second = value;
                    break;
            }
            Count++;
        }

        public readonly override string ToString() => Count switch
        {
            1 => _first!,
            2 => $"{_first} or {_second}",
            _ => string.Empty,
        };
    }

    /// <summary>
    /// General-purpose formatter for an arbitrary number of distinct type names,
    /// producing an English list like <c>"FIX, ATOM, or STRING"</c>.
    /// Uses a <see cref="List{T}"/> internally so it allocates; prefer
    /// <see cref="ListFormatter2"/> when the maximum is two.
    /// </summary>
    internal struct ListFormatterN
    {
        private List<string>? _list;

        public readonly int Count => _list?.Count ?? 0;

        public void Add(string value)
        {
            _list ??= [];
            if (!_list.Contains(value))
                _list.Add(value);
        }

        public readonly override string ToString()
        {
            if (_list == null || _list.Count == 0)
                return string.Empty;

            return _list.Count switch
            {
                1 => _list[0],
                2 => $"{_list[0]} or {_list[1]}",
                _ => $"{string.Join(", ", _list.GetRange(0, _list.Count - 1))}, or {_list[^1]}",
            };
        }
    }
}
