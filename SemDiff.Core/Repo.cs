using SemDiff.Core.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Text.RegularExpressions;

namespace SemDiff.Core
{
    /// <summary>
    /// Local representation of a github repo
    /// </summary>
    public class Repo
    {
        private static readonly ConcurrentDictionary<string, Repo> _repoLookup = new ConcurrentDictionary<string, Repo>();
        public static bool Authentication { get; set; } = true;

        internal static GitHubConfiguration gitHubConfig =
            new GitHubConfiguration((AuthenticationSection)ConfigurationManager.GetSection("SemDiff.Core/authentication"));

        /// <summary>
        /// Looks for the git repo above the current file in the directory higherarchy. Null will be returned if no repo was found.
        /// </summary>
        /// <param name="filePath">Path to file in repo</param>
        /// <returns>Representation of repo or null (to indicate not found)</returns>
        public static Repo GetRepoFor(string filePath)
        {
            return _repoLookup.GetOrAdd(Path.GetDirectoryName(filePath), AddRepo);
        }

        internal static Repo AddRepo(string directoryPath)
        {
            Logger.Debug($"Dir: {directoryPath}");
            var gitconfig = Path.Combine(directoryPath, ".git", "config");
            if (File.Exists(gitconfig))
            {
                Logger.Info($"Git Config File Found: {gitconfig}");
                return RepoFromConfig(gitconfig);
            }
            else
            {
                //Go up a directory and check it out
                var parentDirectory = Path.GetDirectoryName(directoryPath);
                if (parentDirectory == null)
                {
                    //This file is not in a git repo! (GetDirectoryName returns null when given the root directory)
                    return null; //This is much more common than you might think, because offten random files are compiled, this will allow us to exclude them
                }
                return _repoLookup.GetOrAdd(parentDirectory, AddRepo);
            }
        }

        private static readonly Regex _gitHubUrl = new Regex(@"(git@|https:\/\/)github\.com(:|\/)(.*)\/(.*)\.git");

        public string Owner { get; private set; }
        public string Name { get; private set; }
        public GitHub GitHubApi { get; private set; }

        internal static Repo RepoFromConfig(string gitconfigPath)
        {
            var config = File.ReadAllText(gitconfigPath);
            var match = _gitHubUrl.Match(config);
            if (!match.Success)
                throw new NotImplementedException("Git repo doesn't seem to be a GitHub repository");

            var url = match.Value;
            var owner = match.Groups[3].Value;
            var name = match.Groups[4].Value;
            Logger.Debug($"Repo: Owner='{owner}' Name='{name}' Url='{url}'");
            return new Repo(owner, name);
        }

        /// <summary>
        /// Flushes the internal mapings of directories to repos
        /// </summary>
        internal static void ClearLookup()
        {
            _repoLookup.Clear();
        }

        internal Repo(string owner, string name)
        {
            if (Authentication)
            {
                string authToken = gitHubConfig.AuthenicationToken;
                string authUsername = gitHubConfig.Username;
                GitHubApi = new GitHub(owner, name, authUsername, authToken);
            }
            else
            {
                GitHubApi = new GitHub(owner, name);
            }
            Owner = owner;
            Name = name;
        }

        /// <summary>
        /// Gets Pull Requests and the master branch if it has been modified
        /// </summary>
        /// <returns></returns>
        public IEnumerable<RemoteChanges> GetRemoteChanges()
        {
            TriggerUpdate();
            throw new NotImplementedException();
        }

        public void TriggerUpdate()
        {
            throw new NotImplementedException();
        }

        public void Update()
        {
        }
    }
}