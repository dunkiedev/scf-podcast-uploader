using System;
using ScfPodcastUploader.Domain;
using ScfPodcastUploader.Domain.WordPress;

namespace ScfPodcastUploader.Services
{
    public interface IPodcastService
    {
        /// <summary>
        /// Generates an MP3 file by concatenating the intro.wav file and the supplied wav file,
        /// and encoding the result in MP3 format, saving the file in the configured folder.
        /// </summary>
        /// <param name="wavFilePath">Path to the wav file containing just the sermon</param>
        /// <param name="podcastPost">The PodcastPost containing the details for the podcast</param>
        /// <returns></returns>
        string GenerateMp3File(string wavFilePath, PodcastPost podcastPost);

        WordPressResult UploadAudioFile(string filepath);

        WordPressResult CreatePodcastPost(PodcastPost podcastPost);
    }
}