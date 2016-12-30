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

using System.Diagnostics.Contracts;
using System.Runtime.Serialization;
using Zilf.Interpreter.Values;

namespace Zilf.Interpreter
{
    /// <summary>
    /// Indicates that an inner block is repeating.
    /// </summary>
    class AgainException : ControlException
    {
        readonly ZilActivation activation;

        public AgainException(ZilActivation activation)
            : base("AGAIN")
        {
            Contract.Requires(activation != null);

            this.activation = activation;
        }

        protected AgainException(SerializationInfo si, StreamingContext sc)
            : base(si, sc)
        {
        }

        public ZilActivation Activation
        {
            get { return activation; }
        }
    }
}
