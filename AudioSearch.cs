using System;

public static class AudioSearch 
{
    public static void GetAudioStream(string word) 
    {
        string formatedString = FormatWord(word);
        string filePath = $"output/collection.media/{word}.mp3";
        string url = $"http://lexin.nada.kth.se/sound/{formatedString}.mp3";

        var client = new HttpClient();
        if (File.Exists(filePath)) {
            return;
        }      

        Task<Stream> streamTask;

        try {
            streamTask = Task.Run(() => client.GetStreamAsync(url)); 
            streamTask.Wait();
        } catch {
            System.Console.WriteLine("Failed to fetch audio");
            return;
        }

        var fs = new FileStream("output/collection.media/" + word + ".mp3", FileMode.OpenOrCreate); 
        var copyTask = streamTask.Result.CopyToAsync(fs);
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