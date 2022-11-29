using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Linq;

namespace DeckGenerator
{
    public class DictEntry
    {
        public string Class { get; set; }
        public  List<string> Translations { get; set; }
        public string Definition { get; set; }
    }

    public class SweToEngDictionary 
    {
        public Dictionary<string, List<DictEntry>> DictEntriesByWord { get => _dictEntriesByWord; }
        private Dictionary<string, List<DictEntry>> _dictEntriesByWord;

        public Dictionary<KeyValuePair<string, string>, List<string>> WordsByInflection { get => _wordsByInflection; }
        private Dictionary<KeyValuePair<string, string>, List<string>> _wordsByInflection;

        public Dictionary<KeyValuePair<string, string>, List<string>> DerivationsByWord { get => _derivationsByWord; }
        private Dictionary<KeyValuePair<string, string>,  List<string>> _derivationsByWord;

        public Dictionary<KeyValuePair<string, string>, List<KeyValuePair<string, string>>> ExamplesByWord { get => _examplesByWord; }
        private Dictionary<KeyValuePair<string, string>, List<KeyValuePair<string, string>>> _examplesByWord;

        public SweToEngDictionary(dictionary dictionary)
        {
            _wordsByInflection = new Dictionary<KeyValuePair<string, string>, List<string>>();

            _dictEntriesByWord = new Dictionary<string, List<DictEntry>>();
        
            _derivationsByWord = new Dictionary<KeyValuePair<string, string>, List<string>>();
            _examplesByWord = new Dictionary<KeyValuePair<string, string>, List<KeyValuePair<string, string>>>();

            foreach (Word word in dictionary.Words) 
            {
                if (word.Derivations != null) 
                {   
                    foreach (Derivation derivation in word.Derivations) 
                    {
                        if (derivation.Translations == null) {
                            continue;
                        }

                        if (!_derivationsByWord.ContainsKey(new (derivation.Value, word.Class))) {
                            _derivationsByWord.Add(new (derivation.Value, word.Class), new List<string>());
                        }

                        IEnumerable<string> temp = derivation.Translations.Select(
                            x => (x.Value + (x.Comment == null ? "" : " (" + x.Comment + ")")).ToLower()
                        );

                        _derivationsByWord[new (derivation.Value, word.Class)].AddRange(temp);
                    }
                }

                List<string> inflections = new List<string>();

                if (word.Paradigm != null) {
                    foreach (Inflection inflection in word.Paradigm.Inflections) {
                        if (!_wordsByInflection.ContainsKey(new (inflection.Value, word.Class))) {
                            _wordsByInflection.Add(new (inflection.Value, word.Class), new List<string>());
                        }
                        _wordsByInflection[new (inflection.Value, word.Class)].Add(word.Value.ToLower());
                    }
                }

                List<string> translations = new List<string>();

                if (word.Translations != null) {
                    translations = word.Translations.Select(x =>  x.Value.ToLower() + (x.Comment == null ? "" : " (" + x.Comment + ")").ToLower()).ToArray().ToList();
                }

                if (word.Examples != null) 
                {
                    if (!_examplesByWord.ContainsKey(new (word.Value, word.Class))) {
                        _examplesByWord.Add(new (word.Value, word.Class), new List<KeyValuePair<string, string>>());
                    }

                    IEnumerable<KeyValuePair<string, string>> examples = word.Examples.Select(
                        x => new KeyValuePair<string, string>(x.Value, x.Translation?.Value)
                    );

                    _examplesByWord[new (word.Value, word.Class)].AddRange(examples.OrderBy(x => x.Value));
                }

                if (!_dictEntriesByWord.ContainsKey(word.Value.ToLower())) {
                    _dictEntriesByWord.Add(word.Value.ToLower(), new List<DictEntry>());
                }

                string definition = "";
                if (word.Definition != null &&  word.Definition.Translation != null) {
                    definition = word.Definition.Translation.Value;
                }

                DictEntry dictEntry =   new DictEntry{
                    Class = word.Class,
                    Translations = translations,
                    Definition = definition,
                };

                _dictEntriesByWord[word.Value.ToLower()].Add(dictEntry);
            }        }
    }
}