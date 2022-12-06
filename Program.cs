using System;
using System.Text;
using System.Linq;
using System.Net;
using System.Text.Json;

namespace DeckGenerator
{
    public enum CEFR_LEVEL {
        A1 = 2, A2 = 3, B1 = 4, B2 = 5, C1 = 6, NAN = 0
    }

    public static class Program 
    {
        public static int test = 0;
        public static Dictionary<string, string> TranslationsByWord;
        public static Dictionary<string, string> ChapterByWord;
        public static SweToEngDictionary SweToEngDict;
        public static Dictionary<CEFR_LEVEL, List<AnkiCard>> Decks;
        public static List<string> CachedSentences;
        public static readonly CEFR_LEVEL[] LEVELS = new CEFR_LEVEL[] { CEFR_LEVEL.A1, CEFR_LEVEL.A2, CEFR_LEVEL.B1, CEFR_LEVEL.B2, CEFR_LEVEL.C1 };

        public static void Main() 
        {
            CreateOutputFolders();

            ChapterByWord = new Dictionary<string, string>();
            TranslationsByWord = new Dictionary<string, string>();
            string[] lines = System.IO.File.ReadAllLines("src/Rivstart A1A2.tsv");

            foreach (string line in lines) {
                string[] temp = line.Split("\t");
                string key = temp[0].ToLower().Replace(".", "");

                if (!ChapterByWord.ContainsKey(key)) {
                    ChapterByWord.Add(key, temp[4].Replace(" ", "_"));
                }

                if (!TranslationsByWord.ContainsKey(key)) {
                    TranslationsByWord.Add(key, $"<ul><li>{temp[2]}</li></ul>");
                }
            }

            lines = System.IO.File.ReadAllLines("src/SVALex_Korp.tsv");
            string[] fields = lines[0].Split("\t");

            SweToEngDict = new SweToEngDictionary(XmlDictionary.Deserialize("src/People's_Dictionary.xml"));
            Decks = new Dictionary<CEFR_LEVEL, List<AnkiCard>>();

            CachedSentences = LoadCacheCachedSentences();

            foreach (CEFR_LEVEL key in LEVELS) {
                Decks.Add(key, new List<AnkiCard>());
            }

            int count = 1;
            System.Threading.Tasks.Parallel.For(1, lines.Length, new ParallelOptions { MaxDegreeOfParallelism = 3 }, (i) => 
            {                
                string[] tokens = lines[i].Split("\t");

                CEFR_LEVEL key = CEFR_LEVEL.C1;

                // Rivstart A1
                if (float.Parse(tokens[50]) > 0.0) key = CEFR_LEVEL.A1;
                // Rivstart A2
                else if (float.Parse(tokens[26]) > 0.0) key = CEFR_LEVEL.A2;
                // Rivstart B1
                else if (float.Parse(tokens[20]) > 0.0) key = CEFR_LEVEL.B1;
                // Rivstart B2
                else if (float.Parse(tokens[28]) > 0.0) key = CEFR_LEVEL.B2;
                // A1
                else if (float.Parse(tokens[2]) > 2.0) key = CEFR_LEVEL.A1;
                // A2
                else if (float.Parse(tokens[3]) > 2.0) key = CEFR_LEVEL.A2;
                // B1
                else if (float.Parse(tokens[4]) > 2.0) key = CEFR_LEVEL.B1;
                // B2
                else if (float.Parse(tokens[5]) > 2.0) key = CEFR_LEVEL.B2;

                if (GenerateCard(tokens, key, out AnkiCard card)) {
                    Decks[key].Add(card);
                }

                if (count % 100 == 0) {
                    System.Console.WriteLine($"{count}/{lines.Length}");
                }

                count++;
            });

            foreach (CEFR_LEVEL key in LEVELS) {
                System.IO.File.WriteAllText($"output/{key.ToString()}_Deck.tsv", string.Join("\n", Decks[key]));    
            }
        }
        
        public static List<string> LoadCacheCachedSentences() 
        {   
            List<string> sentences = new List<string>(); 
            
            foreach (CEFR_LEVEL level in LEVELS) 
            {
                if (!File.Exists($"src/{level}_Sentences.tsv")) {
                    File.Create($"src/{level}_Sentences.tsv").Close();
                } else {
                    sentences.AddRange(File.ReadAllLines($"src/{level}_Sentences.tsv"));
                }
            }

            return sentences;
        }

        public static bool GetCachedSentence(List<string> sentences, string word, string wordClass, out string sentence) 
        {
            for (int j = 0; j < sentences.Count; j++) {
                string[] values = sentences[j].Split("\t");
                if (values[0] == $"{word} ({wordClass})") {
                    sentence = values[1];
                    return true;
                }
            }
            sentence = "";
            return false;
        }

