/* Copyright 2010-2023 Tara McGrew
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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Zilf.Common;
using Zilf.Common.StringEncoding;
using Zilf.Diagnostics;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.ZModel.Values;
using Zilf.ZModel.Vocab;
using Zilf.ZModel.Vocab.NewParser;
using Zilf.ZModel.Vocab.OldParser;

namespace Zilf.ZModel
{
    /// <summary>
    /// Holds the state built up during the load phase for the compiler to use.
    /// </summary>
    sealed class ZEnvironment
    {
        readonly Context ctx;
        IVocabFormat? vocabFormat;

        public int ZVersion = 3;
        public bool TimeStatusLine;
        public ZilAtom? EntryRoutineName;

        public readonly List<ZilRoutine> Routines = new();

        public readonly List<ZilConstant> Constants = new();

        public readonly List<ZilGlobal> Globals = new();

        public readonly List<ZilModelObject> Objects = new();

        public readonly List<ZilTable> Tables = new();

        /// <summary>
        /// Maps property names to default property values.
        /// (Note: keys are compared by name, not by reference.)
        /// </summary>
        public readonly Dictionary<ZilAtom, ZilObject> PropertyDefaults;

        /// <summary>
        /// Maps flag aliases to original flags.
        /// (Note: keys are compared by name, not by reference.)
        /// </summary>
        public readonly Dictionary<ZilAtom, ZilAtom> BitSynonyms;

        public readonly List<ZilAtom> FlagsOrderedLast = new();

        public readonly List<Syntax> Syntaxes = new();

        /// <summary>
        /// Maps vocab word atoms to parser-specific word structures.
        /// (Note: keys are compared by name, not by reference.)
        /// </summary>
        public readonly Dictionary<ZilAtom, IWord> Vocabulary;

        public readonly List<Synonym> Synonyms = new();

        public readonly List<ZilAtom> Directions = new();

        public readonly List<KeyValuePair<ZilAtom, ISourceLine>> Buzzwords = new();

        /// <summary>
        /// Maps global symbol atoms to the first atom used to define a
        /// global symbol with that name.
        /// (Note: keys are compared by name, not by reference.)
        /// </summary>
        /// <remarks>
        /// This is used for most named ZIL entities that aren't local to a routine:
        /// routines, constants, global variables, objects, flags,
        /// definition sections, etc.
        /// </remarks>
        /// <seealso cref="InternGlobalName"/>
        public readonly Dictionary<ZilAtom, ZilAtom> InternedGlobalNames;

        public ObjectOrdering ObjectOrdering = ObjectOrdering.Default;
        public TreeOrdering TreeOrdering = TreeOrdering.Default;

        public readonly List<TellPattern> TellPatterns = new();

        /// <summary>
        /// The last direction defined with &lt;DIRECTIONS&gt;.
        /// </summary>
        public ZilAtom? LowDirection;

        public ushort NextAction;         // V? (intentions)

        public int HeaderExtensionWords;

        byte[]? zcharCountCache;   // char -> # of Z-chars
        string charset0, charset1, charset2;

        /// <summary>
        /// Compares a Z-machine version number against a range,
        /// treating versions 7 and 8 the same as 5.
        /// </summary>
        /// <param name="candidate">The version number to check.</param>
        /// <param name="rangeMin">The minimum allowed version.</param>
        /// <param name="rangeMax">The maximum allowed version,
        /// or null to allow any version above <paramref name="rangeMin"/>.</param>
        /// <returns><see langword="true"/> if <paramref name="candidate"/> is within the range;
        /// otherwise <see langword="false"/>.</returns>
        public static bool VersionMatches(int candidate, int rangeMin, int? rangeMax)
        {
            // treat V7-8 just like V5 for the purpose of this check
            if (candidate == 7 || candidate == 8)
                candidate = 5;

            return (candidate >= rangeMin) && (rangeMax == null || candidate <= rangeMax);
        }

        /// <summary>
        /// Compares the selected Z-machine version number against a range,
        /// treating versions 7 and 8 the same as 5.
        /// </summary>
        /// <param name="rangeMin">The minimum allowed version.</param>
        /// <param name="rangeMax">The maximum allowed version,
        /// or null to allow any version above <paramref name="rangeMin"/>.</param>
        /// <returns><see langword="true"/> if <see cref="ZVersion"/> is within the range;
        /// otherwise <see langword="false"/>.</returns>
        public bool VersionMatches(int rangeMin, int? rangeMax)
        {
            return VersionMatches(ZVersion, rangeMin, rangeMax);
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
                Debug.Assert(charset0 != null);
                return charset0;
            }

            set
            {
                charset0 = value;
                zcharCountCache = null;
            }
        }

        public string Charset1
        {
            get
            {
                Debug.Assert(charset1 != null);
                return charset1;
            }

            set
            {
                charset1 = value;
                zcharCountCache = null;
            }
        }

        public string Charset2
        {
            get
            {
                Debug.Assert(charset2 != null);
                return charset2;
            }

            set
            {
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
            this.ctx = ctx;

            var defaultLang = Language.Default;

            Language = defaultLang;
            charset0 = defaultLang.Charset0;
            charset1 = defaultLang.Charset1;
            charset2 = defaultLang.Charset2;

            var equalizer = new AtomNameEqualityComparer(ctx.IgnoreCase);

            PropertyDefaults = new Dictionary<ZilAtom, ZilObject>(equalizer);
            BitSynonyms = new Dictionary<ZilAtom, ZilAtom>(equalizer);
            Vocabulary = new Dictionary<ZilAtom, IWord>(equalizer);
            InternedGlobalNames = new Dictionary<ZilAtom, ZilAtom>(equalizer);
        }

        /// <summary>
        /// Gets the vocab word corresponding to an atom, creating it if needed.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public IWord GetVocab(ZilAtom text)
        {
            if (Vocabulary.TryGetValue(text, out var result))
                return result;

            var newWord = VocabFormat.CreateWord(text);
            Vocabulary.Add(text, newWord);
            return newWord;
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
            if (prop.First is ZilAtom atom && prop.Rest?.First != null)
            {
                switch (atom.StdAtom)
                {
                    case StdAtom.LOC:
                        if (prop.Rest.First is ZilAtom loc)
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

        sealed class ObjectOrderingEntry
        {
            public readonly ZilAtom Name;
            public ZilModelObject? Object;
            public readonly ISourceLine? InitialMention;      // only set for objects created from mentions
            public int? DefinitionOrder;
            public readonly int MentionOrder;

            public ObjectOrderingEntry(ZilAtom name, ZilModelObject? obj, ISourceLine? initialMention,
                int? definitionOrder, int mentionOrder)
            {
                Name = name;
                Object = obj;
                InitialMention = initialMention;
                DefinitionOrder = definitionOrder;
                MentionOrder = mentionOrder;
            }
        }

        public IEnumerable<ZilModelObject> ObjectsInDefinitionOrder(Func<ZilAtom, string?> getGlobalDefinitionType)
        {
            /* first, collect objects and note the order(s) in which they were defined and mentioned,
             * where "mentioned" means either defined or used as the IN/LOC/GLOBAL of another object */

            var objectsByName = new Dictionary<ZilAtom, ObjectOrderingEntry>(Objects.Count, new AtomNameEqualityComparer(ctx.IgnoreCase));

            int definitionOrder = 0, mentionOrder = 0;

            foreach (var obj in Objects)
            {
                var atom = obj.Name;

                // add this object if it hasn't already been added
                if (!objectsByName.TryGetValue(atom, out var entry))
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

            switch (ObjectOrdering)
            {
                case ObjectOrdering.Defined:
                    order.AddRange(from e in objectsByName.Values
                                   orderby e.DefinitionOrder, e.MentionOrder
                                   select e);
                    break;

                case ObjectOrdering.RoomsFirst:
                    order.AddRange(from e in objectsByName.Values
                                   orderby IsRoom(e.Object) descending, e.MentionOrder
                                   select e);
                    break;

                case ObjectOrdering.RoomsAndLocalGlobalsFirst:
                    order.AddRange(from e in objectsByName.Values
                                   orderby IsRoom(e.Object) || IsLocalGlobal(e.Object) descending, e.MentionOrder
                                   select e);
                    break;

                case ObjectOrdering.RoomsLast:
                    order.AddRange(from e in objectsByName.Values
                                   orderby IsRoom(e.Object), e.MentionOrder
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
                    var prevDefType = getGlobalDefinitionType(entry.Name);
                    if (prevDefType != null)
                    {
                        ctx.HandleError(new CompilerError(entry.InitialMention,
                            CompilerMessages.Mentioned_Object_0_Is_Defined_Elsewhere_As_A_1, entry.Name, prevDefType));
                        continue;
                    }

                    ctx.HandleError(new CompilerError(entry.InitialMention,
                        CompilerMessages.Mentioned_Object_0_Is_Never_Defined, entry.Name));
                    yield return new ZilModelObject(entry.Name, Array.Empty<ZilList>(), false);
                }
            }
        }

        static bool IsRoom(ZilModelObject? obj) =>
            obj != null && (obj.IsRoom || GetObjectParentName(obj)?.StdAtom == StdAtom.ROOMS);

        static bool IsLocalGlobal(ZilModelObject? obj) =>
            obj != null && GetObjectParentName(obj)?.StdAtom == StdAtom.LOCAL_GLOBALS;

        static ZilAtom? GetObjectParentName(ZilModelObject obj)
        {
            foreach (var p in obj.Properties)
            {
                switch (p.First)
                {
                    case ZilAtom { StdAtom: StdAtom.LOC }:
                    case ZilAtom { StdAtom: StdAtom.IN } when p.Count() == 2:
                        Debug.Assert(p.Rest != null);
                        return p.Rest.First as ZilAtom;
                }
            }

            return null;
        }

        public IEnumerable<ZilModelObject> ObjectsInInsertionOrder()
        {
            switch (TreeOrdering)
            {
                case TreeOrdering.Default:
                    /* insert objects in source code order, except that the first
                     * defined child of each parent is inserted last */

                    var result = new List<ZilModelObject>(Objects);
                    var objectsByParent = Objects.ToLookup(GetObjectParentName);

                    foreach (var obj in Objects)
                    {
                        if (!objectsByParent.Contains(obj.Name))
                            continue;

                        // find the object's first-defined child and move it after the last-defined child
                        var first = objectsByParent[obj.Name].First();
                        var last = objectsByParent[obj.Name].Last();

                        if (first == last)
                            continue;

                        result.Remove(first);
                        result.Insert(result.IndexOf(last) + 1, first);
                    }

                    return result;

                case TreeOrdering.ReverseDefined:
                    // insert objects in source code order, period.
                    return Objects;

                default:
                    throw UnhandledCaseException.FromEnum(TreeOrdering);
            }
        }

        void MakeZcharCountCache()
        {
            // TODO: combine ZEnvironment.MakeZcharCountCache with the zchar counting/caching logic in StringEncoder

            if (charset0 == null || charset1 == null || charset2 == null)
                throw new InvalidOperationException("Missing charset(s)");

            if (zcharCountCache != null)
                return;

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

        /// <summary>
        /// Determines how many of the characters in a string will be preserved by vocabulary encoding,
        /// given the current Z-machine settings.
        /// </summary>
        /// <param name="word">The string that will be encoded.</param>
        /// <returns>The number of significant characters, between 0 and the length of the word (inclusive).</returns>
        int CountVocabZCharacters(string word)
        {
            MakeZcharCountCache();

            return word.Sum(c => (c <= 255) ? zcharCountCache![c] : 4);
        }

        /// <summary>
        /// A callback used to report that two words are being merged due to vocabulary resolution.
        /// </summary>
        /// <param name="mainWord">The first word.</param>
        /// <param name="duplicateWord">The second word.</param>
        /// <param name="blameV3"><b>true</b> if the current target is V3, and V4's higher vocab resolution would
        /// make the words distinguishable. <b>false</b> if the current target is already V4+, or if the words would
        /// still have to be merged in V4+.
        /// </param>
        public delegate void NotifyVocabularyMergeDelegate(IWord mainWord, IWord duplicateWord, bool blameV3);

        /// <summary>
        /// Merges words that are indistinguishable because of the vocabulary resolution.
        /// </summary>
        /// <param name="notifyMerge">A callback to notify the caller that the first word
        /// has absorbed the second, and any references to the second should be retargeted
        /// to the first.</param>
        [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Not normalizing.")]
        public void MergeVocabulary(NotifyVocabularyMergeDelegate notifyMerge)
        {
            /* NOTE: words may end with incomplete multi-ZChar constructs that are still
             * significant for lexing, so even if the printed forms of two vocab words are
             * the same, they may still be distinguishable.
             */

            // initialize string encoder
            var encoder = new StringEncoder();
            encoder.SetCharset(0, charset0.Select(UnicodeTranslation.ToZscii));
            encoder.SetCharset(1, charset1.Select(UnicodeTranslation.ToZscii));
            encoder.SetCharset(2, charset2.Select(UnicodeTranslation.ToZscii));

            var resolution = ZVersion >= 4 ? 9 : 6;
            var groupedWords =
                from pair in Vocabulary
                orderby pair.Key.Text
                let enc = new EncodedWord(encoder.Encode(pair.Value.Atom.Text.ToLowerInvariant(), resolution, StringEncoderMode.NoAbbreviations))
                group new { Atom = pair.Key, Word = pair.Value } by enc;

            foreach (var g in groupedWords)
            {
                if (!g.Skip(1).Any())
                    continue;

                // found a collision: merge words[1..N] into words[0]
                var words = g.ToArray();
                for (int i = 1; i < words.Length; i++)
                {
                    VocabFormat.MergeWords(words[0].Word, words[i].Word);
                    notifyMerge(words[0].Word, words[i].Word, resolution == 6 && CanBlameV3(words[0].Word, words[1].Word));
                    Vocabulary[words[i].Atom] = Vocabulary[words[0].Atom];
                }

                // merge back into words[1..N]
                for (int i = 1; i < words.Length; i++)
                {
                    VocabFormat.MergeWords(words[i].Word, words[0].Word);
                }
            }

            bool CanBlameV3(IWord word1, IWord word2)
            {
                // re-encode with V4+'s higher resolution and see if they become distinguishable
                const int upgradedResolution = 9;
                var upgraded1 = encoder.Encode(word1.Atom.Text.ToLowerInvariant(), upgradedResolution, StringEncoderMode.NoAbbreviations);
                var upgraded2 = encoder.Encode(word2.Atom.Text.ToLowerInvariant(), upgradedResolution, StringEncoderMode.NoAbbreviations);
                return !upgraded1.SequenceEqual(upgraded2);
            }
        }

        readonly struct EncodedWord : IEquatable<EncodedWord>
        {
            readonly byte[] data;
            readonly int hashCode;

            public EncodedWord(byte[] data)
            {
                this.data = data;
                hashCode = ((System.Collections.IStructuralEquatable)data).GetHashCode(EqualityComparer<byte>.Default);
            }

            [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0")]
            public bool Equals(EncodedWord other)
            {
                if (other.hashCode != hashCode)
                    return false;

                return data.SequenceEqual(other.data);
            }

            public override bool Equals(object? obj) => obj is EncodedWord word && Equals(word);

            public override int GetHashCode()
            {
                return hashCode;
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

        [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Z-machine requirement")]
        public bool IsLongWord(IWord word)
        {
            var text = word.Atom.Text.ToLowerInvariant();
            return CountVocabZCharacters(text) > (ZVersion >= 4 ? 9 : 6);
        }

        public bool TryGetBitSynonym(ZilAtom alias, [NotNullWhen(true)] out ZilAtom? original) =>
            BitSynonyms.TryGetValue(alias, out original);

        /// <exception cref="ArgumentException"><paramref name="alias"/> is already defined.</exception>
        public void AddBitSynonym(ZilAtom alias, ZilAtom target)
        {
            if (ctx.GetZVal(alias) != null)
            {
                throw new ArgumentException("Alias is already defined", nameof(alias));
            }

            if (TryGetBitSynonym(target, out var original))
            {
                target = original;
            }

            BitSynonyms[alias] = target;

            if (ctx.GetZVal(target) is { } zval)
            {
                ctx.SetZVal(alias, zval);
            }
        }

        public void EnsureMinimumHeaderExtension(int words)
        {
            HeaderExtensionWords = Math.Max(HeaderExtensionWords, words);
        }

        public ZilAtom InternGlobalName(ZilAtom atom)
        {
            if (InternedGlobalNames.TryGetValue(atom, out var result))
                return result;

            InternedGlobalNames.Add(atom, atom);
            return atom;
        }
    }
}
