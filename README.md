# KetchupBot-Updater
This is the updater component of the KetchupBot Project. This component facilitates grabbing data from the Galaxy Info API and using that data to update the [Galaxypedia](https://robloxgalaxy.wiki).

## Downloading, Running, and Usage

### Running via Docker
**Do note that the CI Docker image assumes a production environment. Please do not use it for development & debugging! It will not print debug information. Do not use the docker image unless you know what you're doing!**

KetchupBot automatically publishes a docker image on every push which you can use to run a reproducible build of KetchupBot. You can find the image [here](https://github.com/smallketchup82/ketchupbot-updater/pkgs/container/ketchupbot-updater).

We recommend this method the most for Continuous Integration, as it's the most reliable and reproducible. It's also the easiest way to run KetchupBot, as you don't have to worry about dependencies or setting up a runtime.

### Running via Binary
We provide prebuilt binaries for running ketchupbot. Everything is contained within the binary, including the runtime, dependencies, and any assets. These binaries also assume a production environment, so they will not print debug information.

These are mainly distributed for ease of use, but we don't really recommend using them. As they're not as flexible as running from source, nor as reproducible as running with Docker. Use this method if you  don't want to deal with dependencies or working with docker.

#### Development Builds
We recommend using these builds when going with prebuilt binaries. They're built on every change and will have all the latest features and bug fixes. You can find the latest development build [here]().

Make sure to check back often for new builds, as they can be rather frequent.

#### Stable Release
You can download the latest stable release from the [releases page](). These are built on every release and are considered stable for production use. However, releases are made infrequently, so they may not have the latest features and bug fixes. We typically use releases more as a checkpoint for the project, rather than a new version. So you should only use these if you want a stable version of KetchupBot and don't want to deal with the hassle of updating it frequently.

### Running from source
If you want to run KetchupBot from source, you can do so by following the development instructions below. This is the recommended way to run KetchupBot if you're developing it and not planning on using it with CI. It also gives you the most control over the program.

### Usage
KetchupBot is primarily controlled via CLI arguments. *For any release, you must set up secrets.* Read *Setting Up Secrets* below to figure out how to do this. It's recommended to run --help to figure out what you can do with it.

#### Scheduling
KetchupBot can run as a daemon (using the built in job scheduler) or as a one-shot application. The typical recommendation is that whereever possible, run KetchupBot as a one-shot application and schedule its runs via an external task scheduler such as Crontab. This ensures that KetchupBot isn't using up RAM while idling. Also, in the unlikely case where a memory leak occurs within the application, running it in one-shot ensures that the leak doesn't go out of control.

Use daemon mode only if your use case requires it. Otherwise its best that you use an external scheduler to avoid wasted resources.

## Developing
KetchupBot is very easy to get up and running. The below steps will walk you through setting up a development environment.

#### Prerequisites:
- [.NET SDK 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- An IDE for C#
  - We recommend using JetBrains Rider
  - Visual Studio should work fine as well
  - If you want to use Visual Studio Code, make sure to install the C# and EditorConfig extensions

#### Downloading the source code
Clone the respository using git:
```bash
git clone https://github.com/smallketchup82/ketchupbot-updater
cd ketchupbot-updater
```

### Setting up secrets
KetchupBot requires a few secrets (passwords) to run properly. These secrets can be stored in a few different ways, but the most common way is to use an `appsettings.json` file. You can create this file by copying the `appsettings.example.json` file and renaming it to `appsettings.json`. You can then fill in the values with your own secrets.

You can also use a `.env` file or environment variables to store secrets. They follow the same naming conventions as the `appsettings.json` file. Using environment variables will overwrite `appsettings.json`.

Whichever method you choose, you will have to put `appsettings.json` into the same directory as the executable. You can add your appsettings.json to the `ketchupbot-updater` project and set it to `Copy if newer` in the properties. This will automatically copy it to your build directory.

### Building
KetchupBot is considered mission critical by the Galaxypedia staff, and for that reason, we use strict coding practices and standards to ensure that the program cannot crash. All of these configurations and practices are advised via eslint. For that reason, we highly recommend using a modern IDE when developing for KetchupBot. I (smallketchup82) personally use Jetbrains WebStorm, but others on the development team use VSCode with the eslint extension. If you are using VSCode, go into your settings and make sure that the "experimental flag config" setting is on for the eslint extension, otherwise it won't be able to use our rules.

We recommend reading the package.json to find some useful npm scripts. We have npm scripts to run via JIT (the developmental way of running), run via a build (to be used in production environments for its reliability), and to build.

#### From an IDE
Open `ketchupbot-updater.sln` in your IDE to get started. Run configurations are included by default for JetBrains Rider. For other ide's, you'll likely have to create your own run configurations.

#### From the command line
Use the following commands to run the project:
```bash
dotnet run --project ketchupbot-updater
```

## Contributing

### General notes for development:
- **NEVER TURN OFF DRY RUN** while working on KetchupBot. This is a mission-critical program, and we don't want to accidentally update the Galaxypedia with incorrect data.
    - Running in Release configuration will automatically turn off dry run. Be careful!
    - You can manually force dry run by passing `--dry-run` as a CLI argument. This can be useful when working on Release configuration.
- When profiling, add the `-c Release` flag to the `dotnet run` command to enable optimizations.
- Please format your code before committing. Use your IDE's formatter tools to do this, or run `dotnet format` from the command line.
- In general, the code is the documentation, so we'd recommend looking through the codebase to get a feel for how things work. We make an effort to extensively document our code, so you should be able to find what you need.

### Contributing to KetchupBot
We welcome contributions to KetchupBot! We recommend looking through currently open issues and trying to tackle them. In general, it would be advised to leave your thoughts in the issue thread(s) before beginning development on the feature so that we can make sure that the feature is developed in line with our vision of KetchupBot. We don't want to waste your time on a feature that we won't be adding in!

We don't have GitHub discussions turned on, as we would prefer any discussion related to KetchupBot development be facilitated in the [#galaxypedia-discussion channel of the Galaxypedia Discord Server](https://discord.gg/C4xhTz9KAD).

As always, we highly encourage you to [reach out to Galaxypedia Staff](https://discord.gg/hsr4Dq6Ha6) if you have any questions or need any help with this, we don't mind, seriously.

## Notices & Terms
This software is Open Source and licensed under the MIT license. You must follow the rules in the license when contributing, modifying, using, and/or redistributing the software. In addition to the license, you must follow the Galaxypedia's [Terms of Service](https://robloxgalaxy.wiki/wiki/Galaxypedia:Terms_of_Service#4._Rules,_Policies,_Guidelines).
