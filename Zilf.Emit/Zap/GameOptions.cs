using JetBrains.Annotations;

namespace Zilf.Emit.Zap
{
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
    public class GameOptions : IGameOptions
    {
        GameOptions()
        {
        }

        public sealed class V3 : GameOptions
        {
            public bool TimeStatusLine { get; set; }
            public bool SoundEffects { get; set; }
        }

        public abstract class V4Plus : GameOptions
        {
            public bool SoundEffects { get; set; }
        }

        public sealed class V4 : V4Plus { }

        public abstract class V5Plus : V4Plus
        {
            public bool DisplayOps { get; set; }
            public bool Undo { get; set; }
            public bool Mouse { get; set; }
            public bool Color { get; set; }

            public ITableBuilder HeaderExtensionTable { get; set; }

            public string Charset0 { get; set; }
            public string Charset1 { get; set; }
            public string Charset2 { get; set; }

            public int LanguageId { get; set; }
            public char? LanguageEscapeChar { get; set; }
        }

        public sealed class V5 : V5Plus { }

        public sealed class V6 : V5Plus
        {
            public bool Menus { get; set; }
        }
    }
}