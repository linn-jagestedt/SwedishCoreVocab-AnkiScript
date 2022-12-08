using System.Text.Json;
using AngleSharp;
using AngleSharp.Dom;

namespace DeckGenerator 
{
    public static class SOSearch 
    {
        public static bool LocalOnly = false;
        public const string TEMP_FOLDER = "temp/SO";
        public const string OUTPUT_FOLDER = "output/collection.media";

        private static bool ParseHtml(string writtenForm, out IDocument document) 
        {   
            Task<IDocument> htmlTask;
            IConfiguration config = Configuration.Default;
            IBrowsingContext context = BrowsingContext.New(config);

            string filename = $"{TEMP_FOLDER}/{writtenForm}.html";

            if (File.Exists(filename)) {
                htmlTask = context.OpenAsync(req => req.Content(File.ReadAllText(filename)));
                htmlTask.Wait();
                document = htmlTask.Result;
                return true;
            }

            if (LocalOnly) {
                document = null;
                return false;
            }

            if (!Directory.Exists(TEMP_FOLDER)) {   
                Directory.CreateDirectory(TEMP_FOLDER);
            } 

            string url = $"https://spraakbanken.gu.se/saolhist/visa_so2009.php?bok=SO2009&lnr=121228&ord={writtenForm}";

            var client = new HttpClient();
            Task<Stream> streamTask;

            try {
                streamTask = client.GetStreamAsync(url);
                streamTask.Wait();
            } catch {
                document = null;
                return false;
            }
            
            FileStream fs = new FileStream(filename, FileMode.OpenOrCreate);
            Task copyTask = streamTask.Result.CopyToAsync(fs);    
            copyTask.Wait();
            fs.Close();

            string text = File.ReadAllText(filename);

            htmlTask = context.OpenAsync(req => req.Content(text));
            htmlTask.Wait();
            document = htmlTask.Result;
            return true;
        }

        public static bool FindElement(string wordClass, IDocument document, out IElement element) 
        {
            IHtmlCollection<IElement> elements = document.QuerySelectorAll(".ordklass");

            if (elements.Length < 1) {
                element = null;
                return false;
            }

            foreach (IElement e in  elements) 
            {
                if (e.TextContent.Trim() == SwedishWordClass(wordClass)) {
                    element = e.ParentElement;
                    return true;
                }
            }


            element = elements[0].ParentElement;
            return true;
        }

        public static bool DownloadAudio(string writtenForm, string wordClass, out string filename) 
        {
            filename = $"{writtenForm}_SO.mp3";

            if (File.Exists($"{OUTPUT_FOLDER}/{filename}")) {
                return true;
            }      

            if (LocalOnly) {
                return false;
            }

            if (!ParseHtml(writtenForm, out IDocument document)) {
                return false;
            }

            if (!FindElement(wordClass, document, out IElement element)) {
                return false;
            }

            var client = new HttpClient();
            Task<Stream> streamTask;
            
            string id = element.Id.Substring(3);

            try {
                streamTask = client.GetStreamAsync($"https://isolve-so-service.appspot.com/pronounce?id={id}_1.mp3");
                streamTask.Wait();
            } catch {
                System.Console.WriteLine("Failed to fetch audio");
                return false;
            }

            var fs = new FileStream($"{OUTPUT_FOLDER}/{filename}", FileMode.OpenOrCreate); 
            var copyTask = streamTask.Result.CopyToAsync(fs);

            return true;
        }

        public static bool GetDefinitions(string writtenForm, string wordClass, out string result) 
        {
            result = "";

            if (!ParseHtml(writtenForm, out IDocument document)) {
                return false;
            }

            if (!FindElement(wordClass, document, out IElement element)) {
                return false;
            }

            IElement def = element.QuerySelector(".def");
            if (def != null) {
                result += def.TextContent;
            }

            IElement deft = element.QuerySelector(".deft");
            if (deft != null) {
                result += $" [{deft.TextContent}]";
            }

            return result != "";
        }

        public static FileStream OpenFileStream(string filename)
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

        public static string SwedishWordClass(string wordClass) 
        {
            if (wordClass == "pp") return "preposition";
            else if (wordClass == "nn") return "substantiv";
            else if (wordClass == "vb") return "verb";
            else if (wordClass == "jj") return "adjektiv";
            else if (wordClass == "ab" || wordClass == "ha") return "adverb";
            else if (wordClass == "ie") return "infinitivmärke";
            else if (wordClass == "sn") return "subjunktion";
            else if (wordClass == "rg") return "räkneord";
            else if (wordClass == "dt") return "determinerare";
            else if (wordClass == "pn" || wordClass == "hp") return "pronomen";
            else if (wordClass == "in") return "interjektion";
            else if (wordClass == "kn") return "konjunktion";
            else if (wordClass == "pm") return "namn";
            else if (wordClass == "abbrev") return "förkortning";
            else return wordClass;
        }
    }
}