using System;
using System.Text;
using AnkiNet;

namespace DeckGenerator
{
    public static class Program 
    {
        public struct AnkiCard 
        {
            public string Question = "";
            public string Word = "";
            public string Class = "";
            public string Gramar = "";
            public string Definition = "";
            public string Example = "";
            public string ExampleTranslated = "";
            public string Audio = "";
            public string Tags = "";

            public AnkiCard() 
            {
                Question = "";
                Word = "";
                Class = "";
                Gramar = "";
                Definition = "";
                Example = "";
                ExampleTranslated = "";
                Audio = "";
                Tags = "";
            }

            public override string ToString() => 
                string.Join("\t", Question, Word, Class, Gramar, Definition, Example, ExampleTranslated, Audio, Tags);

            public string[] ToArray() =>
                new string[] { Question, Word, Class, Gramar, Definition, Example, ExampleTranslated, Audio, Tags };
        }

        public static void Main() 
        {
            if (!Directory.Exists("output")) {
                Directory.CreateDirectory("output");
            }

            if (!Directory.Exists("output/card")) {
                Directory.CreateDirectory("output/card");
            }

            foreach (string file in Directory.GetFiles("src/card")) {
                System.IO.File.Copy(file, "output/card/" + file.Split('/').Last(), true);
            }

            if (!Directory.Exists("output/collection.media")) {
                Directory.CreateDirectory("output/collection.media");
            }

            foreach (string file in Directory.GetFiles("src/collection.media")) {
                System.IO.File.Copy(file, "output/collection.media/" + file.Split('/').Last(), true);
            }

            SweToEngDictionary sweToEngDict = new SweToEngDictionary(XmlDictionary.Deserialize("src/People's_Dictionary.xml"));
            WordList wordList = new WordList(XmlLexicalResource.Deserialize("src/Common_Words.xml"));

            string[] lines = System.IO.File.ReadAllLines("src/Rivstart_Words.tsv");
            Dictionary<string, string> tagsByWord = new Dictionary<string, string>();

            for (int i = 0; i < lines.Length; i++) {
                string[] tokens = lines[i].Split('\t');
                tokens[0] = tokens[0].Replace(".", "").ToLower();

                if (!wordList.ContainsKey(tokens[0])) {
                    wordList.Add(tokens[0], new List<WordProps>());
                }

                if (!tagsByWord.ContainsKey(tokens[0])) {
                    tagsByWord.Add(tokens[0], tokens[1]);
                }
            }

            List<AnkiCard> Deck = new List<AnkiCard>();

            for (int i = 0; i < wordList.Keys.Count(); i++) 
            {
                KeyValuePair<string, List<WordProps>> word = wordList.ElementAt(i);

                foreach (WordProps props in word.Value)  
                {
                    if (sweToEngDict.HasAudioFile.Contains(word.Key)) {
                        Task task = GetAudioStream(word.Key);
                    }

                    string wordClass = FormatWordClass(props.Class);
                    
                    if (props.Class == null) {
                        wordClass = FormatWordClass(sweToEngDict.Words.Where(x => x.Value == word.Key).FirstOrDefault().Class);
                    }

                    string question = word.Key + " (" + wordClass + ")";

                    // If the word is a duplicate, skip it
                    if (Deck.Where(x => x.Question.ToString() == question).Count() > 0) {
                        continue;
                    }

                    string definition = GetDefinitions(new Word(word.Key, props.Class), sweToEngDict);

                    // If no defititions for the word is found, skip it
                    if (definition == "") {
                        continue;
                    }

                    KeyValuePair<string, string> example = GetExamples(word.Key, props.Class, sweToEngDict);

                    AnkiCard field = new AnkiCard {
                        Question = word.Key + " (" + wordClass + ")",
                        Word = word.Key,
                        Class = wordClass,
                        Gramar = props.Gramar,
                        Definition = definition,
                        Example = example.Key,
                        ExampleTranslated = example.Value,
                        Audio = File.Exists("output/audio/" + word.Key + ".mp3") ? "[sound:" + word.Key + ".mp3]" : "",
                        Tags = tagsByWord.ContainsKey(word.Key) ? tagsByWord[word.Key] : ""
                    };

                    Deck.Add(field);
                }

                if (i % 500 == 0) {
                    System.Console.WriteLine($"{i}/{wordList.Keys.Count()}");
                }
            }

            System.IO.File.WriteAllText("output/Deck.tsv", string.Join("\n", Deck));
        }