        public static bool GenerateCard(string[] tokens, CEFR_LEVEL level, out AnkiCard card) 
        {
            string word = tokens[0].Replace("_", " ").ToLower();

            string wordClass = ConvertWordClass(tokens[1].Split('_')[0].ToLower());
            string FormatedWordClass = FormatWordClass(tokens[1].Split('_')[0].ToLower());

            string question = word + " (" + FormatedWordClass + ")";

            if (Decks.Where(x => x.Value.Where(x => x.Question == question).Count() > 0).Count() > 0) {
                card = null;
                return false;
            }

            string definition = SweToEngDict.GetDefinitions(word, wordClass);

            if (definition == "") {
                if (!TranslationsByWord.ContainsKey(word)) {
                    card = null;
                    return false;
                } else {
                    definition = TranslationsByWord[word];
                }
            }

            string sentence = "";
            if (!GetCachedSentence(CachedSentences, word, FormatedWordClass, out sentence)) {
                // if (CorpusSearch.GetSentence(word, wordClass, out sentence)) {
                //     System.IO.File.AppendAllLines($"output/{level.ToString()}_Sentences.tsv", new string[] { $"{word} ({FormatedWordClass})\t{sentence}" });
                // } else {
                //     sentence = SweToEngDict.GetExampleStrict(word, wordClass);
                // }

                sentence = SweToEngDict.GetExampleStrict(word, wordClass);
            }

            // if (sentence == "") {
            //     System.Console.WriteLine($"No sentence found for: {word} ({wordClass})");
            // }

            string abreviations = "";
            if (SweToEngDict.AbreviationByWord.ContainsKey(word)) {
                abreviations = SweToEngDict.AbreviationByWord[word][0];
            }

            string tags = "";

            if (float.Parse(tokens[50]) > 0.0) tags += "Rivstart_A1 ";
            else if (float.Parse(tokens[26]) > 0.0) tags += "Rivstart_A2 ";
            else if (float.Parse(tokens[20]) > 0.0) tags += "Rivstart_B1 ";
            else if (float.Parse(tokens[28]) > 0.0) tags += "Rivstart_B2 ";

            if (ChapterByWord.ContainsKey(word)) {
                tags += ChapterByWord[word];
            }

            if (SweToEngDict.HasAudio.Contains(tokens[0])) {
                AudioSearch.GetAudioStream(tokens[0]);
            } 

            string gramar = "";

            if (wordClass == "nn") {
                gramar = GetGender(tokens[1]);
            } else if(wordClass == "vb") {
                gramar = "att";
            }

            card = new AnkiCard {
                Question = question,
                Word = word,
                Class = FormatedWordClass,
                ClassAbbreviated = wordClass,
                Gender = gramar,
                Abreviations = abreviations,
                Definition = definition,
                Sentence = sentence,
                Audio = File.Exists("output/collection.media/" + word + ".mp3") ? "[sound:" + word + ".mp3]" : "",
                Frequency = tokens[(int)level],
                Tags = tags
            };

            return true;
        }

        public static string GetGender(string wordClass) 
        {
            string[] temp = wordClass.Split('_');

            if (temp.Length > 1) {
                if (temp[1] == "UTR") {
                    return "en";
                } else if (temp[1] == "NEU") {
                    return "ett";
                }
            }

            return "";
        }

        public static string ConvertWordClass(string wordClass) 
        {
            if (wordClass == "vbm") return "vb";
            else if (wordClass == "ppm") return "pp";
            else if (wordClass == "abm") return "ab";
            else if (wordClass == "pnm") return "pn";
            else if (wordClass == "nnm") return "nn";
            else if (wordClass == "pmm") return "pm";
            else if (wordClass == "jjm") return "jj";
            else if (wordClass == "inm") return "in";
            else if (wordClass == "knm") return "kn";
            else if (wordClass == "snm") return "kn";
            else return wordClass;
        }

        public static string FormatWordClass(string wordClass) 
        {
            if (wordClass == "") return "";
            else if (wordClass == "pp") return "preposition";
            else if (wordClass == "nn") return"noun";
            else if (wordClass == "vb") return "verb";
            else if (wordClass == "nn") return "noun";
            else if (wordClass == "jj") return "adjective";
            else if (wordClass == "ab") return "adverb";
            else if (wordClass == "rg") return "numeral";
            else if (wordClass == "pn") return "prounoun";
            else if (wordClass == "in") return "interjection";
            else if (wordClass == "kn") return "conjunction";
            else if (wordClass == "pm") return "name";
            else if (wordClass == "abbrev") return "abreviation";
            else return wordClass;
        }  

        public static void CreateOutputFolders() 
        {
            if (!Directory.Exists("output")) {
                Directory.CreateDirectory("output");
            }

            if (!Directory.Exists("output/card")) {
                Directory.CreateDirectory("output/card");
            }

            foreach (string file in Directory.GetFiles("src/card")) {
                System.IO.File.Copy(file, "output/card/" + file.Split('/').Last(), true);
            }

            if (!Directory.Exists("output/collection.media")) {
                Directory.CreateDirectory("output/collection.media");
            }

            foreach (string file in Directory.GetFiles("src/collection.media")) {
                System.IO.File.Copy(file, "output/collection.media/" + file.Split('/').Last(), true);
            }
        }
    }
}