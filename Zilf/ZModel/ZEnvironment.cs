/* Copyright 2010, 2015 Jesse McGrew
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
using Zilf.StringEncoding;
using Zilf.ZModel.Values;
using Zilf.ZModel.Vocab;
using Zilf.ZModel.Vocab.NewParser;
using Zilf.ZModel.Vocab.OldParser;
using Zilf.Diagnostics;

namespace Zilf.ZModel
{
    /// <summary>
    /// Holds the state built up during the load phase for the compiler to use.
    /// </summary>
    class ZEnvironment
    {
        readonly Context ctx;
        IVocabFormat vocabFormat;

        public int ZVersion = 3;
        public bool TimeStatusLine;
        public ZilAtom EntryRoutineName;

        public readonly List<ZilRoutine> Routines = new List<ZilRoutine>();
        public readonly List<ZilConstant> Constants = new List<ZilConstant>();
        public readonly List<ZilGlobal> Globals = new List<ZilGlobal>();
        public readonly List<ZilModelObject> Objects = new List<ZilModelObject>();
        public readonly List<ZilTable> Tables = new List<ZilTable>();

        public readonly Dictionary<ZilAtom, ZilObject> PropertyDefaults;
        public readonly Dictionary<ZilAtom, ZilAtom> BitSynonyms;
        public readonly List<ZilAtom> FlagsOrderedLast = new List<ZilAtom>();

        public readonly List<Syntax> Syntaxes = new List<Syntax>();
        public readonly Dictionary<ZilAtom, IWord> Vocabulary;
        public readonly List<Synonym> Synonyms = new List<Synonym>();
        public readonly List<ZilAtom> Directions = new List<ZilAtom>();
        public readonly List<KeyValuePair<ZilAtom, ISourceLine>> Buzzwords = new List<KeyValuePair<ZilAtom, ISourceLine>>();

        public readonly Dictionary<ZilAtom, ZilAtom> InternedGlobalNames;

        public ObjectOrdering ObjectOrdering = ObjectOrdering.Default;
        public TreeOrdering TreeOrdering = TreeOrdering.Default;

        public readonly List<TellPattern> TellPatterns = new List<TellPattern>();

        /// <summary>
        /// The last direction defined with &lt;DIRECTIONS&gt;.
        /// </summary>
        public ZilAtom LowDirection;

        public byte NextAction;         // V? (intentions)

        public int HeaderExtensionWords;

        byte[] zcharCountCache;   // char -> # of Z-chars
        string charset0, charset1, charset2;

        /// <summary>
        /// Compares a Z-machine version number against a range,
        /// treating versions 7 and 8 the same as 5.
        /// </summary>
        /// <param name="candidate">The version number to check.</param>
        /// <param name="rangeMin">The minimum allowed version.</param>
        /// <param name="rangeMax">The maximum allowed version,
        /// or null to allow any version above <paramref name="rangeMin"/>.</param>
        /// <returns><b>true</b> if <paramref name="candidate"/> is within the range;
        /// otherwise <b>false</b>.</returns>
        public static bool VersionMatches(int candidate, int rangeMin, int? rangeMax)
        {
            // treat V7-8 just like V5 for the purpose of this check
            if (candidate == 7 || candidate == 8)
                candidate = 5;

            return (candidate >= rangeMin) && (rangeMax == null || candidate <= rangeMax);
        }

        public bool VersionMatches(int rangeMin, int? rangeMax)
        {
            return VersionMatches(this.ZVersion, rangeMin, rangeMax);
        }

        public IVocabFormat VocabFormat
        {
            get
            {
                if (vocabFormat == null)
                {
                    if (ctx.GetGlobalOption(StdAtom.NEW_PARSER_P))
                        vocabFormat = new NewParserVocabFormat(ctx);
                    else
                        vocabFormat = new OldParserVocabFormat(ctx);
                }

                return vocabFormat;
            }
        }

        public string Charset0
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);
                return charset0;
            }
            set
            {
                Contract.Requires(value != null);
                Contract.Ensures(zcharCountCache == null);

                charset0 = value;
                zcharCountCache = null;
            }
        }

        public string Charset1
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);
                return charset1;
            }
            set
            {
                Contract.Requires(value != null);
                Contract.Ensures(zcharCountCache == null);

                charset1 = value;
                zcharCountCache = null;
            }
        }

        public string Charset2
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);
                return charset2;
            }
            set
            {
                Contract.Requires(value != null);
                Contract.Ensures(zcharCountCache == null);

                charset2 = value;
                zcharCountCache = null;
            }
        }

        /// <summary>
        /// Gets or sets the language used for special character encodings.
        /// Setting this does not automatically update the charsets.
        /// </summary>
        public Language Language { get; set; }

        /// <summary>
        /// Gets or sets the character used for language-specific character
        /// escapes. If null, language-specific characters are not decoded.
        /// </summary>
        public char? LanguageEscapeChar { get; set; }

        public ZEnvironment(Context ctx)
        {
            Contract.Requires(ctx != null);

            this.ctx = ctx;

            var defaultLang = Language.Get("DEFAULT");
            Contract.Assume(defaultLang != null);

            this.Language = defaultLang;
            this.Charset0 = defaultLang.Charset0;
            this.Charset1 = defaultLang.Charset1;
            this.Charset2 = defaultLang.Charset2;

            var equalizer = new AtomNameEqualityComparer(ctx.IgnoreCase);

            PropertyDefaults = new Dictionary<ZilAtom, ZilObject>(equalizer);
            BitSynonyms = new Dictionary<ZilAtom, ZilAtom>(equalizer);
            Vocabulary = new Dictionary<ZilAtom, IWord>(equalizer);
            InternedGlobalNames = new Dictionary<ZilAtom, ZilAtom>(equalizer);
        }

        [ContractInvariantMethod]
        void ObjectInvariant()
        {
            Contract.Invariant(this.Language != null);
        }

        public IWord GetVocab(ZilAtom text)
        {
            IWord result;

            if (Vocabulary.TryGetValue(text, out result) == false)
            {
                result = VocabFormat.CreateWord(text);
                Vocabulary.Add(text, result);
            }

            return result;
        }

        public IWord GetVocabPreposition(ZilAtom text, ISourceLine location)
        {
            var result = GetVocab(text);
            VocabFormat.MakePreposition(result, location);
            return result;
        }

        public IWord GetVocabAdjective(ZilAtom text, ISourceLine location)
        {
            var result = GetVocab(text);
            VocabFormat.MakeAdjective(result, location);
            return result;
        }

        public IWord GetVocabNoun(ZilAtom text, ISourceLine location)
        {
            var result = GetVocab(text);
            ctx.ZEnvironment.VocabFormat.MakeObject(result, location);
            return result;
        }

        public IWord GetVocabBuzzword(ZilAtom text, ISourceLine location)
        {
            var result = GetVocab(text);
            VocabFormat.MakeBuzzword(result, location);
            return result;
        }

        public IWord GetVocabVerb(ZilAtom text, ISourceLine location)
        {
            var result = GetVocab(text);
            VocabFormat.MakeVerb(result, location);
            return result;
        }

        public IWord GetVocabDirection(ZilAtom text, ISourceLine location)
        {
            var result = GetVocab(text);
            VocabFormat.MakeDirection(result, location);
            return result;
        }

        public IWord GetVocabSyntaxPreposition(ZilAtom text, ISourceLine location)
        {
            var result = GetVocab(text);
            VocabFormat.MakeSyntaxPreposition(result, location);
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

        static IEnumerable<ZilAtom> ObjectNamesMentionedInProperty(ZilList prop)
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

        class ObjectOrderingEntry
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

            var objectsByName = new Dictionary<ZilAtom, ObjectOrderingEntry>(Objects.Count, new AtomNameEqualityComparer(ctx.IgnoreCase));

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
                case ObjectOrdering.Defined:
                    order.AddRange(from e in objectsByName.Values
                                   orderby e.DefinitionOrder ascending,
                                           e.MentionOrder ascending
                                   select e);
                    break;

                case ObjectOrdering.RoomsFirst:
                    order.AddRange(from e in objectsByName.Values
                                   orderby IsRoom(e.Object) descending,
                                           e.MentionOrder ascending
                                   select e);
                    break;

                case ObjectOrdering.RoomsAndLocalGlobalsFirst:
                    order.AddRange(from e in objectsByName.Values
                                   orderby IsRoom(e.Object) || IsLocalGlobal(e.Object) descending,
                                           e.MentionOrder ascending
                                   select e);
                    break;

                case ObjectOrdering.RoomsLast:
                    order.AddRange(from e in objectsByName.Values
                                   orderby IsRoom(e.Object) ascending,
                                           e.MentionOrder ascending
                                   select e);
                    break;

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
                    ctx.HandleWarning(new CompilerError(entry.InitialMention, CompilerMessages.Mentioned_Object_0_Is_Never_Defined, entry.Name));
                    yield return new ZilModelObject(entry.Name, new ZilList[0], false);
                }
            }
        }

        static bool IsRoom(ZilModelObject obj)
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

        static bool IsLocalGlobal(ZilModelObject obj)
        {
            if (obj == null)
                return false;

            var parent = GetObjectParentName(obj);
            if (parent == null)
                return false;

            return parent.StdAtom == StdAtom.LOCAL_GLOBALS;
        }

        static ZilAtom GetObjectParentName(ZilModelObject obj)
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

        void MakeZcharCountCache()
        {
            Contract.Ensures(zcharCountCache != null);

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
        int CountVocabZCharacters(string word)
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

        /// <summary>
        /// Merges words that are indistinguishable because of the vocabulary resolution.
        /// </summary>
        /// <param name="notifyMerge">A callback to notify the caller that the first word
        /// has absorbed the second, and any references to the second should be retargeted
        /// to the first.</param>
        public void MergeVocabulary(Action<IWord, IWord> notifyMerge)
        {
            Contract.Ensures(Vocabulary.Count <= Contract.OldValue(Vocabulary.Count));

            /* NOTE: words may end with incomplete multi-ZChar constructs that are still
             * significant for lexing, so even if the printed forms of two vocab words are
             * the same, they may still be distinguishable.
             */

            // initialize string encoder
            var encoder = new StringEncoder();
            encoder.SetCharset(0, charset0.Select(c => StringEncoder.UnicodeToZscii(c)));
            encoder.SetCharset(1, charset1.Select(c => StringEncoder.UnicodeToZscii(c)));
            encoder.SetCharset(2, charset2.Select(c => StringEncoder.UnicodeToZscii(c)));

            var resolution = ZVersion >= 4 ? 9 : 6;
            var groupedWords =
                from pair in this.Vocabulary
                orderby pair.Key.Text
                let enc = new EncodedWord(encoder.Encode(pair.Value.Atom.Text.ToLower(), resolution, StringEncoderMode.NoAbbreviations))
                group new { Atom = pair.Key, Word = pair.Value } by enc;

            foreach (var g in groupedWords)
            {
                if (g.Take(2).Count() == 2)
                {
                    // found a collision: merge words[1..N] into words[0]
                    var words = g.ToArray();
                    for (int i = 1; i < words.Length; i++)
                    {
                        VocabFormat.MergeWords(words[0].Word, words[i].Word);
                        notifyMerge(words[0].Word, words[i].Word);
                        this.Vocabulary[words[i].Atom] = this.Vocabulary[words[0].Atom];
                    }

                    // merge back into words[1..N]
                    for (int i = 1; i < words.Length; i++)
                    {
                        VocabFormat.MergeWords(words[i].Word, words[0].Word);
                    }
                }
            }
        }

        struct EncodedWord : IEquatable<EncodedWord>
        {
            readonly byte[] data;

            public EncodedWord(byte[] data)
            {
                this.data = data;
            }

            public bool Equals(EncodedWord other)
            {
                if (other.data == this.data)
                    return true;

                if (other.data.Length != this.data.Length)
                    return false;

                for (int i = 0; i < data.Length; i++)
                {
                    if (other.data[i] != this.data[i])
                        return false;
                }

                return true;
            }

            public override bool Equals(object obj)
            {
                return obj is EncodedWord && Equals((EncodedWord)obj);
            }

            public override int GetHashCode()
            {
                int result = data.Length;

                foreach (byte b in data)
                    result = unchecked(result * 13 + b);

                return result;
            }

            public override string ToString()
            {
                if (data == null)
                    return "[null]";

                var sb = new StringBuilder();
                sb.Append('[');

                foreach (byte b in data)
                {
                    if (sb.Length > 1)
                        sb.Append(',');

                    sb.Append(b);
                }

                sb.Append(']');
                return sb.ToString();
            }
        }

        public bool IsLongWord(IWord word)
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
                throw new ArgumentException("Alias is already defined", nameof(alias));
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

        public ZilAtom InternGlobalName(ZilAtom atom)
        {
            ZilAtom result;
            if (InternedGlobalNames.TryGetValue(atom, out result))
                return result;

            InternedGlobalNames.Add(atom, atom);
            return atom;
        }
    }
}
