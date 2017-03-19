using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using log4net;
using ScfPodcastUploader.Domain;
using ScfPodcastUploader.Domain.WordPress;
using ScfPodcastUploader.Services.Config;
using ScfPodcastUploader.Services.WordPress;

namespace ScfPodcastUploader.Services
{
    public class PodcastService : IPodcastService
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(Program));

        private readonly IWordPressService _wordPressService;
        private readonly IConfigurationService _configurationService;
        public PodcastService(IWordPressService wordPressService, IConfigurationService configurationService)
        {
            _configurationService = configurationService;
            _wordPressService = wordPressService;
        }

        public string GenerateMp3File(string wavFilePath, PodcastPost podcastPost)
        {
            //1. create input file for ffmpeg
            string[] files = new []
            {
                $"file '{_configurationService.Configuration.IntroWavFilePath}'",
                $"file '{wavFilePath}'"
            };

            DirectoryInfo tempFolder = Directory.CreateDirectory("temp");
            string filelistPath = Path.Combine(tempFolder.FullName, "filelist.txt");
            _logger.Info($"Generating filelist.txt at: {filelistPath}");
            File.WriteAllLines(filelistPath, files);

            //2. call ffmpeg to create the wav file
            string podcastWavFilepath = Path.Combine(tempFolder.FullName, "podcast.wav");
            Process ffmpeg = new Process();
            ffmpeg.StartInfo.FileName = "ffmpeg";
            ffmpeg.StartInfo.Arguments = $"-y -f concat -safe 0 -i \"{filelistPath}\" -c copy \"{podcastWavFilepath}\"";
            ffmpeg.StartInfo.UseShellExecute = false;
            ffmpeg.StartInfo.RedirectStandardOutput = true;
            ffmpeg.StartInfo.RedirectStandardError = true;
            _logger.Info($"Calling ffmpeg to create WAV file with arguments: {ffmpeg.StartInfo.Arguments}");
            ffmpeg.Start();
            // string output = ffmpeg.StandardOutput.ReadToEnd();
            string output = ffmpeg.StandardError.ReadToEnd();
            _logger.InfoFormat($"Output from ffmpeg:\n{output}");
            ffmpeg.WaitForExit();
            _logger.Info("WAV file generation finished");

            if(!File.Exists(podcastWavFilepath))
            {
                throw new InvalidOperationException("Intermediate WAV file was not generated");
            }

            //3. call ffmpeg to encode the generated file as an MP3
            int bitrate = _configurationService.Configuration.VbrBitrate;
            string podcastFilename = $"SCF_{podcastPost.Date.ToString("yyyy-MM-dd")}.mp3";
            string podcastFilePath = Path.Combine(_configurationService.Configuration.PodcastAudioFolder, podcastFilename);
            _logger.Info($"Ensuring output path exists: {_configurationService.Configuration.PodcastAudioFolder}");
            Directory.CreateDirectory(_configurationService.Configuration.PodcastAudioFolder);
            _logger.Info($"MP3 file will be saved as: {podcastFilePath}");

            //create the metadata file for the ID3 tags
            string metadataTemplate = File.ReadAllText(_configurationService.Configuration.FfmpegMetadataTemplatePath);
            string metadata = string.Format(metadataTemplate,
                podcastPost.Title,
                podcastPost.Speaker);
            string metadataFilepath = Path.Combine(tempFolder.FullName, "metadata.txt");
            _logger.Info("Writing metadata file");
            File.WriteAllText(metadataFilepath, metadata);

            ffmpeg = new Process();
            ffmpeg.StartInfo.FileName = "ffmpeg";
            ffmpeg.StartInfo.Arguments = $"-y -i \"{podcastWavFilepath}\" -i \"{metadataFilepath}\" -map_metadata 1 -c:a copy -id3v2_version 3 -write_id3v1 1 -codec:a libmp3lame -qscale:a {bitrate} \"{podcastFilePath}\"";
            ffmpeg.StartInfo.UseShellExecute = false;
            ffmpeg.StartInfo.RedirectStandardOutput = true;
            ffmpeg.StartInfo.RedirectStandardError = true;
            _logger.Info($"Calling ffmpeg to create MP3 file with arguments: {ffmpeg.StartInfo.Arguments}");
            ffmpeg.Start();
            // string output = ffmpeg.StandardOutput.ReadToEnd();
            output = ffmpeg.StandardError.ReadToEnd();
            _logger.InfoFormat($"Output from ffmpeg:\n{output}");
            ffmpeg.WaitForExit();
            _logger.Info("MP3 file generation finished");

            if(!File.Exists(podcastFilePath))
            {
                throw new InvalidOperationException("MP3 file was not generated");
            }

            //5. if all went well, tidy up
            try
            {
                tempFolder.Delete(true);
            }
            catch(Exception ex)
            {
                _logger.Warn("Could not delete temp folder", ex);
            }

            return podcastFilePath;
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
                title =  GetTitlePrefixedWithDate(podcastPost),
                content = content,
                date = podcastPost.Date.ToString("yyyy-MM-dd hh:mm:ss"),
                categories = new [] { _configurationService.Configuration.WordPressPodcastCategoryId },
                author = _configurationService.Configuration.WordPressAuthorId
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

        private string GetTitlePrefixedWithDate(PodcastPost podcastPost)
        {
            //e.g. 5th March 2017 – Hosting the Presence of God
            DateTime date = podcastPost.Date;

            return $"{date.Day}{GetDaySuffix(date.Day)} {GetMonthName(date.Month)} {date.Year} - {podcastPost.Title}";
        }

        string GetDaySuffix(int day)
        {
            switch (day)
            {
                case 1:
                case 21:
                case 31:
                    return "st";
                case 2:
                case 22:
                    return "nd";
                case 3:
                case 23:
                    return "rd";
                default:
                    return "th";
            }
        }

        string GetMonthName(int month)
        {
            switch(month)
            {
                case 1: return "January";
                case 2: return "February";
                case 3: return "March";
                case 4: return "April";
                case 5: return "May";
                case 6: return "June";
                case 7: return "July";
                case 8: return "August";
                case 9: return "September";
                case 10: return "October";
                case 11: return "November";
                case 12: return "December";
            }

            throw new ArgumentOutOfRangeException(nameof(month));
        }

    }
}