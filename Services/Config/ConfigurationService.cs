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
                WordPressAuthorId = 3, //admin2
                PodcastAudioFolder = "/Users/phil/Documents/Shenley/SCF Podcast/automated",
                IntroWavFilePath = "/Users/phil/Documents/Shenley/SCF Podcast/To Do/Intro.wav",
                VbrBitrate = 9,
                FfmpegMetadataTemplatePath = "metadata-template.txt"
            };
        }
        public Configuration Configuration => _configuration;
    }
}