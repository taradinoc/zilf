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
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.Diagnostics;

namespace Zilf.ZModel.Vocab.OldParser
{
    class OldParserWord : IWord
    {
        readonly ZilAtom atom;
        public PartOfSpeech PartOfSpeech;
        public PartOfSpeech SynonymTypes;

        readonly Dictionary<PartOfSpeech, byte> speechValues = new Dictionary<PartOfSpeech, byte>(2);
        readonly Dictionary<PartOfSpeech, ISourceLine> definitions = new Dictionary<PartOfSpeech, ISourceLine>(2);

        public OldParserWord(ZilAtom atom)
        {
            if (atom == null)
                throw new ArgumentNullException(nameof(atom));

            this.atom = atom;
        }

        public ZilAtom Atom
        {
            get { return atom; }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append('"');
            sb.Append(Atom.ToString());
            sb.Append("\"");

            switch (PartOfSpeech & PartOfSpeech.FirstMask)
            {
                case PartOfSpeech.AdjectiveFirst:
                    sb.Append(" ADJ1");
                    break;
                case PartOfSpeech.DirectionFirst:
                    sb.Append(" DIR1");
                    break;
                case PartOfSpeech.VerbFirst:
                    sb.Append(" VERB1");
                    break;
                default: //case PartOfSpeech.ObjectFirst:
                    sb.Append(" OBJ1");
                    break;
            }

            if ((PartOfSpeech & PartOfSpeech.Adjective) != 0)
            {
                sb.Append("|ADJ=");
                sb.Append(speechValues[PartOfSpeech.Adjective]);
            }
            if ((PartOfSpeech & PartOfSpeech.Buzzword) != 0)
            {
                sb.Append("|BUZZ");
            }
            if ((PartOfSpeech & PartOfSpeech.Direction) != 0)
            {
                sb.Append("|DIR=");
                sb.Append(speechValues[PartOfSpeech.Direction]);
            }
            if ((PartOfSpeech & PartOfSpeech.Object) != 0)
            {
                sb.Append("|OBJ=");
                sb.Append(speechValues[PartOfSpeech.Object]);
            }
            if ((PartOfSpeech & PartOfSpeech.Preposition) != 0)
            {
                sb.Append("|PREP=");
                sb.Append(speechValues[PartOfSpeech.Preposition]);
            }
            if ((PartOfSpeech & PartOfSpeech.Verb) != 0)
            {
                sb.Append("|VERB=");
                sb.Append(speechValues[PartOfSpeech.Verb]);
            }

            return sb.ToString();
        }

        bool IsNewVoc(Context ctx)
        {
            Contract.Requires(ctx != null);
            return ctx.GetGlobalOption(StdAtom.NEW_VOC_P);
        }

        bool IsCompactVocab(Context ctx)
        {
            Contract.Requires(ctx != null);
            return ctx.GetGlobalOption(StdAtom.COMPACT_VOCABULARY_P);
        }

        /// <summary>
        /// Checks whether adding a new part of speech should set the relevant First flag.
        /// </summary>
        /// <param name="ctx">The current context.</param>
        /// <returns>true if the new part of speech should set the First flag.</returns>
        bool ShouldSetFirst(Context ctx)
        {
            Contract.Requires(ctx != null);

            // if no parts of speech are set yet, this is easy
            if (this.PartOfSpeech == PartOfSpeech.None)
                return true;

            // never add First flags to buzzwords
            if ((this.PartOfSpeech & PartOfSpeech.Buzzword) != 0)
                return false;

            // ignore parts of speech that don't record values in the current context
            var pos = this.PartOfSpeech;
            if (ctx.ZEnvironment.ZVersion >= 4)
                pos &= ~PartOfSpeech.Adjective;
            if (IsNewVoc(ctx))
                pos &= ~PartOfSpeech.Object;
            if (IsCompactVocab(ctx))
                pos &= ~(PartOfSpeech.Object | PartOfSpeech.Preposition);

            return pos == PartOfSpeech.None;
        }

