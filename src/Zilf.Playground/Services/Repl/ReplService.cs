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
using System.Collections.Generic;
using Zilf.Common;
using Zilf.Compiler;

namespace Zilf.Playground.Services.Repl
{
    record ReplExchange(string Input, string Output, bool IsError = false);

    class ReplService
    {
        private readonly List<ReplExchange> _exchanges = new();

        public IReplSession Session { get; private set; } = CreateSession();
        public IReadOnlyList<ReplExchange> Exchanges => _exchanges;

        public event Action ExchangesChanged;

        private static IReplSession CreateSession()
        {
            var frontEnd = new FrontEnd { FileSystem = NullFileSystem.Instance };
            var session = frontEnd.StartRepl();
            session.Quittable = false;
            return session;
        }

        public void RestartSession()
        {
            _exchanges.Clear();
            Session = CreateSession();
            ExchangesChanged?.Invoke();
        }

        public void Execute(string input)
        {
            var output = Session.Evaluate(input);

            if (output != null)
            {
                // evaluation result
                _exchanges.Add(new ReplExchange(input, output));
            }
            else
            {
                var errorMessage = Session.ReadDiagnostics();
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    // error message
                    _exchanges.Add(new ReplExchange(input, errorMessage, true));
                }
                else
                {
                    // empty evaluation
                    _exchanges.Add(new ReplExchange(input, ""));
                }
            }

            ExchangesChanged?.Invoke();
        }
    }
}
