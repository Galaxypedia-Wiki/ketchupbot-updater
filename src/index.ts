import dotenv from "dotenv"
dotenv.config()

const WikiTextParser = require("parse-wikitext")
const wikiTextParser = new WikiTextParser("robloxgalaxy.wiki")
import fetch from "node-fetch"
const NodeMW = require("nodemw")
import chalk from "chalk"
import { promisify } from "util"
import cron from "node-cron"
import fs from "fs/promises"
import { performance } from "perf_hooks"
import express from "express"

// Settings
const verbose: Boolean = process.env.VERBOSE === "true"
const dryrun: Boolean = process.env.DRYRUN === "true"

const SHIP_NAME_MAP: any = {
	2018: "2018 Ship",
	yname: "Yname (ship)"
}

const app = express()

class ShipUpdater {
	bot: any
	logChange!: Function
	SHIP_INFOBOX_REGEX!: RegExp
	logDiscord!: Function
	getArticle!: Function
	editArticle!: Function
	getArticleWikitext!: Function
	getArticleRevisions!: Function
	currentlyUpdating!: Boolean
	shipsData: any
	galaxypediaShipList: any
	async main(bot: any, logChange: Function, logDiscord: Function, manual: Boolean = false) {
		this.SHIP_INFOBOX_REGEX = /{{\s*Ship[ _]Infobox.*?}}/si
		this.bot = bot
		this.logChange = logChange
		this.logDiscord = logDiscord
		this.getArticle = promisify(this.bot.getArticle.bind(this.bot))
		this.editArticle = promisify(this.bot.edit.bind(this.bot))
		this.getArticleWikitext = promisify(wikiTextParser.getArticle.bind(wikiTextParser))
		this.getArticleRevisions = promisify(this.bot.getArticleRevisions.bind(this.bot))

		if (!manual) cron.schedule("0 * * * *", () => this.updateGalaxypediaShips())
		await this.updateGalaxypediaShips()
	}

	async updateGalaxypediaShips () {
		try {
			if (this.currentlyUpdating) {
				console.log(`${chalk.redBright("[!]")} Already updating ships; not updating`)
				return
			}
			this.currentlyUpdating = true
			this.shipsData = await this.getShipsData()
			this.galaxypediaShipList = await this.getGalaxypediaShipList()
			await this.updateShips()
		} catch (error: any) {
			console.error(chalk.redBright("------------ MASS SHIP UPDATE ERROR ------------\n") + error.stack)
			this.logDiscord("Mass update errored (Check console for info)")
		}

		this.currentlyUpdating = false
	}

	async getShipsData () {
		const response = await fetch(`https://galaxy.wingysam.xyz/api/v2/galaxypedia?token=${process.env.GALAXY_INFO_TOKEN}`)
		if (!response.ok) throw new Error("Galaxy Info seems to be down")
		const galaxyInfoShips = await response.json()
		return galaxyInfoShips
	}

	async getGalaxypediaShipList () {
		const response = await fetch("https://robloxgalaxy.wiki/api.php?action=query&format=json&list=categorymembers&cmtitle=Category%3AShips&cmlimit=5000")
		if (!response.ok) throw new Error("Galaxypedia appears to be down.")

		const galaxypediaPageList: String[] = (await response.json()).query.categorymembers
			.map((page: any) => page.title)
		const shipsList: String[] = galaxypediaPageList
			.filter((pageName: any) => !pageName.startsWith("Category:"))

		return shipsList
	}

	async updateShips () {
		for (const shipName of Object.keys(this.shipsData).sort()) {
			await this.handleShip(this.shipsData[shipName])
		}
		console.log(chalk.greenBright("Ships updated!"))
	}

	async handleShip (ship: any) {
		if (process.env.SHIP && ship.title !== process.env.SHIP) return
		try {
			console.log(`${chalk.yellow("Processing ")} ${chalk.cyanBright(ship.title)}...`)
			const steps = await this.updateShip(ship)

			// Grab the most recent edit made by the bot & send the revid to the discord webhook logger
			var latestrevision = (await this.getArticleRevisions(ship.title)).reverse()
			var revision = null
			if (latestrevision && latestrevision[0].user && latestrevision[0].revid) {
				latestrevision = await latestrevision.filter((val: any) => val.user === process.env.MW_LOGIN)[0]
				if (latestrevision && latestrevision.user && latestrevision.revid) {
					const timestamp = new Date(latestrevision.timestamp)
					const rn = new Date()
					if (timestamp.getDate() === rn.getDate() && timestamp.getMonth() === rn.getMonth() && timestamp.getFullYear() === rn.getFullYear()) {
						revision = latestrevision
					}
				}
			}

			const perf = verbose ? ` perf: ${steps.join(", ")}` : ""
			console.log(`${chalk.green("Updated")} ${chalk.cyanBright(ship.title)}!` + perf)
			await this.logChange(ship.title, revision)
		} catch (error: any) {
				console.log(`${chalk.red("[!]")} ${chalk.cyanBright(ship.title)}: ${chalk.red(error.message)}`)
				if (verbose) {
					console.log(error.stack)
				}
		}
	}

