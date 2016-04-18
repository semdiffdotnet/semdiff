using System;

namespace SemDiff.Core
{
    //This code was copied from the LibGit2Sharp library
    //https://raw.githubusercontent.com/libgit2/libgit2sharp/63771c5c231cda501fb8f9330f573dfa98ee2e28/LibGit2Sharp/Core/Platform.cs

    /* The MIT License
     * Copyright (c) LibGit2Sharp contributors
     * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
     * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
     * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
     */

    internal enum OperatingSystemType
    {
        Windows,
        Unix,
        MacOSX
    }

    internal static class Platform
    {
        public static string ProcessorArchitecture
        {
            get { return Environment.Is64BitProcess ? "amd64" : "x86"; }
        }

        public static OperatingSystemType OperatingSystem
        {
            get
            {
                // See http://www.mono-project.com/docs/faq/technical/#how-to-detect-the-execution-platform
                switch ((int)Environment.OSVersion.Platform)
                {
                    case 4:
                    case 128:
                        return OperatingSystemType.Unix;

                    case 6:
                        return OperatingSystemType.MacOSX;

                    default:
                        return OperatingSystemType.Windows;
                }
            }
        }
    }
}