using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Linq;

namespace Dictionary
{
    public static class Dictionary 
    {
        public static JSONDictionary GetDictionary() 
        {
            if (File.Exists("Dictionary/output/People's_Dictionary.json")) {
                using (Stream reader = new FileStream("Dictionary/output/People's_Dictionary.json", FileMode.Open)) {
                    return (JSONDictionary)JsonSerializer.Deserialize(reader, typeof(JSONDictionary));
                }
            }
            else {
                dictionary dict = dictionary.Deserialize("Dictionary/src/People's_Dictionary.xml");
                JSONDictionary newDict = Convert(dict);

                string output = JsonSerializer.Serialize(newDict, new JsonSerializerOptions { Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)} );
                System.IO.File.WriteAllText("Dictionary/output/People's_Dictionary.json", output);
                
                return newDict;
            }
        }
        
        public static JSONDictionary Convert(dictionary dict) 
        {
            Dictionary<string, List<JSONWord>> words = new Dictionary<string, List<JSONWord>>();

            foreach (Word word in dict.Words) 
            {
                Dictionary<string, string[]> derivations = new Dictionary<string, string[]>();

                if (word.Derivations != null) 
                {
                    foreach (Derivation derivation in word.Derivations) 
                    {
                        IEnumerable<string> t = new List<string>();

                        if (derivation.Translations != null) {
                            t = derivation.Translations.Select(x => (x.Comment == null ? "" : "(" + x.Comment + ")").ToLower() + x.Value.ToLower());
                        }

                        derivations.Add(
                            derivation.Value.ToLower(),
                            t.ToArray()
                        );
                    }
                }


                IEnumerable<string> inflections = new List<string>();

                if (word.Paradigm != null) {
                    inflections = word.Paradigm.Inflections.Select(x => x.Value.ToLower());
                }

                IEnumerable<string> translations = new List<string>();

                if (word.Translations != null) {
                    translations = word.Translations.Select(x =>  x.Value.ToLower() + (x.Comment == null ? "" : " (" + x.Comment + ")").ToLower()).ToArray();
                }

                Example example = new Example();

                if (word.Examples != null) 
                {
                    for (int i = 0; i < word.Examples.Length; i++) 
                    {
                        if (word.Examples[i].Translation != null) {
                            example = word.Examples[i];
                            break;
                        } else if (i == word.Examples.Length - 1) {
                            example = word.Examples[i];
                        }
                    }
                }

                if (!words.ContainsKey(word.Value.ToLower())) {
                    words.Add(word.Value.ToLower(), new List<JSONWord>());
                }

                string definition = "";
                if (word.Definition != null &&  word.Definition.Translation != null) {
                    definition = word.Definition.Translation.Value;
                }

                words[word.Value.ToLower()].Add(
                    new JSONWord{
                        Class = word.Class,
                        Translations = translations.ToArray(),
                        Derivations = derivations,
                        Inflections = inflections.ToArray(),
                        Definition = definition,
                        Example = example.Value,
                        Example_Translation = example.Translation == null ? "" : example.Translation.Value
                    }
                );
            }

            return new JSONDictionary(words);
        }
    }
}