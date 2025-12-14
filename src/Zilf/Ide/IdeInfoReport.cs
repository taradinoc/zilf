using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Nodes;
using Zilf.Diagnostics;
using Zilf.Compiler.Builtins;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.ZModel;
using Zilf.ZModel.Vocab;
using Zilf.ZModel.Vocab.Glulx;
using Zilf.ZModel.Vocab.NewParser;
using Zilf.ZModel.Vocab.OldParser;
using Zilf.ZModel.Values;

namespace Zilf.Ide
{
    internal static class IdeInfoReport
    {
        public static JsonObject Build(Context ctx, string? inputFile)
        {
            var zenv = ctx.ZEnvironment;

            var result = new JsonObject
            {
                ["format"] = "zilf-ide-info",
                ["formatVersion"] = 1,
                ["inputFile"] = inputFile,
                ["success"] = ctx.ErrorCount == 0,
                ["errorCount"] = ctx.ErrorCount,
                ["warningCount"] = ctx.WarningCount,
                ["suppressedWarningCount"] = ctx.SuppressedWarningCount,
            };

            result["target"] = BuildTarget(ctx);
            result["filesRead"] = new JsonArray(ctx.GetSourceFilesRead().Select(p => (JsonNode)p).ToArray());
            result["prefixMacros"] = new JsonArray(ctx.ParserMacros.GetInstalledPrefixMacros().Select(c => (JsonNode)c.ToString()).ToArray());
            result["compilationFlags"] = BuildCompilationFlags(ctx);
            result["defaultDefinitions"] = BuildDefaultDefinitions(ctx);
            result["diagnostics"] = BuildDiagnostics(ctx);

            result["callables"] = new JsonObject
            {
                ["mdl"] = BuildMdlCallables(ctx),
                ["zcode"] = BuildZcodeCallables(ctx),
            };

            result["entities"] = BuildEntities(ctx);

            return result;
        }

        static JsonArray BuildDiagnostics(Context ctx)
        {
            var items = ctx.Diagnostics
                .Select(DiagnosticToJson)
                .ToArray();

            return new JsonArray(items);
        }

        static JsonObject DiagnosticToJson(Diagnostic diag)
        {
            var obj = new JsonObject
            {
                ["code"] = diag.Code,
                ["severity"] = diag.Severity.ToString(),
                ["noisy"] = diag.Noisy,
                ["message"] = diag.GetFormattedMessage(),
                ["origin"] = Origin(diag.Location),
            };

            if (!string.IsNullOrEmpty(diag.StackTrace))
                obj["stackTrace"] = diag.StackTrace;

            if (diag.SubDiagnostics.Count > 0)
                obj["subDiagnostics"] = new JsonArray(diag.SubDiagnostics.Select(DiagnosticToJson).ToArray());
            else
                obj["subDiagnostics"] = new JsonArray();

            return obj;
        }

        static JsonObject BuildTarget(Context ctx)
        {
            var zenv = ctx.ZEnvironment;

            // VERSION? clauses are evaluated when their condition matches the current target.
            // Report which query-name groups are active for this build.
            var versionQueryNames = new JsonArray();
            void AddVersionQueryNames(bool active, params string[] names)
            {
                versionQueryNames.Add(new JsonObject
                {
                    ["names"] = new JsonArray(names.Select(n => (JsonNode)n).ToArray()),
                    ["active"] = active
                });
            }

            bool isGlulx32 = ctx.ZEnvironment.TargetPlatform == TargetPlatform.Glulx32;
            AddVersionQueryNames(!isGlulx32 && zenv.ZVersion == 3, "ZIP", "3");
            AddVersionQueryNames(!isGlulx32 && zenv.ZVersion == 4, "EZIP", "4");
            AddVersionQueryNames(!isGlulx32 && zenv.ZVersion == 5, "XZIP", "5");
            AddVersionQueryNames(!isGlulx32 && zenv.ZVersion == 6, "YZIP", "6");
            AddVersionQueryNames(!isGlulx32 && zenv.ZVersion == 7, "7");
            AddVersionQueryNames(!isGlulx32 && zenv.ZVersion == 8, "8");
            AddVersionQueryNames(isGlulx32, "GLULX");

            return new JsonObject
            {
                ["platform"] = zenv.TargetPlatform.ToString(),
                ["zVersion"] = zenv.ZVersion,
                ["versionQueryNames"] = versionQueryNames,
            };
        }

