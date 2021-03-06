// Copyright (c) 2015 semdiffdotnet. Distributed under the MIT License.
// See LICENSE file or opensource.org/licenses/MIT.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SemDiff.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SemDiff.Test
{
    public static class Extensions
    {
        public static SyntaxTree Parse(this string content) => CSharpSyntaxTree.ParseText(content);

        public static SyntaxTree ParseFile(this string path, string setPath = null) => CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: setPath == null ? path : setPath);

        public static string WrapWithMethod(this string content, bool isAsync = false, string type = "Class1", string method = "Foo", string returntype = "void", string nameSpace = "ConsoleApplication", IEnumerable<string> usings = null)
        {
            return content.Method(method, returntype, isAsync).WrapWithClass(type: type, nameSpace: nameSpace, usings: usings);
        }

        public static string Method(this string content, string method, string returntype = "void", bool isAsync = false)
        {
            return $@"
            public {(isAsync ? "async " : "")}{returntype} {method}()
            {{
                {content}
            }}
";
        }

        public static string WrapWithClass(this string content, string type = "Class1", string nameSpace = "ConsoleApplication", IEnumerable<string> usings = null)
        {
            return content.Class(type).WrapWithNamespace(nameSpace: nameSpace, usings: usings);
        }

        public static string Class(this string content, string type)
        {
            return $@"
        class {type}
        {{
            {content}
        }}";
        }

        public static string WrapWithNamespace(this string content, string nameSpace = "ConsoleApplication", IEnumerable<string> usings = null)
        {
            return $@"
    using System;{string.Join("", (usings ?? Enumerable.Empty<string>()).Select(u => "    " + u + "\n"))}

    namespace {nameSpace}
    {{
        {content}
    }}";
        }

        public static string BlankLines(this int num) => string.Join("", Enumerable.Range(0, num).Select(o => Environment.NewLine));

        public static void AssertRateLimit(this Repo repo)
        {
            repo.UpdateLimitAsync().Wait();
            var limit = repo.RequestsLimit;
            var remaining = repo.RequestsRemaining;
            if ((float)remaining / limit < 0.1)
            {
                Assert.Inconclusive("There are less than 10% of requests remaining before the rate limit is hit");
            }
        }
    }
}