	async updateShip (ship: any) {
		const steps: string[] = []
		async function step (name: string, prom: Promise<any>) {
			const start = performance.now()
			const returned = await prom
			const end = performance.now()
			steps.push(`${name} ${(end - start).toFixed(2)}ms`)
			return returned
		}

		const pageName = await step("getShipPageName", this.getShipPageName(ship))
		const oldWikitext = await step("getArticle", this.getArticle(pageName))
		if (!oldWikitext) throw new Error(`Wikitext for ${pageName} missing`)

		const oldData = await step("parseWikiText", this.parseWikitext(oldWikitext))
		const newData = await step("mergeData", this.mergeData(oldData, ship))

		const newWikitext = await step("formatDataIntoWikitext", this.formatDataIntoWikitext(newData, oldWikitext))
		if (newWikitext === oldWikitext) throw new Error("Already up-to-date")

		if (!dryrun) await step("editArticle", this.editArticle(pageName, newWikitext, "Automatic Infobox Update", false))
		return steps
	}

	async getShipPageName (ship: any) {
		if (this.galaxypediaShipList.includes(ship.title)) return ship.title
		const mappedName: string = SHIP_NAME_MAP[ship.title]
		if (mappedName && this.galaxypediaShipList.includes(mappedName)) return mappedName
		throw new Error(`Can't find page name for ${ship.title}`)
	}

	async parseWikitext (wikitext: any) {
		const matches = wikitext.match(this.SHIP_INFOBOX_REGEX)
		if (!matches) throw new Error("Could not find infobox!")

		var data = wikiTextParser.parseTemplate(matches[0]).namedParts
		if (data.image && data.image.startsWith("<gallery")) {
			const boo = wikitext.match(/<gallery.*?>.*?<\/gallery>/sg)
			if (boo) {
				data.image = boo[0]
			}
		}
		
		
		if (verbose) console.log("Ship Data Raw\n" + JSON.stringify(data, null, "\t"))
		return data
	}

	async mergeData (...objects: any[]) {
		const data: any = {}
		function mergeObjectIn (obj: any) {
			for (const key of Object.keys(obj)) {
				if (obj[key] === "") continue
				data[key] = obj[key]
			}
		}

		for (const obj of objects) {
			mergeObjectIn(obj)
		}

		// Sort the json alphabetically
		const sorted: any = {}
		const keys: any[] = []
	
		for (const key in data) {
			keys.push(key)
		}
	
		keys.sort((a, b) => a.localeCompare(b))
	
		for (const key of keys) {
			sorted[key] = data[key]
		}

		if (verbose) console.log("Ship Data Merged: ", { objects, sorted })
		return sorted
	}

	async formatDataIntoWikitext (data: any, oldWikitext: string) {
		const newWikitext = oldWikitext.replace(this.SHIP_INFOBOX_REGEX, "{{Ship Infobox\n|" + Object.entries(data).map(([key, val]) => `${key} = ${val}`).join("\n|") + "\n}}")

		if (verbose) {
			console.log(chalk.blueBright("------------ OLD PAGE WIKITEXT ------------"))
			console.log(oldWikitext)
			console.log(chalk.blueBright("------------ NEW PAGE WIKITEXT ------------"))
			console.log(newWikitext)
		}
		return newWikitext
	}
}

class TurretsUpdater {
	TURRET_TABLE_REGEX!: RegExp
	bot: any
	logChange!: Function
	logDiscord!: Function
	getArticle!: Function
	editArticle!: Function
	getArticleWikitext!: Function
	getArticleRevisions!: Function
	currentlyUpdating!: boolean
	async main (bot: any, logChange: Function, logDiscord: Function, manual: boolean = false) {
		this.TURRET_TABLE_REGEX = /{\|\s*class="wikitable sortable".*?\|}/sig
		this.bot = bot
		this.logChange = logChange
		this.logDiscord = logDiscord
		this.getArticle = promisify(this.bot.getArticle.bind(this.bot))
		this.editArticle = promisify(this.bot.edit.bind(bot))
		this.getArticleWikitext = promisify(wikiTextParser.getArticle.bind(wikiTextParser))
		this.getArticleRevisions = promisify(this.bot.getArticleRevisions.bind(this.bot))
		
		if (!manual) cron.schedule("30 * * * *", () => this.updateGalaxypediaTurrets())
		await this.updateGalaxypediaTurrets()
	}

