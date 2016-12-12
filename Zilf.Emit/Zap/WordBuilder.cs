namespace Zilf.Emit.Zap
{
    class WordBuilder : TableBuilder, IWordBuilder
    {
        readonly string word;

        public WordBuilder(string tableName, string word)
            : base(tableName)
        {
            this.word = word;
        }

        public string Word
        {
            get { return word; }
        }
    }
}