        static JsonArray BuildMdlCallables(Context ctx)
        {
            var items = new List<JsonObject>();

            foreach (var (name, binding) in ctx.GetGlobalBindings())
            {
                var value = binding.Value;
                if (value == null)
                    continue;

                switch (value)
                {
                    case ZilFSubr:
                        items.Add(Callable(name, "fsubr", null));
                        break;
                    case ZilSubr:
                        items.Add(Callable(name, "subr", null));
                        break;
                    case ZilFunction:
                        items.Add(Callable(name, "define", value.SourceLine ?? name.SourceLine));
                        break;
                    case ZilEvalMacro:
                        items.Add(Callable(name, "defmac", value.SourceLine ?? name.SourceLine));
                        break;
                }
            }

            return new JsonArray(items.OrderBy(i => (string?)i["name"], StringComparer.OrdinalIgnoreCase).ToArray());
        }

        static JsonArray BuildZcodeCallables(Context ctx)
        {
            var zenv = ctx.ZEnvironment;
            var items = new List<JsonObject>();

            foreach (var name in ZBuiltins.GetBuiltinNames().OrderBy(n => n, StringComparer.OrdinalIgnoreCase))
            {
                items.Add(new JsonObject
                {
                    ["name"] = name,
                    ["kind"] = "zbuiltin",
                });
            }

            foreach (var routine in zenv.Routines)
            {
                if (routine.Name == null)
                    continue;

                items.Add(new JsonObject
                {
                    ["name"] = routine.Name.Text,
                    ["kind"] = "routine",
                    ["origin"] = Origin(routine.Name.SourceLine ?? routine.SourceLine)
                });
            }

            foreach (var (name, binding) in ctx.GetGlobalBindings())
            {
                var value = binding.Value;
                if (value is ZilEvalMacro)
                {
                    items.Add(Callable(name, "defmac", value.SourceLine ?? name.SourceLine));
                }
            }

            return new JsonArray(items.OrderBy(i => (string?)i["name"], StringComparer.OrdinalIgnoreCase).ToArray());
        }

        static JsonObject BuildEntities(Context ctx)
        {
            var zenv = ctx.ZEnvironment;

            return new JsonObject
            {
                ["objects"] = new JsonArray(zenv.Objects.Select(o => Entity(o.Name, o.IsRoom ? "room" : "object", o.SourceLine ?? o.Name.SourceLine)).ToArray()),
                ["constants"] = new JsonArray(zenv.Constants.Select(c => Entity(c.Name, c.SourceLine ?? c.Name.SourceLine)).ToArray()),
                ["globals"] = new JsonArray(zenv.Globals.Select(g => Entity(g.Name, g.SourceLine ?? g.Name.SourceLine)).ToArray()),
                ["tables"] = new JsonArray(zenv.Tables.Select(t => Entity(t.Name ?? "<unnamed>", t.SourceLine)).ToArray()),
                ["properties"] = new JsonArray(zenv.PropertyDefaults.Keys.Select(a => Entity(a, a.SourceLine)).ToArray()),
                ["flags"] = BuildFlags(ctx),
                ["flagSynonyms"] = new JsonArray(zenv.BitSynonyms.Select(kvp => new JsonObject
                {
                    ["alias"] = kvp.Key.Text,
                    ["original"] = kvp.Value.Text,
                    ["origin"] = Origin(kvp.Key.SourceLine)
                }).ToArray()),
                ["vocabFormat"] = GetVocabFormat(ctx),
                ["vocabWords"] = BuildVocabWords(ctx),
                ["vocabSynonyms"] = new JsonArray(zenv.Synonyms.Select(s => new JsonObject
                {
                    ["synonym"] = s.SynonymWord.Atom.Text,
                    ["original"] = s.OriginalWord.Atom.Text,
                    ["origin"] = Origin(s.SynonymWord.Atom.SourceLine)
                }).ToArray()),
                ["verbs"] = BuildVerbs(ctx),
                ["actions"] = BuildActions(ctx),
            };
        }

        static string GetVocabFormat(Context ctx)
        {
            var zenv = ctx.ZEnvironment;

            // There should be only one vocab format in use at a time.
            if (zenv.Vocabulary.Values.Any(v => v is NewParserWord))
                return "new";
            if (zenv.Vocabulary.Values.Any(v => v is GlulxParserWord))
                return "glulx";
            if (zenv.Vocabulary.Values.Any(v => v is OldParserWord))
                return "old";

            return "unknown";
        }

        static JsonArray BuildVerbs(Context ctx)
        {
            var zenv = ctx.ZEnvironment;
            var verbs = zenv.Syntaxes.Select(s => s.Verb.Atom).Distinct(new AtomNameEqualityComparer(ctx.IgnoreCase));
            return new JsonArray(verbs.Select(v => new JsonObject
            {
                ["name"] = v.Text,
                ["origin"] = Origin(v.SourceLine)
            }).OrderBy(o => (string?)o["name"], StringComparer.OrdinalIgnoreCase).ToArray());
        }

