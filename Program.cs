using System.Diagnostics;

namespace DeckGenerator
{
    public static class Program 
    {
        private static SVALexKorp _svaLexKorp;
        private static RivstartVocabList _rivstartVocabList;
        private static SweToEngDictionary _sweToEngDict;
        public static void Main() 
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

            CorpusSearch.LocalOnly = true;

            Dictionary<Corpus, List<AnkiCard>> decks = new Dictionary<Corpus, List<AnkiCard>>();

            _rivstartVocabList  = new RivstartVocabList();

            _svaLexKorp = new SVALexKorp();
            _sweToEngDict = new SweToEngDictionary();

            decks = new Dictionary<Corpus, List<AnkiCard>>();

            foreach (Corpus corpus in Enum.GetValues(typeof(Corpus))) {
                decks.Add(corpus,new List<AnkiCard>());
            }

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            int i = 0;

            Parallel.ForEach(_svaLexKorp.Entries.Keys, new ParallelOptions { MaxDegreeOfParallelism = 6 }, (word) => 
            {   
                if (GenerateCard(_svaLexKorp.Entries[word], out AnkiCard card))
                {
                    // Rivstart Deck

                    if (_svaLexKorp.Entries[word].GetLowestRivstartLevel(out Corpus rivstartLevel)) 
                    {
                        if (RivstartVocabList.ChapterByWord.ContainsKey(_svaLexKorp.Entries[word].WrittenForm)) {
                            card.Tags = RivstartVocabList.ChapterByWord[_svaLexKorp.Entries[word].WrittenForm];
                        }
                        card.Frequency = _svaLexKorp.Entries[word].Frequency[rivstartLevel].ToString();
                        decks[rivstartLevel].Add(card);
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
                        decks[cefrLevel].Add(card);
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
                    decks[Corpus.Total].Add(card);
                }

                if (i % 200 == 0) {
                    float time = stopwatch.ElapsedMilliseconds == 0 ? 0.0f : stopwatch.ElapsedMilliseconds / 1000.0f;
                    Console.WriteLine($"{i} of {_svaLexKorp.Entries.Count} cards created ({(i / time).ToString("N1")} cards per seccond)");
                }

                i++;
            });

            foreach (Corpus corpus in Enum.GetValues(typeof(Corpus))) {
                if (decks[corpus].Count > 0) {
                    System.IO.File.WriteAllText($"output/{corpus.ToString()}_Deck.tsv", string.Join("\n", decks[corpus]));    
                } 
            }
        }

        public static bool GenerateCard(SVALexEntry word, out AnkiCard card) 
        {
            if (!_sweToEngDict.GetDefinitions(word.WrittenForm, word.WordClass, out string definition)) {
                if (!RivstartVocabList.TranslationsByWord.ContainsKey(word.WrittenForm)) {
                    card = new AnkiCard();
                    return false;
                } 
                
                definition = RivstartVocabList.TranslationsByWord[word.WrittenForm];
            }

            string sentence = "";

            if (!CorpusSearch.GetSentence(word.WrittenForm, word.WordClass, out sentence)) {
                sentence = _sweToEngDict.GetExampleStrict(word.WrittenForm, word.WordClass);
            }
         
            string abreviations = "";

            if (_sweToEngDict.AbreviationByWord.ContainsKey(word.WrittenForm)) {
                abreviations = _sweToEngDict.AbreviationByWord[word.WrittenForm][0];
            }

            string audioFile = "";

            if (_sweToEngDict.HasAudio.Contains(word.WrittenForm)) {
                if (AudioSearch.GetAudioStream(word.WrittenForm)) {
                    audioFile = "[sound:" + word.WrittenForm + ".mp3]";
                }
            } 

            card = new AnkiCard {
                Question = word.Word,
                Word = word.WrittenForm,
                Class = word.FormatedWordClass,
                Gender = word.Gender,
                Abreviations = abreviations,
                Definition = definition,
                Sentence = sentence,
                Audio = audioFile,
                Tags = ""
            };

            return true;
        }
    }
}