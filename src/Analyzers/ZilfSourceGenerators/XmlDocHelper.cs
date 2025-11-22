/* Copyright 2010-2025 Tara McGrew
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

using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace ZilfSourceGenerators
{
    /// <summary>
    /// Helper for extracting structured information from XML documentation comments.
    /// </summary>
    internal static class XmlDocHelper
    {
        /// <summary>
        /// Extracts the text content of the &lt;summary&gt; element from a symbol's XML documentation.
        /// Parses directly from syntax tree trivia to work in source generators without GenerateDocumentationFile.
        /// </summary>
        /// <param name="symbol">The symbol to extract documentation from.</param>
        /// <returns>The summary text, or null if not found.</returns>
        public static string? ExtractSummary(ISymbol symbol)
        {
            var xml = GetDocumentationXml(symbol);
            if (string.IsNullOrWhiteSpace(xml))
                return null;

            try
            {
                var doc = XDocument.Parse(xml);
                var summaryElement = doc.Root?.Element("summary");
                if (summaryElement == null)
                    return null;

                return NormalizeWhitespace(summaryElement.Value);
            }
            catch (Exception)
            {
                // If XML parsing fails, return null
                return null;
            }
        }

        /// <summary>
        /// Extracts all &lt;param&gt; element summaries from a method's XML documentation.
        /// Parses directly from syntax tree trivia to work in source generators without GenerateDocumentationFile.
        /// </summary>
        /// <param name="methodSymbol">The method symbol to extract parameter documentation from.</param>
        /// <returns>A dictionary mapping parameter names to their summary text.</returns>
        public static Dictionary<string, string> ExtractParamSummaries(IMethodSymbol methodSymbol)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);

            var xml = GetDocumentationXml(methodSymbol);
            if (string.IsNullOrWhiteSpace(xml))
                return result;

            try
            {
                var doc = XDocument.Parse(xml);
                var paramElements = doc.Root?.Elements("param");
                if (paramElements == null)
                    return result;

                foreach (var paramElement in paramElements)
                {
                    var nameAttr = paramElement.Attribute("name");
                    if (nameAttr == null)
                        continue;

                    var paramName = nameAttr.Value;
                    var summary = NormalizeWhitespace(paramElement.Value);

                    if (!string.IsNullOrWhiteSpace(summary))
                    {
                        result[paramName] = summary;
                    }
                }
            }
            catch (Exception)
            {
                // If XML parsing fails, return empty dictionary
            }

            return result;
        }

        /// <summary>
    /// Extracts XML documentation from a symbol.
        /// </summary>
        private static string? GetDocumentationXml(ISymbol symbol)
        {
            return symbol.GetDocumentationCommentXml();
        }

        /// <summary>
        /// Normalizes whitespace in XML element content by collapsing multiple spaces/newlines into single spaces
        /// and trimming leading/trailing whitespace.
        /// </summary>
        private static string NormalizeWhitespace(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // Split by whitespace, filter out empty entries, and rejoin with single spaces
            var parts = text.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" ", parts);
        }
    }
}