        static JsonArray BuildActions(Context ctx)
        {
            var zenv = ctx.ZEnvironment;

            var actions = zenv.Syntaxes
                .Select(s => new { s.ActionName, s.SourceLine })
                .Where(x => x.ActionName != null)
                .GroupBy(x => x.ActionName, new AtomNameEqualityComparer(ctx.IgnoreCase))
                .Select(g => new { Atom = g.Key, SourceLine = g.Select(x => x.SourceLine).FirstOrDefault() })
                .ToArray();

            return new JsonArray(actions.Select(a => new JsonObject
            {
                ["name"] = a.Atom.Text,
                ["origin"] = Origin(a.Atom.SourceLine ?? a.SourceLine)
            }).OrderBy(o => (string?)o["name"], StringComparer.OrdinalIgnoreCase).ToArray());
        }

        static JsonArray BuildFlags(Context ctx)
        {
            var zenv = ctx.ZEnvironment;

            var equalizer = new AtomNameEqualityComparer(ctx.IgnoreCase);
            var flags = new Dictionary<ZilAtom, ISourceLine?>(equalizer);

            static ISourceLine? PreferOrigin(ISourceLine? current, ISourceLine? candidate)
            {
                if (candidate == null)
                    return current;

                if (current == null)
                    return candidate;

                // Prefer file-based origins when available.
                if (current is FileSourceLine)
                    return current;
                if (candidate is FileSourceLine)
                    return candidate;

                return current;
            }

            void AddFlag(ZilAtom atom, ISourceLine? origin)
            {
                var original = zenv.TryGetBitSynonym(atom, out var orig) ? orig : atom;

                if (flags.TryGetValue(original, out var existing))
                    flags[original] = PreferOrigin(existing, origin);
                else
                    flags.Add(original, origin);
            }

            // 1) Flags referenced by objects via (FLAGS ...)
            foreach (var obj in zenv.Objects)
            {
                foreach (var prop in obj.Properties)
                {
                    if (!prop.IsCons(out var head, out var body) || head is not ZilAtom propName)
                        continue;

                    if (propName.StdAtom != StdAtom.FLAGS)
                        continue;

                    foreach (var flagAtom in body.OfType<ZilAtom>())
                    {
                        AddFlag(flagAtom, flagAtom.SourceLine ?? prop.SourceLine ?? obj.SourceLine ?? obj.Name.SourceLine);
                    }
                }
            }

            // 2) Flags explicitly ordered last.
            foreach (var a in zenv.FlagsOrderedLast)
                AddFlag(a, a.SourceLine);

            // 3) Flags used in syntax find flags.
            foreach (var syn in zenv.Syntaxes)
            {
                if (syn.FindFlag1 != null)
                    AddFlag(syn.FindFlag1, syn.FindFlag1.SourceLine ?? syn.SourceLine);
                if (syn.FindFlag2 != null)
                    AddFlag(syn.FindFlag2, syn.FindFlag2.SourceLine ?? syn.SourceLine);
            }

            // 4) Original flags referenced by aliases.
            foreach (var a in zenv.BitSynonyms.Values)
                AddFlag(a, a.SourceLine);

            return new JsonArray(flags
                .Select(kvp => Entity(kvp.Key, kvp.Value ?? kvp.Key.SourceLine))
                .OrderBy(o => (string?)o["name"], StringComparer.OrdinalIgnoreCase)
                .ToArray());
        }

