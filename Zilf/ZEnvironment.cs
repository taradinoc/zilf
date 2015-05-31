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
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;

namespace Zilf
{
    /// <summary>
    /// Specifies the order in which object numbers are assigned.
    /// </summary>
    enum ObjectOrdering
    {
        /// <summary>
        /// Reverse mention order.
        /// </summary>
        Default,
        /// <summary>
        /// Definition order, then mention order for objects with no definitions.
        /// </summary>
        Defined,
        /// <summary>
        /// Like <see cref="Defined"/>, but with all rooms having lower numbers than non-rooms.
        /// </summary>
        /// <remarks>
        /// "Rooms" include all objects declared with the ROOM FSUBR (instead of OBJECT), and
        /// all objects whose initial LOC is the object called ROOMS.
        /// </remarks>
        RoomsFirst,
        /// <summary>
        /// Like <see cref="RoomsFirst"/>, but with all local globals also having lower numbers
        /// than non-rooms and non-local-globals.
        /// </summary>
        /// <remarks>
        /// "Local globals" includes all objects whose initial LOC is the object called
        /// LOCAL-GLOBALS.
        /// </remarks>
        RoomsAndLocalGlobalsFirst,
        /// <summary>
        /// Like <see cref="Defined"/>, but with all non-rooms having lower numbers than rooms.
        /// </summary>
        RoomsLast,
    }

    /// <summary>
    /// Specifies the order of links in the object tree.
    /// </summary>
    /// <remarks>
    /// These values are named with regard to how the game will traverse the objects (following FIRST? and NEXT?).
    /// The compiler processes them in the opposite order when inserting them into the tree.
    /// </remarks>
    enum TreeOrdering
    {
        /// <summary>
        /// Reverse definition order, except for the first defined child of each parent, which remains the first child linked.
        /// </summary>
        Default,
        /// <summary>
        /// Reverse definition order.
        /// </summary>
        ReverseDefined,
    }

    [Flags]
    enum LowCoreFlags
    {
        None = 0,

        Byte = 1,
        Writable = 2,
        Extended = 4,
    }

    sealed class LowCoreField
    {
        public int Offset { get; private set; }
        public LowCoreFlags Flags { get; private set; }
        public int MinVersion { get; private set; }

        private LowCoreField(int offset, LowCoreFlags flags = LowCoreFlags.None, int minVersion = 3)
        {
            Contract.Requires(offset >= 0);
            Contract.Requires(minVersion >= 1);

            this.Offset = offset;
            this.Flags = flags;
            this.MinVersion = minVersion;
        }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(Offset >= 0);
            Contract.Invariant(MinVersion >= 1);
        }

        private static readonly Dictionary<string, LowCoreField> allFields = new Dictionary<string, LowCoreField>()
        {
            { "ZVERSION", new LowCoreField(0) },
            { "ZORKID", new LowCoreField(1) },
            { "RELEASEID", new LowCoreField(1) },
            { "ENDLOD", new LowCoreField(2) },
            { "START", new LowCoreField(3) },
            { "VOCAB", new LowCoreField(4) },
            { "OBJECT", new LowCoreField(5) },
            { "GLOBALS", new LowCoreField(6) },
            { "PURBOT", new LowCoreField(7) },
            { "FLAGS", new LowCoreField(8, LowCoreFlags.Writable) },
            { "SERIAL", new LowCoreField(9) },
            { "SERI1", new LowCoreField(10) },
            { "SERI2", new LowCoreField(11) },
            { "FWORDS", new LowCoreField(12) },
            { "PLENTH", new LowCoreField(13) },
            { "PCHKSM", new LowCoreField(14) },
            { "INTWRD", new LowCoreField(15) },
            { "INTID", new LowCoreField(30, LowCoreFlags.Byte) },
            { "INTVR", new LowCoreField(31, LowCoreFlags.Byte) },
            { "SCRWRD", new LowCoreField(16, minVersion: 4) },
            { "SCRV", new LowCoreField(32, LowCoreFlags.Byte, minVersion: 4) },
            { "SCRH", new LowCoreField(33, LowCoreFlags.Byte, minVersion: 4) },
            { "HWRD", new LowCoreField(17, minVersion: 5) },
            { "VWRD", new LowCoreField(18, minVersion: 5) },
            { "FWRD", new LowCoreField(19, minVersion: 5) },
            { "LMRG", new LowCoreField(20, minVersion: 5) },
            { "RMRG", new LowCoreField(21, minVersion: 5) },
            { "CLRWRD", new LowCoreField(22, minVersion: 5) },
            { "TCHARS", new LowCoreField(23, minVersion: 5) },
            { "CRCNT", new LowCoreField(24, LowCoreFlags.Writable, minVersion: 5) },
            { "CRFUNC", new LowCoreField(25, LowCoreFlags.Writable, minVersion: 5) },
            { "CHRSET", new LowCoreField(26, minVersion: 5) },
            { "EXTAB", new LowCoreField(27, minVersion: 5) },
            { "MSLOCX", new LowCoreField(1, LowCoreFlags.Extended, minVersion: 5) },
            { "MSLOCY", new LowCoreField(2, LowCoreFlags.Extended, minVersion: 5) },
            { "MSETBL", new LowCoreField(3, LowCoreFlags.Extended | LowCoreFlags.Writable, minVersion: 5) },
            { "MSEDIR", new LowCoreField(4, LowCoreFlags.Extended | LowCoreFlags.Writable, minVersion: 5) },
            { "MSEINV", new LowCoreField(5, LowCoreFlags.Extended | LowCoreFlags.Writable, minVersion: 5) },
            { "MSEVRB", new LowCoreField(6, LowCoreFlags.Extended | LowCoreFlags.Writable, minVersion: 5) },
            { "MSEWRD", new LowCoreField(7, LowCoreFlags.Extended | LowCoreFlags.Writable, minVersion: 5) },
            { "BUTTON", new LowCoreField(8, LowCoreFlags.Extended | LowCoreFlags.Writable, minVersion: 5) },
            { "JOYSTICK", new LowCoreField(9, LowCoreFlags.Extended | LowCoreFlags.Writable, minVersion: 5) },
            { "BSTAT", new LowCoreField(10, LowCoreFlags.Extended | LowCoreFlags.Writable, minVersion: 5) },
            { "JSTAT", new LowCoreField(11, LowCoreFlags.Extended | LowCoreFlags.Writable, minVersion: 5) },

            { "STDREV", new LowCoreField(25) },
            { "UNITBL", new LowCoreField(3, LowCoreFlags.Extended, minVersion: 5) },
            { "FLAGS3", new LowCoreField(4, LowCoreFlags.Extended, minVersion: 5) },
            { "TRUFGC", new LowCoreField(5, LowCoreFlags.Extended, minVersion: 5) },
            { "TRUBGC", new LowCoreField(6, LowCoreFlags.Extended, minVersion: 5) },
        };

