# KetchupBot-Updater
This is the updater component of the KetchupBot Project. This component facilitates grabbing data from the Galaxy Info API and using that data to update the [Galaxypedia](https://robloxgalaxy.wiki).

## Notices & Terms
This software is Open Source and licensed under the CC-BY-SA license. You must follow the rules in the license when contributing, modifying, using, and/or redistributing the software. In addition to the license, you must follow the Galaxypedia's [Terms of Service](https://robloxgalaxy.wiki/wiki/Galaxypedia:Terms_of_Service#4._Rules,_Policies,_Guidelines).

In general, we don't want multiple KetchupBot's running on the Galaxypedia, as that can cause a mess if they all aren't up-to-date. We highly encourage you to **NEVER TURN OFF DRY RUN** while working on KetchupBot.

## Running & Usage
**Do note that the docker image assumes a production environment and runs its own command line arguments. It will not allow you to specify your own arguments. Do not use the docker image unless you know what you're doing!**

Apart from building, KetchupBot automatically publishes a docker image on every push which you can use to run KetchupBot without needing to have a dev environment. You can find the image [here](https://github.com/smallketchup82/ketchupbot-updater/pkgs/container/ketchupbot-updater).

KetchupBot is primarily controlled via CLI arguments, while authentication is handled with a .env file (but can also be passed via the CLI). In general, the code is the documentation, and we recommend running --help to figure out what you can do with it.

## Building
KetchupBot, ever since its rewrite, is very intelligent and easy to get up and running. The below steps will walk you through setting up a development environment and getting the program running.

We recommend using NodeJS v20 or later.

1. Clone the repository and enter into the directory
2. Run `npm install`
3. Rename .env.example to .env and fill it out (we know you won't have a Galaxy Info Token, please contact Galaxypedia staff for help with this)
4. Run `npm run run -- --help`  
If the above command succeeds, you should have a functional development build. Replace --help with the arguments of your choice to change KetchupBot's behaviour.

## Setting up a dev environment
KetchupBot is considered mission critical by the Galaxypedia staff, and for that reason, we use strict coding practices and standards to ensure that the program cannot crash. All of these configurations and practices are advised via eslint. For that reason, we highly recommend using a modern IDE when developing for KetchupBot. I (smallketchup82) personally use Jetbrains WebStorm, but others on the development team use VSCode with the eslint extension. If you are using VSCode, go into your settings and make sure that the "experimental flag config" setting is on for the eslint extension, otherwise it won't be able to use our rules.

We recommend reading the package.json to find some useful npm scripts. We have npm scripts to run via JIT (the developmental way of running), run via a build (to be used in production environments for its reliability), and to build.

## Contributing
We'd recommend looking through currently open issues and trying to tackle them. In general, it would be advised to leave your thoughts in the issue thread(s) before beginning development on the feature so that we can make sure that the feature is developed in line with our vision of KetchupBot. We don't want to waste your time on a feature that we won't be adding in.

We don't have github discussions turned on, as we would prefer any discussion related to KetchupBot development be faciliatated in the [#galaxypedia-discussion channel of the Galaxypedia Discord Server](https://discord.gg/C4xhTz9KAD).

As always, we highly encourage you to [reach out to Galaxypedia Staff](https://discord.gg/hsr4Dq6Ha6) if you have any questions or need any help with this, we don't mind, seriously.
