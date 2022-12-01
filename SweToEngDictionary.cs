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
        public Dictionary<string, string> DefinitionByClass;
        public Dictionary<string, List<List<string>>> TranslationsByClass;
        public Dictionary<string, List<(string, string)>> ExamplesByClass;

        public DictEntry() 
        {
            DefinitionByClass = new Dictionary<string, string>();
            TranslationsByClass = new Dictionary<string, List<List<string>>>();
            ExamplesByClass = new Dictionary<string, List<(string, string)>>();
        }

        public void AddData(XmlWord word) 
        {
            string key = word.Class == null ? "" : word.Class;
        
            // Get Definitions

            if (word.Definition != null &&  word.Definition.Translation != null) {
                if (!DefinitionByClass.ContainsKey(key)) {
                    DefinitionByClass.Add(key, word.Definition.Translation.Value);
                }
            }
            
            // Get translations

            if (word.Translations != null) {
                if (!TranslationsByClass.ContainsKey(key)) {
                    TranslationsByClass.Add(key, new List<List<string>>());
                }

                TranslationsByClass[key].Add(word.Translations.Select(x => x.Value + (x.Comment == null ? "" : " [" + x.Comment + "]")).ToList());
            }

            // Get examples

            if (word.Examples != null) {
                if (!ExamplesByClass.ContainsKey(key)) {
                    ExamplesByClass.Add(key, new List<(string, string)>());
                }

                IEnumerable<(string, string)> examples = word.Examples.Select(
                    x => (x.Value, x.Translation == null ? "" : x.Translation.Value)
                );

                ExamplesByClass[key].AddRange(examples);
            }
        }
    }

    public class SweToEngDictionary 
    {
        public Dictionary<string, DictEntry> DictEntryByWord;
        public Dictionary<string, List<(string, string)>> WordAndClassByInflection;
        public Dictionary<string, List<string>> WordByDerivation;
        public HashSet<string> HasAudio;
        public SweToEngDictionary(XmlDictionary dictionary)
        {
            DictEntryByWord = new Dictionary<string, DictEntry>();
            WordAndClassByInflection = new Dictionary<string, List<(string, string)>>();
            WordByDerivation = new Dictionary<string, List<string>>();
            HasAudio = new HashSet<string>();

            foreach (XmlWord word in dictionary.Words) 
            {  
                if (word.Class == "rg") {
                    continue;
                }

                // Add word

                if (!DictEntryByWord.ContainsKey(word.Value)) {
                    DictEntryByWord.Add(word.Value, new DictEntry());
                }

                DictEntryByWord[word.Value].AddData(word);

                // Add inflections pointing to words

                if (word.Paradigm != null) {
                    foreach (XmlInflection inflection in word.Paradigm.Inflections) {
                        if (!WordAndClassByInflection.ContainsKey(inflection.Value)) {
                            WordAndClassByInflection.Add(inflection.Value, new List<(string, string)>());
                        }

                        WordAndClassByInflection[inflection.Value].Add((word.Value, word.Class == null ? "" : word.Class));
                    }
                }

                // Add derivations pointing to words

                if (word.Derivations != null) {
                    foreach (XmlDerivation derivation in word.Derivations) {
                        if (!WordByDerivation.ContainsKey(derivation.Value)) {
                            WordByDerivation.Add(derivation.Value, new List<string>());
                        }

                        WordByDerivation[derivation.Value].Add((word.Value));
                    }
                }

                // Has audio file 

                if (word.Phonetic != null) {
                    if (!HasAudio.Contains(word.Value) && word.Value + ".swf" == word.Phonetic.SoundFile) {
                        HasAudio.Add(word.Value);
                    }
                }
            }
        }
    }
}