        /// <summary>
        /// Check whether the word has too many parts of speech defined, and
        /// if so, issue a warning and discard the extras.
        /// </summary>
        /// <param name="ctx">The current context.</param>
        /// <remarks>
        /// Generally, a vocabulary word is limited to no more than two parts
        /// of speech. However, in V4+, <see cref="PartOfSpeech.Adjective"/> does
        /// not count against this limit; and in any version when the global
        /// atom NEW-VOC? is true, <see cref="PartOfSpeech.Object"/> does not
        /// count against this limit. If the global atom COMPACT-VOCABULARY?
        /// is true (which should happen on V4+ only), the limit is one instead of
        /// two, and <see cref="PartOfSpeech.Preposition"/> and
        /// <see cref="PartOfSpeech.Buzzword"/> also don't count toward it.
        /// </remarks>
        void CheckTooMany(Context ctx)
        {
            Contract.Requires(ctx != null);

            var b = (byte)(PartOfSpeech & ~PartOfSpeech.FirstMask);
            byte count = 0;

            while (b != 0)
            {
                b &= (byte)(b - 1);
                count++;
            }

            bool freeObject = false, freeAdjective = false, freePrep = false, freeBuzz = false;

            // when ,NEW-VOC? or ,COMPACT-VOCABULARY? are true, Object is free
            var newVoc = IsNewVoc(ctx);
            var compactVocab = IsCompactVocab(ctx);
            if ((PartOfSpeech & PartOfSpeech.Object) != 0)
            {
                if (newVoc || compactVocab)
                {
                    freeObject = true;
                    count--;
                }
            }

            // when ,COMPACT-VOCABULARY? is true, Preposition is free
            if ((PartOfSpeech & PartOfSpeech.Preposition) != 0)
            {
                if (compactVocab)
                {
                    freePrep = true;
                    count--;
                }
            }

            // when ,COMPACT-VOCABULARY? is true, Buzzword is free
            if ((PartOfSpeech & PartOfSpeech.Buzzword) != 0)
            {
                if (compactVocab)
                {
                    freeBuzz = true;
                    count--;
                }
            }

            // Adjective is always free in V4+
            if ((PartOfSpeech & PartOfSpeech.Adjective) != 0)
            {
                if (ctx.ZEnvironment.ZVersion > 3)
                {
                    freeAdjective = true;
                    count--;
                }
            }

            int limit = compactVocab ? 1 : 2;
            if (count > limit)
            {
                ctx.HandleWarning(new CompilerError(CompilerMessages.Too_Many_Parts_Of_Speech_For_0_1, Atom, ListDefinitionLocations()));

                /* The order we trim is mostly arbitrary, except that adjective and object are first
                * since they can sometimes be recognized without the part-of-speech flags. */
                var partsToTrim = new[] {
                    new { part = PartOfSpeech.Adjective, free = freeAdjective },
                    new { part = PartOfSpeech.Object, free = freeObject },
                    new { part = PartOfSpeech.Buzzword, free = freeBuzz },
                    new { part = PartOfSpeech.Preposition, free = freePrep },
                    new { part = PartOfSpeech.Verb, free = false },
                    new { part = PartOfSpeech.Direction, free = false }
                };

                foreach (var trim in partsToTrim)
                {
                    if ((PartOfSpeech & trim.part) != 0 && !trim.free)
                    {
                        ctx.HandleWarning(new CompilerError(
                            GetDefinition(trim.part),
                            CompilerMessages.Discarding_The_0_Part_Of_Speech_For_1,
                            trim.part,
                            Atom));

                        UnsetPartOfSpeech(ctx, trim.part);

                        count--;
                        if (count <= 2)
                            break;
                    }
                }
            }
        }

        string ListDefinitionLocations()
        {
            var sb = new StringBuilder();

            foreach (var pair in definitions)
            {
                if (sb.Length != 0)
                {
                    sb.Append(", ");
                }

                sb.Append(pair.Key.ToString());
                sb.Append(" (");
                sb.Append(pair.Value.SourceInfo);
                sb.Append(")");
            }

            return sb.ToString();
        }

        public void SetObject(Context ctx, ISourceLine location)
        {
            Contract.Requires(ctx != null);
            Contract.Ensures((PartOfSpeech & PartOfSpeech.Object) != 0);

            if ((PartOfSpeech & PartOfSpeech.Object) == 0)
            {
                // there is no PartOfSpeech.ObjectFirst, so don't change the First flags

                PartOfSpeech |= PartOfSpeech.Object;
                speechValues[PartOfSpeech.Object] = 1;
                definitions[PartOfSpeech.Object] = location;
            }
        }

        public void SetVerb(Context ctx, ISourceLine location, byte value)
        {
            Contract.Requires(ctx != null);
            Contract.Ensures((PartOfSpeech & PartOfSpeech.Verb) != 0);

            if ((PartOfSpeech & PartOfSpeech.Verb) == 0)
            {
                if (ShouldSetFirst(ctx))
                    PartOfSpeech |= PartOfSpeech.VerbFirst;

                PartOfSpeech |= PartOfSpeech.Verb;
                speechValues[PartOfSpeech.Verb] = value;
                definitions[PartOfSpeech.Verb] = location;
            }
        }

