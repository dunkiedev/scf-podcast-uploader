using System;
using System.Text;

namespace ScfPodcastUploader.Domain.Config
{
    public class Configuration
    {
        public const string BasicAuthMethod = "BASIC";
        public const string OAuthMethod = "OAUTH";


        public string WordPressBaseAddress { get; set; }
        public string ProxyHost { get; set; }
        public int? ProxyPort { get; set; }
        public bool UseProxy { get; set; }

        /// <summary>
        /// Either BASIC or OAUTH
        /// </summary>
        /// <returns></returns>
        public string AuthMethod { get; set; }

        public string BasicAuthUsername { get; set; }

        public string BasicAuthPassword { get; set; }

        public string GetBasicAuthString()
        {
            return Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(BasicAuthUsername + ":" + BasicAuthPassword));
        }

        public string GetProxyUriString()
        {
            return $"http://{ProxyHost}:{(ProxyPort == null ? 80 : ProxyPort)}";
        }

        public string PostTemplatePath { get; set; }

        public int WordPressPodcastCategoryId { get; set; }

        public int WordPressAuthorId { get; set; }

        /// <summary>
        /// Path to the folder where we keep the generated audio files
        /// </summary>
        /// <returns></returns>
        public string PodcastAudioFolder { get; set; }

        /// <summary>
        /// Path to the intro wav file.
        /// </summary>
        /// <returns></returns>
        public string IntroWavFilePath { get; set; }

        /// <summary>
        /// The VBR bitrate encoding quality. Should be a number between 0-9. The smallest
        /// file is created with an encoding of 9, which is what we usually use.
        /// </summary>
        /// <returns></returns>
        public int VbrBitrate { get; set; }
    }
}