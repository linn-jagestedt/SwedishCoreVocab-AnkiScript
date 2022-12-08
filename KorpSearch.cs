using System.Text.Json;

namespace DeckGenerator 
{
    public class Match {
        public int position { get; set; }
        public int start { get; set; }
        public int end { get; set; }
    }

    public class Token 
    {
        public string word { get; set; }
    }

    public class Structs {
        public string lesson_level { get; set; }
        public string lesson_cefr_level { get; set; }
        public string level { get => lesson_level == null ? lesson_cefr_level == null ? "" : lesson_cefr_level : lesson_level; }
    }

    public class Sentence 
    {
        public string corpus { get; set; }
        public Match match { get; set; }
        public Structs structs { get; set; }
        public Token[] tokens { get; set; }
    }

    public class SearchResult 
    {
        public Sentence[] kwic { get; set; }
        public int hits { get; set; }
        public string[] corpus_order { get; set; }
        public string query_data { get; set; }
        public float time { get; set; }
    }   

    public static class KorpSearch 
    {
        public static bool LocalOnly = false;
        private static Dictionary<string, List<string>> _cache;
        public const string CACHEFILE = "temp/Corpus_sentences.tsv";

        public static bool GetSentence(string writtenForm, string wordClass, out string result) 
        {
            if (_cache == null) {
                _cache = ReadCache();
            }

            string word = $"{writtenForm} ({wordClass})";

            if (_cache.ContainsKey(word)) {
                result = _cache[word][0].ToLower();
                return true;
            }

            if (LocalOnly) {
                result = "";
                return false;
            }

            //string corpus = "COCTAILL-LT,SIC2";
            string corpus = "COCTAILL-LT";
            
            string url;

            if (writtenForm.Contains(" ")) {
                url = $"https://ws.spraakbanken.gu.se/ws/korp/v8/query?corpus={corpus}&default_context=1%20sentence&cqp=%5Blemma%20contains%20%22{writtenForm.Replace(" ", "_")}%22%5D&show_struct=lesson_level,lesson_cefr_level";
            } else {
                url = $"https://ws.spraakbanken.gu.se/ws/korp/v8/query?corpus={corpus}&default_context=1%20sentence&cqp=%5Bpos%20%3D%20%22{wordClass.ToUpper()}%22%20%26%20lemma%20contains%20%22{writtenForm}%22%5D&show_struct=lesson_level,lesson_cefr_level";
            }

            if (SearchCorpus(url, out string[] sentences)) 
            {
                using (StreamWriter writer = new StreamWriter(OpenFileStream(CACHEFILE))) {
                    writer.WriteLine($"{word}\t{string.Join("\t", sentences)}");
                }
  
                result = sentences[0].ToLower();
                return true;
            }
                
            result = "";
            return false;
        }

        private static bool SearchCorpus(string url, out string[] result) 
        {
            result = null;

            Task<string> jsonTask;

            try { jsonTask = new HttpClient().GetStringAsync(url); } 
            catch { return false; }
            
            jsonTask.Wait();
            
            SearchResult searchResult = (SearchResult)JsonSerializer.Deserialize(jsonTask.Result, typeof(SearchResult));
            
            if (searchResult.kwic.Where(x => CountWords(x.tokens) < 20 && CountWords(x.tokens) > 3).Count() > 0) {
                searchResult.kwic = searchResult.kwic.Where(x => CountWords(x.tokens) < 20 && CountWords(x.tokens) > 3).ToArray();
            } else { return false; }

            if (searchResult.kwic.Length < 1) { return false; } 
            searchResult.kwic = searchResult.kwic.OrderBy(x => x.structs != null ? x.structs.level : "Z1").ToArray();

            result = searchResult.kwic.Select(x => TokensToString(x.tokens)).ToArray();
            return true; 
        }

        private static FileStream OpenFileStream(string filename)
        {
            bool Locked = true;
            FileStream fileStream = null;

            while (Locked == true) {
                try {
                    fileStream = File.Open(
                        filename, 
                        FileMode.Append, 
                        FileAccess.Write, 
                        FileShare.None
                    );
                    Locked = false;
                } catch {
                    Thread.Sleep(10); 
                    Locked = true; 
                }
            }

            return fileStream;
        }

        private static Dictionary<string, List<string>> ReadCache() 
        {   
            Dictionary<string, List<string>> sentences = new Dictionary<string, List<string>>(); 

            if (!File.Exists(CACHEFILE)) {
                File.Create(CACHEFILE).Close();
            }

            string[] lines = File.ReadAllLines(CACHEFILE);

            foreach (string line in lines) {
                string[] tokens = line.Split("\t");
                sentences.Add(tokens[0], new List<string>());
                for (int i = 1; i < tokens.Length; i++) {
                    sentences[tokens[0]].Add(tokens[i]);
                }
            }

            return sentences;
        }

        private static int CountWords(Token[] tokens) {
            int result = 0;
            foreach (Token token in tokens) {
                if (!new string[] { ",",  ".", "\"", "(", ")", "[", "]", "!", "?", ":", "-", "”", "“" }.Contains(token.word)) {
                    result++;
                }
            }
            return result;
        }

        private static string TokensToString(Token[] tokens) 
        {
            string result = "";

            foreach (Token token in tokens) {
                if (token.word == "." || token.word == "," || token.word == ")" || token.word == "]" || token.word == "?" || token.word == "!" || token.word == ":")  
                {
                    if (result != "") {
                        result = result.Substring(0, result.Length - 1);
                    }
                    result += token.word + " ";
                } 
                else if (token.word == "(" || token.word == "[") 
                {
                    result += token.word;
                } 
                else if (token.word == "/") 
                {
                    if (result != "") {
                        result = result.Substring(0, result.Length - 1);
                    }
                    result += token.word;
                } 
                else if (token.word != "-" && token.word != "\"" && token.word != "”" && token.word != "“") 
                {
                    result += token.word + " ";
                }
            }

            return result;
        }
    }
}