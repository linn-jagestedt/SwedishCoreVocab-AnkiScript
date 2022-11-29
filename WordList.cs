using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace DeckGenerator
{
    public class WordProps
    {
        public string Class { get; set; }
        public string Gramar { get; set; }
    }

    public class WordList : Dictionary<string, List<WordProps>>
    {
        public WordList(LexicalResource wordList) : base()
        {
            foreach (LexicalEntry entry in wordList.Lexicon.LexicalEntries) 
            {
                Feat[] feats = entry.Lemma.FormRepresentation.Feats;

                if (!ContainsKey(feats[0].Value.ToLower())) {
                    Add(feats[0].Value.ToLower(), new List<WordProps>());
                }

                if (this[feats[0].Value.ToLower()].Where(x => x.Class == feats[1].Value.ToLower()).Count() > 0) {
                    continue;
                }

                this[feats[0].Value.ToLower()].Add(
                    new WordProps{
                        Class = feats.Where(x => x.Attribute == "partOfSpeech").FirstOrDefault().Value,
                        Gramar = feats.Where(x => x.Attribute == "gram").FirstOrDefault().Value,
                    }
                );
            }
        }
    }
}