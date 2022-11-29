using System;
using System.Text.Json;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace DeckGenerator
{
    public static class Program 
    {
        public struct AnkiCard 
        {
            public string Question;
            public string Word;
            public string Class;
            public string Gramar;
            public string Definition;
            public string Example;
            public string ExampleTranslated;
            public string Tags;

            public override string ToString() => 
                string.Join("\t", Question, Word, Class, Gramar, Definition, Example, ExampleTranslated, Tags);
        }

        public static void Main() 
        {
            SweToEngDictionary sweToEngDict = new SweToEngDictionary(dictionary.Deserialize("src/People's_Dictionary.xml"));
            WordList wordList = new WordList(LexicalResource.Deserialize("src/Common_Words.xml"));

            string[] lines = System.IO.File.ReadAllLines("src/Rivstart A1 & A2.tsv");
            Dictionary<string, string> tagsByWord = new Dictionary<string, string>();

            for (int i = 0; i < lines.Length; i++) {
                string[] tokens = lines[i].Split('\t');
                tokens[0] = tokens[0].Replace(".", "").ToLower();

                if (wordList.ContainsKey(tokens[0])) {
                    foreach (WordProps props in wordList[tokens[0]]) {
                        props.isFromRivstart = true;
                    }
                } else {
                    wordList.Add(tokens[0], new List<WordProps>());
                    wordList[tokens[0]].Add(new WordProps { isFromRivstart = true });
                }

                if (!tagsByWord.ContainsKey(tokens[0])) {
                    tagsByWord.Add(tokens[0], tokens[5]);
                }
            }

            List<AnkiCard> Core6kDeck = new List<AnkiCard>();
            List<AnkiCard> A1A2Deck = new List<AnkiCard>();

            for (int i = 0; i < wordList.Keys.Count(); i++) 
            {
                KeyValuePair<string, List<WordProps>> word = wordList.ElementAt(i);

                foreach (WordProps props in word.Value)  
                {
                    string wordClass = FormatWordClass(props.Class);
                    
                    if (props.Class == null && sweToEngDict.DictEntriesByWord.ContainsKey(word.Key)) {
                        wordClass = FormatWordClass(sweToEngDict.DictEntriesByWord[word.Key][0].Class);
                    }

                    string question = word.Key + " (" + wordClass + ")";

                    // If the word is a duplicate, skip it
                    if (Core6kDeck.Concat(A1A2Deck).Where(x => x.Question == question).Count() > 0) {
                        continue;
                    }

                    string definition = GetDefinitions(word.Key, props.Class, sweToEngDict);

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
                        Tags = tagsByWord.ContainsKey(word.Key) ? tagsByWord[word.Key] : ""
                    };

                    // Store card in different list depending on the tags
                    if (field.Tags == "") {
                        Core6kDeck.Add(field);
                    } else {
                        A1A2Deck.Add(field);
                    }
                }

                if (i % 500 == 0) {
                    System.Console.WriteLine($"{i}/{wordList.Keys.Count()}");
                }
            }

            System.IO.File.WriteAllText("output/Core6k.tsv", string.Join("\n", Core6kDeck.Select(x => x.ToString())));
            System.IO.File.WriteAllText("output/Rivstart_A1A2.tsv", string.Join("\n", A1A2Deck.Select(x => x.ToString())));
            //System.IO.File.WriteAllText("output/Rivstart_A1A2_Leftover.tsv", outputA1A2Leftover);
        }

        public static KeyValuePair<string, string> GetExamples(string searchParam, string wordClass, SweToEngDictionary sweToEngDict) 
        {
            if (sweToEngDict.ExamplesByWord.ContainsKey(new (searchParam, wordClass))) {
                return sweToEngDict.ExamplesByWord[new (searchParam, wordClass)][0];
            }

            return new KeyValuePair<string, string>("", "");
        }

        public static string GetDefinitions(string searchParam, string wordClass, SweToEngDictionary sweToEngDict) 
        {
            List<List<string>> translations = new List<List<string>>();

            translations.AddRange(GetTranslationsFromEntry(searchParam, wordClass, sweToEngDict));
            
            if (sweToEngDict.DerivationsByWord.ContainsKey(new (searchParam, wordClass))) {
                translations.Add(sweToEngDict.DerivationsByWord[new (searchParam, wordClass)]);
            }

            if (sweToEngDict.WordsByInflection.ContainsKey(new (searchParam, wordClass))) {
                foreach (string word in sweToEngDict.WordsByInflection[new (searchParam, wordClass)]) {
                    translations.AddRange(GetTranslationsFromEntry(word, wordClass, sweToEngDict));
                }
            }
            
            if (translations.Count > 0) {
                return "<ul>" + GenerateDefinition(translations) + "</ul>";
            } else {
                return "";
            }
        }

        public static List<List<string>> GetTranslationsFromEntry(string searchParam, string wordClass, SweToEngDictionary sweToEngDict) 
        {
            List<List<string>> translations = new List<List<string>>();

            if (!sweToEngDict.DictEntriesByWord.ContainsKey(searchParam)) 
                return translations;

            List<DictEntry> entries = sweToEngDict.DictEntriesByWord[searchParam];
            
            foreach (DictEntry entry in entries) {
                if (!MatchingWordClass(entry.Class, wordClass)) 
                    continue;
                if (entry.Definition.Length > 0) 
                    entry.Translations.Add(entry.Definition);
                translations.Add(entry.Translations);
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

        public static bool MatchingWordClass(string a, string b) {
            if (a == null || b == null) return true;
            else if (a == "jj" && b == "av") return true;
            else if (a == "rg" && b == "nl") return true;
            else if (a == "abbrev" || b == "abbrev") return false;
            return a == b;
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
    }
}