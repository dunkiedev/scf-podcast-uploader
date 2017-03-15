using System;
using ScfPodcastUploader.Domain;
using ScfPodcastUploader.Domain.WordPress;
using ScfPodcastUploader.Services;
using StructureMap;

namespace ScfPodcastUploader
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IContainer container = Container.For<ScfPodcastUploaderRegistry>();
            Program program = container.GetInstance<Program>();
            program.Run();
            //service.GetPost(2605).Wait();

            // WordPressPost post = new WordPressPost
            //     {
            //         title = "Test post1",
            //         status = "publish",
            //         content = "Test content",
            //     };
            // bool isSuccess = service.AddPost(post);

            //bool isSuccess = service.AddMedia2("/Users/phil/Documents/Shenley/SCF Podcast/SCF_2017-03-05.mp3");
            //bool isSuccess = service.AddMedia2("/Users/phil/Documents/Shenley/SCF Podcast/SCF_2016-01-24.mp3");
            //bool isSuccess = service.AddMedia("/Users/phil/Documents/Shenley/SCF Podcast/dummy3.mp3");

            //Console.Out.WriteLine($"Success: {isSuccess}");
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
