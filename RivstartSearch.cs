using System;
using System.Text;
using System.Linq;
using System.Net;
using System.Text.Json;

namespace DeckGenerator
{
    public class RivstartSearch 
    {
        public static Dictionary<string, string> TranslationsByWord;
        public static Dictionary<string, string> ChapterByWord;
        public const string SOURCE_FILE = "src/Rivstart.tsv";

        public RivstartSearch() 
        {
            ChapterByWord = new Dictionary<string, string>();
            TranslationsByWord = new Dictionary<string, string>();

            if (!File.Exists(SOURCE_FILE)) {
                System.Console.WriteLine($"RivstartVocab Error, cannot find file: {SOURCE_FILE}");
            }

            string[] lines = System.IO.File.ReadAllLines(SOURCE_FILE);
         
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
        }
    }
}