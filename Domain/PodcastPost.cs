using System;
using System.Diagnostics;
using System.IO;
using ScfPodcastUploader.Core;

namespace ScfPodcastUploader.Domain
{
    public class PodcastPost
    {
        public string Title { get; set; }

        public string Speaker { get; set; }

        public string BibleText { get; set; }

        public string AudioFilePath { get; set; }

        public DateTime Date { get; set; }

        /// <summary>
        /// URL of the podcast media i.see. the MP3 file once it has been uploaded.
        /// </summary>
        /// <returns></returns>
        public string PodcastMediaUrl { get; set; }

        /// <summary>
        /// The URL of the actual post (not the media item).
        /// </summary>
        /// <returns></returns>
        public string PodcastPostUrl { get; set; }

        public int CategoryId { get; set; }

        public int AuthorId { get; set; }

        public string GetFormattedSize()
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = new FileInfo(AudioFilePath).Length;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            // Adjust the format string to your preferences. For example "{0:0.#}{1}" would
            // show a single decimal place, and no space.
            return string.Format("{0:0.#}{1}", len, sizes[2]);
        }

        /// <summary>
        /// Gets the size of the MP3 file in bytes.
        /// </summary>
        /// <returns></returns>
        public double GetMediaSizeInBytes()
        {
            return new FileInfo(AudioFilePath).Length;
        }

        public string GetFormattedDuration()
        {
            //relies on ffmpeg
            Process ffmpeg = new Process();
            ffmpeg.StartInfo.FileName = "ffmpeg";
            // ffmpeg.StartInfo.Arguments = $"-i \"{AudioFilePath}\" 2>&1 | grep \"Duration\"| cut -d ' ' -f 4 | sed s/,//";
            ffmpeg.StartInfo.Arguments = $"-i \"{AudioFilePath}\"";
            ffmpeg.StartInfo.UseShellExecute = false;
            ffmpeg.StartInfo.RedirectStandardOutput = true;
            ffmpeg.StartInfo.RedirectStandardError = true;
            ffmpeg.Start();

            // string output = ffmpeg.StandardOutput.ReadToEnd();
            string output = ffmpeg.StandardError.ReadToEnd();
            ffmpeg.WaitForExit();

            //need to look for a line like this:
            //Duration: 00:42:21.84, start: 0.025057, bitrate: 49 kb/s
            int start = output.IndexOf("Duration: ");
            if(start > -1)
            {
                int end = output.IndexOf(',', start);
                string duration = output.Substring(start + 13, end - start - 16);
                return duration;
            }

            return "Unknown";
        }

        /// <summary>
        /// Gets the date in RSS format (RFC822) e.g. Sun, 19 Mar 2017 11:00:00 +0000
        /// </summary>
        /// <returns></returns>
        public string GetDateInRssFormat()
        {
            return Date.ToRFC822String();
        }
    }
}