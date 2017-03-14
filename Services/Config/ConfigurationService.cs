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
                BasicAuthPassword = "Passw0rd",
                PostTemplatePath = "podcast-post-template.html",
                WordPressPodcastCategoryId = 6, //Podcast
                WordPressAuthorId = 3 //admin2 = Phil Dunkerley
            };
        }
        public Configuration Configuration => _configuration;
    }
}