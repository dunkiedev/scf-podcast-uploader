namespace ScfPodcastUploader.Domain.WordPress
{
    public class WordPressResult
    {
        public bool IsSuccess { get; set; }

        public int Id { get; set; }

        public string Url { get; set; }

        public string ErrorMessage { get; set; }
    }
}