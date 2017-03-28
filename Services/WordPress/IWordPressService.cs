using ScfPodcastUploader.Domain;
using ScfPodcastUploader.Domain.WordPress;

namespace ScfPodcastUploader.Services.WordPress
{
    public interface IWordPressService
    {
         WordPressResult AddPost(WordPressPost post);

         WordPressResult AddMedia(string filepath, string mimeType);

         WordPressMedia FindMediaByTitle(string title);

         WordPressResult DeleteMedia(int id);
    }
}