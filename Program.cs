using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using log4net;
using log4net.Config;
using log4net.Repository;
using ScfPodcastUploader.Domain;
using ScfPodcastUploader.Domain.WordPress;
using ScfPodcastUploader.Services;
using StructureMap;

namespace ScfPodcastUploader
{
    public class Program
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(Program));

        public static void Main(string[] args)
        {
            //configure log4net
            ILoggerRepository defaultRepository = log4net.LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.ConfigureAndWatch(defaultRepository, new FileInfo("log4net.config"));

            IContainer container = Container.For<ScfPodcastUploaderRegistry>();
            Program program = container.GetInstance<Program>();
            program.Run();
        }
        private readonly IPodcastService _podcastService;

        public Program(IPodcastService podcastService)
        {
            _podcastService = podcastService;
        }

        public void Run()
        {
            string audioFilepath = "/Users/phil/Documents/Shenley/SCF Podcast/SCF_2017-03-05.mp3";

            //upload the file
            WordPressResult audioFileResult = _podcastService.UploadAudioFile(audioFilepath);
            Console.Out.WriteLine("URL = " + audioFileResult.Url);
            
            PodcastPost podcastPost = new PodcastPost()
            {
                Title = "Hosting the Presence of God",
                Speaker = "Ross Dilnot",
                BibleText = "Psalm 16:11, 1 Samuel 16:21-23, Acts 2:1-41, Acts 5:12-16, Acts 19:11-12",
                AudioFilePath = audioFilepath,
                PodcastUrl = audioFileResult.Url
            };

            WordPressResult createPostResult = _podcastService.CreatePodcastPost(podcastPost);
            Console.Out.WriteLine("Post id = " + createPostResult.Id);
        }

    }
}
