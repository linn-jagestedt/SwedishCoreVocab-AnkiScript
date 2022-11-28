using System;
using System.Xml.Serialization;

namespace DeckGenerator
{
    public static class Program 
    {
        public static void Main() 
        {
            dictionary dict = DeserializeDictionary("src/People's_Dictionary.xml");

            string output = "";

            foreach (LexicalEntry entry in DeserializeWordList("src/Common_Words.xml").Lexicon.LexicalEntries) 
            {
                Feat[] feats = entry.Lemma.FormRepresentation.Feats;

                if (output.Contains("\n" + feats[0].Value.ToLower() + "\t")) {
                    continue;
                }

                string word = feats[0].Value.ToLower();
                string wordClass = feats[1].Value.ToLower();
                string frequency = feats[5].Value.ToLower();
                string gramar = feats.Length > 8 ? feats[8].Value.ToLower() : "";

                if (DictionarySearch(word, wordClass, dict, out DictionaryData data)) {
                    output +=           
                        word + "\t" +
                        data.Definition + "\t" +
                        data.Example + "\t" +
                        data.ExampleTranslated + "\t" +
                        wordClass + "\t" +
                        frequency + "\t" +                        
                        gramar + "\n";
                }
            }

            System.IO.File.WriteAllText("output/Swedish Core 8k.tsv", output);
        }

        public struct DictionaryData 
        {
            public string Definition;
            public string Example;
            public string ExampleTranslated;
        }

        public static bool DictionarySearch(string searchParam, string wordClass, dictionary dict, out DictionaryData data) 
        {
            data = new DictionaryData();

            List<Translation[]> translations = new List<Translation[]>();
            
            // Find All words in dictionary with the match
            Word[] words = Array.FindAll(dict.Words, x => x.Value == searchParam && MatchingWordClass(x.Class, wordClass));
            translations.AddRange(words.Select(x => x.Translations));

            if (words.Length > 0) {
                for (int i = 0; i < words.Length; i++) {
                    if (words[i].Examples != null) {
                        if (words[i].Examples[0].Translation != null) {
                            data.Example = words[i].Examples[0].Value;
                            data.ExampleTranslated = words[i].Examples[0].Translation.Value;
                            break;
                        } else if (i == words.Length - 1) {
                            data.Example = words[i].Examples[0].Value;
                        } 
                    }
                }
            }

            foreach (Word word in dict.Words) 
            {
                if (!MatchingWordClass(word.Class, wordClass)) continue;

                // If the searchterm is an gather all the words that the searchterm is an inflection of
                if (word.Paradigm != null) {
                    Inflection[] inflections = Array.FindAll(word.Paradigm.Inflections, x => x.Value == searchParam);
                    if (inflections.Length > 0 && word.Translations != null) {
                        translations.Add(word.Translations);
                    }
                }

                // If the searchterm is an derivation, gather the translations and return true
                if (word.Derivations != null) {
                    Derivation[] derivations = Array.FindAll(word.Derivations, x => x.Value == searchParam);
                    if (derivations.Length > 0) {
                        translations.AddRange(derivations.Select(x => x.Translations));
                    }
                }
            }
            
            data.Definition = "<ul>" + GetTranslations(translations) + "</ul>";

            return translations.Count() > 0;
        }

        public static bool MatchingWordClass(string a, string b) {
            if (a == "jj" || b == "jj") return true;
            else if (a == "abbrev" || b == "abbrev") return true;
            else if (a == "rg" && b == "nl") return true;
            else if (a == "nl" && b == "rg") return true;
            else return a == b;
        }

        public static string GetTranslations(List<Translation[]> translations) 
        {
            string result = "";

            foreach (Translation[] array in translations) 
            {
                if (array != null) {
                    string temp = "<li>";
                    
                    List<string> values = new List<string>();

                    for (int i = 0; i < array.Length; i++)
                    {
                        if (Array.IndexOf(array, array[i]) >= i) {
                            string value = array[i].Value;
                            value += array[i].Comment == null ? "" : " (" + array[i].Comment + ")";
                            values.Add(value);
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

        public static LexicalResource DeserializeWordList(string file) 
        {
            XmlSerializer serializer = new XmlSerializer(typeof(LexicalResource));
            LexicalResource lexicon;

            using (Stream reader = new FileStream(file, FileMode.Open)) {
                lexicon = (LexicalResource)serializer.Deserialize(reader);
            }

            return lexicon;
        }

        public static dictionary DeserializeDictionary(string file) 
        {
            XmlSerializer serializer = new XmlSerializer(typeof(dictionary));
            dictionary dictionary;

            using (Stream reader = new FileStream(file, FileMode.Open)) {
                dictionary = (dictionary)serializer.Deserialize(reader);
            }

            for (int i = 0; i < dictionary.Words.Length; i++) 
            {
                Word[] words = dictionary.Words;

                if (words[i].Value.Contains(" (")) {
                    words[i].Value = words[i].Value.Substring(0, words[i].Value.IndexOf(" ("));
                }

                words[i].Value = words[i].Value.Replace("|", "").ToLower();

                if (words[i].Derivations != null) {
                    for (int j = 0; j < words[i].Derivations.Length; j++) {
                        words[i].Derivations[j].Value = words[i].Derivations[j].Value.Replace("|", "").ToLower();
                    }
                }
            }

            return dictionary;
        }
    }
}