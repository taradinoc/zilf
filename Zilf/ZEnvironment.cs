/* Copyright 2010, 2012 Jesse McGrew
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

namespace Zilf
{
    enum ObjectOrdering
    {
        Default,
        Defined,
        RoomsFirst,
        RoomsLast,
    }

    /// <summary>
    /// Holds the state built up during the load phase for the compiler to use.
    /// </summary>
    class ZEnvironment
    {
        private readonly Context ctx;

        public int ZVersion = 3;

        public readonly List<ZilRoutine> Routines = new List<ZilRoutine>();
        public readonly List<ZilConstant> Constants = new List<ZilConstant>();
        public readonly List<ZilGlobal> Globals = new List<ZilGlobal>();
        public readonly List<ZilModelObject> Objects = new List<ZilModelObject>();
        public readonly List<ZilTable> Tables = new List<ZilTable>();

        public readonly Dictionary<ZilAtom, ZilObject> PropertyDefaults = new Dictionary<ZilAtom, ZilObject>();

        public readonly List<Syntax> Syntaxes = new List<Syntax>();
        public readonly Dictionary<ZilAtom, Word> Vocabulary = new Dictionary<ZilAtom, Word>();
        public readonly List<Synonym> Synonyms = new List<Synonym>();
        public readonly List<ZilAtom> Directions = new List<ZilAtom>();
        public readonly List<ZilAtom> Buzzwords = new List<ZilAtom>();

        public readonly List<ZilAtom> FirstObjects = new List<ZilAtom>();
        public readonly List<ZilAtom> LastObjects = new List<ZilAtom>();
        public ObjectOrdering ObjectOrdering;

        /// <summary>
        /// The last direction defined with &lt;DIRECTIONS&gt;.
        /// </summary>
        public ZilAtom LowDirection;

        public byte NextAdjective = 255;    // A?
        public byte NextBuzzword = 255;     // B?
        public byte NextPreposition = 255;  // PR?
        public byte NextVerb = 255;         // ACT? (verb words)

        public byte NextAction = 0;         // V? (intentions)

        public ZEnvironment(Context ctx)
        {
            this.ctx = ctx;
        }

        public Word GetVocabPreposition(ZilAtom text)
        {
            Word result;

            if (Vocabulary.TryGetValue(text, out result) == false)
            {
                result = new Word(text);
                Vocabulary.Add(text, result);
            }

            if ((result.PartOfSpeech & PartOfSpeech.Preposition) == 0)
            {
                if (NextPreposition == 0)
                    throw new InvalidOperationException("Too many prepositions");

                result.SetPreposition(ctx, NextPreposition--);
            }

            return result;
        }

        public Word GetVocabAdjective(ZilAtom text)
        {
            Word result;

            if (Vocabulary.TryGetValue(text, out result) == false)
            {
                result = new Word(text);
                Vocabulary.Add(text, result);
            }

            if ((result.PartOfSpeech & PartOfSpeech.Adjective) == 0)
            {
                // adjective numbers only exist in V3
                if (ZVersion == 3)
                {
                    if (NextAdjective == 0)
                        throw new InvalidOperationException("Too many adjectives");

                    result.SetAdjective(ctx, NextAdjective--);
                }
                else
                {
                    result.SetAdjective(ctx, 0);
                }
            }

            return result;
        }

        public Word GetVocabNoun(ZilAtom text)
        {
            Word result;

            if (Vocabulary.TryGetValue(text, out result) == false)
            {
                result = new Word(text);
                Vocabulary.Add(text, result);
            }

            result.SetObject(ctx);
            return result;
        }

        public Word GetVocabBuzzword(ZilAtom text)
        {
            Word result;

            if (Vocabulary.TryGetValue(text, out result) == false)
            {
                result = new Word(text);
                Vocabulary.Add(text, result);
            }

            if ((result.PartOfSpeech & PartOfSpeech.Buzzword) == 0)
            {
                if (NextBuzzword == 0)
                    throw new InvalidOperationException("Too many buzzwords");

                result.SetBuzzword(ctx, NextBuzzword--);
            }

            return result;
        }

        public Word GetVocabVerb(ZilAtom text)
        {
            Word result;

            if (Vocabulary.TryGetValue(text, out result) == false)
            {
                result = new Word(text);
                Vocabulary.Add(text, result);
            }

            if ((result.PartOfSpeech & PartOfSpeech.Verb) == 0)
            {
                if (NextVerb == 0)
                    throw new InvalidOperationException("Too many verbs");

                result.SetVerb(ctx, NextVerb--);
            }

            return result;
        }

        public Word GetVocabDirection(ZilAtom text)
        {
            Word result;

            if (Vocabulary.TryGetValue(text, out result) == false)
            {
                result = new Word(text);
                Vocabulary.Add(text, result);
            }

            if ((result.PartOfSpeech & PartOfSpeech.Direction) == 0)
            {
                int index = Directions.IndexOf(text);
                if (index == -1)
                    throw new ArgumentException("Not a direction");

                result.SetDirection(ctx, (byte)index);
            }

            return result;
        }

        public void SortObjects()
        {
            // apply FIRST/LAST
            var origOrder = new Dictionary<ZilModelObject, int>(Objects.Count);
            for (int i = 0; i < Objects.Count; i++)
                origOrder.Add(Objects[i], i);

            Objects.Sort((a, b) =>
            {
                int ai = FirstObjects.IndexOf(a.Name);
                int bi = FirstObjects.IndexOf(b.Name);

                if (ai != bi)
                {
                    if (ai >= 0 && bi >= 0)
                        return ai - bi;
                    else if (ai >= 0)
                        return -1;
                    else
                        return 1;
                }

                ai = LastObjects.IndexOf(a.Name);
                bi = LastObjects.IndexOf(b.Name);

                if (ai != bi)
                {
                    if (ai >= 0 && bi >= 0)
                        return ai - bi;
                    else if (ai >= 0)
                        return 1;
                    else
                        return -1;
                }

                return origOrder[a] - origOrder[b];
            });

            // apply ROOMS-FIRST/ROOMS-LAST
            int count = Objects.Count;

            IEnumerable<ZilModelObject> finalOrder = Objects;

            switch (ObjectOrdering)
            {
                case ObjectOrdering.RoomsFirst:
                case ObjectOrdering.RoomsLast:
                    var rooms = from o in Objects
                                where o.IsRoom
                                select o;
                    var nonrooms = from o in Objects
                                   where !o.IsRoom
                                   select o;
                    if (ObjectOrdering == ObjectOrdering.RoomsFirst)
                        finalOrder = rooms.Concat(nonrooms);
                    else
                        finalOrder = nonrooms.Concat(rooms);
                    break;
            }

            if (finalOrder != Objects)
            {
                var temp = new List<ZilModelObject>(Objects.Count);
                temp.AddRange(finalOrder);
                Objects.Clear();
                Objects.AddRange(temp);
            }
        }

        public void MergeVocabulary()
        {
            // XXX merge words that are indistinguishable because of the vocabulary resolution
        }
    }

    [Flags]
    enum PartOfSpeech : byte
    {
        None = 0,
        FirstMask = 3,

        //ObjectFirst = 0,
        VerbFirst = 1,
        AdjectiveFirst = 2,
        DirectionFirst = 3,

        //PrepositionFirst = 0,
        //BuzzwordFirst = 0,

        Buzzword = 4,
        Preposition = 8,
        Direction = 16,
        Adjective = 32,
        Verb = 64,
        Object = 128,
    }

    [Flags]
    enum ScopeFlags : byte
    {
        None = 0,

        Have = 2,
        Many = 4,
        Take = 8,
        OnGround = 16,
        InRoom = 32,
        Carried = 64,
        Held = 128,

        Default = OnGround | InRoom | Carried | Held,
    }

    class Syntax : ISourceLine
    {
        public readonly int NumObjects;
        public readonly Word Verb, Preposition1, Preposition2;
        public readonly ScopeFlags Options1, Options2;
        public readonly ZilAtom FindFlag1, FindFlag2;
        public readonly ZilAtom Action, Preaction;

        private readonly ISourceLine src;

        public Syntax(ISourceLine src, Word verb, int numObjects, Word prep1, Word prep2,
            ScopeFlags options1, ScopeFlags options2, ZilAtom findFlag1, ZilAtom findFlag2,
            ZilAtom action, ZilAtom preaction)
        {
            this.src = src;

            this.Verb = verb;
            this.NumObjects = numObjects;
            this.Preposition1 = prep1;
            this.Preposition2 = prep2;
            this.Options1 = options1;
            this.Options2 = options2;
            this.FindFlag1 = findFlag1;
            this.FindFlag2 = findFlag2;
            this.Action = action;
            this.Preaction = preaction;
        }

        public static Syntax Parse(ISourceLine src, IEnumerable<ZilObject> definition, Context ctx)
        {
            int numObjects = 0;
            ZilAtom verb = null, prep1 = null, prep2 = null, action = null, preaction = null;
            ZilList bits1 = null, find1 = null, bits2 = null, find2 = null;
            bool rightSide = false;

            // main parsing
            foreach (ZilObject obj in definition)
            {
                if (verb == null)
                {
                    ZilAtom atom = obj as ZilAtom;
                    if (atom == null || atom.StdAtom == StdAtom.Eq)
                        throw new InterpreterError("missing verb in syntax definition");

                    verb = atom;
                }
                else if (!rightSide)
                {
                    // left side:
                    //   [[prep] OBJECT [(FIND ...)] [(options...) ...] [[prep] OBJECT [(FIND ...)] [(options...)]]]
                    ZilAtom atom = obj as ZilAtom;
                    if (atom != null)
                    {
                        switch (atom.StdAtom)
                        {
                            case StdAtom.OBJECT:
                                numObjects++;
                                if (numObjects > 2)
                                    throw new InterpreterError("too many OBJECT in syntax definition");
                                break;

                            case StdAtom.Eq:
                                rightSide = true;
                                break;

                            default:
                                if ((numObjects == 0 && prep1 != null) ||
                                    (numObjects == 1 && prep2 != null) ||
                                    numObjects == 2)
                                    throw new InterpreterError("too many prepositions in syntax definition");
                                else if (numObjects == 0)
                                    prep1 = atom;
                                else
                                    prep2 = atom;
                                break;
                        }
                    }
                    else
                    {
                        ZilList list = obj as ZilList;
                        if (list != null && list.GetTypeAtom(ctx).StdAtom == StdAtom.LIST)
                        {
                            if (numObjects == 0)
                                throw new InterpreterError("misplaced list in syntax definition");

                            atom = list.First as ZilAtom;
                            if (atom == null)
                                throw new InterpreterError("list in syntax definition must start with an atom");

                            if (atom.StdAtom == StdAtom.FIND)
                            {
                                if ((numObjects == 1 && find1 != null) || find2 != null)
                                    throw new InterpreterError("too many FIND lists in syntax definition");
                                else if (numObjects == 1)
                                    find1 = list;
                                else
                                    find2 = list;
                            }
                            else
                            {
                                if (numObjects == 1)
                                {
                                    if (bits1 != null)
                                        bits1 = new ZilList(Enumerable.Concat(bits1, list));
                                    else
                                        bits1 = list;
                                }
                                else
                                {
                                    if (bits2 != null)
                                        bits2 = new ZilList(Enumerable.Concat(bits2, list));
                                    else
                                        bits2 = list;
                                }
                            }
                        }
                        else
                            throw new InterpreterError("unexpected value in syntax definition");
                    }
                }
                else
                {
                    // right side:
                    //   action [preaction]
                    ZilAtom atom = obj as ZilAtom;
                    if (atom != null)
                    {
                        if (atom.StdAtom == StdAtom.Eq)
                            throw new InterpreterError("too many = in syntax definition");

                        if (action == null)
                            action = atom;
                        else if (preaction == null)
                            preaction = atom;
                        else
                            throw new InterpreterError("too many atoms after = in syntax definition");
                    }
                    else
                        throw new InterpreterError("non-atom after = in syntax definition");
                }
            }

            // validation
            Word verbWord = ctx.ZEnvironment.GetVocabVerb(verb);
            Word word1 = (prep1 == null) ? null : ctx.ZEnvironment.GetVocabPreposition(prep1);
            Word word2 = (prep2 == null) ? null : ctx.ZEnvironment.GetVocabPreposition(prep2);
            ScopeFlags flags1 = ParseScopeFlags(bits1);
            ScopeFlags flags2 = ParseScopeFlags(bits2);
            ZilAtom findFlag1 = ParseFindFlag(find1);
            ZilAtom findFlag2 = ParseFindFlag(find2);

            return new Syntax(
                src,
                verbWord, numObjects,
                word1, word2, flags1, flags2, findFlag1, findFlag2,
                action, preaction);
        }

        private static ZilAtom ParseFindFlag(ZilList list)
        {
            if (list == null)
                return null;

            ZilAtom atom;
            if (list.IsEmpty || list.Rest.IsEmpty || !list.Rest.Rest.IsEmpty ||
                (atom = list.Rest.First as ZilAtom) == null)
                throw new InterpreterError("FIND must be followed by a single atom");

            return atom;
        }

        private static ScopeFlags ParseScopeFlags(ZilList list)
        {
            if (list == null)
                return ScopeFlags.Default;

            ScopeFlags result = ScopeFlags.None;

            foreach (ZilObject obj in list)
            {
                ZilAtom atom = obj as ZilAtom;
                if (atom == null)
                    throw new InterpreterError("object options in syntax must be atoms");

                switch (atom.StdAtom)
                {
                    case StdAtom.TAKE:
                        result |= ScopeFlags.Take;
                        break;
                    case StdAtom.HAVE:
                        result |= ScopeFlags.Have;
                        break;
                    case StdAtom.MANY:
                        result |= ScopeFlags.Many;
                        break;
                    case StdAtom.HELD:
                        result |= ScopeFlags.Held;
                        break;
                    case StdAtom.CARRIED:
                        result |= ScopeFlags.Carried;
                        break;
                    case StdAtom.ON_GROUND:
                        result |= ScopeFlags.OnGround;
                        break;
                    case StdAtom.IN_ROOM:
                        result |= ScopeFlags.InRoom;
                        break;
                    default:
                        throw new InterpreterError("unrecognized object option: " + atom.ToString());
                }
            }

            return result;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            // verb
            sb.Append(Verb.Atom);

            // object clauses
            var items = new[] {
                new { Prep = Preposition1, Find = FindFlag1, Opts = Options1 },
                new { Prep = Preposition2, Find = FindFlag2, Opts = Options2 },
            };
            
            foreach (var item in items.Take(NumObjects))
            {
                if (item.Prep != null)
                {
                    sb.Append(' ');
                    sb.Append(item.Prep.Atom);
                }

                sb.Append(" OBJECT");

                if (item.Find != null)
                {
                    sb.Append(" (FIND ");
                    sb.Append(item.Find);
                    sb.Append(')');
                }

                if (item.Opts != ScopeFlags.Default)
                {
                    sb.Append(" (");
                    sb.Append(item.Opts);
                    sb.Append(')');
                }
            }

            // actions
            sb.Append(" = ");
            sb.Append(Action);
            if (Preaction != null)
            {
                sb.Append(' ');
                sb.Append(Preaction);
            }

            return sb.ToString();
        }

        public string SourceInfo
        {
            get
            {
                if (src != null)
                    return src.SourceInfo;
                else
                    return "syntax for " + Verb.Atom;
            }
        }
    }

    class Word
    {
        public readonly ZilAtom Atom;
        public PartOfSpeech PartOfSpeech;

        private Dictionary<PartOfSpeech, byte> speechValues = new Dictionary<PartOfSpeech, byte>(2);

        public Word(ZilAtom atom)
        {
            if (atom == null)
                throw new ArgumentNullException("atom");

            this.Atom = atom;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
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
                sb.Append(speechValues[Zilf.PartOfSpeech.Adjective]);
            }
            if ((PartOfSpeech & PartOfSpeech.Buzzword) != 0)
            {
                sb.Append("|BUZZ");
            }
            if ((PartOfSpeech & PartOfSpeech.Direction) != 0)
            {
                sb.Append("|DIR=");
                sb.Append(speechValues[Zilf.PartOfSpeech.Direction]);
            }
            if ((PartOfSpeech & PartOfSpeech.Object) != 0)
            {
                sb.Append("|OBJ=");
                sb.Append(speechValues[Zilf.PartOfSpeech.Object]);
            }
            if ((PartOfSpeech & PartOfSpeech.Preposition) != 0)
            {
                sb.Append("|PREP=");
                sb.Append(speechValues[Zilf.PartOfSpeech.Preposition]);
            }
            if ((PartOfSpeech & PartOfSpeech.Verb) != 0)
            {
                sb.Append("|VERB=");
                sb.Append(speechValues[Zilf.PartOfSpeech.Verb]);
            }

            return sb.ToString();
        }

        private bool IsNewVoc(Context ctx)
        {
            ZilObject value = ctx.GetGlobalVal(ctx.GetStdAtom(StdAtom.NEW_VOC_P));
            return (value != null && value.IsTrue);
        }

        /// <summary>
        /// Checks whether adding a new part of speech should set the relevant First flag.
        /// </summary>
        /// <param name="ctx">The current context.</param>
        /// <returns>true if the new part of speech should set the First flag.</returns>
        private bool ShouldSetFirst(Context ctx)
        {
            // if no parts of speech are set yet, this is easy
            if (this.PartOfSpeech == Zilf.PartOfSpeech.None)
                return true;

            // ignore parts of speech that don't record values in the current context
            var pos = this.PartOfSpeech;
            if (ctx.ZEnvironment.ZVersion >= 4)
                pos &= ~Zilf.PartOfSpeech.Adjective;
            if (IsNewVoc(ctx))
                pos &= ~Zilf.PartOfSpeech.Object;

            return pos == Zilf.PartOfSpeech.None;
        }

        /// <summary>
        /// Check whether the word has too many parts of speech defined, and
        /// raise an exception if so.
        /// </summary>
        /// <param name="ctx">The current context.</param>
        /// <exception cref="CompilerError">
        /// The word has too many parts of speech defined.
        /// </exception>
        /// <remarks>
        /// Generally, a vocabulary word is limited to no more than two parts
        /// of speech. However, in V4+, <see cref="PartOfSpeech.Adjective"/> does
        /// not count against this limit; and in any version when the global
        /// atom NEW-VOC? is true, <see cref="PartOfSpeech.Object"/> does not
        /// count against this limit.
        /// </remarks>
        private void CheckTooMany(Context ctx)
        {
            byte b = (byte)(PartOfSpeech & ~PartOfSpeech.FirstMask);
            byte count = 0;

            while (b != 0)
            {
                b &= (byte)(b - 1);
                count++;
            }

            // when ,NEW-VOC? is true, Object is free
            if ((PartOfSpeech & PartOfSpeech.Object) != 0)
            {
                if (IsNewVoc(ctx))
                    count--;
            }

            // Adjective is always free in V4+
            if ((PartOfSpeech & PartOfSpeech.Adjective) != 0)
            {
                if (ctx.ZEnvironment.ZVersion > 3)
                    count--;
            }

            if (count > 2)
                throw new CompilerError("too many parts of speech for " + Atom);
        }

        public void SetObject(Context ctx)
        {
            if ((PartOfSpeech & PartOfSpeech.Buzzword) != 0)
                throw new InterpreterError("buzzwords may not be used as any other part of speech");

            if ((PartOfSpeech & PartOfSpeech.Object) == 0)
            {
                // there is no PartOfSpeech.ObjectFirst, so don't change the First flags

                PartOfSpeech |= PartOfSpeech.Object;
                speechValues[Zilf.PartOfSpeech.Object] = 1;
                CheckTooMany(ctx);
            }
        }

        public void SetVerb(Context ctx, byte value)
        {
            if ((PartOfSpeech & PartOfSpeech.Buzzword) != 0)
                throw new InterpreterError("buzzwords may not be used as any other part of speech");

            if ((PartOfSpeech & PartOfSpeech.Verb) == 0)
            {
                if (ShouldSetFirst(ctx))
                    PartOfSpeech |= PartOfSpeech.VerbFirst;

                PartOfSpeech |= PartOfSpeech.Verb;
                speechValues[Zilf.PartOfSpeech.Verb] = value;
                CheckTooMany(ctx);
            }
        }

        public void SetAdjective(Context ctx, byte value)
        {
            if ((PartOfSpeech & PartOfSpeech.Buzzword) != 0)
                throw new InterpreterError("buzzwords may not be used as any other part of speech");
            
            if ((PartOfSpeech & PartOfSpeech.Adjective) == 0)
            {
                if (ctx.ZEnvironment.ZVersion < 4 && ShouldSetFirst(ctx))
                    PartOfSpeech |= PartOfSpeech.AdjectiveFirst;

                PartOfSpeech |= PartOfSpeech.Adjective;
                speechValues[Zilf.PartOfSpeech.Adjective] = value;
                CheckTooMany(ctx);
            }
        }

        public void SetDirection(Context ctx, byte value)
        {
            if ((PartOfSpeech & PartOfSpeech.Buzzword) != 0)
                throw new InterpreterError("buzzwords may not be used as any other part of speech");
            
            if ((PartOfSpeech & PartOfSpeech.Direction) == 0)
            {
                if (ShouldSetFirst(ctx))
                    PartOfSpeech |= PartOfSpeech.DirectionFirst;

                PartOfSpeech |= PartOfSpeech.Direction;
                speechValues[Zilf.PartOfSpeech.Direction] = value;
                CheckTooMany(ctx);
            }
        }

        public void SetBuzzword(Context ctx, byte value)
        {
            if (PartOfSpeech != PartOfSpeech.None)
                throw new InterpreterError("buzzwords may not be used as any other part of speech");

            PartOfSpeech = PartOfSpeech.Buzzword;
            speechValues[Zilf.PartOfSpeech.Buzzword] = value;
        }

        public void SetPreposition(Context ctx, byte value)
        {
            if ((PartOfSpeech & PartOfSpeech.Buzzword) != 0)
                throw new InterpreterError("buzzwords may not be used as any other part of speech");

            if ((PartOfSpeech & PartOfSpeech.Preposition) == 0)
            {
                // preposition value is always first
                PartOfSpeech |= PartOfSpeech.Preposition;
                PartOfSpeech &= ~PartOfSpeech.FirstMask;
                speechValues[Zilf.PartOfSpeech.Preposition] = value;
                CheckTooMany(ctx);
            }
        }

        public byte GetValue(PartOfSpeech part)
        {
            switch (part)
            {
                case Zilf.PartOfSpeech.Verb:
                case Zilf.PartOfSpeech.Adjective:
                case Zilf.PartOfSpeech.Direction:
                case Zilf.PartOfSpeech.Buzzword:
                case Zilf.PartOfSpeech.Preposition:
                case Zilf.PartOfSpeech.Object:
                    return speechValues[part];

                default:
                    throw new ArgumentOutOfRangeException("Unexpected part of speech: " + part);
            }
        }

        public void WriteToBuilder(Context ctx, Emit.IWordBuilder wb, Func<byte, Emit.IOperand> dirIndexToPropertyOperand)
        {
            var pos = this.PartOfSpeech;
            var partsToWrite = new List<PartOfSpeech>(2);

            // expand parts of speech, observing the First flags and a few special cases
            if ((pos & Zilf.PartOfSpeech.Adjective) != 0)
            {
                // in V4+, don't write a value for Adjective
                if (ctx.ZEnvironment.ZVersion < 4)
                {
                    if ((pos & Zilf.PartOfSpeech.FirstMask) == Zilf.PartOfSpeech.AdjectiveFirst)
                        partsToWrite.Insert(0, Zilf.PartOfSpeech.Adjective);
                    else
                        partsToWrite.Add(Zilf.PartOfSpeech.Adjective);
                }
            }
            if ((pos & Zilf.PartOfSpeech.Buzzword) != 0)
            {
                // there is no BuzzwordFirst, but Buzzword must be on its own anyway
                System.Diagnostics.Debug.Assert(pos == Zilf.PartOfSpeech.Buzzword);
                partsToWrite.Add(Zilf.PartOfSpeech.Buzzword);
            }
            if ((pos & Zilf.PartOfSpeech.Direction) != 0)
            {
                if ((pos & Zilf.PartOfSpeech.FirstMask) == Zilf.PartOfSpeech.DirectionFirst)
                    partsToWrite.Insert(0, Zilf.PartOfSpeech.Direction);
                else
                    partsToWrite.Add(Zilf.PartOfSpeech.Direction);
            }
            if ((pos & Zilf.PartOfSpeech.Verb) != 0)
            {
                if ((pos & Zilf.PartOfSpeech.FirstMask) == Zilf.PartOfSpeech.VerbFirst)
                    partsToWrite.Insert(0, Zilf.PartOfSpeech.Verb);
                else
                    partsToWrite.Add(Zilf.PartOfSpeech.Verb);
            }
            if ((pos & Zilf.PartOfSpeech.Object) != 0)
            {
                // for NewVoc, don't write a value for Object
                if (!IsNewVoc(ctx))
                {
                    // there is no ObjectFirst, so keep it first if all other First flags are clear
                    if ((pos & Zilf.PartOfSpeech.FirstMask) == 0)
                        partsToWrite.Insert(0, Zilf.PartOfSpeech.Object);
                    else
                        partsToWrite.Add(Zilf.PartOfSpeech.Object);
                }
            }
            if ((pos & Zilf.PartOfSpeech.Preposition) != 0)
            {
                // there is no PrepositionFirst because Preposition always comes first
                System.Diagnostics.Debug.Assert((pos & Zilf.PartOfSpeech.FirstMask) == 0);
                partsToWrite.Insert(0, Zilf.PartOfSpeech.Preposition);
            }

            // write part of speech flags
            wb.AddByte((byte)pos);

            // write values
            for (int i = 0; i < 2; i++)
            {
                if (i < partsToWrite.Count)
                {
                    var p = partsToWrite[i];
                    var value = GetValue(p);

                    if (p == Zilf.PartOfSpeech.Direction)
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
    }

    class Synonym
    {
        public readonly Word OriginalWord;
        public readonly Word SynonymWord;

        public Synonym(Word original, Word synonym)
        {
            this.OriginalWord = original;
            this.SynonymWord = synonym;
        }

        public virtual void Apply(Context ctx)
        {
            if ((OriginalWord.PartOfSpeech & PartOfSpeech.Adjective) != 0)
                SynonymWord.SetAdjective(ctx, OriginalWord.GetValue(PartOfSpeech.Adjective));

            if ((OriginalWord.PartOfSpeech & PartOfSpeech.Buzzword) != 0)
                SynonymWord.SetBuzzword(ctx, OriginalWord.GetValue(PartOfSpeech.Buzzword));

            if ((OriginalWord.PartOfSpeech & PartOfSpeech.Direction) != 0)
                SynonymWord.SetDirection(ctx, OriginalWord.GetValue(PartOfSpeech.Direction));

            if ((OriginalWord.PartOfSpeech & PartOfSpeech.Object) != 0)
                SynonymWord.SetObject(ctx);

            if ((OriginalWord.PartOfSpeech & PartOfSpeech.Preposition) != 0)
                SynonymWord.SetPreposition(ctx, OriginalWord.GetValue(PartOfSpeech.Preposition));

            if ((OriginalWord.PartOfSpeech & PartOfSpeech.Verb) != 0)
                SynonymWord.SetVerb(ctx, OriginalWord.GetValue(PartOfSpeech.Verb));
        }
    }

    class VerbSynonym : Synonym
    {
        public VerbSynonym(Word original, Word synonym)
            : base(original, synonym)
        {
        }

        public override void Apply(Context ctx)
        {
            base.Apply(ctx);
        }
    }
}