        static JsonArray BuildVocabWords(Context ctx)
        {
            var zenv = ctx.ZEnvironment;

            var words = zenv.Vocabulary
                .Select(kvp => new { Atom = kvp.Key, Word = kvp.Value })
                .OrderBy(x => x.Atom.Text, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var arr = new JsonArray();
            foreach (var w in words)
            {
                var obj = new JsonObject
                {
                    ["word"] = w.Atom.Text,
                };

                switch (w.Word)
                {
                    case OldParserWord opw:
                        obj["partsOfSpeech"] = PartsOfSpeech(opw.PartOfSpeech, part => opw.GetDefinition(part));
                        break;
                    case GlulxParserWord gpw:
                        obj["partsOfSpeech"] = PartsOfSpeech(gpw.PartOfSpeech, part => gpw.GetDefinition(part));
                        break;
                    case NewParserWord npw:
                        obj["classification"] = npw.Classification;
                        obj["partsOfSpeech"] = new JsonArray();
                        break;
                    default:
                        obj["partsOfSpeech"] = new JsonArray();
                        break;
                }

                arr.Add(obj);
            }

            return arr;
        }

        static JsonArray PartsOfSpeech(PartOfSpeech parts, Func<PartOfSpeech, ISourceLine> getDefinition)
        {
            static IEnumerable<PartOfSpeech> EnumerateBaseParts()
            {
                yield return PartOfSpeech.Object;
                yield return PartOfSpeech.Verb;
                yield return PartOfSpeech.Adjective;
                yield return PartOfSpeech.Direction;
                yield return PartOfSpeech.Buzzword;
                yield return PartOfSpeech.Preposition;
            }

            var arr = new JsonArray();
            foreach (var part in EnumerateBaseParts())
            {
                if ((parts & part) == 0)
                    continue;

                arr.Add(new JsonObject
                {
                    ["part"] = part.ToString(),
                    ["origin"] = Origin(getDefinition(part))
                });
            }

            return arr;
        }

        static JsonArray BuildCompilationFlags(Context ctx)
        {
            var flags = ctx.EnumerateCompilationFlags()
                .OrderBy(x => x.Name.Text, StringComparer.OrdinalIgnoreCase)
                .Select(x => new JsonObject
                {
                    ["name"] = x.Name.Text,
                    ["value"] = ZilValue(x.Value),
                    ["origin"] = Origin(x.Name.SourceLine)
                })
                .ToArray();

            return new JsonArray(flags);
        }

        static JsonArray BuildDefaultDefinitions(Context ctx)
        {
            var replaceProp = ctx.GetStdAtom(StdAtom.REPLACE_DEFINITION);
            var defaultMarker = ctx.GetStdAtom(StdAtom.DEFAULT_DEFINITION);
            var delayMarker = ctx.GetStdAtom(StdAtom.DELAY_DEFINITION);

            var items = new List<JsonObject>();

            foreach (var atom in ctx.ZEnvironment.InternedGlobalNames.Values.Distinct(new AtomNameEqualityComparer(ctx.IgnoreCase)))
            {
                var state = ctx.GetProp(atom, replaceProp);
                if (state == null)
                    continue;

                var status = state switch
                {
                    ZilAtom a when ReferenceEquals(a, defaultMarker) => "default",
                    ZilAtom a when ReferenceEquals(a, delayMarker) => "delayed",
                    ZilAtom a when ReferenceEquals(a, replaceProp) => "replaced",
                    ZilVector => "replacement-pending",
                    _ => "unknown"
                };

                items.Add(new JsonObject
                {
                    ["name"] = atom.Text,
                    ["status"] = status,
                    ["origin"] = Origin(atom.SourceLine)
                });
            }

            return new JsonArray(items.OrderBy(i => (string?)i["name"], StringComparer.OrdinalIgnoreCase).ToArray());
        }

        static JsonObject Callable(ZilAtom name, string kind, ISourceLine? src)
        {
            var obj = new JsonObject
            {
                ["name"] = name.Text,
                ["kind"] = kind,
            };

            if (!IsInternalCallableKind(kind))
                obj["origin"] = Origin(src);

            return obj;
        }

        static bool IsInternalCallableKind(string kind) =>
            string.Equals(kind, "subr", StringComparison.OrdinalIgnoreCase)
            || string.Equals(kind, "fsubr", StringComparison.OrdinalIgnoreCase)
            || string.Equals(kind, "zbuiltin", StringComparison.OrdinalIgnoreCase);

        static JsonObject Entity(ZilAtom name, string kind, ISourceLine? src) =>
            new()
            {
                ["name"] = name.Text,
                ["kind"] = kind,
                ["origin"] = Origin(src)
            };

        static JsonObject Entity(ZilAtom name, ISourceLine? src) =>
            new()
            {
                ["name"] = name.Text,
                ["origin"] = Origin(src)
            };

        static JsonObject Entity(string name, ISourceLine? src) =>
            new()
            {
                ["name"] = name,
                ["origin"] = Origin(src)
            };

        static JsonObject Origin(ISourceLine? src, string? fallbackKind = null)
        {
            if (src is FileSourceLine fsl)
            {
                if (IsInternalName(fsl.FileName))
                {
                    return new JsonObject
                    {
                        ["kind"] = "internal"
                    };
                }

                return new JsonObject
                {
                    ["kind"] = "file",
                    ["file"] = fsl.FileName,
                    ["line"] = fsl.Line
                };
            }

            if (src == null)
            {
                return new JsonObject
                {
                    ["kind"] = fallbackKind ?? "internal"
                };
            }

            if (IsInternalName(src.SourceInfo))
            {
                return new JsonObject
                {
                    ["kind"] = "internal"
                };
            }

            return new JsonObject
            {
                ["kind"] = fallbackKind ?? "other",
                ["text"] = src.SourceInfo
            };
        }

        static bool IsInternalName(string? text)
        {
            if (string.IsNullOrEmpty(text))
                return true;

            return text.Length >= 2 && text[0] == '<' && text[^1] == '>';
        }

        static JsonNode? ZilValue(ZilObject? value)
        {
            if (value == null)
                return null;

            if (value.IsTrue)
                return true;

            if (value is ZilFalse)
                return false;

            return value switch
            {
                ZilFix fix => fix.Value,
                ZilString str => str.Text,
                _ => value.ToString(),
            };
        }
    }
}
