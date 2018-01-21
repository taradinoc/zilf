/* Copyright 2010-2018 Jesse McGrew
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

using JetBrains.Annotations;

namespace Zilf.Language.Signatures
{
    interface ISignatureVisitor
    {
        void Visit([NotNull] AdeclPart part);
        void Visit([NotNull] AlternativesPart part);
        void Visit([NotNull] AnyPart part);
        void Visit([NotNull] ConstrainedPart part);
        void Visit([NotNull] FormPart part);
        void Visit([NotNull] ListPart part);
        void Visit([NotNull] LiteralPart part);
        void Visit([NotNull] OptionalPart part);
        void Visit([NotNull] QuotedPart part);
        void Visit([NotNull] SequencePart part);
        void Visit([NotNull] VarArgsPart part);
    }
}
