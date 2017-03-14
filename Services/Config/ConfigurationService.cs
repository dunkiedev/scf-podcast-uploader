using System;
using ScfPodcastUploader.Domain.Config;

namespace ScfPodcastUploader.Services.Config
{
    public class ConfigurationService : IConfigurationService
    {
        private Configuration _configuration;

        public ConfigurationService()
        {
            _configuration = new Configuration
            {
                WordPressBaseAddress = "http://windows7vm/wordpress",
                ProxyHost = "windows7vm",
                ProxyPort = 8888,
                UseProxy = true,
                AuthMethod = "BASIC",
                BasicAuthUsername = "admin2",
                BasicAuthPassword = "66booksinthebible",
                PostTemplatePath = "podcast-post-template.html"
            };
        }
        public Configuration Configuration => _configuration;
    }
}