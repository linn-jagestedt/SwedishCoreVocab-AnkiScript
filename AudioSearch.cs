using System;

public static class AudioSearch 
{
    public const string OUTPUTFOLDER = "output/collection.media";

    public static bool GetAudioStream(string word) 
    {
        if (File.Exists($"{OUTPUTFOLDER}/{word}.mp3")) {
            return true;
        }      

        string formatedString = FormatWord(word);
        string url = $"http://lexin.nada.kth.se/sound/{formatedString}.mp3";
        var client = new HttpClient();
        Task<Stream> streamTask;

        try {
            streamTask = client.GetStreamAsync(url);
            streamTask.Wait();
        } catch {
            System.Console.WriteLine("Failed to fetch audio");
            return false;
        }

        var fs = new FileStream($"{OUTPUTFOLDER}/{word}.mp3", FileMode.OpenOrCreate); 
        var copyTask = streamTask.Result.CopyToAsync(fs);

        return true;
    }

    public static string FormatWord(string word) 
    {
        for (int j = 0; j < word.Length; j++) {
            if (word[j] > 127) {
                char c = word[j];
                word = word.Remove(j, 1);
                word = word.Insert(j, "0" + ((int)c).ToOctal().ToString());
            }
        }

        return word;
    }

    public static int ToOctal(this int n)
    {
        int result = 0;
        List<int> octalNum = new List<int>();
        for (int i = 0; n != 0; i++) {
            result += (int)MathF.Pow(10, i) * (n % 8);
            n /= 8;
        }

        return result;
    }
}