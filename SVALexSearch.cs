using System;
using System.Text;
using System.Linq;
using System.Net;
using System.Text.Json;

namespace DeckGenerator
{
    public enum Corpus {
        NAN = 0, 
        A1 = 2,     Rivstart_A1 = 50,   På_Svenska_A1 = 33,     Nya_Mål_A1 = 32,    Svenska_Utifrån_A1 = 34,    Språkporten_B2 = 38,    Skrivtrappan_C1  = 22,
        A2 = 3,     Rivstart_A2 = 26,   På_Svenska_A2 = 35,     Nya_Mål_A2 = 46,    Svenska_Utifrån_A2 = 18,    Språkporten_C1 = 21,
        B1 = 4,     Rivstart_B1 = 20,   På_Svenska_B1 = 37,     Nya_Mål_B1 = 36,    Svenska_Utifrån_B1 = 27,
        B2 = 5,     Rivstart_B2 = 28,   På_Svenska_B2 = 15,     Nya_Mål_B2 = 14,   
        C1 = 6,         
        Total = 7,                                                                                             
    }

    public class SVALexEntry 
    {
        public string Word = "";
        public string WrittenForm = "";
        public string WordClass = "";
        public string FormatedWordClass = "";
        public string Gender = "";
        public Dictionary<Corpus, float> Frequency = new Dictionary<Corpus, float>();

        public bool GetLowestRivstartLevel(out Corpus result) 
        {   
            result = Corpus.NAN;

            if (Frequency[Corpus.Rivstart_A1] > 0) result = Corpus.Rivstart_A1; 
            else if (Frequency[Corpus.Rivstart_A2] > 0) result = Corpus.Rivstart_A2; 
            else if (Frequency[Corpus.Rivstart_B1] > 0) result = Corpus.Rivstart_B1; 
            else if (Frequency[Corpus.Rivstart_B2] > 0) result = Corpus.Rivstart_B2; 
            
            return result != Corpus.NAN;
        }

        public bool GetLowestSvenskaUtifrånLevel(out Corpus result) 
        {   
            result = Corpus.NAN;

            if (Frequency[Corpus.Svenska_Utifrån_A1] > 0) result = Corpus.Svenska_Utifrån_A1; 
            else if (Frequency[Corpus.Svenska_Utifrån_A2] > 0) result = Corpus.Svenska_Utifrån_A2; 
            else if (Frequency[Corpus.Svenska_Utifrån_B1] > 0) result = Corpus.Svenska_Utifrån_B1; 
            
            return result != Corpus.NAN;
        }


        public bool GetLowestNyaMålLevelLevel(out Corpus result) 
        {   
            result = Corpus.NAN;

            if (Frequency[Corpus.Nya_Mål_A1] > 0) result = Corpus.Nya_Mål_A1; 
            else if (Frequency[Corpus.Nya_Mål_A2] > 0) result = Corpus.Nya_Mål_A2; 
            else if (Frequency[Corpus.Nya_Mål_B1] > 0) result = Corpus.Nya_Mål_B1; 
            else if (Frequency[Corpus.Nya_Mål_B2] > 0) result = Corpus.Nya_Mål_B2; 
            
            return result != Corpus.NAN;
        }

        public bool GetLowestPåSvenskaLevel(out Corpus result) 
        {   
            result = Corpus.NAN;

            if (Frequency[Corpus.På_Svenska_A1] > 0) result = Corpus.På_Svenska_A1; 
            else if (Frequency[Corpus.På_Svenska_A2] > 0) result = Corpus.På_Svenska_A2; 
            else if (Frequency[Corpus.På_Svenska_B1] > 0) result = Corpus.På_Svenska_B1; 
            else if (Frequency[Corpus.På_Svenska_B2] > 0) result = Corpus.På_Svenska_B2; 
            
            return result != Corpus.NAN;
        }


        public bool GetLowestCEFRLevel(out Corpus result) 
        {    
            result = Corpus.NAN;

            if (Frequency[Corpus.A1] > 0) result = Corpus.A1; 
            else if (Frequency[Corpus.A2] > 0) result = Corpus.A2; 
            else if (Frequency[Corpus.B1] > 0) result = Corpus.B1; 
            else if (Frequency[Corpus.B2] > 0) result = Corpus.B2; 
            else if (Frequency[Corpus.C1] > 0) result = Corpus.C1; 
            
            return result != Corpus.NAN;
        }
    }

    public class SVALexSearch 
    {
        public Dictionary<string, SVALexEntry> Entries;

        public const string SOURCE_FILE = "src/SVALex.tsv";

        public SVALexSearch() 
        {
            Entries = new Dictionary<string, SVALexEntry>();
            
            if (!File.Exists(SOURCE_FILE)) {
                System.Console.WriteLine($"SAVLex Error, Cannot find file: {SOURCE_FILE}");
                return;
            }

            string[] lines = System.IO.File.ReadAllLines(SOURCE_FILE);
            string[] fields = lines[0].Split("\t");

            for(int i = 1; i < lines.Length; i++)
            {                
                string[] tokens = lines[i].Split("\t");

                string writtenForm = tokens[0].Replace("_", " ").ToLower();
                string wordClass = ConvertWordClass(tokens[1].Split('_')[0].ToLower());
                string FormatedWordClass = FormatWordClass(wordClass);
                string word = writtenForm + " (" + FormatedWordClass + ")";

                Dictionary<Corpus, float> frequency = new Dictionary<Corpus, float>();
                
                foreach (Corpus corpus in Enum.GetValues(typeof(Corpus))) {
                    if (corpus != Corpus.NAN) {
                        frequency.Add(corpus, float.Parse(tokens[(int)corpus]));
                    }
                }

                if (!Entries.ContainsKey(word)) {
                    Entries.Add(word, new SVALexEntry {
                        Word = word,
                        WrittenForm = writtenForm,
                        WordClass = wordClass,
                        FormatedWordClass = FormatedWordClass,
                        Gender = wordClass == "nn" ? GetGender(tokens[1]) : wordClass == "vb" ? "att" : "",
                        Frequency = frequency
                    });
                }
            }
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
            else if (wordClass == "snm") return "sn";
            else return wordClass;
        }

        public static string FormatWordClass(string wordClass) 
        {
            if (wordClass == "") return "";
            else if (wordClass == "pp") return "preposition";
            else if (wordClass == "vb") return "verb";
            else if (wordClass == "nn") return "noun";
            else if (wordClass == "jj") return "adjective";
            else if (wordClass == "ab" || wordClass == "ha") return "adverb";
            else if (wordClass == "ie") return "infinitive marker";
            else if (wordClass == "sn") return "subjunktion";
            else if (wordClass == "rg") return "numeral";
            else if (wordClass == "dt") return "determiner";
            else if (wordClass == "pn" || wordClass == "hp") return "prounoun";
            else if (wordClass == "in") return "interjection";
            else if (wordClass == "kn") return "conjunction";
            else if (wordClass == "pm") return "name";
            else if (wordClass == "abbrev") return "abreviation";
            else return wordClass;
        }  
    }
}