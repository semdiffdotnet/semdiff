# SemDiff (or semantic diff)

A tool which diffs C# code semantically, rather than simply by the program's text in order to predict possible merge conflicts.

This project aims to make developers aware of relevant changes made in other pull requests, such as:

 * The base class of an edited method being changed.
 * A method being moved and changed both locally and in a pull request

The goal of this project is to ultimately save significant time for large projects.

## Requirements

 * Only C# projects are supported
 * Requires a Visual Studio Version that supports Roslyn Diagnostic Analyzers
   * SemDiff was developed and tested on Visual Studio 2015
 * Git/GitHub must be used for source control and project hosting
   * The GitHub repo that contains pull requests must be the first in the config file.
     * For example, if you fork the repository before you begin working. Add an "upstream" remote before the "origin". Otherwise SemDiff will search the forked repository for pull requests.

## Installation

SemDiff is available on the [NuGet Gallery](https://www.nuget.org/packages/SemDiff)

```
Install-Package SemDiff
```

Consult the [Configuration Guide](https://github.com/semdiffdotnet/semdiff/wiki/Configuration) to learn how to add GitHub credentials to the config file.


## Contribution Policy

1. Create an Issue describing the work you want to do.
2. Fork the repo, create a branch, and work on that branch.
3. Make a pull request describing what you did and reference your issue from (1)
4. Discuss the code and have it merged in!
