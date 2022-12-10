using System.Diagnostics;
using System.Collections.Concurrent;

namespace DeckGenerator
{
    public static class Program 
    {
        private static SVALexSearch _svaLexKorp;
        private static RivstartSearch _rivstartVocabList;
        private static FOSearch _sweToEngDict;
        
        private static Dictionary<Corpus, ConcurrentBag<AnkiCard>> _decks;


        public static void Main() 
        {
            if (!Directory.Exists("temp")) {
                Directory.CreateDirectory("temp");
            }

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

            //FOSearch.LocalOnly = true;
            //KorpSearch.LocalOnly = true;
            //SOSearch.LocalOnly = true;

            _rivstartVocabList  = new RivstartSearch();
            _svaLexKorp = new SVALexSearch();
            _sweToEngDict = new FOSearch();

            _decks = new Dictionary<Corpus, ConcurrentBag<AnkiCard>>();

            foreach (Corpus corpus in Enum.GetValues(typeof(Corpus))) {
                _decks.Add(corpus,new ConcurrentBag<AnkiCard>());
            }

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            int i = 0;

            Timer timer = new Timer((object state) => {
                float time = stopwatch.ElapsedMilliseconds / 1000.0f;
                Console.WriteLine($"{i} of {_svaLexKorp.Entries.Count} cards processed ({(i / time).ToString("N1")} cards per seccond)");
            }, null, 0, 2000);

            Parallel.ForEach(_svaLexKorp.Entries.Keys, new ParallelOptions { MaxDegreeOfParallelism = 4 }, (word) => 
            {   
                if (GenerateCard(_svaLexKorp.Entries[word], out AnkiCard card)) {
                    AddToDecks(word, card);
                }

                i++;
            });

            foreach (Corpus corpus in Enum.GetValues(typeof(Corpus))) {
                if (_decks[corpus].Count > 0) {
                    System.IO.File.WriteAllText($"output/{corpus.ToString()}_Deck.tsv", string.Join("\n", _decks[corpus].OrderByDescending(x => float.Parse(x.Frequency))));    
                } 
            }
        }

        public static void AddToDecks(string word, AnkiCard card) 
        {
            // Rivstart Deck

            if (_svaLexKorp.Entries[word].GetLowestRivstartLevel(out Corpus rivstartLevel)) 
            {
                if (RivstartSearch.ChapterByWord.ContainsKey(_svaLexKorp.Entries[word].WrittenForm)) {
                    card.Tags = RivstartSearch.ChapterByWord[_svaLexKorp.Entries[word].WrittenForm];
                }
                card.Frequency = _svaLexKorp.Entries[word].Frequency[rivstartLevel].ToString();
                _decks[rivstartLevel].Add(card);
            }

            // Nya Mål Deck

            if (_svaLexKorp.Entries[word].GetLowestNyaMålLevelLevel(out Corpus nyamålLevel)) 
            {
                card.Tags = "";
                card.Frequency = _svaLexKorp.Entries[word].Frequency[nyamålLevel].ToString();
                _decks[nyamålLevel].Add(card);
            }

            // På Svenska Deck

            if (_svaLexKorp.Entries[word].GetLowestPåSvenskaLevel(out Corpus påsvenskaLevel)) 
            {
                card.Tags = "";
                card.Frequency = _svaLexKorp.Entries[word].Frequency[påsvenskaLevel].ToString();
                _decks[påsvenskaLevel].Add(card);
            }

            // Svenska Utifrån Deck

            if (_svaLexKorp.Entries[word].GetLowestSvenskaUtifrånLevel(out Corpus svenskautifrånLevel)) 
            {
                card.Tags = "";
                card.Frequency = _svaLexKorp.Entries[word].Frequency[svenskautifrånLevel].ToString();
                _decks[svenskautifrånLevel].Add(card);
            }

            // CEFR Graded deck

            if (_svaLexKorp.Entries[word].GetLowestCEFRLevel(out Corpus cefrLevel)) 
            {
                card.Tags = "";
                Corpus[] exlude = new Corpus[] { Corpus.A1, Corpus.A2, Corpus.B1, Corpus.B2, Corpus.C1, Corpus.Total, Corpus.NAN };

                foreach (Corpus corpus in Enum.GetValues(typeof(Corpus))) {
                    if (!exlude.Contains(corpus)) {
                        if (_svaLexKorp.Entries[word].Frequency[corpus] > 0.0) {
                            card.Tags += corpus.ToString() + " ";
                        }
                    }
                }

                card.Frequency = _svaLexKorp.Entries[word].Frequency[cefrLevel].ToString();
                _decks[cefrLevel].Add(card);
            }

            // Core 9k deck

            card.Tags = "";
            
            Corpus[] cefrLevels = new Corpus[] { Corpus.A1, Corpus.A2, Corpus.B1, Corpus.B2, Corpus.C1 };

            foreach (Corpus corpus in cefrLevels) {
                if (_svaLexKorp.Entries[word].Frequency[corpus] > 0.0) {
                    card.Tags += corpus.ToString() + " ";
                }
            }

            card.Frequency = _svaLexKorp.Entries[word].Frequency[Corpus.Total].ToString();
            _decks[Corpus.Total].Add(card);

        }

        public static bool GenerateCard(SVALexEntry word, out AnkiCard card) 
        {
            if (!_sweToEngDict.GetDefinitions(word.WrittenForm, word.WordClass, out string enDefinition)) {
                if (!RivstartSearch.TranslationsByWord.ContainsKey(word.WrittenForm)) {
                    card = new AnkiCard();
                    return false;
                } 
                
                enDefinition = RivstartSearch.TranslationsByWord[word.WrittenForm];
            }

            string sentence = "";

            if (!KorpSearch.GetSentence(word.WrittenForm, word.WordClass, out sentence)) {
                sentence = _sweToEngDict.GetExampleStrict(word.WrittenForm, word.WordClass);
            }
         
            string abreviations = "";

            if (_sweToEngDict.AbreviationByWord.ContainsKey(word.WrittenForm)) {
                abreviations = _sweToEngDict.AbreviationByWord[word.WrittenForm][0];
            }

            if (!SOSearch.DownloadAudio(word.WrittenForm, word.WordClass, out string audioFile)) 
            {
                audioFile = "";
                
                if (_sweToEngDict.HasAudio.Contains(word.WrittenForm)) {
                    if (!FOSearch.DownloadAudio(word.WrittenForm, out audioFile)) {
                        audioFile = "";
                    }
                }
            }      

            if (!SOSearch.GetDefinitions(word.WrittenForm, word.WordClass, out string svDefinition)) {
                svDefinition = "";
            }

            card = new AnkiCard {
                Word = word.Word,
                WrittenForm = word.WrittenForm,
                Class = word.FormatedWordClass,
                Gender = word.Gender,
                Abreviation = abreviations,
                EnglishDefinition = enDefinition,
                SwedishDefinition = svDefinition,
                Sentence = sentence,
                Audio = audioFile == "" ? "" : $"[sound:{audioFile}]",
                Tags = ""
            };

            return true;
        }
    }
}