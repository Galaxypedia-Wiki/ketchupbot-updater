#!/bin/bash

# This script is used to create a release of the project.
# It assumes that you have already tagged and changed the version numbers

## Check if p7zip is installed
if ! [ -x "$(command -v 7z)" ]; then
    echo "Error: 7z is not installed." >&2
    echo "Please install 7z to continue."
    exit 1
fi

## Check to see if the output directory exists
if [ -d "output" ]; then
    echo "Output directory exists"
    echo "Deleting contents of output directory"
    rm -rf output/*
else
    echo "Output directory does not exist. Creating it now..."
    mkdir output
fi

## Build & Package for windows
echo "Building for Windows"
dotnet publish ketchupbot-updater -c Release -r win-x64 -o output/win-x64 --self-contained -p:PublishSingleFile=true
7z a -tzip output/ketchupbot-updater-win-x64.zip ./output/win-x64/ketchupbot-updater.exe
rm -rf output/win-x64
echo "Windows build complete"

## Build & Package for MacOS
echo "Building for MacOS"
dotnet publish ketchupbot-updater -c Release -r osx-x64 -o output/osx-x64 --self-contained -p:PublishSingleFile=true
tar -czvf output/ketchupbot-updater-osx-x64.tar.gz -C ./output/osx-x64 ketchupbot-updater
rm -rf output/osx-x64
echo "MacOS build complete"

## Build & Package for Linux
echo "Building for Linux"
dotnet publish ketchupbot-updater -c Release -r linux-x64 -o output/linux-x64 --self-contained -p:PublishSingleFile=true
tar -czvf output/ketchupbot-updater-linux-x64.tar.gz -C ./output/linux-x64 ketchupbot-updater
rm -rf output/linux-x64
echo "Linux build complete"

echo "All builds complete"