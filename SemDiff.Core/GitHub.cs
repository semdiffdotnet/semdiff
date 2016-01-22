using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Collections;

namespace SemDiff.Core
{
    /// <summary>
    /// Contains all the methods and classes nessasary to use the GitHub api to pull down data about pull requests. Additionally, contains the logic for authenticating
    /// </summary>
    public class GitHub
    {
        //Figure out how to ignore the IP, possibly parse by any number and ignore blank
        static string APIRateLimitNonOAuthError = "API rate limit exceeded for xxx.xxx.xxx.xxx. (But here's the good news: Authenticated requests get a higher rate limit. Check out the documentation for more details.)";
        static string APIDoesNotExistError = "Not Found";
        public GitHub(string repoOwner, string repoName)
        {
            RepoOwner = repoOwner;
            RepoName = repoName;
            RequestsRemaining = 1;
            Client = new HttpClient //TODO: Enable gzip!
            {
                BaseAddress = new Uri("https://api.github.com/")
            };
            Client.DefaultRequestHeaders.UserAgent.ParseAdd(nameof(SemDiff));
        }

        public GitHub(string repoOwner, string repoName, string authUsername, string authToken) : this(repoOwner, repoName)
        {
            AuthUsername = authUsername;
            AuthToken = authToken;
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{AuthUsername}:{AuthToken}")));
        }

        public string AuthToken { get; set; }
        public string AuthUsername { get; set; }
        public string RepoName { get; set; }
        public string RepoOwner { get; set; }
        public int RequestsRemaining { get; private set; }
        public HttpClient Client { get; private set; }

        private async void APIError(string content)
        {
            //TODO: implement Error handling



            //temp
            RequestsRemaining = 0;
        }
        /// <summary>
        /// Make a request to GitHub with nessasary checks, then parse the result into the specified type
        /// </summary>
        /// <param name="url">relative url to the requested resource</param>
        /// <typeparam name="T">Type the request is expected to contain</typeparam>
        private async Task<T> HttpGetAsync<T>(string url)
        {
            var content = await HttpGetAsync(url);
            try
            {
                return JsonConvert.DeserializeObject<T>(content);
            }
            catch (Exception e)
            {
                APIError(content);
                return default(T);
            }

        }

        private async Task<string> HttpGetAsync(string url)
        {
            //TODO: Handle Errors Here vv
            var response = await Client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                //TODO: Implement Check
            }
            return await response.Content.ReadAsStringAsync();
        }

        public IList<PullRequest> GetPullRequests()
        {
            //TODO: Investigate using the If-Modified-Since and If-None-Match headers https://developer.github.com/v3/#conditional-requests
            //$"/repos/{RepoOwner}/{RepoName}/pulls"
            //$"/repos/{RepoOwner}/{RepoName}/pulls/{id}/files"
            var url = Client.BaseAddress + "repos/" + RepoOwner + "/" + RepoName + "/pulls";
            var requests = HttpGetAsync<IList<PullRequest>>(url).Result;
            if (requests != default(IList<PullRequest>))
                foreach (var pr in requests)
                {
                    var url2 = url + "/" + pr.number + "/files";
                    var files = HttpGetAsync<IList<Files>>(url2).Result;
                    pr.files = files;
                }
            return requests;
        }

        /// <summary>
        /// Download files for a given pull request.
        /// Store the files in the AppData folder with a subfolder for the pull request
        /// </summary>
        /// <param name="pr">the PullRequest for which the files need to be downloaded</param>
        public void DownloadFiles(PullRequest pr)
        {
            var x = 0;
            var filesEnumerator = pr.files.GetEnumerator();
            while (filesEnumerator.MoveNext())
            {
                x++;
                using (WebClient client = new WebClient())
                {
                    var current = filesEnumerator.Current;
                    var csFileTokens = current.filename.Split('.');
                    if (csFileTokens[csFileTokens.Length - 1] == "cs")
                    {
                        var rawText = client.DownloadString(current.raw_url);
                        var directoryTokens = current.filename.Split('/');
                        var dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/SemDiff/";
                        foreach (var token in directoryTokens)
                        {
                            if (token != directoryTokens[0])
                            {
                                dir = dir + "/" + token;
                            }
                            else
                            {
                                dir = dir + token + "/" + pr.number;
                            }
                        }
                        (new FileInfo(dir)).Directory.Create();
                        System.IO.File.WriteAllText(@dir, rawText);
                    }
                }
            }
        }
        public class PullRequest
        {
            public int number { get; set; }
            public string state { get; set; }
            public bool locked { get; set; }
            public DateTime updated_at { get; set; }
            public User user { get; set; }
            public IList<Files> files { get; set; }

        }
        public class Files
        {
            public string filename { get; set; }
            public string raw_url { get; set; }
        }
        public class User
        {
            public string login { get; set; }
        }
        private class GitHubError
        {
            public string message { get; set; }
        }
    }
}