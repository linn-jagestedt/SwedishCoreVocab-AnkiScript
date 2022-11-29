using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace WordList
{
    public static class WordList 
    {
        public static JSONWordList GetWordList()  
        {
            if (File.Exists("WordList/output/Common_Words.json")) {
                using (Stream reader = new FileStream("WordList/output/Common_Words.json", FileMode.Open)) {
                    return (JSONWordList)JsonSerializer.Deserialize(reader, typeof(JSONWordList));
                }
            }
            else {
                LexicalResource wordList = LexicalResource.Deserialize("WordList/src/Common_Words.xml");
                JSONWordList newWordList = Convert(wordList);

                string output = JsonSerializer.Serialize(newWordList, new JsonSerializerOptions { Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)} );
                System.IO.File.WriteAllText("WordList/output/Common_Words.json", output);
                
                return newWordList;
            }
        }
        
        public static JSONWordList Convert(LexicalResource lexicon) 
        {
            Dictionary<string, List<JSONWord>> words = new Dictionary<string, List<JSONWord>>();

            foreach (LexicalEntry entry in lexicon.Lexicon.LexicalEntries) 
            {
                Feat[] feats = entry.Lemma.FormRepresentation.Feats;

                if (!words.ContainsKey(feats[0].Value.ToLower())) {
                    words.Add(feats[0].Value.ToLower(), new List<JSONWord>());
                }

                if (words[feats[0].Value.ToLower()].Where(x => x.Class == feats[1].Value.ToLower()).Count() > 0) {
                    continue;
                }

                words[feats[0].Value.ToLower()].Add(
                    new JSONWord{
                        Class = feats.Where(x => x.Attribute == "partOfSpeech").FirstOrDefault().Value,
                        Gramar = feats.Where(x => x.Attribute == "gram").FirstOrDefault().Value,
                    }
                );
            }

            return new JSONWordList(words);
        }
    }
}