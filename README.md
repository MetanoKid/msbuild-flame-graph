# MSBuild Flame Graph

Explore MSBuild execution to find why your build is slow. Initially targeted for C++ projects, it can be used for other MSBuild-based projects.

This is the graph for the default Visual Studio 2015 C++ project:

![Flame Graph: default Visual Studio 2015](https://github.com/MetanoKid/msbuild-flame-graph/tree/master/readme-samples/blank-project.png "Flame graph: default Visual Studio 2015 project")

This graph represents a build from [this repository](https://github.com/randomascii/main/tree/master/xperf/vc_parallel_compiles).

![Flame Graph: Bruce Dawson's parallel projects](https://github.com/MetanoKid/msbuild-flame-graph/tree/master/readme-samples/random-ascii-parallel.png "Flame graph: Bruce Dawson's parallel projects")

## Features

  * MSBuild 15: builds VS2015 and VS2017 C++ projects.
  * Processes `/Bt+` and `/time+`:
    ![Flame Graph: /Bt+ and /time+](https://github.com/MetanoKid/msbuild-flame-graph/tree/master/readme-samples/bt-plus-time-plus.png "Flame graph: /Bt+ and /time+")
  * Processes `/d1reportTime` (exclusive to VS2017 projects):
    ![Flame Graph: /d1reportTime](https://github.com/MetanoKid/msbuild-flame-graph/tree/master/readme-samples/d1reportTime.png "Flame graph: /d1reportTime")
  * Builds projects into an `Events.json` file, converts it into `Trace.json` in a separate step.

## Getting started

  * Install Visual Studio 2017+ Community Edition.
  * Install .NET Framework v4.6.2 SDK.
  * Clone repository.
  * Open solution, build and run.

## Codebase overview

Should you want to explore what's in the repository, these are the main parts:

### Projects

  * `Builder`: contains the UI (built in WPF) to interact with the tool.
  * `MSBuildWrapper`: defines MSBuild loggers, interacts with MSBuild API and converts MSBuild events to custom abstractions.
  * `BuildTimeline`: represents timelines and everything it needs, from events to entries.
  * `TimelineSerializer`: includes a way to convert a `Timeline` to a Google Chrome's trace.
  * `Model`: represents data to be used by other projects.

### Main flow

  * As part of `MSBuildWrapper/Compilation/Compilation.cs`, when a build starts every MSBuild event is displayed in the UI and gets stored in memory.
  * When the build finishes, most events get converted into a custom format (some events and some properties are discarded).
  * When the build is finished, `Builder/ViewModel/Commands.cs` stores every custom in an `Events.json` file.
  * A `Trace.json` file can be exported from an `Events.json` file via `Builder/ViewModel/Commands.cs`. This is useful to build different timelines (even to different formats) without having to repeat the build.

## License

This project is released under [GNU GPLv3](https://github.com/MetanoKid/msbuild-flame-graph/blob/master/LICENSE.md) license.

I started this project thanks to the community, so I wanted to give it back. You are encouraged to alter it in any way you want, but please continue making it public so the community can benefit from it.

## Acknowledgements

  * Thanks to [@aras_p](https://twitter.com/aras_p) for his blog posts investigating build times, [this one](https://aras-p.info/blog/2019/01/16/time-trace-timeline-flame-chart-profiler-for-Clang/) was the main inspiration for this tool.
  * Thanks to Microsoft's dev team for building these API and new tools like [vcperf](https://github.com/microsoft/vcperf) or [MSBuild Structured Log Viewer](https://github.com/KirillOsenkov/MSBuildStructuredLog).