        public void SetAdjective(Context ctx, ISourceLine location, byte value)
        {
            Contract.Requires(ctx != null);
            Contract.Ensures((PartOfSpeech & PartOfSpeech.Adjective) != 0);

            if ((PartOfSpeech & PartOfSpeech.Adjective) == 0)
            {
                if (ctx.ZEnvironment.ZVersion < 4 && ShouldSetFirst(ctx))
                    PartOfSpeech |= PartOfSpeech.AdjectiveFirst;

                PartOfSpeech |= PartOfSpeech.Adjective;
                speechValues[PartOfSpeech.Adjective] = value;
                definitions[PartOfSpeech.Adjective] = location;
            }
        }

        public void SetDirection(Context ctx, ISourceLine location, byte value)
        {
            Contract.Requires(ctx != null);
            Contract.Ensures((PartOfSpeech & PartOfSpeech.Direction) != 0);

            if ((PartOfSpeech & PartOfSpeech.Direction) == 0)
            {
                if (ShouldSetFirst(ctx))
                    PartOfSpeech |= PartOfSpeech.DirectionFirst;

                PartOfSpeech |= PartOfSpeech.Direction;
                speechValues[PartOfSpeech.Direction] = value;
                definitions[PartOfSpeech.Direction] = location;
            }
        }

        public void SetBuzzword(Context ctx, ISourceLine location, byte value)
        {
            Contract.Requires(ctx != null);
            Contract.Ensures((PartOfSpeech & PartOfSpeech.Buzzword) != 0);

            if ((PartOfSpeech & PartOfSpeech.Buzzword) == 0)
            {
                // buzzword value comes before everything but preposition, except in CompactVocab
                if (!IsCompactVocab(ctx))
                    PartOfSpeech &= ~PartOfSpeech.FirstMask;

                PartOfSpeech |= PartOfSpeech.Buzzword;
                speechValues[PartOfSpeech.Buzzword] = value;
                definitions[PartOfSpeech.Buzzword] = location;
            }
        }

        public void SetPreposition(Context ctx, ISourceLine location, byte value)
        {
            Contract.Requires(ctx != null);
            Contract.Ensures((PartOfSpeech & PartOfSpeech.Preposition) != 0);

            if ((PartOfSpeech & PartOfSpeech.Preposition) == 0)
            {
                // preposition value is always first, except in CompactVocab
                if (!IsCompactVocab(ctx))
                    PartOfSpeech &= ~PartOfSpeech.FirstMask;

                PartOfSpeech |= PartOfSpeech.Preposition;
                speechValues[PartOfSpeech.Preposition] = value;
                definitions[PartOfSpeech.Preposition] = location;
            }
        }

        void UnsetPartOfSpeech(Context ctx, PartOfSpeech part)
        {
            Contract.Requires(ctx != null);
            Contract.Ensures((PartOfSpeech & part) == 0);

            var query = from pair in speechValues
                        where pair.Key != part
                        orderby pair.Key
                        select new { part = pair.Key, value = pair.Value, location = definitions[pair.Key] };
            var remainingParts = query.ToArray();

            PartOfSpeech = 0;
            speechValues.Clear();
            definitions.Clear();

            foreach (var p in remainingParts)
            {
                switch (p.part)
                {
                    case PartOfSpeech.Verb:
                        SetVerb(ctx, p.location, p.value);
                        break;
                    case PartOfSpeech.Adjective:
                        SetAdjective(ctx, p.location, p.value);
                        break;
                    case PartOfSpeech.Direction:
                        SetDirection(ctx, p.location, p.value);
                        break;
                    case PartOfSpeech.Buzzword:
                        SetBuzzword(ctx, p.location, p.value);
                        break;
                    case PartOfSpeech.Preposition:
                        SetPreposition(ctx, p.location, p.value);
                        break;
                    case PartOfSpeech.Object:
                        SetObject(ctx, p.location);
                        break;
                    default:
                        throw new NotImplementedException("Unexpected part of speech: " + p);
                }
            }
        }

        public byte GetValue(PartOfSpeech part)
        {
            switch (part)
            {
                case PartOfSpeech.Verb:
                case PartOfSpeech.Adjective:
                case PartOfSpeech.Direction:
                case PartOfSpeech.Buzzword:
                case PartOfSpeech.Preposition:
                case PartOfSpeech.Object:
                    return speechValues[part];

                default:
                    throw new ArgumentOutOfRangeException("Unexpected part of speech: " + part);
            }
        }

        public ISourceLine GetDefinition(PartOfSpeech part)
        {
            return definitions[part];
        }

        public void WriteToBuilder(Context ctx, Emit.IWordBuilder wb, Func<byte, Emit.IOperand> dirIndexToPropertyOperand)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(wb != null);
            Contract.Requires(dirIndexToPropertyOperand != null);

            // discard excess parts of speech if needed
            CheckTooMany(ctx);

            var pos = this.PartOfSpeech;
            var partsToWrite = new List<PartOfSpeech>(2);
            var compactVocab = IsCompactVocab(ctx);

