using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Linq;

namespace DeckGenerator
{
    public struct Word : IEquatable<Word>
    {
        public string Value;
        public string Class;

        public Word(string value = "", string wordClass = "") {
            Value = value;
            Class = wordClass;
        }

        public static implicit operator Word(XmlWord w) => new Word(w.Value, w.Class);

        public bool Equals(Word w) {
            return Value == w.Value && MatchingWordClass(Class, w.Class);
        }

        public static bool MatchingWordClass(string a, string b) {
            if (a == "" || b == "") return true;
            return a == b;
        }

    }

    public class SweToEngDictionary 
    {
        public HashSet<Word> Words { get; private set; }

        public SweToEngDictionary(XmlDictionary dictionary)
        {
            Words = new HashSet<Word>();
            DefinitionByWord = new Dictionary<Word, string>();
            WordsByInflection = new Dictionary<Word, List<Word>>();
            ExamplesByWord = new Dictionary<Word, List<KeyValuePair<string, string>>>();
            TranslationsByWord = new Dictionary<Word, List<string>>();
            HasAudioFile = new HashSet<string>();

            foreach (XmlWord xmlWord in dictionary.Words) 
            {                    
                Words.Add(xmlWord);
                GetDefinitions(xmlWord);
                GetDerivations(xmlWord);
                GetInflections(xmlWord);
                GetExamples(xmlWord);
                GetAudioFiles(xmlWord);
                GetTranslations(xmlWord);
            }
        }

        public Dictionary<Word, string> DefinitionByWord { get; private set; }

        public void GetDefinitions(XmlWord xmlWord) {
            if (!DefinitionByWord.ContainsKey(xmlWord)) {
                if (xmlWord.Definition != null &&  xmlWord.Definition.Translation != null) {
                    DefinitionByWord.Add(xmlWord, xmlWord.Definition.Translation.Value);
                }
            }
        }

        private void GetDerivations(XmlWord xmlWord) 
        {
            if (xmlWord.Derivations == null) {   
                return;
            }

            foreach (XmlDerivation derivation in xmlWord.Derivations) 
            {
                if (derivation.Translations == null) {
                    continue;
                }

                Word word = new Word(derivation.Value, "");

                if (!Words.Contains(word)) {
                    Words.Add(word);
                }

                if (!TranslationsByWord.ContainsKey(word)) {
                    TranslationsByWord.Add(word, new List<string>());
                }
                
                TranslationsByWord[word].AddRange(derivation.Translations.Select(x => x.Value + (x.Comment == "" ? "" : " (" + x.Comment + ")")));
            }
        }

        public Dictionary<Word, List<Word>> WordsByInflection { get; private set; }

        private void GetInflections(XmlWord xmlWord) 
        {
            if (xmlWord.Paradigm == null) {
                return;
            }

            foreach (XmlInflection inflection in xmlWord.Paradigm.Inflections) {
                Word word = new Word(inflection.Value, xmlWord.Class);

                if (!WordsByInflection.ContainsKey(new Word(inflection.Value, xmlWord.Class))) {
                    WordsByInflection.Add(word, new List<Word>());
                }

                WordsByInflection[word].Add(xmlWord);
            }
        }

        public Dictionary<Word, List<KeyValuePair<string, string>>> ExamplesByWord { get; private set; }

        private void GetExamples(XmlWord xmlWord) 
        {
            if (xmlWord.Examples == null) {
                return;
            }

            if (!ExamplesByWord.ContainsKey(xmlWord)) {
                ExamplesByWord.Add(xmlWord, new List<KeyValuePair<string, string>>());
            }

            IEnumerable<KeyValuePair<string, string>> examples = xmlWord.Examples.Select(
                x => new KeyValuePair<string, string>(x.Value, x.Translation == null ? "" : x.Translation.Value)
            );

            ExamplesByWord[xmlWord].AddRange(examples.OrderBy(x => x.Value));
        }

        public HashSet<string> HasAudioFile { get; private set; }

        private void GetAudioFiles(XmlWord xmlWord) 
        {
            if (xmlWord.Phonetic != null) {
                if (!HasAudioFile.Contains(xmlWord.Value) && xmlWord.Value + ".swf" == xmlWord.Phonetic.SoundFile) {
                    HasAudioFile.Add(xmlWord.Value);
                }
            }
        }

        public Dictionary<Word, List<string>> TranslationsByWord { get; private set; }

        private void GetTranslations(XmlWord xmlWord) {
            if (xmlWord.Translations != null) {
                if (!TranslationsByWord.ContainsKey(xmlWord)) {
                    TranslationsByWord.Add(xmlWord, new List<string>());
                }

                TranslationsByWord[xmlWord].AddRange(xmlWord.Translations.Select(x =>  x.Value.ToLower() + (x.Comment == null ? "" : " (" + x.Comment + ")").ToLower()));
            }
        }
    }
}