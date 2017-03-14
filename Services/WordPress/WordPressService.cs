using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ScfPodcastUploader.Core;
using ScfPodcastUploader.Domain;
using ScfPodcastUploader.Domain.Config;
using ScfPodcastUploader.Domain.WordPress;
using ScfPodcastUploader.Services.Config;

namespace ScfPodcastUploader.Services.WordPress
{
    public class WordPressService : IWordPressService
    {
        private string WordPressPostsUrl => _configurationService.Configuration.WordPressBaseAddress + "/wp-json/wp/v2/posts";
        private string WordPressMediaUrl => _configurationService.Configuration.WordPressBaseAddress + "/wp-json/wp/v2/media";
        private readonly IConfigurationService _configurationService;

        public WordPressService(IConfigurationService configurationService)
        {
            _configurationService = configurationService;
        }

        public WordPressResult AddPost(WordPressPost post)
        {
            HttpClient client = CreateHttpClient();

            var content = new StringContent(JsonConvert.SerializeObject(post).ToString(), Encoding.UTF8, "application/json");

            var response = client.PostAsync(WordPressPostsUrl, content).Result;

            if (response.IsSuccessStatusCode)
            {
                string resultString = response.Content.ReadAsStringAsync().Result;
                JObject jsonResult = JObject.Parse(resultString);

                return new WordPressResult()
                {
                    IsSuccess = true,
                    Id = jsonResult.Value<int>("id"),
                };
            }
            else
            {
                //TODO get error details and add them to the result
                return new WordPressResult()
                {
                    IsSuccess = false,
                };
            }
        }

        public WordPressResult AddMedia(string filepath)
        {
            //TODO add error handling
            return UploadFile(filepath, WordPressMediaUrl, "audio/mp3");
        }

        /// <summary>
        /// This is a broken version of this method - it works for small MP3 files, but not for large ones.
        /// Gave up trying to get it to work and just used the older HttpWebRequest style.
        /// </summary>
        /// <param name="filepath"></param>
        /// <returns></returns>
        private bool AddMediaBroken(string filepath)
        {
            HttpClient client = CreateHttpClient();

            FileStream fileStream = File.Open(filepath, FileMode.Open);
            StreamContent streamContent = new StreamContent(fileStream);
            streamContent.Headers.Add("Content-Disposition", "form-data; name=\"file\"; filename=\"" + Path.GetFileName(filepath) + "\"");
            streamContent.Headers.Add("Content-Type", "audio/mp3");

            var content = new MultipartFormDataContent();
            content.Add(streamContent);

            var response = client.PostAsync("http://windows7vm/wordpress/wp-json/wp/v2/media", content).Result;

            string result = response.Content.ReadAsStringAsync().Result;
            Console.Out.WriteLine("Result: " + result);
            return response.IsSuccessStatusCode;
        }



        public async Task GetPost(int id)
        {
            HttpClient client = CreateHttpClient();

            Task<string> stringTask = client.GetStringAsync("http://windows7vm/wordpress/wp-json/wp/v2/posts/2605");

            var msg = await stringTask;
            Console.Write(msg);
        }

        private HttpClient CreateHttpClient()
        {
            Configuration configuration = _configurationService.Configuration;

            var httpClientHandler = new HttpClientHandler
            {
                Proxy = new MyProxy(configuration.GetProxyUriString()),
                UseProxy = configuration.UseProxy
            };

            HttpClient client = new HttpClient(httpClientHandler);

            client.DefaultRequestHeaders.ExpectContinue = false;

            if (configuration.AuthMethod == Configuration.BasicAuthMethod)
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", _configurationService.Configuration.GetBasicAuthString());
                // client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", "YWRtaW4yOjY2Ym9va3NpbnRoZWJpYmxl");
            }
            //TODO add oauth headers as appropriate

            return client;
        }

        private WordPressResult UploadFile(string path, string url, string contentType)
        {
            Configuration configuration = _configurationService.Configuration;

            // Build request
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "Post";
            //request.AllowWriteStreamBuffering = false;
            request.ContentType = contentType;
            
            if(configuration.UseProxy)
            {
                request.Proxy = new MyProxy(configuration.GetProxyUriString());
            }

            if(configuration.AuthMethod == Configuration.BasicAuthMethod)
            {
                request.Headers["Authorization"] = $"Basic {configuration.GetBasicAuthString()}";
            }
            //TODO add oauth headers as appropriate

            string fileName = Path.GetFileName(path);
            request.Headers["Content-Disposition"] = string.Format("file; filename=\"{0}\"", fileName);

            try
            {
                // Open source file
                using (var fileStream = File.OpenRead(path))
                {
                    // Set content length based on source file length                    
                    //request.ContentLength = fileStream.Length;

                    // Get the request stream with the default timeout
                    using (var requestStream = request.GetRequestStreamWithTimeout())
                    {
                        // Upload the file with no timeout
                        fileStream.CopyTo(requestStream);
                    }
                }

                // Get response with the default timeout, and parse the response body
                using (var response = request.GetResponseWithTimeout())
                using (var responseStream = response.GetResponseStream())
                using (var reader = new StreamReader(responseStream))
                {
                    string json = reader.ReadToEnd();
                    var j = JObject.Parse(json);

                    return new WordPressResult
                    {
                        IsSuccess = true,
                        Id = j.Value<int>("id"),
                        Url = j.Value<string>("source_url")
                    };
                }
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.Timeout)
                {
                    Console.Out.WriteLine("Timeout while uploading '{0}'", fileName);
                    Console.Out.WriteLine(ex);
                }
                else
                {
                    Console.Out.WriteLine("Error while uploading '{0}'", fileName);
                    Console.Out.WriteLine(ex);
                }
                throw;
            }
        }
    }
}