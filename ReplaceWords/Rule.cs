namespace ReplaceWords
{
    public class Rule
    {
        public Rule(string searchTerm, string replacementTerm, string examples, int count, int uniqueCount)
        {
            SearchTerm = searchTerm;
            ReplacementTerm = replacementTerm;
            Examples = examples;
            DisplayCount = count + $" ({uniqueCount} unique)";
        }
        public string SearchTerm { get; set; }
        public string ReplacementTerm { get; set; }
        public string Examples { get; set; }
        public string DisplayCount { get; set; }
    }
}