            // expand parts of speech, observing the First flags and a few special cases
            if ((pos & PartOfSpeech.Adjective) != 0)
            {
                // in V4+, don't write a value for Adjective
                if (ctx.ZEnvironment.ZVersion < 4)
                {
                    if ((pos & PartOfSpeech.FirstMask) == PartOfSpeech.AdjectiveFirst)
                        partsToWrite.Insert(0, PartOfSpeech.Adjective);
                    else
                        partsToWrite.Add(PartOfSpeech.Adjective);
                }
            }
            if ((pos & PartOfSpeech.Direction) != 0)
            {
                if ((pos & PartOfSpeech.FirstMask) == PartOfSpeech.DirectionFirst)
                    partsToWrite.Insert(0, PartOfSpeech.Direction);
                else
                    partsToWrite.Add(PartOfSpeech.Direction);
            }
            if ((pos & PartOfSpeech.Verb) != 0)
            {
                if ((pos & PartOfSpeech.FirstMask) == PartOfSpeech.VerbFirst)
                    partsToWrite.Insert(0, PartOfSpeech.Verb);
                else
                    partsToWrite.Add(PartOfSpeech.Verb);
            }
            if ((pos & PartOfSpeech.Object) != 0)
            {
                // for CompactVocab and NewVoc, don't write a value for Object
                if (!compactVocab && !IsNewVoc(ctx))
                {
                    // there is no ObjectFirst, so keep it first if all other First flags are clear
                    if ((pos & PartOfSpeech.FirstMask) == 0)
                        partsToWrite.Insert(0, PartOfSpeech.Object);
                    else
                        partsToWrite.Add(PartOfSpeech.Object);
                }
            }
            if ((pos & PartOfSpeech.Buzzword) != 0)
            {
                // for CompactVocab, don't write a value for Buzzword
                if (!compactVocab)
                {
                    // there is no BuzzwordFirst: Buzzword comes before everything but Preposition
                    partsToWrite.Insert(0, PartOfSpeech.Buzzword);
                }
            }
            if ((pos & PartOfSpeech.Preposition) != 0)
            {
                // for CompactVocab, don't write a value for Preposition
                if (!compactVocab)
                {
                    // there is no PrepositionFirst because Preposition always comes first
                    Contract.Assume((pos & PartOfSpeech.FirstMask) == 0);
                    partsToWrite.Insert(0, PartOfSpeech.Preposition);
                }
            }

            // write part of speech flags
            wb.AddByte((byte)pos);

            // write values
            int limit = compactVocab ? 1 : 2;
            for (int i = 0; i < limit; i++)
            {
                if (i < partsToWrite.Count)
                {
                    var p = partsToWrite[i];
                    var value = GetValue(p);

                    if (p == PartOfSpeech.Direction)
                        wb.AddByte(dirIndexToPropertyOperand(value));
                    else
                        wb.AddByte(value);
                }
                else
                {
                    wb.AddByte(0);
                }
            }
        }

        public void MarkAsSynonym(PartOfSpeech synonymTypes)
        {
            this.SynonymTypes |= synonymTypes;
        }

        public bool IsSynonym(PartOfSpeech synonymTypes)
        {
            return (this.SynonymTypes & synonymTypes) != 0;
        }

        public void Merge(Context ctx, OldParserWord other)
        {
            Contract.Requires(ctx != null);

            if ((other.PartOfSpeech & PartOfSpeech.Adjective) != 0)
                this.SetAdjective(ctx, other.GetDefinition(PartOfSpeech.Adjective), other.GetValue(PartOfSpeech.Adjective));

            if ((other.PartOfSpeech & PartOfSpeech.Buzzword) != 0)
                this.SetBuzzword(ctx, other.GetDefinition(PartOfSpeech.Buzzword), other.GetValue(PartOfSpeech.Buzzword));

            if ((other.PartOfSpeech & PartOfSpeech.Direction) != 0)
                this.SetDirection(ctx, other.GetDefinition(PartOfSpeech.Direction), other.GetValue(PartOfSpeech.Direction));

            if ((other.PartOfSpeech & PartOfSpeech.Object) != 0)
                this.SetObject(ctx, other.GetDefinition(PartOfSpeech.Object));

            if ((other.PartOfSpeech & PartOfSpeech.Preposition) != 0)
                this.SetPreposition(ctx, other.GetDefinition(PartOfSpeech.Preposition), other.GetValue(PartOfSpeech.Preposition));

            if ((other.PartOfSpeech & PartOfSpeech.Verb) != 0)
                this.SetVerb(ctx, other.GetDefinition(PartOfSpeech.Verb), other.GetValue(PartOfSpeech.Verb));

            this.MarkAsSynonym(other.PartOfSpeech & ~PartOfSpeech.FirstMask);
        }
    }
}