# README #

## What is ZILF?

It's a set of tools for working with the ZIL interactive fiction language, including a compiler, assembler, disassembler, and game library.

## How can I start using ZILF?

You can try out ZILF in your browser by visiting [zilf.io](https://zilf.io/) and using the REPL or Project tabs, but if you want to write a game, we recommend installing ZILF on your own computer.

Visit [zilf.io](https://zilf.io/) or the [releases page](https://foss.heptapod.net/zilf/zilf/-/releases) to download an installable package for your platform. If an installable package isn't available for your platform, see below for instructions on building ZILF from source.

Visit [the wiki](https://foss.heptapod.net/zilf/zilf/-/wikis/Getting-Started) to learn how to use ZILF to build and run ZIL games. Several sample games are included as a demonstration of ZILF's capabilities, such as Advent (Colossal Cave).

Once ZILF is installed, you can compile a ZIL game by running it like so:

    zilf mygame.zil

That will produce a file called something like `mygame.z3`, which you can then run with a [Z-code interpreter](https://www.ifwiki.org/Z-code_interpreters).

## Contribution guidelines

To build ZILF from source, you'll need the .NET 10 SDK. With that installed, clone this repository and run:

    dotnet build Zilf.sln

If you want to build nice neat packages like the ones we distribute, you'll need to refer to `.github/workflows/build-packages.yml`. This uses the PowerShell scripts in `tools`.

To run the full test suite, use:

    dotnet test Zilf.sln

To run only the fast tests (skipping the ZILF library tests and full game tests), use:

    dotnet test Zilf.sln --filter "Category!=Slow"

If you'd like to contribute code, please request to join the project on [Heptapod](https://foss.heptapod.net/zilf/zilf/), commit your work to a topic branch, and submit a merge request. Alternatively, you may create an issue on [JIRA](https://vaporware.atlassian.net/projects/ZILF) and submit a patch.

## Who do I talk to?

The primary contact is Tara McGrew (a.k.a. vaporware), who may be found on the [IntFiction forum](http://intfiction.org/forum/), [ifMUD](http://ifmud.port4000.com/), or [Discord](https://discord.gg/eR98YMN).

To report a bug or request a feature, please use our JIRA server at [vaporware.atlassian.net](https://vaporware.atlassian.net/projects/ZILF).

## Special thanks

This project's ongoing development is made possible by the hosting services generously provided by [Octobus](https://octobus.net/) and [Clever Cloud](https://www.clever-cloud.com/).

In addition, ZILF as we know it wouldn't exist without the contributions of Adam Sommerfield, Alex Proudfoot, Arthur O'Dwyer, Daniel M. Stelzer, Dannii Willis, Henrik Åsman, Jason Scott, Jason Self, Jayson Smith, Jonathan Blask, Josh Lawrence, Kate Matthews, Matthew Russotto, Nick Turner, Paul Marquis, Rick Thornquist, and Toby Ott...

The prior work of Andy Baio, David Given, David M. Baggett, Graham Nelson, and Kent Tessman...

And of course, the people at Infocom and elsewhere who created and documented MDL, ZIL, and the Z-machine in the first place: David Lebling, Greg Pfister, Marc Blank, Mike Dornbrook, Steve Meretzky, Stu Galley, Tim Anderson, and the rest of the team.
