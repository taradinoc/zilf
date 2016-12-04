/* Copyright 2010, 2016 Jesse McGrew
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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zilf.Diagnostics
{
    public class DefaultDiagnosticFormatter : IDiagnosticFormatter
    {
        public string Format(Diagnostic diagnostic)
        {
            string severity;

            switch (diagnostic.Severity)
            {
                case Severity.Error:
                    severity = "error";
                    break;

                case Severity.Info:
                    severity = "info";
                    break;

                case Severity.Warning:
                    severity = "warning";
                    break;

                default:
                    throw new NotImplementedException();
            }

            var sb = new StringBuilder(80);

            sb.Append('[');
            sb.Append(severity);

            //if (diagnostic.Code != 0)
            {
                sb.Append(' ');
                sb.Append(diagnostic.CodePrefix);
                sb.Append(diagnostic.Code.ToString("0000"));
            }

            sb.Append("] ");
            sb.Append(diagnostic.Location.SourceInfo);
            sb.Append(": ");
            sb.AppendFormat(diagnostic.MessageFormat, diagnostic.MessageArgs);

            if (diagnostic.StackTrace != null)
            {
                sb.Append('\n');
                sb.Append(diagnostic.StackTrace);
            }

            return sb.ToString();
        }
    }
}
