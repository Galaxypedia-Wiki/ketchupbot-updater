import dotenv from "dotenv"
dotenv.config()

const NodeMW = require("nodemw")
import fs from "fs/promises"
import chalk from "chalk"
import { promisify } from "util"
import fetch from "node-fetch"
import readline from "readline"

const rl = readline.createInterface({
    input: process.stdin,
    output: process.stdout
})

const dryrun: Boolean = process.env.DRYRUN === "true"

async function getUnusedFiles() {
    const res = await fetch("https://robloxgalaxy.wiki/api.php?action=query&format=json&generator=querypage&gqppage=Unusedimages&gqplimit=max")

    if (!res.ok) {
        throw new Error("Failed to get unused files")
    }

    const json = await res.json()
    const pages = Object.values(json.query.pages).map((page: any) => page.title)

    return pages
}

async function deleteFile(file: string, bot: any) {
    console.log(`${chalk.green("[-]")} Deleting ${file}...`)

    const deleteAFile = promisify(bot.delete.bind(bot))

    if (!dryrun) {
        await deleteAFile(file, "Unused file")
    } else {
        console.log(`${chalk.red("[!]")} Dry run is enabled! Skipping deletion for ${file}`)
    }
}

async function main(bot: any) {
    const files = await getUnusedFiles()
    if (files.length === 0) {
        console.log("No more unused files found!")
        process.exit(0)
    }

    console.log(`${chalk.green("[+]")} Got ${files.length} unused files.`)

    for (const file of files) {
        await deleteFile(file, bot)
    }
    console.log(`${chalk.green("[+]")} Done!`)
    main(bot)
}

async function initialize() {
    console.log((await fs.readFile("banner.txt")).toString())
    console.log("Written by smallketchup82 & yname\n---------------------------------")

    if (dryrun) console.log(`${chalk.red("[!]")} Dry run is enabled!`)

    const bot: any = new NodeMW({
        protocol: "https",
        server: "robloxgalaxy.wiki",
        path: "/",
    });

    const logIn = promisify(bot.logIn.bind(bot))
    logIn(process.env.MW_LOGIN, process.env.MW_PASS)

    console.log(`${chalk.green("[+]")} Logged in.`)

    rl.question("Are you sure you want to delete all unused files? (y/N) ", async (answer: string) => {
        if (answer.toLowerCase() !== "y") {
            console.log("Aborting...")
            process.exit(0)
        }
        await main(bot)
    })
}

initialize()