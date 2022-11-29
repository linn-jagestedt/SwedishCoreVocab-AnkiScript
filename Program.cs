using System;
using System.Text.Json;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Dictionary;
using WordList;

namespace DeckGenerator
{
    public static class Program 
    {
        public static void Main() 
        {
            JSONDictionary dictionary = Dictionary.Dictionary.GetDictionary();
            JSONWordList wordList = WordList.WordList.GetWordList();
            
            Dictionary<string, string> TagsByWord = new Dictionary<string, string>();
            Dictionary<string, string> PageByWord = new Dictionary<string, string>();

            string[] lines = System.IO.File.ReadAllLines("src/Rivstart A1 & A2.tsv");

            foreach (string line in lines) {
                string[] tokens = line.Split('\t');
                TagsByWord.Add(tokens[0], tokens.Last());
                PageByWord.Add(tokens[0], tokens[3]);
            }

            string output = "";

            foreach (string word in wordList.Words.Keys) 
            {
                foreach(WordList.JSONWord jsonWord in wordList.Words[word])  
                {
                    if (DictionarySearch(word, jsonWord.Class, dictionary, out DictionaryData data)) {
                        string tag = "";
                        string page = "";

                        if (TagsByWord.ContainsKey(word)) {
                            tag = TagsByWord[word];
                        }

                        if (PageByWord.ContainsKey(word)) {
                            page = PageByWord[word];
                        }

                        output +=          
                            word + ", " + FormatWordClass(jsonWord.Class) + "\t" +
                            word + "\t" +
                            data.Definition + "\t" +
                            data.Example + "\t" +
                            data.ExampleTranslation + "\t" +
                            FormatWordClass(jsonWord.Class) + "\t" +
                            jsonWord.Gramar + "\t" + 
                            page + "\t" + 
                            tag + "\n";
                    }
                }
            }

            System.IO.File.WriteAllText("output/Swedish Core 7k.tsv", output);
        }

        public struct DictionaryData 
        {
            public string Definition;
            public string Example;
            public string ExampleTranslation;
        }

        public static bool DictionarySearch(string searchParam, string wordClass, JSONDictionary dict, out DictionaryData data) 
        {
            data = new DictionaryData();

            List<string[]> translations = new List<string[]>();

            if (dict.Words.ContainsKey(searchParam)) {
                Dictionary.JSONWord[] words = dict.Words[searchParam].Where(x => MatchingWordClass(x.Class, wordClass)).ToArray();
                translations.AddRange(words.Select(x => x.Translations));

                for (int i = 0; i < words.Length; i++) {
                    if (words[0].Example != "") {
                        data.Example = words[0].Example;
                        data.ExampleTranslation = words[0].Example_Translation;
                    }
                }
            }

            foreach (string word in dict.Words.Keys) 
            {
                foreach(Dictionary.JSONWord jsonWord in dict.Words[word])  
                {
                    if (!MatchingWordClass(jsonWord.Class, wordClass)) continue;

                    string[] inflections = Array.FindAll(jsonWord.Inflections, x => x == searchParam);
                    if (inflections.Length > 0 && jsonWord.Translations != null) {
                        translations.Add(jsonWord.Translations);
                    }

                    if (jsonWord.Derivations.ContainsKey(searchParam)) {
                        translations.Add(jsonWord.Derivations[searchParam]);
                    }
                }
            }
            
            data.Definition = "<ul>" + GetTranslations(translations) + "</ul>";
            return translations.Count() > 0;
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
            if (a == "abbrev" || b == "abbrev") return true;
            else if (a == null || b == null) return true;
            else if (a == "jj" && b == "av") return true;
            else if (a == "av" && b == "jj") return true;
            else if (a == "rg" && b == "nl") return true;
            else if (a == "nl" && b == "rg") return true;
            else return a == b;
        }

        public static string GetTranslations(List<string[]> translations) 
        {
            string result = "";

            foreach (string[] array in translations) 
            {
                if (array != null) {
                    string temp = "<li>";
                    
                    List<string> values = new List<string>();

                    for (int i = 0; i < array.Length; i++)
                    {
                        if (Array.IndexOf(array, array[i]) >= i) {
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