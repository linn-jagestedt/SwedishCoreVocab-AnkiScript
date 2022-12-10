namespace DeckGenerator 
{
    public struct AnkiCard 
    {
        public string Word = "";
        public string WrittenForm = "";
        public string Class = "";
        public string Gender = "";
        public string Abreviation = "";
        public string EnglishDefinition = "";
        public string SwedishDefinition = "";
        public string Sentence = "";
        public string Audio = "";
        public string Frequency = "";
        public string Tags = "";

        public AnkiCard() 
        {
            Word = "";
            WrittenForm = "";
            Class = "";
            Gender = "";
            Abreviation = "";
            EnglishDefinition = "";
            SwedishDefinition = "";
            Sentence = "";
            Audio = "";
            Frequency = "";
            Tags = "";
        }

        public override string ToString() => string.Join("\t", ToArray());

        public string[] ToArray() => new string[] { 
            Word, 
            WrittenForm, 
            Class, 
            Gender,
            Abreviation, 
            EnglishDefinition, 
            SwedishDefinition,
            Sentence, 
            Audio, 
            Frequency, 
            Tags 
        };
    }
}