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

        private static IReplSession CreateSession() => new FrontEnd { FileSystem = NullFileSystem.Instance }.StartRepl();

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
