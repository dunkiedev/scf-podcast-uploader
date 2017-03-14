using System;
using System.Collections.Generic;
using System.IO;
using ScfPodcastUploader.Domain;
using ScfPodcastUploader.Domain.WordPress;
using ScfPodcastUploader.Services.Config;
using ScfPodcastUploader.Services.WordPress;

namespace ScfPodcastUploader.Services
{
    public class PodcastService : IPodcastService
    {
        private readonly IWordPressService _wordPressService;
        private readonly IConfigurationService _configurationService;
        public PodcastService(IWordPressService wordPressService, IConfigurationService configurationService)
        {
            _configurationService = configurationService;
            _wordPressService = wordPressService;
        }

        public WordPressResult CreatePodcastPost(PodcastPost podcastPost)
        {
            //1. read in template
            string templateText = ReadTemplate();

            //2. make substitutions to template
            List<string> substitutions = new List<string>{
                podcastPost.Speaker, //{0}
                podcastPost.BibleText, //{1}
                podcastPost.GetFormattedDuration(), //{2}
                podcastPost.GetFormattedSize(), //{3}
                podcastPost.PodcastUrl, //{4}
                podcastPost.Title //{5}
            };
            string content = string.Format(templateText, substitutions.ToArray());

            //3. call WordPress to add the post
            WordPressPost wordPressPost = new WordPressPost()
            {
                status = "publish",
                title = podcastPost.Title,
                content = content
            };
            WordPressResult result = _wordPressService.AddPost(wordPressPost);

            //4. return result
            return result;
        }

        public WordPressResult UploadAudioFile(string filepath)
        {
            return _wordPressService.AddMedia(filepath);
        }

        private string ReadTemplate()
        {
            string templatePath = _configurationService.Configuration.PostTemplatePath;
            if(!File.Exists(templatePath))
            {
                throw new ArgumentException("PostTemplatePath not specified");
            }

            return File.ReadAllText(_configurationService.Configuration.PostTemplatePath);
        }

    }
}