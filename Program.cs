using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using log4net;
using log4net.Config;
using log4net.Repository;
using Microsoft.Extensions.Configuration;
using ScfPodcastUploader.Domain;
using ScfPodcastUploader.Domain.WordPress;
using ScfPodcastUploader.Services;
using ScfPodcastUploader.Services.Config;
using StructureMap;

namespace ScfPodcastUploader
{
    public class Program
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(Program));

        public static void Main(string[] args)
        {
            //configure log4net
            ILoggerRepository defaultRepository = log4net.LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.ConfigureAndWatch(defaultRepository, new FileInfo("log4net.config"));

            IContainer container = Container.For<ScfPodcastUploaderRegistry>();
            Program program = container.GetInstance<Program>();

            try
            {
                program.Run(args);
            }
            catch (Exception ex)
            {
                _logger.Error("Fatal exception was thrown", ex);
            }
        }

        private readonly IPodcastService _podcastService;
        private readonly IConfigurationService _configurationService;

        public Program(IPodcastService podcastService, IConfigurationService configurationService)
        {
            _configurationService = configurationService;
            _podcastService = podcastService;
        }

        public void Run(string[] args)
        {
            CommandLineArgs commandLineArgs = ParseArgs(args);

            //where should we get the podcast details from?
            PodcastPost podcastPost;
            if (commandLineArgs.IsInteractive)
            {
                podcastPost = PromptUserForPodcastDetails();
            }
            else
            {
                podcastPost = LoadDetailsFromFile();
            }

            //create the MP3 file
            podcastPost.AudioFilePath = CreateMp3File(podcastPost.AudioFilePath, podcastPost);

            //upload the file
            WordPressResult audioFileResult = UploadAudioFile(podcastPost);
            if (!audioFileResult.IsSuccess) { return; }

            podcastPost.PodcastMediaUrl = audioFileResult.Url;

            //create the post on WordPress
            WordPressResult createPostResult = CreatePost(podcastPost);
            podcastPost.PodcastPostUrl = createPostResult.Url;

            //update the RSS feed
            UpdateRssFeed(podcastPost);
        }

        private PodcastPost PromptUserForPodcastDetails()
        {
            PodcastPost podcastPost = new PodcastPost(_configurationService.Configuration);

            Console.Write("Title: ");
            podcastPost.Title = Console.ReadLine();
            _logger.Info("Title entered: " + podcastPost.Title);

            Console.Write("Speaker: ");
            podcastPost.Speaker = Console.ReadLine();
            _logger.Info("Speaker entered: " + podcastPost.Speaker);

            Console.Write("Bible text(s): ");
            podcastPost.BibleText = Console.ReadLine();
            _logger.Info("Bible text(s) entered: " + podcastPost.BibleText);

            podcastPost.Date = PromptForDate();

            podcastPost.AudioFilePath = PromptForAudioFilePath();

            return podcastPost;
        }

        private static DateTime PromptForDate()
        {
            DateTime date;
            string dateString;
            bool isFirstEntryOfDate = true;
            do
            {
                if (isFirstEntryOfDate)
                {
                    Console.Write("Date - dd/mm/yyyy: ");
                }
                else
                {
                    Console.Write("Invalid date - must be in format dd/mm/yyyy e.g. 31/12/2016: ");
                }

                dateString = Console.ReadLine();
                _logger.Info("Date entered: " + dateString);
                isFirstEntryOfDate = false;
            } while (!DateTime.TryParseExact(dateString, new[] { "dd/MM/yyyy", "d/MM/yyyy", "dd/M/yyyy" }, null, DateTimeStyles.None, out date));
            //must be valid now - set the time to 11:00am
            return date.AddHours(11);
        }

        private static string PromptForAudioFilePath()
        {
            bool isFirstEntryOfPath = true;
            string path;
            do
            {
                if (isFirstEntryOfPath)
                {
                    Console.Write("Path to audio file: ");
                }
                else
                {
                    Console.Write("File does not exist - please enter a valid path: ");
                }

                path = Console.ReadLine();
                _logger.Info("Audio file path entered: " + path);
                isFirstEntryOfPath = false;
            } while (!File.Exists(path));

            return path;
        }

        private string CreateMp3File(string filepath, PodcastPost podcastPost)
        {
            if (filepath.EndsWith(".mp3"))
            {
                Console.WriteLine("Supplied audio file is MP3 file so skipping MP3 generation");
                return filepath;
            }

            //must be a WAV file, so create the MP3
            Console.Write("Generating MP3 file - please be patient... ");
            try
            {
                string mp3Filepath = _podcastService.GenerateMp3File(filepath, podcastPost);
                Console.WriteLine("Success!");
                return mp3Filepath;
            }
            catch (System.Exception ex)
            {
                Console.WriteLine("Failed with error:");
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        private WordPressResult UploadAudioFile(PodcastPost podcastPost)
        {
            Console.Write("Uploading the audio file... ");
            _logger.Info("Beginning upload of audio file");

            WordPressResult result = _podcastService.UploadAudioFile(podcastPost.AudioFilePath);
            if (result.IsSuccess)
            {
                Console.WriteLine($"Success! (Id = {result.Id})");
                _logger.Info($"Upload was successful - id = {result.Id}");
                _logger.Info($"Audio file URL = {result.Url}");
            }
            else
            {
                Console.WriteLine("Oh dear, it didn't work - here is the error: ");
                Console.WriteLine(result.ErrorMessage);
                Console.WriteLine("See the log file for more details...");

                _logger.Error($"Upload failed with error: {result.ErrorMessage}");
                //TODO need to capture the detailed error somehow
            }

            return result;
        }

        private WordPressResult CreatePost(PodcastPost podcastPost)
        {
            Console.Write("Creating WordPress post...  ");
            _logger.Info("Beginning creation of post");

            WordPressResult result = _podcastService.CreatePodcastPost(podcastPost);
            if (result.IsSuccess)
            {
                Console.WriteLine($"Success! (Id = {result.Id})");
                Console.WriteLine($"The URL of the new post is:\n{result.Url}");
                _logger.Info($"Post creation succeeded - id = {result.Id}");
                _logger.Info($"Post URL = {result.Url}");
            }
            else
            {
                Console.WriteLine("Oh dear, it didn't work - here is the error: ");
                Console.WriteLine(result.ErrorMessage);
                Console.WriteLine("See the log file for more details...");
            }

            return result;
        }

        private void UpdateRssFeed(PodcastPost podcastPost)
        {
            Console.Write("Updating RSS feed... ");
            _logger.Info("Updating RSS feed");

            _podcastService.UpdateRssFeed(podcastPost);

            Console.WriteLine("Success!");
            _logger.Info("RSS feed updated");
        }

        private CommandLineArgs ParseArgs(string[] args)
        {
            CommandLineArgs commandLineArgs = new CommandLineArgs();

            if (args.Length == 1)
            {
                if (new string[] { "-i", "--i", "/i" }.Contains(args[0]))
                {
                    commandLineArgs.IsInteractive = true;
                }
            }
            else if (args.Length != 0)
            {
                Console.WriteLine("Usage: ScfPodcastUploader [-i]");
                Console.WriteLine("-i = enter details interactively");
                Console.WriteLine("If the -i argument is not passed then details are read from PodcastDetails.ini");
                Environment.Exit(-1);
            }

            return commandLineArgs;
        }

        private PodcastPost LoadDetailsFromFile()
        {
            IConfigurationBuilder builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddIniFile("PodcastDetails.ini");

            IConfigurationRoot config = builder.Build();

            string title = config["PodcastDetails:Title"];
            string speaker = config["PodcastDetails:Speaker"];
            string bibleText = config["PodcastDetails:BibleText"];
            string dateString = config["PodcastDetails:Date"];
            string audioFilePath = config["PodcastDetails:AudioFilePath"];

            DateTime date;
            if (!DateTime.TryParseExact(dateString, new[] { "dd/MM/yyyy", "d/MM/yyyy", "dd/M/yyyy" }, null, DateTimeStyles.None, out date))
            {
                throw new ArgumentException("The date in the PodcastDetails.ini file must be in the format dd/mm/yyyy e.g. 31/01/2017");
            }

            return new PodcastPost(_configurationService.Configuration)
            {
                Title = title,
                Speaker = speaker,
                BibleText = bibleText,
                Date = date.AddHours(11),   //set the date to 11am
                AudioFilePath = audioFilePath
            };
        }
    }

    public class CommandLineArgs
    {
        /// <summary>
        /// Indicates if the user wants to enter the details interactively.
        /// </summary>
        /// <returns></returns>
        public bool IsInteractive { get; set; }
    }
}
