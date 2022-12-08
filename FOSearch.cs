using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Linq;

namespace DeckGenerator
{
    public class FOEntry
    {   
        public Dictionary<string, string> DefinitionByClass;
        public Dictionary<string, List<List<string>>> TranslationsByClass;
        public Dictionary<string, List<string>> ExamplesByClass;

        public FOEntry() 
        {
            DefinitionByClass = new Dictionary<string, string>();
            TranslationsByClass = new Dictionary<string, List<List<string>>>();
            ExamplesByClass = new Dictionary<string, List<string>>();
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
                    ExamplesByClass.Add(key, new List<string>());
                }

                ExamplesByClass[key].AddRange(word.Examples.Select(x => x.Value));
            }

            if (word.Idioms != null) {
                if (!ExamplesByClass.ContainsKey(key)) {
                    ExamplesByClass.Add(key, new List<string>());
                }

                ExamplesByClass[key].AddRange( word.Idioms.Select(x => x.Value));
            }
        }
    }

    public class FOSearch 
    {
        public Dictionary<string, FOEntry> DictEntryByWord;
        public Dictionary<string, List<(string, string)>> WordAndClassByInflection;
        public Dictionary<string, List<string>> WordByDerivation;
        public Dictionary<string, List<string>> AbreviationByWord;
        public HashSet<string> HasAudio;
        public const string SOURCE_FILE = "src/Folkets_ordbok.xml";
        public const string OUTPUT_FOLDER = "output/collection.media";
        public static bool LocalOnly;

        public FOSearch()
        {
            DictEntryByWord = new Dictionary<string, FOEntry>();
            WordAndClassByInflection = new Dictionary<string, List<(string, string)>>();
            WordByDerivation = new Dictionary<string, List<string>>();
            AbreviationByWord = new Dictionary<string, List<string>>();
            HasAudio = new HashSet<string>();

            if (!File.Exists(SOURCE_FILE)) {
                System.Console.WriteLine($"SweToEngDictionary Error, cannot find file: {SOURCE_FILE}");
            }

            XmlDictionary dictionary = XmlDictionary.Deserialize(SOURCE_FILE);

            foreach (XmlWord word in dictionary.Words) 
            {  
                // Add word

                if (!new string[] { "rg", "abbrev", "pm" }.Contains(word.Class)) {
                    if (!DictEntryByWord.ContainsKey(word.Value)) {
                        DictEntryByWord.Add(word.Value, new FOEntry());
                    }
                    
                    DictEntryByWord[word.Value].AddData(word);
                }
                
                // Add abreviation pointing to words

                if (word.Class == "abbrev" && word.Definition != null) {
                    if (!AbreviationByWord.ContainsKey(word.Value)) {
                        AbreviationByWord.Add(word.Definition.Value, new List<string>());
                    }

                    AbreviationByWord[word.Definition.Value].Add(word.Value);
                    continue;
                }

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

        public bool GetDefinitions(string searchParam, string wordClass, out string result) 
        {
            List<List<string>> translations = new List<List<string>>();

            translations.AddRange(GetTranslationStrict(searchParam, wordClass));
            translations.Add(new List<string> { GetDefinitionStrict(searchParam, wordClass) });

            if (WordByDerivation.ContainsKey(searchParam)) {
                foreach (string word in WordByDerivation[searchParam]) {
                    translations.AddRange(GetTranslation(word));
                }
            }

            if (WordAndClassByInflection.ContainsKey(searchParam)) 
            {
                foreach ((string, string) wordAndClass in WordAndClassByInflection[searchParam]) 
                {
                    if (wordClass == wordAndClass.Item2) {
                        translations.AddRange(GetTranslationStrict(wordAndClass.Item1, wordAndClass.Item2));
                        translations.Add(new List<string> { GetDefinitionStrict(wordAndClass.Item1, wordAndClass.Item2) });
                    }
                }
            }

            result = GenerateDefinition(translations);
            result = result == "" ? "" : "<ul>" + result + "</ul>";

            return result != ""; 
        }

        public List<List<string>> GetTranslation(string searchParam) 
        {
            if (DictEntryByWord.ContainsKey(searchParam)) {
                return DictEntryByWord[searchParam].TranslationsByClass.SelectMany(x => x.Value).ToList();
            }

            return new List<List<string>>();
        }

        public List<List<string>> GetTranslationStrict(string searchParam, string wordClass) 
        {
            if (DictEntryByWord.ContainsKey(searchParam)) {
                if (DictEntryByWord[searchParam].TranslationsByClass.ContainsKey(wordClass)) {
                    return DictEntryByWord[searchParam].TranslationsByClass[wordClass];
                }
            }

            return new List<List<string>>();
        }

        public List<string> GetDefinition(string searchParam) 
        {
            if (DictEntryByWord.ContainsKey(searchParam)) {
                return DictEntryByWord[searchParam].DefinitionByClass.Select(x => x.Value).ToList();
            }

            return new List<string>();
        }

        public string GetDefinitionStrict(string searchParam, string wordClass) 
        {
            if (DictEntryByWord.ContainsKey(searchParam)) {
                if (DictEntryByWord[searchParam].DefinitionByClass.ContainsKey(wordClass)) {
                    return DictEntryByWord[searchParam].DefinitionByClass[wordClass];
                }
            }

            return "";
        }

        public string GetExample(string searchParam) 
        {
            if (DictEntryByWord.ContainsKey(searchParam)) {
                return DictEntryByWord[searchParam].ExamplesByClass.SelectMany(x => x.Value).FirstOrDefault();
            }

            return "";
        }

        public string GetExampleStrict(string searchParam, string wordClass) 
        {
            if (DictEntryByWord.ContainsKey(searchParam)) {
                if (DictEntryByWord[searchParam].ExamplesByClass.ContainsKey(wordClass)) {
                    return DictEntryByWord[searchParam].ExamplesByClass[wordClass].FirstOrDefault();
                }
            }

            return "";
        }

        public static string GenerateDefinition(List<List<string>> definitions) 
        {
            string result = "";

            foreach (List<string> definition in definitions)
            {
                definition.RemoveAll(x => x == "");

                if (definition.Count() < 1) {
                    continue;
                }
                
                List<string> values = new List<string>();

                for (int i = 0; i < definition.Count(); i++)
                {
                    if (definition.IndexOf(definition[i]) >= i) {
                        values.Add(definition[i]);
                    }
                }

                string temp = "<li>" + string.Join(", ", values) + "</li>";

                if (!result.Contains(temp)) {
                    result += temp;
                }
            }

            return result;
        }

        public static bool DownloadAudio(string writtenForm, out string filename) 
        {
            filename = $"{writtenForm}_FO.mp3";

            if (File.Exists($"{OUTPUT_FOLDER}/{filename}")) {
                return true;
            }      

            if (LocalOnly) {
                return false;
            }

            string formatedString = FormatWord(writtenForm);
            string url = $"http://lexin.nada.kth.se/sound/{formatedString}.mp3";

            var client = new HttpClient();
            Task<Stream> streamTask;

            try {
                streamTask = client.GetStreamAsync(url);
                streamTask.Wait();
            } catch {
                System.Console.WriteLine("Failed to fetch audio");
                return false;
            }

            var fs = new FileStream($"{OUTPUT_FOLDER}/{filename}", FileMode.OpenOrCreate); 
            var copyTask = streamTask.Result.CopyToAsync(fs);

            return true;
        }

        public static string FormatWord(string word) 
        {
            for (int j = 0; j < word.Length; j++) {
                if (word[j] > 127) {
                    char c = word[j];
                    word = word.Remove(j, 1);
                    word = word.Insert(j, "0" + ToOctal((int)c).ToString());
                }
            }

            return word;
        }

        public static int ToOctal(int n)
        {
            int result = 0;
            List<int> octalNum = new List<int>();
            for (int i = 0; n != 0; i++) {
                result += (int)MathF.Pow(10, i) * (n % 8);
                n /= 8;
            }

            return result;
        }
    }
}