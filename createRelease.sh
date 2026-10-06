#!/bin/bash
set -euo pipefail

# NativeAOT must be published on the target operating system.
case "$(uname -s)/$(uname -m)" in
    Darwin/arm64) rid=osx-arm64 ;;
    Darwin/x86_64) rid=osx-x64 ;;
    Linux/x86_64) rid=linux-x64 ;;
    *) echo "Use the release workflow for this platform." >&2; exit 1 ;;
esac

task_publish_dir=$(mktemp -d)
trap 'rm -rf "$task_publish_dir"' EXIT
mkdir -p output
dotnet publish KBot.CLI -c Release -r "$rid" -o "$task_publish_dir" --self-contained
tar -czf "output/KBot-$rid.tar.gz" -C "$task_publish_dir" KBot.CLI
