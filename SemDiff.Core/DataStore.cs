using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace SemDiff.Core
{
    /// <summary>
    /// Simplifies the retrieval of syntax trees based on the repo they are in and allows updating
    /// the tree without relocating the repo based on the path
    /// </summary>
    internal class DataStore
    {
        //At the highest level we have a dictionary that maps the assembly
        //name to info about the repository and it's files
#pragma warning disable CC0052 // Make field readonly

        private ImmutableDictionary<string, RepoFileSyntaxTreeInfo> store =
                ImmutableDictionary<string, RepoFileSyntaxTreeInfo>.Empty;

#pragma warning restore CC0052 // Make field readonly

        public ImmutableDictionary<string, RepoFileSyntaxTreeInfo> Store => store;

        public RepoFileSyntaxTreeInfo InterlockedAddOrUpdate(string assembly,
            IEnumerable<SyntaxTree> trees, Func<SyntaxTree, Repo> getRepoFunc)
        {
            return ImmutableInterlocked.AddOrUpdate(ref store,
                        assembly, s => InUpdate(trees, getRepoFunc),
                        (s, r) => InUpdate(trees, getRepoFunc, r));
        }

        private static RepoFileSyntaxTreeInfo InUpdate(IEnumerable<SyntaxTree> trees,
            Func<SyntaxTree, Repo> getRepoFunc, RepoFileSyntaxTreeInfo? previous = null)
        {
            return trees.Aggregate(previous ?? RepoFileSyntaxTreeInfo.Empty,
                        (rfsti, tree) => rfsti.AddOrUpdateSyntaxTree(tree, getRepoFunc));
        }

        //This is basically a customized tuple that contains two dictionaries that
        //ultimately maps the Repo to all the syntax trees
        internal struct RepoFileSyntaxTreeInfo
        {
            //This maps from the (absolute) path of a file to the SyntaxTree that represents it
            public ImmutableDictionary<string, SyntaxTree> FileSyntaxTreeLookup;

            //This maps from our repositories to a list of all the files (under version control),
            //the string represents the absolute path of the file locally
            public ImmutableDictionary<Repo, ImmutableList<string>> RepoFileLookup;

            public static RepoFileSyntaxTreeInfo Empty { get; } = new RepoFileSyntaxTreeInfo
            {
                RepoFileLookup = ImmutableDictionary<Repo, ImmutableList<string>>.Empty,
                FileSyntaxTreeLookup = ImmutableDictionary<string, SyntaxTree>.Empty,
            };

            public IEnumerable<Repo> Repos => RepoFileLookup.Keys;

            public RepoFileSyntaxTreeInfo AddOrUpdateSyntaxTree(SyntaxTree tree, Func<SyntaxTree, Repo> getRepoFunc)
            {
                //Note that if the file isn't in a repo it will be in
                //the FileSyntaxTreeLookup, but it will not be in the repo
                return new RepoFileSyntaxTreeInfo
                {
                    FileSyntaxTreeLookup = FileSyntaxTreeLookup.SetItem(tree.FilePath, tree),
                    //Use the presence in the File Syntax Lookup as an indicator of if we have already found the repo
                    RepoFileLookup = FileSyntaxTreeLookup.ContainsKey(tree.FilePath)
                                        ? RepoFileLookup
                                        : AddFileToRepo(RepoFileLookup, getRepoFunc?.Invoke(tree), tree.FilePath)
                };
            }

            public IEnumerable<SyntaxTree> GetTreesForRepo(Repo repo)
            {
                var fileTreeLookup = FileSyntaxTreeLookup; //Makes the compiler happy about the following closure
                return RepoFileLookup[repo].Select(s => fileTreeLookup[s]);
            }

            private static ImmutableDictionary<Repo, ImmutableList<string>> AddFileToRepo(
                ImmutableDictionary<Repo, ImmutableList<string>> initial, Repo repo, string filePath)
            {
                return repo != null
                    ? initial.ContainsKey(repo)
                      ? initial.SetItem(repo, initial[repo].Add(filePath))
                      : initial.Add(repo, ImmutableList<string>.Empty.Add(filePath))
                    : initial;
            }
        }
    }
}