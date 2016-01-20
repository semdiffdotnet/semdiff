using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Linq;

namespace SemDiff.Test
{
    public static class Extensions
    {
        public static SyntaxTree Parse(this string content) => CSharpSyntaxTree.ParseText(content);

        public static string WrapWithMethod(this string content, bool isAsync = false, string type = "Class1", string method = "Foo", string returntype = "void", string nameSpace = "ConsoleApplication", IEnumerable<string> usings = null)
        {
            return content.Method(isAsync, method, returntype).WrapWithClass(type: type, nameSpace: nameSpace, usings: usings);
        }

        private static string Method(this string content, bool isAsync, string method, string returntype)
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

        private static string Class(this string content, string type)
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
    }
}