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

using System;
using System.Collections.Generic;
using System.Text;
using Zilf.Emit;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;

namespace Zilf.ZModel.Vocab.Glulx
{
    sealed class GlulxParserWord(ZilAtom atom) : IWord
    {
        public PartOfSpeech PartOfSpeech;
        public PartOfSpeech SynonymTypes;

        // these are the only parts of speech whose values actually matter,
        // so instead of porting the original convoluted algorithm to figure
        // out which values go in which slots, we just give them each a slot
        int directionValue;
        int prepositionValue;
        int verbValue;

        readonly Dictionary<PartOfSpeech, ISourceLine> definitions = new(2);

        public ZilAtom Atom { get; } = atom ?? throw new ArgumentNullException(nameof(atom));

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append('"');
            sb.Append(Atom);
            sb.Append('"');

            if ((PartOfSpeech & PartOfSpeech.Adjective) != 0)
            {
                sb.Append("|ADJ");
            }
            if ((PartOfSpeech & PartOfSpeech.Buzzword) != 0)
            {
                sb.Append("|BUZZ");
            }
            if ((PartOfSpeech & PartOfSpeech.Direction) != 0)
            {
                sb.Append("|DIR=");
                sb.Append(directionValue);
            }
            if ((PartOfSpeech & PartOfSpeech.Object) != 0)
            {
                sb.Append("|OBJ=");
            }
            if ((PartOfSpeech & PartOfSpeech.Preposition) != 0)
            {
                sb.Append("|PREP=");
                sb.Append(prepositionValue);
            }
            if ((PartOfSpeech & PartOfSpeech.Verb) != 0)
            {
                sb.Append("|VERB=");
                sb.Append(verbValue);
            }

            return sb.ToString();
        }

        string ListDefinitionLocations()
        {
            var sb = new StringBuilder();

            foreach (var (word, sourceLine) in definitions)
            {
                if (sb.Length != 0)
                {
                    sb.Append(", ");
                }

                sb.Append(word);
                sb.Append(" (");
                sb.Append(sourceLine.SourceInfo);
                sb.Append(')');
            }

            return sb.ToString();
        }

        public void SetObject(ISourceLine location)
        {
            if ((PartOfSpeech & PartOfSpeech.Object) == 0)
            {
                PartOfSpeech |= PartOfSpeech.Object;
                definitions[PartOfSpeech.Object] = location;
            }
        }

        public void SetVerb(Context ctx, ISourceLine location, int value)
        {
            if ((PartOfSpeech & PartOfSpeech.Verb) == 0)
            {
                PartOfSpeech |= PartOfSpeech.Verb;
                verbValue = value;
                definitions[PartOfSpeech.Verb] = location;
            }
        }

        public void SetAdjective(Context ctx, ISourceLine location, int value)
        {
            if ((PartOfSpeech & PartOfSpeech.Adjective) == 0)
            {
                PartOfSpeech |= PartOfSpeech.Adjective;
                definitions[PartOfSpeech.Adjective] = location;
            }
        }

        public void SetDirection(Context ctx, ISourceLine location, int value)
        {
            if ((PartOfSpeech & PartOfSpeech.Direction) == 0)
            {
                PartOfSpeech |= PartOfSpeech.Direction;
                directionValue = value;
                definitions[PartOfSpeech.Direction] = location;
            }
        }

        public void SetBuzzword(Context ctx, ISourceLine location, int value)
        {
            if ((PartOfSpeech & PartOfSpeech.Buzzword) == 0)
            {
                PartOfSpeech |= PartOfSpeech.Buzzword;
                definitions[PartOfSpeech.Buzzword] = location;
            }
        }

        public void SetPreposition(Context ctx, ISourceLine location, int value)
        {
            if ((PartOfSpeech & PartOfSpeech.Preposition) == 0)
            {
                PartOfSpeech |= PartOfSpeech.Preposition;
                prepositionValue = value;
                definitions[PartOfSpeech.Preposition] = location;
            }
        }

        void UnsetPartOfSpeech(Context ctx, PartOfSpeech part)
        {
            PartOfSpeech &= ~part;
            definitions.Remove(part);

            switch (part)
            {
                case PartOfSpeech.Direction:
                    directionValue = 0;
                    break;

                case PartOfSpeech.Preposition:
                    prepositionValue = 0;
                    break;

                case PartOfSpeech.Verb:
                    verbValue = 0;
                    break;
            }
        }

        public int GetValue(PartOfSpeech part)
        {
            return part switch
            {
                PartOfSpeech.Direction => directionValue,
                PartOfSpeech.Preposition => prepositionValue,
                PartOfSpeech.Verb => verbValue,
                PartOfSpeech.Adjective or PartOfSpeech.Buzzword or PartOfSpeech.Object => 0,
                _ => throw new ArgumentOutOfRangeException(nameof(part), "Unexpected part of speech: " + part),
            };
        }

        public ISourceLine GetDefinition(PartOfSpeech part) => definitions[part];

        public void WriteToBuilder(Context ctx, IWordBuilder wb, DirIndexToPropertyOperandDelegate dirIndexToPropertyOperand)
        {
            var pos = PartOfSpeech;

            // write part of speech flags
            wb.AddByte((byte)pos);

            // write values
            if (PartOfSpeech.HasFlag(PartOfSpeech.Direction))
                wb.AddWord(dirIndexToPropertyOperand(directionValue));
            else
                wb.AddWord(directionValue);
            wb.AddWord(prepositionValue);
            wb.AddWord(verbValue);
        }

        public void MarkAsSynonym(PartOfSpeech synonymTypes)
        {
            SynonymTypes |= synonymTypes;
        }

        public void Merge(Context ctx, GlulxParserWord other)
        {
            if ((other.PartOfSpeech & PartOfSpeech.Adjective) != 0)
            {
                UnsetPartOfSpeech(ctx, PartOfSpeech.Adjective);
                SetAdjective(ctx, other.GetDefinition(PartOfSpeech.Adjective), other.GetValue(PartOfSpeech.Adjective));
            }

            if ((other.PartOfSpeech & PartOfSpeech.Buzzword) != 0)
            {
                UnsetPartOfSpeech(ctx, PartOfSpeech.Buzzword);
                SetBuzzword(ctx, other.GetDefinition(PartOfSpeech.Buzzword), other.GetValue(PartOfSpeech.Buzzword));
            }

            if ((other.PartOfSpeech & PartOfSpeech.Direction) != 0)
            {
                UnsetPartOfSpeech(ctx, PartOfSpeech.Direction);
                SetDirection(ctx, other.GetDefinition(PartOfSpeech.Direction), other.GetValue(PartOfSpeech.Direction));
            }

            if ((other.PartOfSpeech & PartOfSpeech.Object) != 0)
            {
                UnsetPartOfSpeech(ctx, PartOfSpeech.Object);
                SetObject(other.GetDefinition(PartOfSpeech.Object));
            }

            if ((other.PartOfSpeech & PartOfSpeech.Preposition) != 0)
            {
                UnsetPartOfSpeech(ctx, PartOfSpeech.Preposition);
                SetPreposition(ctx, other.GetDefinition(PartOfSpeech.Preposition), other.GetValue(PartOfSpeech.Preposition));
            }

            if ((other.PartOfSpeech & PartOfSpeech.Verb) != 0)
            {
                UnsetPartOfSpeech(ctx, PartOfSpeech.Verb);
                SetVerb(ctx, other.GetDefinition(PartOfSpeech.Verb), other.GetValue(PartOfSpeech.Verb));
            }

            MarkAsSynonym(other.PartOfSpeech & ~PartOfSpeech.FirstMask);
        }
    }
}