        static int DecToOctal(int n)
        {
            int result = 0;
            List<int> octalNum = new List<int>();
            for (int i = 0; n != 0; i++) {
                result += (int)MathF.Pow(10, i) * (n % 8);
                n /= 8;
            }

            return result;
        }

        public static KeyValuePair<string, string> GetExamples(string searchParam, string wordClass, SweToEngDictionary sweToEngDict) 
        {
            if (sweToEngDict.ExamplesByWord.ContainsKey(new (searchParam, wordClass))) {
                return sweToEngDict.ExamplesByWord[new (searchParam, wordClass)][0];
            }

            return new KeyValuePair<string, string>("", "");
        }

        public static string GetDefinitions(Word word, SweToEngDictionary sweToEngDict) 
        {
            List<List<string>> translations = new List<List<string>>();

            translations.AddRange(GetTranslationsFromEntry(word, sweToEngDict));

            if (sweToEngDict.WordsByInflection.ContainsKey(word)) {
                foreach (Word w in sweToEngDict.WordsByInflection[word]) {
                    translations.AddRange(GetTranslationsFromEntry(w, sweToEngDict));
                }
            }
            
            if (translations.Count > 0) {
                return "<ul>" + GenerateDefinition(translations) + "</ul>";
            } else {
                return "";
            }
        }

        public static List<List<string>> GetTranslationsFromEntry(Word word, SweToEngDictionary sweToEngDict) 
        {
            List<List<string>> translations = new List<List<string>>();

            if (sweToEngDict.TranslationsByWord.ContainsKey(word)) {
                translations.Add(sweToEngDict.TranslationsByWord[word]);
            }

            return translations;
        }

        public static string FormatWordClass(string wordClass) 
        {
            if (wordClass == "pp") return "preposition";
            else if (wordClass == "nn") return"noun";
            else if (wordClass == "vb") return "verb";
            else if (wordClass == "nn") return "noun";
            else if (wordClass == "av") return "adjective";
            else if (wordClass == "jj") return "adjective";
            else if (wordClass == "ab") return "adverb";
            else if (wordClass == "nl") return "numeral";
            else if (wordClass == "rg") return "numeral";
            else if (wordClass == "pn") return "prounoun";
            else if (wordClass == "in") return "interjection";
            else if (wordClass == "kn") return "conjunction";
            else if (wordClass == "pm") return "name";
            else return wordClass;
        }

        public static string GenerateDefinition(List<List<string>> translations) 
        {
            string result = "";

            foreach (List<string> array in translations) 
            {
                if (array != null) {
                    string temp = "<li>";
                    
                    List<string> values = new List<string>();

                    for (int i = 0; i < array.Count(); i++)
                    {
                        if (array.IndexOf(array[i]) >= i) {
                            values.Add(array[i]);
                        }
                    }

                    temp += string.Join(", ", values) + "</li>";

                    if (!result.Contains(temp)) {
                        result += temp;
                    }
                }
            }

            return result;
        }
    
        public static async Task GetAudioStream(string word) {
            using (var client = new HttpClient()) {
                if (!File.Exists("output/collection.media/" + word + ".mp3")) {
                    
                    string formatedString = word;

                    for (int j = 0; j < formatedString.Length; j++) {
                        if (formatedString[j] > 127) {
                            char c = formatedString[j];
                            formatedString = formatedString.Remove(j, 1);
                            formatedString = formatedString.Insert(j, "0" + DecToOctal((int)c).ToString());
                        }
                    }
                    
                    try {
                        using (var s = client.GetStreamAsync("http://lexin.nada.kth.se/sound/" + formatedString + ".mp3")) {
                            using (var fs = new FileStream("output/collection.media/" + word + ".mp3", FileMode.OpenOrCreate)) {
                                await s.Result.CopyToAsync(fs);
                            }
                        }
                    } catch {
                        File.Delete("output/collection.media/" + word + ".mp3");
                    }
                }
            }
        }

    }
}