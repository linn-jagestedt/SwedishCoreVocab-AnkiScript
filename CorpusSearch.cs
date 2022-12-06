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

    public static class CorpusSearch 
    {
        public static bool GetSentence(string word, string wordClass, out string result) 
        {
            string url;
            string corpus = "COCTAILL-LT,SIC2";

            if (word.Contains(" ")) {
                url = $"https://ws.spraakbanken.gu.se/ws/korp/v8/query?corpus={corpus}&default_context=1%20sentence&cqp=%5Blemma%20contains%20%22{word.Replace(" ", "_")}%22%5D&show_struct=lesson_level,lesson_cefr_level";
            } else {
                url = $"https://ws.spraakbanken.gu.se/ws/korp/v8/query?corpus={corpus}&default_context=1%20sentence&cqp=%5Bpos%20%3D%20%22{wordClass.ToUpper()}%22%20%26%20lemma%20contains%20%22{word}%22%5D&show_struct=lesson_level,lesson_cefr_level";
            }

            if (SearchCorpus(url, out result)) {
                return true;
            }

            return false;
        }

        public static bool SearchCorpus(string url, out string result) 
        {
            result = "";

            Task<string> jsonTask;

            try { jsonTask = new HttpClient().GetStringAsync(url); } 
            catch { return false; }
            
            jsonTask.Wait();
            
            SearchResult searchResult = (SearchResult)JsonSerializer.Deserialize(jsonTask.Result, typeof(SearchResult));
            
            if (searchResult.kwic.Where(x => CountWords(x.tokens) < 20 && CountWords(x.tokens) > 3).Count() > 0) {
                searchResult.kwic = searchResult.kwic.Where(x => CountWords(x.tokens) < 20 && CountWords(x.tokens) > 3).ToArray();
            } else {
                return false;
            }

            if (searchResult.kwic.Length < 1) {
                return false;
            } 

            searchResult.kwic = searchResult.kwic.OrderBy(x => x.structs != null ? x.structs.level : "Z1").ToArray();

            result = TokensToString(searchResult.kwic[0].tokens);

            if (result == "") {
                return false; 
            }

            return true; 
        }

        public static int CountWords(Token[] tokens) {
            int result = 0;
            foreach (Token token in tokens) {
                if (!new string[] { ",",  ".", "\"", "(", ")", "[", "]", "!", "?", ":", "-", "”", "“" }.Contains(token.word)) {
                    result++;
                }
            }
            return result;
        }

        public static string TokensToString(Token[] tokens) 
        {
            string result = "";

            foreach (Token token in tokens) {
                if (token.word == "." || token.word == "," || token.word == ")" || token.word == "]" || token.word == "?" || token.word == "!" || token.word == ":")  {
                    if (result != "") {
                        result = result.Substring(0, result.Length - 1);
                    }
                    result += token.word + " ";
                } else if (token.word == "(" || token.word == "[") {
                    result += token.word;
                } else if (token.word == "/") {
                    if (result != "") {
                        result = result.Substring(0, result.Length - 1);
                    }
                    result += token.word;
                } else if (token.word != "-" && token.word != "\"" && token.word != "”" && token.word != "“") {
                    result += token.word + " ";
                }
            }

            return result;
        }
    }
}