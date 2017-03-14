using ScfPodcastUploader.Services.Config;
using StructureMap;

namespace ScfPodcastUploader
{
    public class ScfPodcastUploaderRegistry : Registry
    {
        public ScfPodcastUploaderRegistry()
        {
            Scan(scan =>
            {
                scan.TheCallingAssembly();
                scan.WithDefaultConventions();
            });

            For<IConfigurationService>().Singleton();
        }
    }
}