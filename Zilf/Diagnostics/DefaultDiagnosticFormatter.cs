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
            var sb = new StringBuilder(80);

            sb.Append($"[{diagnostic.Severity} {diagnostic.CodePrefix}{diagnostic.Code.ToString("0000")}] {diagnostic.Location.SourceInfo}: ");
            sb.AppendFormat(diagnostic.MessageFormat, diagnostic.MessageArgs);

            foreach (var sd in diagnostic.SubDiagnostics)
            {
                sb.Append($"\n  [{sd.Severity} {sd.CodePrefix}{sd.Code.ToString("0000")}] ");
                sb.AppendFormat(sd.MessageFormat, sd.MessageArgs);
            }

            if (diagnostic.StackTrace != null)
            {
                sb.Append('\n');
                sb.Append(diagnostic.StackTrace);
            }

            return sb.ToString();
        }
    }
}
