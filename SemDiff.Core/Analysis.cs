using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SemDiff.Core
{
    /// <summary>
    /// Class that handles all higher level analysis like finding FalsePositives and FalseNegatives
    /// </summary>
    public class Analysis
    {
        /// <summary>
        /// Given a Repo and a Tree find any possible FalsePositives
        /// </summary>
        public static IEnumerable<DetectedFalsePositive> ForFalsePositive(Repo repo, SyntaxTree tree, string filePath)
        {
            var pulls = GetPulls(repo, filePath);
            throw new NotImplementedException();
        }

        /// <summary>
        /// Given a Repo and a Semantic Model find any possible FalseNegatives
        /// </summary>
        public static IEnumerable<DetectedFalseNegative> ForFalseNegative(Repo repo, SemanticModel semanticModel)
        {
            var baseClassPath = ""; //TODO: find using semantic model
            var pulls = GetPulls(repo, baseClassPath);
            throw new NotImplementedException();
        }

        internal static string GetRelativePath(string localDirectory, string filePath)
        {
            var local = Path.GetFullPath(localDirectory);
            var file = Path.GetFullPath(filePath);
            if (file.StartsWith(local))
            {
                return file.Substring(local.Length);
            }
            else
            {
                return null;
            }
        }

        internal static IEnumerable<Tuple<RemoteFile, RemoteChanges>> GetPulls(Repo repo, string lookForFile)
        {
            var relativePath = GetRelativePath(repo.LocalDirectory, lookForFile);
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return Enumerable.Empty<Tuple<RemoteFile, RemoteChanges>>();
            }
            return repo.GetRemoteChanges().SelectMany(p => p.Files.Select(f => new { n = f.Filename, f, p })).Where(a => a.n == relativePath).Select(a => Tuple.Create(a.f, a.p)).ToList();
        }
    }
}