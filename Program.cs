using System;
using System.Text;
using AnkiNet;
using System.Linq;
using System.Collections.Generic;

namespace DeckGenerator
{
    public static class Program 
    {
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
                    if (sweToEngDict.HasAudio.Contains(word.Key)) {
                        Task task = GetAudioStream(word.Key);
                    }

                    string wordClass = FormatWordClass(props.Class);
                    string question = word.Key + " (" + wordClass + ")";

                    // If the word is a duplicate, skip it
                    if (Deck.Where(x => x.Question.ToString() == question).Count() > 0) {
                        continue;
                    }

                    string definition = GetDefinitions(word.Key, props.Class, sweToEngDict);

                    // If no defititions for the word is found, skip it
                    if (definition == "") {
                        continue;
                    }

                    (string, string) example;

                    if (props.Class != "") {
                        example = GetExampleStrict(word.Key, props.Class, sweToEngDict);
                    } else {
                        example = GetExample(word.Key, sweToEngDict);
                    }

                    AnkiCard field = new AnkiCard {
                        Question = word.Key + " (" + wordClass + ")",
                        Word = word.Key,
                        Class = wordClass,
                        Gramar = props.Gramar,
                        Definition = definition,
                        Example = example.Item1,
                        ExampleTranslated = example.Item2,
                        Audio = File.Exists("output/collection.media/" + word.Key + ".mp3") ? "[sound:" + word.Key + ".mp3]" : "",
                        KellyID = props.KellyID,
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

        public static string GetDefinitions(string searchParam, string wordClass, SweToEngDictionary sweToEngDict) 
        {
            List<List<string>> translations = new List<List<string>>();

            translations.AddRange(GetTranslationStrict(searchParam, wordClass, sweToEngDict));
            translations.Add(new List<string> { GetDefinitionStrict(searchParam, wordClass, sweToEngDict) });

            if (sweToEngDict.WordByDerivation.ContainsKey(searchParam)) {
                foreach (string word in sweToEngDict.WordByDerivation[searchParam]) {
                    translations.AddRange(GetTranslation(word, sweToEngDict));
                }
            }

            if (sweToEngDict.WordAndClassByInflection.ContainsKey(searchParam)) 
            {
                foreach ((string, string) wordAndClass in sweToEngDict.WordAndClassByInflection[searchParam]) 
                {
                    // If no translations are found so far, do a loose search
                    if (translations.SelectMany(x => x.SelectMany(x => x)).Count() < 1) 
                    {
                        translations.AddRange(GetTranslation(wordAndClass.Item1, sweToEngDict));
                        translations.Add(GetDefinition(wordAndClass.Item1, sweToEngDict));
                    } 
                    else 
                    {
                        if (wordClass == wordAndClass.Item2) {
                            translations.AddRange(GetTranslationStrict(wordAndClass.Item1, wordAndClass.Item2, sweToEngDict));
                            translations.Add(new List<string> { GetDefinitionStrict(wordAndClass.Item1, wordAndClass.Item2, sweToEngDict) });
                        }
                    }
                }
            }

            string result = GenerateDefinition(translations);
            return result == "" ? "" : "<ul>" + result + "</ul>"; 
        }

        public static List<List<string>> GetTranslation(string searchParam, SweToEngDictionary sweToEngDict) 
        {
            if (sweToEngDict.DictEntryByWord.ContainsKey(searchParam)) {
                return sweToEngDict.DictEntryByWord[searchParam].TranslationsByClass.SelectMany(x => x.Value).ToList();
            }

            return new List<List<string>>();
        }

        public static List<List<string>> GetTranslationStrict(string searchParam, string wordClass, SweToEngDictionary sweToEngDict) 
        {
            if (sweToEngDict.DictEntryByWord.ContainsKey(searchParam)) {
                if (sweToEngDict.DictEntryByWord[searchParam].TranslationsByClass.ContainsKey(wordClass)) {
                    return sweToEngDict.DictEntryByWord[searchParam].TranslationsByClass[wordClass];
                }
            }

            return new List<List<string>>();
        }

        public static List<string> GetDefinition(string searchParam, SweToEngDictionary sweToEngDict) 
        {
            if (sweToEngDict.DictEntryByWord.ContainsKey(searchParam)) {
                return sweToEngDict.DictEntryByWord[searchParam].DefinitionByClass.Select(x => x.Value).ToList();
            }

            return new List<string>();
        }

        public static string GetDefinitionStrict(string searchParam, string wordClass, SweToEngDictionary sweToEngDict) 
        {
            if (sweToEngDict.DictEntryByWord.ContainsKey(searchParam)) {
                if (sweToEngDict.DictEntryByWord[searchParam].DefinitionByClass.ContainsKey(wordClass)) {
                    return sweToEngDict.DictEntryByWord[searchParam].DefinitionByClass[wordClass];
                }
            }

            return "";
        }

        public static (string, string) GetExample(string searchParam, SweToEngDictionary sweToEngDict) 
        {
            if (sweToEngDict.DictEntryByWord.ContainsKey(searchParam)) {
                return sweToEngDict.DictEntryByWord[searchParam].ExamplesByClass.SelectMany(x => x.Value).FirstOrDefault();
            }

            return ("", "");
        }

        public static (string, string) GetExampleStrict(string searchParam, string wordClass, SweToEngDictionary sweToEngDict) 
        {
            if (sweToEngDict.DictEntryByWord.ContainsKey(searchParam)) {
                if (sweToEngDict.DictEntryByWord[searchParam].ExamplesByClass.ContainsKey(wordClass)) {
                    return sweToEngDict.DictEntryByWord[searchParam].ExamplesByClass[wordClass].FirstOrDefault();
                }
            }

            return ("", "");
        }

        public static string GenerateDefinition(List<List<string>> definitions) 
        {
            string result = "";

            foreach (List<string> definition in definitions)
            {
                definition.RemoveAll(x => x == "");

                if (definition.Count() < 1) {
                    continue;
                }
                
                List<string> values = new List<string>();

                for (int i = 0; i < definition.Count(); i++)
                {
                    if (definition.IndexOf(definition[i]) >= i) {
                        values.Add(definition[i]);
                    }
                }

                string temp = "<li>" + string.Join(", ", values) + "</li>";

                if (!result.Contains(temp)) {
                    result += temp;
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
                            formatedString = formatedString.Insert(j, "0" + ((int)c).ToOctal().ToString());
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

        public static int ToOctal(this int n)
        {
            int result = 0;
            List<int> octalNum = new List<int>();
            for (int i = 0; n != 0; i++) {
                result += (int)MathF.Pow(10, i) * (n % 8);
                n /= 8;
            }

            return result;
        }
    }
}