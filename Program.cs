namespace youtubeVideoFetcher;

class Program
{
    static void Main(string[] args)
    {
        LogManager.LogMan.Info("Please enter a google API key");
        string apiKey = LogManager.LogMan.ReadLine();
        VideoFetch fetcher = new VideoFetch(apiKey);

        string command = null;

        while ((command = LogManager.LogMan.ReadLine()) != "stop")
        {
            string[] arguments = command.Split();

            if (arguments[0] == "creator")
            {
                fetcher.FetchVideosByChannel(arguments[1]);
            }

            if (arguments[0] == "url")
            {
                fetcher.FetchVideoByURL(arguments[1]);
            }
        }
    }
}