        public static LowCoreField Get(ZilAtom atom)
        {
            Contract.Requires(atom != null);

            LowCoreField result;
            allFields.TryGetValue(atom.Text, out result);
            return result;
        }
    }

    /// <summary>
    /// Holds the state built up during the load phase for the compiler to use.
    /// </summary>
    class ZEnvironment
    {
        private readonly Context ctx;

        public int ZVersion = 3;
        public bool TimeStatusLine = false;
        public ZilAtom EntryRoutineName;

        public readonly List<ZilRoutine> Routines = new List<ZilRoutine>();
        public readonly List<ZilConstant> Constants = new List<ZilConstant>();
        public readonly List<ZilGlobal> Globals = new List<ZilGlobal>();
        public readonly List<ZilModelObject> Objects = new List<ZilModelObject>();
        public readonly List<ZilTable> Tables = new List<ZilTable>();

        public readonly Dictionary<ZilAtom, ZilObject> PropertyDefaults = new Dictionary<ZilAtom, ZilObject>();
        public readonly Dictionary<ZilAtom, ZilAtom> BitSynonyms = new Dictionary<ZilAtom, ZilAtom>();
        public readonly List<ZilAtom> FlagsOrderedLast = new List<ZilAtom>();

        public readonly List<Syntax> Syntaxes = new List<Syntax>();
        public readonly Dictionary<ZilAtom, Word> Vocabulary = new Dictionary<ZilAtom, Word>();
        public readonly List<Synonym> Synonyms = new List<Synonym>();
        public readonly List<ZilAtom> Directions = new List<ZilAtom>();
        public readonly List<KeyValuePair<ZilAtom, ISourceLine>> Buzzwords = new List<KeyValuePair<ZilAtom, ISourceLine>>();

        public ObjectOrdering ObjectOrdering = ObjectOrdering.Default;
        public TreeOrdering TreeOrdering = TreeOrdering.Default;
        public bool GenerateLongWords = false;

        public readonly List<TellPattern> TellPatterns = new List<TellPattern>();

        /// <summary>
        /// The last direction defined with &lt;DIRECTIONS&gt;.
        /// </summary>
        public ZilAtom LowDirection;

        public byte NextAdjective = 255;    // A?
        public byte NextBuzzword = 255;     // B?
        public byte NextPreposition = 255;  // PR?
        public byte NextVerb = 255;         // ACT? (verb words)

        public byte NextAction = 0;         // V? (intentions)

        public int HeaderExtensionWords = 0;

        private byte[] zcharCountCache;   // char -> # of Z-chars
        private string charset0, charset1, charset2;

        public const string DefaultCharset0 = "abcdefghijklmnopqrstuvwxyz";     // 26 chars
        public const string DefaultCharset1 = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";     // 26 chars
        public const string DefaultCharset2 = "0123456789.,!?_#'\"/\\-:()";     // 24 chars(!)

        public string Charset0
        {
            get { return charset0; }
            set { charset0 = value; zcharCountCache = null; }
        }
        public string Charset1
        {
            get { return charset1; }
            set { charset1 = value; zcharCountCache = null; }
        }
        public string Charset2
        {
            get { return charset2; }
            set { charset2 = value; zcharCountCache = null; }
        }

        public ZEnvironment(Context ctx)
        {
            this.ctx = ctx;

            Charset0 = DefaultCharset0;
            Charset1 = DefaultCharset1;
            Charset2 = DefaultCharset2;
        }

        public Word GetVocab(ZilAtom text)
        {
            Word result;

            if (Vocabulary.TryGetValue(text, out result) == false)
            {
                result = new Word(text);
                Vocabulary.Add(text, result);
            }

            return result;
        }

        public Word GetVocabPreposition(ZilAtom text, ISourceLine location)
        {
            Word result = GetVocab(text);

            if ((result.PartOfSpeech & PartOfSpeech.Preposition) == 0)
            {
                if (NextPreposition == 0)
                    throw new InvalidOperationException("Too many prepositions");

                result.SetPreposition(ctx, location, NextPreposition--);
            }

            return result;
        }

        public Word GetVocabAdjective(ZilAtom text, ISourceLine location)
        {
            Word result = GetVocab(text);
            
            if ((result.PartOfSpeech & PartOfSpeech.Adjective) == 0)
            {
                // adjective numbers only exist in V3
                if (ZVersion == 3)
                {
                    if (NextAdjective == 0)
                        throw new InvalidOperationException("Too many adjectives");

                    result.SetAdjective(ctx, location, NextAdjective--);
                }
                else
                {
                    result.SetAdjective(ctx, location, 0);
                }
            }

            return result;
        }

        public Word GetVocabNoun(ZilAtom text, ISourceLine location)
        {
            Word result = GetVocab(text);

            result.SetObject(ctx, location);
            return result;
        }

        public Word GetVocabBuzzword(ZilAtom text, ISourceLine location)
        {
            Word result = GetVocab(text);

            if ((result.PartOfSpeech & PartOfSpeech.Buzzword) == 0)
            {
                if (NextBuzzword == 0)
                    throw new InvalidOperationException("Too many buzzwords");

                result.SetBuzzword(ctx, location, NextBuzzword--);
            }

            return result;
        }

        public Word GetVocabVerb(ZilAtom text, ISourceLine location)
        {
            Word result = GetVocab(text);

            if ((result.PartOfSpeech & PartOfSpeech.Verb) == 0)
            {
                if (NextVerb == 0)
                    throw new InvalidOperationException("Too many verbs");

                result.SetVerb(ctx, location, NextVerb--);
            }

            return result;
        }

        public Word GetVocabDirection(ZilAtom text, ISourceLine location)
        {
            Word result = GetVocab(text);

            if ((result.PartOfSpeech & PartOfSpeech.Direction) == 0)
            {
                int index = Directions.IndexOf(text);
                if (index == -1)
                    throw new ArgumentException("Not a direction");

                result.SetDirection(ctx, location, (byte)index);
            }

            return result;
        }

        /*public void SortObjects()
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

                // last object defined gets the lowest number
                return origOrder[b] - origOrder[a];
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
        }*/

        private static IEnumerable<ZilAtom> ObjectNamesMentionedInProperty(ZilList prop)
        {
            if (prop.First is ZilAtom && prop.Rest.First != null)
            {
                switch (((ZilAtom)prop.First).StdAtom)
                {
                    case StdAtom.LOC:
                        var loc = prop.Rest.First as ZilAtom;
                        if (loc != null)
                            yield return loc;
                        break;
                    case StdAtom.IN:
                        if (prop.Count() == 2)
                            goto case StdAtom.LOC;
                        break;
                    case StdAtom.GLOBAL:
                        foreach (var g in prop.Rest.OfType<ZilAtom>())
                            yield return g;
                        break;
                }
            }
        }

        private class ObjectOrderingEntry
        {
            public readonly ZilAtom Name;
            public ZilModelObject Object;
            public readonly ISourceLine InitialMention;      // only set for objects created from mentions
            public int? DefinitionOrder;
            public readonly int MentionOrder;

            public ObjectOrderingEntry(ZilAtom name, ZilModelObject obj, ISourceLine initialMention,
                int? definitionOrder, int mentionOrder)
            {
                this.Name = name;
                this.Object = obj;
                this.InitialMention = initialMention;
                this.DefinitionOrder = definitionOrder;
                this.MentionOrder = mentionOrder;
            }
        }

        public IEnumerable<ZilModelObject> ObjectsInDefinitionOrder()
        {
            /* first, collect objects and note the order(s) in which they were defined and mentioned,
             * where "mentioned" means either defined or used as the IN/LOC/GLOBAL of another object */

            var objectsByName = new Dictionary<ZilAtom, ObjectOrderingEntry>(Objects.Count);

            int definitionOrder = 0, mentionOrder = 0;

            foreach (var obj in Objects)
            {
                var atom = obj.Name;

                // add this object if it hasn't already been added
                ObjectOrderingEntry entry;
                if (objectsByName.TryGetValue(atom, out entry) == false)
                {
                    // add this object
                    entry = new ObjectOrderingEntry(atom, obj, null, definitionOrder, mentionOrder++);
                    objectsByName.Add(atom, entry);
                }
                else
                {
                    // it was added by a mention before, set the object and definition order
                    entry.Object = obj;
                    entry.DefinitionOrder = definitionOrder;
                }

                definitionOrder++;

                // same with objects it mentions
                var mentioned = from p in obj.Properties
                                from o in ObjectNamesMentionedInProperty(p)
                                select o;

                foreach (var m in mentioned)
                {
                    if (!objectsByName.ContainsKey(m))
                    {
                        objectsByName.Add(m, new ObjectOrderingEntry(m, null, obj.SourceLine, null, mentionOrder++));
                    }
                }
            }

            // now, apply the selected object ordering
            var order = new List<ObjectOrderingEntry>(objectsByName.Count);

            switch (this.ObjectOrdering)
            {
                case Zilf.ObjectOrdering.Defined:
                    order.AddRange(from e in objectsByName.Values
                                   orderby e.DefinitionOrder ascending,
                                           e.MentionOrder ascending
                                   select e);
                    break;

                case Zilf.ObjectOrdering.RoomsFirst:
                    order.AddRange(from e in objectsByName.Values
                                   orderby IsRoom(e.Object) descending,
                                           e.MentionOrder ascending
                                   select e);
                    break;

                case Zilf.ObjectOrdering.RoomsAndLocalGlobalsFirst:
                    order.AddRange(from e in objectsByName.Values
                                   orderby IsRoom(e.Object) || IsLocalGlobal(e.Object) descending,
                                           e.MentionOrder ascending
                                   select e);
                    break;

                case Zilf.ObjectOrdering.RoomsLast:
                    order.AddRange(from e in objectsByName.Values
                                   orderby IsRoom(e.Object) ascending,
                                           e.MentionOrder ascending
                                   select e);
                    break;

                case Zilf.ObjectOrdering.Default:
                default:
                    // reverse mention order
                    order.AddRange(from e in objectsByName.Values
                                   orderby e.MentionOrder descending
                                   select e);
                    break;
            }

            foreach (var entry in order)
            {
                if (entry.Object != null)
                {
                    yield return entry.Object;
                }
                else
                {
                    Contract.Assume(entry.InitialMention != null);
                    Errors.CompWarning(ctx, entry.InitialMention, "mentioned object {0} is never defined", entry.Name);
                    yield return new ZilModelObject(entry.Name, new ZilList[0], false);
                }
            }
        }

        private static bool IsRoom(ZilModelObject obj)
        {
            if (obj == null)
                return false;

            if (obj.IsRoom)
                return true;

            var parent = GetObjectParentName(obj);
            if (parent == null)
                return false;

            return parent.StdAtom == StdAtom.ROOMS;
        }

        private static bool IsLocalGlobal(ZilModelObject obj)
        {
            if (obj == null)
                return false;

            var parent = GetObjectParentName(obj);
            if (parent == null)
                return false;

            return parent.StdAtom == StdAtom.LOCAL_GLOBALS;
        }

        private static ZilAtom GetObjectParentName(ZilModelObject obj)
        {
            Contract.Requires(obj != null);

            foreach (var p in obj.Properties)
            {
                var name = p.First as ZilAtom;
                if (name == null)
                    continue;

                if (name.StdAtom == StdAtom.LOC ||
                    (name.StdAtom == StdAtom.IN && p.Count() == 2))
                {
                    return p.Rest.First as ZilAtom;
                }
            }

            return null;
        }

        public IEnumerable<ZilModelObject> ObjectsInInsertionOrder()
        {
            switch (this.TreeOrdering)
            {
                case TreeOrdering.Default:
                    /* insert objects in source code order, except that the first
                     * defined child of each parent is inserted last */

                    var result = new List<ZilModelObject>(Objects);
                    var objectsByParent = Objects.ToLookup(obj => GetObjectParentName(obj));

                    foreach (var obj in Objects)
                    {
                        // find the object's first-defined child and move it after the last-defined child
                        var first = objectsByParent[obj.Name].FirstOrDefault();
                        var last = objectsByParent[obj.Name].LastOrDefault();

                        if (first != last)
                        {
                            result.Remove(first);
                            result.Insert(result.IndexOf(last) + 1, first);
                        }
                    }

                    return result;

                case TreeOrdering.ReverseDefined:
                    // insert objects in source code order, period.
                    return Objects;

                default:
                    throw new NotImplementedException();
            }
        }

        private void MakeZcharCountCache()
        {
            if (zcharCountCache == null)
            {
                // Charset 0 takes one Z-char.
                // Charset 1 and 2 take two Z-chars.
                // Everything else takes 4 Z-chars.

                zcharCountCache = new byte[256];

                for (int i = 0; i < zcharCountCache.Length; i++)
                    zcharCountCache[i] = 4;

                foreach (char c in charset2)
                    zcharCountCache[(byte)c] = 2;

                foreach (char c in charset1)
                    zcharCountCache[(byte)c] = 2;

                foreach (char c in charset0)
                    zcharCountCache[(byte)c] = 1;
            }
        }

        /// <summary>
        /// Determines how many of the characters in a string will be preserved by vocabulary encoding,
        /// given the current Z-machine settings.
        /// </summary>
        /// <param name="word">The string that will be encoded.</param>
        /// <returns>The number of significant characters, between 0 and the length of the word (inclusive).</returns>
        private int CountVocabZCharacters(string word)
        {
            Contract.Requires(word != null);

            MakeZcharCountCache();

            int result = 0;

            foreach (char c in word)
            {
                var zchars = (c <= 255) ? zcharCountCache[c] : 4;
                result += zchars;
            }

            return result;
        }

        public void MergeVocabulary()
        {
            Contract.Ensures(Vocabulary.Count <= Contract.OldValue(Vocabulary.Count));

            //XXX merge words that are indistinguishable because of the vocabulary resolution

            /* NOTE: words may end with incomplete multi-ZChar constructs that are still
             * significant for lexing, so even if the printed forms of two vocab words are
             * the same, they may still be distinguishable.
             */
        }

        public bool IsLongWord(Word word)
        {
            Contract.Requires(word != null);

            var text = word.Atom.Text.ToLower();
            return CountVocabZCharacters(text) > (ZVersion >= 4 ? 9 : 6);
        }

        public bool TryGetBitSynonym(ZilAtom alias, out ZilAtom original)
        {
            Contract.Requires(alias != null);
            Contract.Ensures(Contract.ValueAtReturn(out original) != null || Contract.Result<bool>() == false);

            return BitSynonyms.TryGetValue(alias, out original);
        }

        public void AddBitSynonym(ZilAtom alias, ZilAtom target)
        {
            Contract.Requires(alias != null);
            Contract.Requires(target != null);

            if (ctx.GetZVal(alias) != null)
            {
                throw new InterpreterError(string.Format("{0} is already defined", alias, target));
            }

            ZilAtom original;
            if (TryGetBitSynonym(target, out original))
            {
                target = original;
            }

            BitSynonyms[alias] = target;

            var zval = ctx.GetZVal(target);
            if (zval != null)
            {
                ctx.SetZVal(target, zval);
            }
        }

        public void EnsureMinimumHeaderExtension(int words)
        {
            Contract.Requires(words >= 0);
            Contract.Ensures(HeaderExtensionWords >= Contract.OldValue(HeaderExtensionWords));
            Contract.Ensures(HeaderExtensionWords >= words);

            HeaderExtensionWords = Math.Max(HeaderExtensionWords, words);
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

    class Syntax : IProvideSourceLine
    {
        public readonly int NumObjects;
        public readonly Word Verb, Preposition1, Preposition2;
        public readonly ScopeFlags Options1, Options2;
        public readonly ZilAtom FindFlag1, FindFlag2;
        public readonly ZilAtom Action, Preaction, ActionName;
        public readonly IList<ZilAtom> Synonyms;

        private static readonly ZilAtom[] EmptySynonyms = new ZilAtom[0];

        public Syntax(ISourceLine src, Word verb, int numObjects, Word prep1, Word prep2,
            ScopeFlags options1, ScopeFlags options2, ZilAtom findFlag1, ZilAtom findFlag2,
            ZilAtom action, ZilAtom preaction, ZilAtom actionName,
            IEnumerable<ZilAtom> synonyms = null)
        {
            Contract.Requires(verb != null);
            Contract.Requires(numObjects >= 0 & numObjects <= 2);
            Contract.Requires(action != null);

            this.SourceLine = src;

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
            this.ActionName = actionName;

            if (synonyms == null)
                this.Synonyms = EmptySynonyms;
            else
                this.Synonyms = new List<ZilAtom>(synonyms).AsReadOnly();
        }

        public static Syntax Parse(ISourceLine src, IEnumerable<ZilObject> definition, Context ctx)
        {
            Contract.Requires(definition != null);
            Contract.Requires(ctx != null);
            Contract.Ensures(Contract.Result<Syntax>() != null);

            int numObjects = 0;
            ZilAtom verb = null, prep1 = null, prep2 = null;
            ZilAtom action = null, preaction = null, actionName = null;
            ZilList bits1 = null, find1 = null, bits2 = null, find2 = null, syns = null;
            bool rightSide = false;
            int rhsCount = 0;

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
                            atom = list.First as ZilAtom;
                            if (atom == null)
                                throw new InterpreterError("list in syntax definition must start with an atom");

                            if (numObjects == 0)
                            {
                                // could be a list of synonyms, but could also be a mistake (scope/find flags in the wrong place)
                                switch (atom.StdAtom)
                                {
                                    case StdAtom.FIND:
                                    case StdAtom.TAKE:
                                    case StdAtom.HAVE:
                                    case StdAtom.MANY:
                                    case StdAtom.HELD:
                                    case StdAtom.CARRIED:
                                    case StdAtom.ON_GROUND:
                                    case StdAtom.IN_ROOM:
                                        Errors.TerpWarning(ctx, src, "ignoring list of flags in syntax definition with no preceding OBJECT");
                                        break;

                                    default:
                                        if (syns != null)
                                            throw new InterpreterError("too many synonym lists in syntax definition");

                                        syns = list;
                                        break;
                                }
                            }
                            else
                            {
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
                        }
                        else
                            throw new InterpreterError("unexpected value in syntax definition");
                    }
                }
                else
                {
                    // right side:
                    //   action [preaction [action-name]]
                    ZilAtom atom = obj as ZilAtom;
                    if (atom != null)
                    {
                        if (atom.StdAtom == StdAtom.Eq)
                            throw new InterpreterError("too many = in syntax definition");
                    }
                    else if (!(obj is ZilFalse))
                    {
                        throw new InterpreterError("values after = must be FALSE or atoms");
                    }

                    switch (rhsCount)
                    {
                        case 0:
                            action = atom;
                            break;

                        case 1:
                            preaction = atom;
                            break;

                        case 2:
                            actionName = atom;
                            break;

                        default:
                            throw new InterpreterError("too many values after = in syntax definition");
                    }

                    rhsCount++;
                }
            }

            // validation
            Contract.Assume(numObjects <= 2);
            if (numObjects < 1)
            {
                prep1 = null;
                find1 = null;
                bits1 = null;
            }
            if (numObjects < 2)
            {
                prep2 = null;
                find2 = null;
                bits2 = null;
            }

            Word verbWord = ctx.ZEnvironment.GetVocabVerb(verb, src);
            Word word1 = (prep1 == null) ? null : ctx.ZEnvironment.GetVocabPreposition(prep1, src);
            Word word2 = (prep2 == null) ? null : ctx.ZEnvironment.GetVocabPreposition(prep2, src);
            ScopeFlags flags1 = ParseScopeFlags(bits1);
            ScopeFlags flags2 = ParseScopeFlags(bits2);
            ZilAtom findFlag1 = ParseFindFlag(find1);
            ZilAtom findFlag2 = ParseFindFlag(find2);
            IEnumerable<ZilAtom> synAtoms = null;

            if (syns != null)
            {
                if (!syns.All(s => s is ZilAtom))
                    throw new InterpreterError("verb synonyms must be atoms");

                synAtoms = syns.Cast<ZilAtom>();
            }

            if (action == null)
            {
                throw new InterpreterError("action routine must be specified");
            }

            if (actionName == null)
            {
                var sb = new StringBuilder(action.ToString());
                if (sb.Length > 2 && sb[0] == 'V' && sb[1] == '-')
                {
                    sb[1] = '?';
                }
                else
                {
                    sb.Insert(0, "V?");
                }

                actionName = ZilAtom.Parse(sb.ToString(), ctx);
            }
            else
            {
                var actionNameStr = actionName.ToString();
                if (!actionNameStr.StartsWith("V?"))
                {
                    actionName = ZilAtom.Parse("V?" + actionNameStr, ctx);
                }
            }

            return new Syntax(
                src,
                verbWord, numObjects,
                word1, word2, flags1, flags2, findFlag1, findFlag2,
                action, preaction, actionName, synAtoms);
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

        public ISourceLine SourceLine { get; set; }
    }

    class Word
    {
        public readonly ZilAtom Atom;
        public PartOfSpeech PartOfSpeech;
        public PartOfSpeech SynonymTypes;

        private readonly Dictionary<PartOfSpeech, byte> speechValues = new Dictionary<PartOfSpeech, byte>(2);
        private readonly Dictionary<PartOfSpeech, ISourceLine> definitions = new Dictionary<PartOfSpeech, ISourceLine>(2);

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
            Contract.Requires(ctx != null);
            return ctx.GetGlobalOption(StdAtom.NEW_VOC_P);
        }

        private bool IsCompactVocab(Context ctx)
        {
            Contract.Requires(ctx != null);
            return ctx.GetGlobalOption(StdAtom.COMPACT_VOCABULARY_P);
        }

        /// <summary>
        /// Checks whether adding a new part of speech should set the relevant First flag.
        /// </summary>
        /// <param name="ctx">The current context.</param>
        /// <returns>true if the new part of speech should set the First flag.</returns>
        private bool ShouldSetFirst(Context ctx)
        {
            Contract.Requires(ctx != null);

            // if no parts of speech are set yet, this is easy
            if (this.PartOfSpeech == Zilf.PartOfSpeech.None)
                return true;

            // never add First flags to buzzwords
            if ((this.PartOfSpeech & Zilf.PartOfSpeech.Buzzword) != 0)
                return false;

            // ignore parts of speech that don't record values in the current context
            var pos = this.PartOfSpeech;
            if (ctx.ZEnvironment.ZVersion >= 4)
                pos &= ~Zilf.PartOfSpeech.Adjective;
            if (IsNewVoc(ctx))
                pos &= ~Zilf.PartOfSpeech.Object;
            if (IsCompactVocab(ctx))
                pos &= ~(Zilf.PartOfSpeech.Object | Zilf.PartOfSpeech.Preposition);

            return pos == Zilf.PartOfSpeech.None;
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
        private void CheckTooMany(Context ctx)
        {
            Contract.Requires(ctx != null);

            byte b = (byte)(PartOfSpeech & ~PartOfSpeech.FirstMask);
            byte count = 0;

            while (b != 0)
            {
                b &= (byte)(b - 1);
                count++;
            }

            bool freeObject = false, freeAdjective = false, freePrep = false, freeBuzz = false;

            // when ,NEW-VOC? or ,COMPACT-VOCABULARY? are true, Object is free
            bool newVoc = IsNewVoc(ctx);
            bool compactVocab = IsCompactVocab(ctx);
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
                Errors.CompWarning(ctx, string.Format("too many parts of speech for {0}: {1}", Atom, ListDefinitionLocations()));

                /* The order we trim is mostly arbitrary, except that adjective and object are first
                 * since they can sometimes be recognized without the part-of-speech flags. */
                var partsToTrim = new[] {
                    new { part = PartOfSpeech.Adjective, free = freeAdjective },
                    new { part = PartOfSpeech.Object, free = freeObject },
                    new { part = PartOfSpeech.Buzzword, free = freeBuzz },
                    new { part = PartOfSpeech.Preposition, free = freePrep },
                    new { part = PartOfSpeech.Verb, free = false },
                    new { part = PartOfSpeech.Direction, free = false },
                };

                foreach (var trim in partsToTrim)
                {
                    if ((PartOfSpeech & trim.part) != 0 && !trim.free)
                    {
                        Errors.CompWarning(ctx, GetDefinition(trim.part),
                            string.Format("discarding the {0} part of speech for {1}", trim.part, Atom));

                        UnsetPartOfSpeech(ctx, trim.part);

                        count--;
                        if (count <= 2)
                            break;
                    }
                }
            }
        }

        private string ListDefinitionLocations()
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
            Contract.Ensures((PartOfSpeech & Zilf.PartOfSpeech.Object) != 0);

            if ((PartOfSpeech & PartOfSpeech.Object) == 0)
            {
                // there is no PartOfSpeech.ObjectFirst, so don't change the First flags

                PartOfSpeech |= PartOfSpeech.Object;
                speechValues[Zilf.PartOfSpeech.Object] = 1;
                definitions[Zilf.PartOfSpeech.Object] = location;
            }
        }

        public void SetVerb(Context ctx, ISourceLine location, byte value)
        {
            Contract.Requires(ctx != null);
            Contract.Ensures((PartOfSpeech & Zilf.PartOfSpeech.Verb) != 0);

            if ((PartOfSpeech & PartOfSpeech.Verb) == 0)
            {
                if (ShouldSetFirst(ctx))
                    PartOfSpeech |= PartOfSpeech.VerbFirst;

                PartOfSpeech |= PartOfSpeech.Verb;
                speechValues[Zilf.PartOfSpeech.Verb] = value;
                definitions[Zilf.PartOfSpeech.Verb] = location;
            }
        }

        public void SetAdjective(Context ctx, ISourceLine location, byte value)
        {
            Contract.Requires(ctx != null);
            Contract.Ensures((PartOfSpeech & Zilf.PartOfSpeech.Adjective) != 0);

            if ((PartOfSpeech & PartOfSpeech.Adjective) == 0)
            {
                if (ctx.ZEnvironment.ZVersion < 4 && ShouldSetFirst(ctx))
                    PartOfSpeech |= PartOfSpeech.AdjectiveFirst;

                PartOfSpeech |= PartOfSpeech.Adjective;
                speechValues[Zilf.PartOfSpeech.Adjective] = value;
                definitions[Zilf.PartOfSpeech.Adjective] = location;
            }
        }

        public void SetDirection(Context ctx, ISourceLine location, byte value)
        {
            Contract.Requires(ctx != null);
            Contract.Ensures((PartOfSpeech & Zilf.PartOfSpeech.Direction) != 0);

            if ((PartOfSpeech & PartOfSpeech.Direction) == 0)
            {
                if (ShouldSetFirst(ctx))
                    PartOfSpeech |= PartOfSpeech.DirectionFirst;

                PartOfSpeech |= PartOfSpeech.Direction;
                speechValues[Zilf.PartOfSpeech.Direction] = value;
                definitions[Zilf.PartOfSpeech.Direction] = location;
            }
        }

        public void SetBuzzword(Context ctx, ISourceLine location, byte value)
        {
            Contract.Requires(ctx != null);
            Contract.Ensures((PartOfSpeech & Zilf.PartOfSpeech.Buzzword) != 0);

            if ((PartOfSpeech & Zilf.PartOfSpeech.Buzzword) == 0)
            {
                // buzzword value comes before everything but preposition, except in CompactVocab
                if (!IsCompactVocab(ctx))
                    PartOfSpeech &= ~PartOfSpeech.FirstMask;

                PartOfSpeech |= PartOfSpeech.Buzzword;
                speechValues[Zilf.PartOfSpeech.Buzzword] = value;
                definitions[Zilf.PartOfSpeech.Buzzword] = location;
            }
        }

        public void SetPreposition(Context ctx, ISourceLine location, byte value)
        {
            Contract.Requires(ctx != null);
            Contract.Ensures((PartOfSpeech & Zilf.PartOfSpeech.Preposition) != 0);

            if ((PartOfSpeech & PartOfSpeech.Preposition) == 0)
            {
                // preposition value is always first, except in CompactVocab
                if (!IsCompactVocab(ctx))
                    PartOfSpeech &= ~PartOfSpeech.FirstMask;

                PartOfSpeech |= PartOfSpeech.Preposition;
                speechValues[Zilf.PartOfSpeech.Preposition] = value;
                definitions[Zilf.PartOfSpeech.Preposition] = location;
            }
        }

        private void UnsetPartOfSpeech(Context ctx, PartOfSpeech part)
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
                    case Zilf.PartOfSpeech.Verb:
                        SetVerb(ctx, p.location, p.value);
                        break;
                    case Zilf.PartOfSpeech.Adjective:
                        SetAdjective(ctx, p.location, p.value);
                        break;
                    case Zilf.PartOfSpeech.Direction:
                        SetDirection(ctx, p.location, p.value);
                        break;
                    case Zilf.PartOfSpeech.Buzzword:
                        SetBuzzword(ctx, p.location, p.value);
                        break;
                    case Zilf.PartOfSpeech.Preposition:
                        SetPreposition(ctx, p.location, p.value);
                        break;
                    case Zilf.PartOfSpeech.Object:
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
                // for CompactVocab and NewVoc, don't write a value for Object
                if (!compactVocab && !IsNewVoc(ctx))
                {
                    // there is no ObjectFirst, so keep it first if all other First flags are clear
                    if ((pos & Zilf.PartOfSpeech.FirstMask) == 0)
                        partsToWrite.Insert(0, Zilf.PartOfSpeech.Object);
                    else
                        partsToWrite.Add(Zilf.PartOfSpeech.Object);
                }
            }
            if ((pos & Zilf.PartOfSpeech.Buzzword) != 0)
            {
                // for CompactVocab, don't write a value for Buzzword
                if (!compactVocab)
                {
                    // there is no BuzzwordFirst: Buzzword comes before everything but Preposition
                    partsToWrite.Insert(0, Zilf.PartOfSpeech.Buzzword);
                }
            }
            if ((pos & Zilf.PartOfSpeech.Preposition) != 0)
            {
                // for CompactVocab, don't write a value for Preposition
                if (!compactVocab)
                {
                    // there is no PrepositionFirst because Preposition always comes first
                    Contract.Assume((pos & Zilf.PartOfSpeech.FirstMask) == 0);
                    partsToWrite.Insert(0, Zilf.PartOfSpeech.Preposition);
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

        public void MarkAsSynonym(PartOfSpeech synonymTypes)
        {
            this.SynonymTypes |= synonymTypes;
        }

        public bool IsSynonym(PartOfSpeech synonymTypes)
        {
            return (this.SynonymTypes & synonymTypes) != 0;
        }
    }

    class Synonym
    {
        public readonly Word OriginalWord;
        public readonly Word SynonymWord;

        public Synonym(Word original, Word synonym)
        {
            Contract.Requires(original != null);
            Contract.Requires(synonym != null);

            this.OriginalWord = original;
            this.SynonymWord = synonym;
        }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(OriginalWord != null);
            Contract.Invariant(SynonymWord != null);
        }

        public virtual void Apply(Context ctx)
        {
            Contract.Requires(ctx != null);

            if ((OriginalWord.PartOfSpeech & PartOfSpeech.Adjective) != 0)
                SynonymWord.SetAdjective(ctx, OriginalWord.GetDefinition(PartOfSpeech.Adjective), OriginalWord.GetValue(PartOfSpeech.Adjective));

            if ((OriginalWord.PartOfSpeech & PartOfSpeech.Buzzword) != 0)
                SynonymWord.SetBuzzword(ctx, OriginalWord.GetDefinition(PartOfSpeech.Buzzword), OriginalWord.GetValue(PartOfSpeech.Buzzword));

            if ((OriginalWord.PartOfSpeech & PartOfSpeech.Direction) != 0)
                SynonymWord.SetDirection(ctx, OriginalWord.GetDefinition(PartOfSpeech.Direction), OriginalWord.GetValue(PartOfSpeech.Direction));

            if ((OriginalWord.PartOfSpeech & PartOfSpeech.Object) != 0)
                SynonymWord.SetObject(ctx, OriginalWord.GetDefinition(PartOfSpeech.Object));

            if ((OriginalWord.PartOfSpeech & PartOfSpeech.Preposition) != 0)
                SynonymWord.SetPreposition(ctx, OriginalWord.GetDefinition(PartOfSpeech.Preposition), OriginalWord.GetValue(PartOfSpeech.Preposition));

            if ((OriginalWord.PartOfSpeech & PartOfSpeech.Verb) != 0)
                SynonymWord.SetVerb(ctx, OriginalWord.GetDefinition(PartOfSpeech.Verb), OriginalWord.GetValue(PartOfSpeech.Verb));

            SynonymWord.MarkAsSynonym(OriginalWord.PartOfSpeech & ~PartOfSpeech.FirstMask);
        }
    }

    class VerbSynonym : Synonym
    {
        public VerbSynonym(Word original, Word synonym)
            : base(original, synonym)
        {
            Contract.Requires(original != null);
            Contract.Requires(synonym != null);
        }
    }

    class PrepSynonym : Synonym
    {
        public PrepSynonym(Word original, Word synonym)
            : base(original, synonym)
        {
            Contract.Requires(original != null);
            Contract.Requires(synonym != null);
        }
    }

    class AdjSynonym : Synonym
    {
        public AdjSynonym(Word original, Word synonym)
            : base(original, synonym)
        {
            Contract.Requires(original != null);
            Contract.Requires(synonym != null);
        }
    }

    class DirSynonym : Synonym
    {
        public DirSynonym(Word original, Word synonym)
            : base(original, synonym)
        {
            Contract.Requires(original != null);
            Contract.Requires(synonym != null);
        }
    }
}
