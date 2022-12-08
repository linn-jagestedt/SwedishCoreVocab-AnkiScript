namespace DeckGenerator 
{
    public struct AnkiCard 
    {
        public string Question = "";
        public string Word = "";
        public string Class = "";
        public string Gender = "";
        public string Abreviations = "";
        public string EnglishDefinition = "";
        public string SwedishDefinition = "";
        public string Sentence = "";
        public string Audio = "";
        public string Frequency = "";
        public string Tags = "";

        public AnkiCard() 
        {
            Question = "";
            Word = "";
            Class = "";
            Gender = "";
            Abreviations = "";
            EnglishDefinition = "";
            SwedishDefinition = "";
            Sentence = "";
            Audio = "";
            Frequency = "";
            Tags = "";
        }

        public override string ToString() => string.Join("\t", ToArray());

        public string[] ToArray() => new string[] { 
            Question, 
            Word, 
            Class, 
            Gender,
            Abreviations, 
            EnglishDefinition, 
            SwedishDefinition,
            Sentence, 
            Audio, 
            Frequency, 
            Tags 
        };
    }
}