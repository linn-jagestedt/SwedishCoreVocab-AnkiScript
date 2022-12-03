namespace DeckGenerator 
{
    public struct AnkiCard 
    {
        public string Question = "";
        public string Word = "";
        public string Class = "";
        public string Gramar = "";
        public string Abreviations = "";
        public string Definition = "";
        public string Example = "";
        public string ExampleTranslated = "";
        public string Audio = "";
        public string KellyID = "";
        public string Tags = "";

        public AnkiCard() 
        {
            Question = "";
            Word = "";
            Class = "";
            Gramar = "";
            Abreviations = "";
            Definition = "";
            Example = "";
            ExampleTranslated = "";
            Audio = "";
            KellyID = "";
            Tags = "";
        }

        public override string ToString() => string.Join("\t", ToArray());

        public string[] ToArray() => new string[] { 
            Question, 
            Word, 
            Class, 
            Gramar,
            Abreviations, 
            Definition, 
            Example, 
            ExampleTranslated, 
            Audio, 
            KellyID, 
            Tags 
        };
    }
}