using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SemDiff.Core
{
    /// <summary>
    /// Contains all the methods and classes nessasary to use the GitHub api to pull down data about pull requests. Additionally, contains the logic for authenticating
    /// </summary>
    public class GitHub
    {
        public GitHub(string repoUser, string repoName)
        {
            RepoUser = repoUser;
            RepoName = repoName;
            Client = new HttpClient //TODO: Enable gzip!
            {
                BaseAddress = new Uri("https://api.github.com/")
            };
            Client.DefaultRequestHeaders.UserAgent.ParseAdd(nameof(SemDiff));
        }

        public GitHub(string repoUser, string repoName, string authUsername, string authToken) : this(repoUser, repoName)
        {
            AuthUsername = authUsername;
            AuthToken = authToken;
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{AuthUsername}:{AuthToken}")));
        }

        public string AuthToken { get; set; }
        public string AuthUsername { get; set; }
        public string RepoName { get; set; }
        public string RepoUser { get; set; }
        public HttpClient Client { get; private set; }

        /// <summary>
        /// Checks how close the user is to the rate limit. Throws different warnings depending of if the calls are authenticated or not.
        /// </summary>
        /// <param name="current">Current numbers of API requests</param>
        /// <param name="cap">Maximum number of API requests</param>
        private void RateLimit(int current, int cap)
        {

        }
        private async void APIError(string content)
        {
            
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
                var ret = JsonConvert.DeserializeObject<T>(content);
                return JsonConvert.DeserializeObject<T>(content);
            }
            catch(Exception e)
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
            //$"/repos/{RepoUser}/{RepoName}/pulls"
            //$"/repos/{RepoUser}/{RepoName}/pulls/{id}/files"
            var url = "https://api.github.com/repos/" + RepoUser + "/" + RepoName + "/pulls";
            System.IO.File.WriteAllText(@"C:\Users\Public\WriteText.txt", url);
            var requests = HttpGetAsync<IList<PullRequest>>(url).Result;
            foreach (var pr in requests)
            {
                url = url + "/"+ pr.number + "/files";
                System.IO.File.WriteAllText(@"C:\Users\Public\WriteText2.txt", url);
                var files = HttpGetAsync<IList<Files>>(url).Result;
                pr.files = files;
            }
            return requests;
        }

        //TODO: Add More Methods As Needed (one for each type of requests)

        public class PullRequest
        {
            public int number { get; set; }
            public string state { get; set; }
            public bool locked { get; set; }
            public string updated_at { get; set; }
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
        private class JsonError
        {
            public string message { get; set; }
        }
    }
}