	async updateGalaxypediaTurrets() {
		try {
			if (this.currentlyUpdating) {
				console.log(`${chalk.redBright("[!]")} Already updating turrets; not updating`)
				return
			}
			this.currentlyUpdating = true

			const turretsData = await this.getTurretsData()
			await this.updateTurrets(turretsData)
		} catch (error: any) {
			console.error(chalk.red("------------ TURRET UPDATE ERROR ------------\n") + error.stack)
			this.logDiscord("Mass Turret Update errored (Look at console for more information)")
		}

		this.currentlyUpdating = false
	}

	async getTurretsData() {
		const response = await fetch("https://galaxy.wingysam.xyz/api/v2/ships-turrets/raw")
		if (!response.ok) throw new Error("Galaxy Info seems to be down - Turrets")
		const galaxyInfoTurrets = await response.json()
		return galaxyInfoTurrets.serializedTurrets
	}

	async updateTurrets(turretData: any) {
		const turretPageWikitext = await this.getArticleWikitext("Turrets")
		var cum = turretPageWikitext

		const turrettables = turretPageWikitext.match(this.TURRET_TABLE_REGEX)
		if (turrettables.length > 6) throw new Error("Irregular number of tables found on Turret page, ensure that the number of tables stays at 6")

		for (const [index, table] of turrettables.entries()) {
			if (verbose) console.log(index)
			const tablesplit = table.split("|-")
	
			const relevantturrets = Object.entries(turretData).filter(([, data]: any) => {
				if (index === 0) return data.TurretType === "Mining"
				else if (index === 1) return data.TurretType === "Laser"
				else if (index === 2) return data.TurretType === "Railgun"
				else if (index === 3) return data.TurretType === "Flak"
				else if (index === 4) return data.TurretType === "Cannon"
				else if (index === 5) return data.TurretType === "PDL"
			})
			const turretsparsed = relevantturrets.map(([, turret]: any) => {
				return `\n| ${turret.Name}\n| ${turret.Size}\n| ${turret.BaseAccuracy.toFixed(4)}\n| ${turret.Damage.toFixed()}\n| ${turret.Range.toFixed()}\n| ${turret.Reload.toFixed(2)}\n| ${turret.SpeedDenominator.toFixed()}\n| ${turret.DPS.toFixed(2)}`
			})
			if (verbose) console.table(turretsparsed)
			const test = `${tablesplit[0].trim()}\n|-\n${(turretsparsed.join("\n|-")).trim()}\n|}`
	
			cum = cum.replace(turrettables[index], test)
		}

		if (turretPageWikitext === cum) return console.log(chalk.greenBright("Turrets page is up to date!"))

		if (!dryrun) await this.editArticle("Turrets", cum, "Automatic Turret Update", false)
		console.log(chalk.greenBright("Updated turrets!😋"))
	}
}

(async () => {
	console.log((await fs.readFile("banner.txt")).toString())
	console.log("Written by smallketchup82 & yname\n---------------------------------")
	
	if (dryrun) {
		console.log(`${chalk.red("[!]")} Dry run is enabled! Halting for 3 seconds, terminate program if unintentional.`)
		await new Promise(resolve => setTimeout(resolve, 3000))
	}

	const bot: any = new NodeMW({
		protocol: "https",
		server: "robloxgalaxy.wiki",
		path: "/",
		debug: verbose
	});

	const logIn = promisify(bot.logIn.bind(bot))

	logIn(process.env.MW_LOGIN, process.env.MW_PASS)

	async function logChange (name: string, revision: { revid: string | number }) {
		if (dryrun) return
		await fetch(process.env.WEBHOOK!, {
			method: "POST",
			headers: {
				"Content-Type": "application/json"
			},
			body: JSON.stringify({
				content: `Updated **${name}**! ${(revision ? `([diff](<https://robloxgalaxy.wiki/index.php?title=${encodeURIComponent(name)}&diff=prev&oldid=${encodeURIComponent(revision.revid)}>))` : "")}`
			})
		})
	}

	async function logDiscord (content: any) {
		if (dryrun) return
		await fetch(process.env.WEBHOOK!, {
			method: "POST",
			headers: {
				"Content-Type": "application/json"
			},
			body: JSON.stringify({
				content: content.toString()
			})
		})
	}

	if (process.env.TURRETSONLY === "false") {
		const shipupdater = new ShipUpdater()
		await shipupdater.main(bot, logChange, logDiscord)
	}

	if (process.env.SHIPSONLY === "false") {
		const turretupdater = new TurretsUpdater()
		await turretupdater.main(bot, logChange, logDiscord)
	}
})()