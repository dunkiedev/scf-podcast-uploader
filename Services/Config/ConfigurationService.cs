using System.IO;
using Microsoft.Extensions.Configuration;
using ScfPodcastUploader.Domain.Config;

namespace ScfPodcastUploader.Services.Config
{
    public class ConfigurationService : IConfigurationService
    {
        private Configuration _configuration;

        public ConfigurationService()
        {
            IConfigurationBuilder builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            IConfigurationRoot configurationRoot = builder.Build();

            _configuration = new Configuration();
            configurationRoot.GetSection("App").Bind(_configuration);
        }
        public Configuration Configuration => _configuration;
    }
}