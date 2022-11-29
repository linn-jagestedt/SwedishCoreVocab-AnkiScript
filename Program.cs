using System;
using System.Text.Json;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace DeckGenerator
{
    public static class Program 
    {
        public static void Main() 
        {
            
            SweToEngDictionary sweToEngDict = new SweToEngDictionary(dictionary.Deserialize("src/People's_Dictionary.xml"));
            WordList wordList = new WordList(LexicalResource.Deserialize("src/Common_Words.xml"));

            string[] lines = System.IO.File.ReadAllLines("src/Rivstart A1 & A2.tsv");
            Dictionary<string, string> tagsByWord = new Dictionary<string, string>();

            foreach (string line in lines) {
                string[] tokens = line.Split('\t');
                tagsByWord.Add(tokens[0], tokens[5]);
            }

            string output = "";

            for (int i = 0; i < wordList.Keys.Count(); i++) 
            {
                KeyValuePair<string, List<WordProps>> word = wordList.ElementAt(i);

                foreach (WordProps props in word.Value)  
                {
                    if (output.Contains(word.Key + " (" + FormatWordClass(props.Class) + ")" + "\t")) {
                        continue;
                    }

                    string definition = GetDefinitions(word.Key, props.Class, sweToEngDict);

                    if (definition == "") {
                        continue;
                    }

                    string wordClass = FormatWordClass(props.Class);

                    output +=          
                        word.Key + " (" + wordClass + ")" + "\t" +
                        word.Key + "\t" +
                        wordClass + "\t" +
                        props.Gramar + "\t" +
                        definition + "\t";

                    KeyValuePair<string, string> example = GetExamples(word.Key, props.Class, sweToEngDict);
                    
                    output += 
                        example.Key + "\t" +
                        example.Value + "\t";

                    string tag = "";

                    if (tagsByWord.ContainsKey(word.Key)) {
                        tag = tagsByWord[word.Key];
                    }

                    output += tag + "\n";
                }

                if (i % 500 == 0) {
                    System.Console.WriteLine($"{i}/{wordList.Keys.Count()}");
                }
            }

            System.IO.File.WriteAllText("output/Swedish Core 7k.tsv", output);
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
            else if (wordClass == "ab") return "adverb";
            else if (wordClass == "nl") return "numeral";
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