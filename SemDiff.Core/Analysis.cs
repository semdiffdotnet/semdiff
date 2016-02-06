using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;

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
            throw new NotImplementedException();
        }

        /// <summary>
        /// Given a Repo and a Semantic Model find any possible FalseNegatives
        /// </summary>
        public static IEnumerable<DetectedFalseNegative> ForFalseNegative(Repo repo, SemanticModel semanticModel)
        {
            throw new NotImplementedException();
        }
    }
}