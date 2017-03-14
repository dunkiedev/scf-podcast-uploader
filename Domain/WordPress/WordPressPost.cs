namespace ScfPodcastUploader.Domain
{
    public class WordPressPost
    {
        public string status { get; set; }

        public string title { get; set; }

        public string content { get; set; }

        public string slug { get; set; }

        public int[] category { get; set; }

        public int author { get; set; }
    }
}