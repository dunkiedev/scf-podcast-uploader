using ScfPodcastUploader.Domain.Config;

namespace ScfPodcastUploader.Services.Config
{
    public interface IConfigurationService
    {
        Configuration Configuration { get; }
    }
}