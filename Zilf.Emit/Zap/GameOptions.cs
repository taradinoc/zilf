namespace Zilf.Emit.Zap
{
    public class GameOptions : IGameOptions
    {
        private GameOptions()
        {
        }

        public sealed class V3 : GameOptions
        {
            public bool TimeStatusLine { get; set; }
            public bool SoundEffects { get; set; }
        }

        public sealed class V4 : GameOptions
        {
            public bool SoundEffects { get; set; }
        }

        public sealed class V5 : GameOptions
        {
            public bool DisplayOps { get; set; }
            public bool Undo { get; set; }
            public bool Mouse { get; set; }
            public bool Color { get; set; }
            public bool SoundEffects { get; set; }

            public ITableBuilder HeaderExtensionTable { get; set; }

            public string Charset0 { get; set; }
            public string Charset1 { get; set; }
            public string Charset2 { get; set; }

            public int LanguageId { get; set; }
            public char? LanguageEscapeChar { get; set; }
        }
    }
}