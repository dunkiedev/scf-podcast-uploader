using ScfPodcastUploader.Domain;
using ScfPodcastUploader.Domain.WordPress;

namespace ScfPodcastUploader.Services
{
    public interface IPodcastService
    {
         WordPressResult UploadAudioFile(string filepath);

         WordPressResult CreatePodcastPost(PodcastPost podcastPost);
    }
}