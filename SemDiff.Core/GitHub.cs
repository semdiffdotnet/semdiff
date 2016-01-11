﻿using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

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
        /// <summary>
        /// Make a request to GitHub with nessasary checks, then parse the result into the specified type
        /// </summary>
        /// <param name="url">relative url to the requested resource</param>
        /// <typeparam name="T">Type the request is expected to contain</typeparam>
        private async Task<T> HttpGetAsync<T>(string url)
        {
            var content = await HttpGetAsync(url);
            return JsonConvert.DeserializeObject<T>(content);
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

        public PullRequst GetPullRequests()
        {
            //TODO: Investigate using the If-Modified-Since and If-None-Match headers https://developer.github.com/v3/#conditional-requests
            //$"/repos/{RepoUser}/{RepoName}/pulls"
            //$"/repos/{RepoUser}/{RepoName}/pulls/{id}/files"
            throw new NotImplementedException();
        }

        //TODO: Add More Methods As Needed (one for each type of requests)

        public class PullRequst
        {
        }
    }
}