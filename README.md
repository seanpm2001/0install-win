Zero Install for Windows
========================

Zero Install is a decentralized cross-platform software-installation system available under the LGPL.  
Zero Install for Windows is built upon **[Zero Install .NET](https://github.com/0install/0install-dotnet)**.

[![Build status](https://img.shields.io/appveyor/ci/0install/0install-win.svg)](https://ci.appveyor.com/project/0install/0install-win)

**[Download Zero Install for Windows](http://0install.de/downloads/)**

Directory structure
-------------------
- `src` contains source code.
- `lib` contains pre-compiled 3rd party libraries which are not available via NuGet.
- `artifacts` contains the results of various compilation processes. It is created on first usage.
- `release` contains scripts for creating a Zero Install feed and archive for publishing a build.

Building
--------
You need to install [Visual Studio 2017](https://www.visualstudio.com/downloads/) to build this project.  
Run `.\build.ps1` in PowerShell to build everything. This script takes a version number as an input argument. The source code itself only contains dummy version numbers. The actual version is picked by continuous integration using [GitVersion](http://gitversion.readthedocs.io/).

If you wish to deploy the release after compilation as the default Zero Install instance in your user profile run `.\build.ps1 -Deploy`.  
To deploy it for all users use `.\build.ps1 -Deploy -Machine`.

Environment variables
---------------------
- `ZEROINSTALL_PORTABLE_BASE`: Set by the C# code to to inform the OCaml code of Portable mode.
- `ZEROINSTALL_EXTERNAL_FETCHER`: Set by the C# code to make the OCaml code delegate downloading files back to the C# implementation.
- `ZEROINSTALL_EXTERNAL_STORE`: Set by the C# code to make the OCaml code delegate extracting archives back to the C# implementation.
