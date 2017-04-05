using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OAuth;
using ScfPodcastUploader.Core;
using ScfPodcastUploader.Domain;
using ScfPodcastUploader.Domain.Config;
using ScfPodcastUploader.Domain.WordPress;
using ScfPodcastUploader.Services.Config;

namespace ScfPodcastUploader.Services.WordPress
{
    public class WordPressService : IWordPressService
    {
        private static class HttpMethods
        {
            public const string Delete = "DELETE";
            public const string Get = "GET";
            public const string Post = "POST";
            public const string Put = "PUT";
        }

        private string WordPressPostsUrl => _configurationService.Configuration.WordPressBaseAddress + "/wp-json/wp/v2/posts";
        private string WordPressMediaUrl => _configurationService.Configuration.WordPressBaseAddress + "/wp-json/wp/v2/media";
        private readonly IConfigurationService _configurationService;

        public WordPressService(IConfigurationService configurationService)
        {
            _configurationService = configurationService;
        }

        public WordPressResult AddPost(WordPressPost post)
        {
            HttpClient client = CreateHttpClient(WordPressPostsUrl, HttpMethods.Post);

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
                    Url = jsonResult.Value<string>("link")
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

        public WordPressResult AddMedia(string filepath, string mimeType)
        {
            //TODO add error handling
            return UploadFile(filepath, WordPressMediaUrl, mimeType);
        }

        public WordPressResult DeleteMedia(int id)
        {
            string url = WordPressMediaUrl + $"/{id}?force=true";

            NameValueCollection parameters = new NameValueCollection();
            parameters.Add("force", "true");

            HttpClient client = CreateHttpClient(url, HttpMethods.Delete, parameters);
            var response = client.DeleteAsync(url).Result;

            if(response.IsSuccessStatusCode)
            {
                return new WordPressResult()
                {
                    IsSuccess = true,
                    Id = id
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

        public WordPressMedia FindMediaByTitle(string title)
        {
            string url = WordPressMediaUrl + $"?filter[title]={title}";
            
            NameValueCollection parameters = new NameValueCollection();
            parameters.Add("filter%5Btitle%5D", title);

            HttpClient client = CreateHttpClient(WordPressMediaUrl, HttpMethods.Get, parameters);
            var response = client.GetAsync(url).Result;

            if(response.IsSuccessStatusCode)
            {
                string resultString = response.Content.ReadAsStringAsync().Result;
                JArray jsonResult = JArray.Parse(resultString);

                if(jsonResult.Count == 0)
                {
                    throw new InvalidOperationException("Could not find a WordPress media item with the title " + title);
                }
                else if(jsonResult.Count > 1)
                {
                    throw new InvalidOperationException("Found more than one media item claiming to be the RSS feed!");
                }

                return new WordPressMedia()
                {                    
                    id = (int)jsonResult[0]["id"],
                    title = (string)jsonResult[0]["title"]["rendered"],
                    source_url = (string)jsonResult[0]["source_url"]
                };
            }
            else
            {
                throw new InvalidOperationException($"Error retrieving RSS feed: status={response.StatusCode}, message={response.ToString()}");
            }
        }

        /// <summary>
        /// This is a broken version of this method - it works for small MP3 files, but not for large ones.
        /// Gave up trying to get it to work and just used the older HttpWebRequest style.
        /// </summary>
        /// <param name="filepath"></param>
        /// <returns></returns>
        private bool AddMediaBroken(string filepath)
        {
            string url = "http://windows7vm/wordpress/wp-json/wp/v2/media";

            FileStream fileStream = File.Open(filepath, FileMode.Open);
            StreamContent streamContent = new StreamContent(fileStream);
            streamContent.Headers.Add("Content-Disposition", "form-data; name=\"file\"; filename=\"" + Path.GetFileName(filepath) + "\"");
            streamContent.Headers.Add("Content-Type", "audio/mp3");

            var content = new MultipartFormDataContent();
            content.Add(streamContent);

            HttpClient client = CreateHttpClient(url, HttpMethods.Post);
            var response = client.PostAsync(url, content).Result;

            string result = response.Content.ReadAsStringAsync().Result;
            Console.Out.WriteLine("Result: " + result);
            return response.IsSuccessStatusCode;
        }

        public async Task GetPost(int id)
        {
            string url = "http://windows7vm/wordpress/wp-json/wp/v2/posts/2605";
            HttpClient client = CreateHttpClient(url, HttpMethods.Get);

            Task<string> stringTask = client.GetStringAsync(url);

            var msg = await stringTask;
            Console.Write(msg);
        }

        private HttpClient CreateHttpClient(string url, string httpMethod, NameValueCollection parameters = null)
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
            }
            else if (configuration.AuthMethod == Configuration.OAuthMethod)
            {
                string oauthHeader = GetOAuthHeader(url, httpMethod, parameters);
                client.DefaultRequestHeaders.Add("Authorization", oauthHeader);
            }

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
            else if(configuration.AuthMethod == Configuration.OAuthMethod)
            {
                string oauthHeader = GetOAuthHeader(url, HttpMethods.Post);
                request.Headers["Authorization"] = oauthHeader;
            }

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

        private string GetOAuthHeader(string url, string httpMethod, NameValueCollection parameters = null)
        {
            Configuration configuration = _configurationService.Configuration;
            
            string consumerKey = configuration.OAuthConsumerKey;
            string consumerSecret = configuration.OAuthConsumerSecret;
            string token = configuration.OAuthToken;
            string tokenSecret = configuration.OAuthTokenSecret;

            OAuthRequest oauthRequest = OAuthRequest.ForProtectedResource(httpMethod, consumerKey, consumerSecret, token, tokenSecret);
            oauthRequest.RequestUrl = url;

            return oauthRequest.GetAuthorizationHeader(parameters != null ? parameters : new NameValueCollection());
        }
    }
}