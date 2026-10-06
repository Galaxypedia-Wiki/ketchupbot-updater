# KetchupBot-Updater
This is the updater component of the KetchupBot Project. This component facilitates grabbing data from the Galaxy Info API and using that data to update the [Galaxypedia](https://wiki.galaxy.casa).

## Downloading, Running, and Usage

### Running via Binary
We provide prebuilt binaries for running ketchupbot. Everything is contained within the binary, including the runtime, dependencies, and any assets. These binaries also assume a production environment, so they will not print debug information.

Use these binaries if you want to run KetchupBot without installing the .NET runtime or building from source.

#### Development Builds
We recommend using these builds when going with prebuilt binaries. They're built on every change and will have all the latest features and bug fixes. You can find the latest development build [here](). Make sure to check back often for new builds, as they can be rather frequent.

#### Stable Release
You can download the latest stable release from the [releases page](). These are built on every release and are considered stable for production use. However, releases are made infrequently, so they may not have the latest features and bug fixes. We typically use releases more as a checkpoint for the project, rather than a new version. So you should only use these if you want a stable version of KetchupBot and don't want to deal with the hassle of updating it frequently.

### Running from source
If you want to run KetchupBot from source, you can do so by following the development instructions below. This is the recommended way to run KetchupBot if you're developing it and not planning on using it with CI. It also gives you the most control over the program.

### Usage
KetchupBot is primarily controlled via CLI arguments. *For any release, you must set up secrets.* Read *Setting Up Secrets* below to figure out how to do this. It's recommended to run --help to figure out what you can do with it.

#### Scheduling
Since https://github.com/Galaxypedia-Wiki/ketchupbot-updater/pull/165, KetchupBot no longer includes a daemon mode (built-in job scheduler). We recommend that you run KetchupBot as a one-shot application and schedule its runs via an external task scheduler such as [cron](https://en.wikipedia.org/wiki/Cron) or [Windows Task Scheduler](https://en.wikipedia.org/wiki/Windows_Task_Scheduler). This ensures that KetchupBot isn't using up RAM while idling. And, in the unlikely case where a memory leak occurs within the application, running it as one-shot ensures that the leak doesn't go out of control.

#### systemd examples (Linux)
Optional [service](ketchupbot-updater.service) and [timer](ketchupbot-updater.timer) examples are provided. You can also run the application manually or use another scheduler.

The service assumes a Linux binary at `/opt/ketchupbot-updater/ketchupbot-updater`, a dedicated `ketchupbot` user, and an `appsettings.json` file in `/opt/ketchupbot-updater`. Give that user permission to execute the binary and read the settings. Edit the service's user, paths, and arguments to match your installation; the sample updates all ships and turrets. The timer runs every hour at `xx:00` in the system's local timezone and catches up once if a scheduled run was missed while the machine was off.

After reviewing and editing the examples, install them:

```bash
sudo cp ketchupbot-updater.{service,timer} /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable --now ketchupbot-updater.timer
```

For a manual run and logs:

```bash
sudo systemctl start ketchupbot-updater.service
sudo journalctl -u ketchupbot-updater.service
```

Enable the timer only if you want scheduled runs. To stop scheduling, run `sudo systemctl disable --now ketchupbot-updater.timer`.

#### cron example (Linux)
The [example crontab](ketchupbot-updater.crontab) runs every hour at `xx:00` in the system's local timezone. Edit the paths and arguments, and ensure the user running the job can execute the binary, read `appsettings.json`, and write the log file.

Use `crontab -e` as that user and add the example's job line to your existing crontab. Choose either cron or the systemd timer to avoid duplicate runs.

## Developing
KetchupBot is very easy to get up and running. The steps below will walk you through setting up a development environment.

#### Prerequisites:
- [.NET SDK 10.0](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
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
Copy `ketchupbot-updater/appsettings.example.json` to `appsettings.json` and fill in `GIAPI_URL`, `MWUSERNAME`, and `MWPASSWORD`. All three settings must be non-empty. By default, the application reads this file from the executable's directory. Use `--secrets-directory /path/to/config` to select another directory; relative paths are resolved from the working directory.

You can supply the same keys through environment variables, which override JSON values. The JSON file is optional when all required settings are supplied through other sources. `.env` files are no longer loaded automatically.

For development, use .NET User Secrets:

```bash
dotnet user-secrets set "MWUSERNAME" "your-username" --project ketchupbot-updater
dotnet user-secrets set "MWPASSWORD" "your-password" --project ketchupbot-updater
```

Set `DOTNET_ENVIRONMENT=Development` to load User Secrets. Configuration precedence is environment variables, then development User Secrets, then JSON. Keep credential files out of source control and restrict their permissions to the account running the updater.

### Building
KetchupBot is considered mission critical by the Galaxypedia staff, and for that reason, we use strict coding practices and standards to ensure that the program cannot crash. All of these configurations and practices are advised via ESLint. For that reason, we highly recommend using a modern IDE when developing for KetchupBot. I (smallketchup82) personally use Jetbrains WebStorm, but others on the development team use VSCode with the ESLint extension. If you are using VSCode, go into your settings and make sure that the "experimental flag config" setting is on for the ESLint extension, otherwise it won't be able to use our rules.

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
    - You can manually force dry run by passing `--dry-run` as a CLI argument. This can be useful when working on the release configuration.
- When profiling, add the `-c Release` flag to the `dotnet run` command to enable optimizations.
- Please format your code before committing. Use your IDE's formatter tools to do this, or run `dotnet format` from the command line.
- In general, the code is the documentation, so we'd recommend looking through the codebase to get a feel for how things work. We make an effort to extensively document our code, so you should be able to find what you need.

### Contributing to KetchupBot
We welcome contributions to KetchupBot! We recommend looking through currently open issues and trying to tackle them. In general, it would be advised to leave your thoughts in the issue thread(s) before beginning development on the feature so that we can make sure that the feature is developed in line with our vision of KetchupBot. We don't want to waste your time on a feature that we won't be adding in!

We don't have GitHub discussions turned on, as we would prefer any discussion related to KetchupBot development be facilitated in the [#galaxypedia-discussion channel of the Galaxypedia Discord Server](https://discord.gg/C4xhTz9KAD).

As always, we highly encourage you to [reach out to Galaxypedia Staff](https://discord.gg/hsr4Dq6Ha6) if you have any questions or need any help with this; we don't mind, seriously.

## Notices & Terms
This software is Open Source and licensed under the MIT license. You must follow the rules in the license when contributing, modifying, using, and/or redistributing the software. In addition to the license, you must follow the Galaxypedia's [Terms of Service](https://wiki.galaxy.casa/wiki/Project:Terms_of_Service).
