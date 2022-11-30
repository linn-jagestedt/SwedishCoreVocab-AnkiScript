using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace DeckGenerator
{
    public struct WordProps
    {
        public string Class = "";
        public string Gramar = "";
        public bool isFromRivstart = false;

        public WordProps() 
        {
            Class = "";
            Gramar = "";
            isFromRivstart = false;
        }
    }

    public class WordList : Dictionary<string, List<WordProps>>
    {
        public WordList(XmlLexicalResource wordList) : base()
        {
            foreach (XmlLexicalEntry entry in wordList.Lexicon.LexicalEntries) 
            {
                XmlFeat[] feats = entry.Lemma.FormRepresentation.Feats;

                string word = feats[0].Value.ToLower();

                if (!ContainsKey(word)) {
                    Add(word, new List<WordProps>());
                }

                string Class = feats.Where(x => x.Attribute == "partOfSpeech").FirstOrDefault().Value;
                Class = Class == null ? "" : Class;
                Class = ConvertClass(Class);

                string gramar = feats.Where(x => x.Attribute == "gram").FirstOrDefault().Value;
                gramar = gramar == null ? "" : gramar;

                if (this[word].Where(x => x.Class == Class).Count() > 0) {
                    continue;
                }

                this[word].Add(
                    new WordProps{
                        Class = Class,
                        Gramar = gramar,
                    }
                );
            }
        }

        public static string ConvertClass(string a) {
            if (a == "av") return "jj";
            if (a == "nl") return "rg";
            return a;
        }
    }
}