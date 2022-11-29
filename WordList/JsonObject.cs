using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace WordList
{
    public class JSONWordList 
    {
        public Dictionary<string, List<JSONWord>> Words { get; set; }

        public JSONWordList(Dictionary<string, List<JSONWord>> words) 
        {
            Words = words;
        }
    }

    public class JSONWord
    {
        public string Class { get; set; }
        public string Gramar { get; set; }
    }
}