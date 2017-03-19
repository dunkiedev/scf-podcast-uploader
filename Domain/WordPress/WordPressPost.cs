namespace ScfPodcastUploader.Domain
{
    public class WordPressPost
    {
        public string status { get; set; }

        public string title { get; set; }

        public string content { get; set; }

        public string slug { get; set; }

        public int[] categories { get; set; }

        public int author { get; set; }

        public string date { get; set; }
    }
}