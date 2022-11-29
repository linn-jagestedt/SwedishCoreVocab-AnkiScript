using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace Dictionary
{
    public class JSONDictionary 
    {
        public Dictionary<string, List<JSONWord>> Words { get; set; }

        public JSONDictionary(Dictionary<string, List<JSONWord>> words) 
        {
            Words = words;
        }
    }

    public class JSONWord
    {
        public string Class { get; set; }
        public string[] Translations { get; set; }
        public Dictionary<string, string[]> Derivations { get; set; }
        public string Example { get; set; }
        public string Example_Translation { get; set; }
        public string[] Inflections { get; set; }
        public string Definition { get; set; }
    }
}