import dotenv from "dotenv"
dotenv.config()

// eslint-disable-next-line @typescript-eslint/no-var-requires
const WikiTextParser = require("parse-wikitext")
const wikiTextParser = new WikiTextParser("robloxgalaxy.wiki")
import fetch from "node-fetch"
import NodeMW from "nodemw"
import chalk from "chalk"
import { promisify } from "util"
import cron from "node-cron"
import fs from "fs/promises"
import { performance } from "perf_hooks"

// Settings
const verbose: boolean = process.env.VERBOSE === "true"
const dryrun: boolean = process.env.DRYRUN === "true"
if (process.env.SHIP && process.env.SHIP !== "") {
	console.log(chalk.yellowBright(`[!] Ship specified: ${process.env.SHIP}`))
	process.env.SHIPSONLY = "true"
}

// Manually map the ship name obtained from the API to the name of the page on the wiki
// TODO: Move these to the env file
type ShipNameMap = {
	[key: string]: string
}
const SHIP_NAME_MAP: ShipNameMap = {
	2018: "2018 Ship",
	yname: "Yname (ship)"
}

// Exempt certain parameters from the final result.
const parameters_to_exempt: string[] = [
	"damage_res"
]

let updatedShips: string[] = []
let skippedShips: string[] = []

/**
 * Updates the ship infobox on the wiki with the data from the Galaxy Info API
 * 
 * @property {NodeMW} bot - The NodeMW bot instance. Provided by {@link initBot}
 * @property {Function} logChange - Edit logger
 * @property {Function} logDiscord - Discord logger
 * @property {boolean} currentlyUpdating - Whether the updater is currently running
 * @property {any} shipsData - The data from the Galaxy Info API
 * @property {any} galaxypediaShipList - The list of ships on the wiki
 * @property {number} runcount - The number of times the updater has run
 * @property {RegExp} SHIP_INFOBOX_REGEX - The regex to match the ship infobox
 * @property {Function} getArticle - The getArticle function from NodeMW
 * @property {Function} editArticle - The editArticle function from NodeMW
 * @property {Function} getArticleWikitext - The getArticleWikitext function from parse-wikitext
 * @property {Function} getArticleRevisions - The getArticleRevisions function from NodeMW
*/
export class ShipUpdater {
	bot: NodeMW
	logChange: (name: string, revision: { revid: string | number } | null) => Promise<void>
	SHIP_INFOBOX_REGEX: RegExp = /{{\s*Ship[ _]Infobox(?:[^{}]|{{[^{}]*}}|{{{[^{}]*}}})+(?:(?!{{(?:[^{}]|{{[^{}]*}}|{{{[^{}]*}}})*)}})/si
	logDiscord: (content: string) => Promise<void>
	getArticle: (articleName: string) => Promise<any>
	editArticle: (title: string, content: string, summary: string, minor: boolean) => Promise<any>
	getArticleWikitext: (title: string) => Promise<any>
	getArticleRevisions: (title: string) => Promise<any>
	currentlyUpdating: boolean = false
	shipsData: any
	galaxypediaShipList: any
	runcount: number

	constructor(bot: NodeMW, logChange: (name: string, revision: { revid: string | number } | null) => Promise<void>, logDiscord: (content: string) => Promise<void>) {
		this.bot = bot
		this.logChange = logChange
		this.logDiscord = logDiscord
		this.getArticle = promisify(this.bot.getArticle.bind(this.bot))
		this.editArticle = promisify(this.bot.edit.bind(this.bot))
		this.getArticleWikitext = promisify(wikiTextParser.getArticle.bind(wikiTextParser))
		this.getArticleRevisions = promisify(this.bot.getArticleRevisions.bind(this.bot))
		this.runcount = 0
	}

	/**
	 * Start the updater. You only need this if you want the library to handle scheduling. You can run the functions manually if you want to handle scheduling yourself.
	 * 
	 * @returns {cron.ScheduledTask}
	 */
	startScheduler(): cron.ScheduledTask {
		const scheduler = cron.schedule("0 * * * *", async () => {
			await this.updateGalaxypediaShips()
		})
		return scheduler
	}

	/**
	 * Initial handler for the update process. You probably want to be using this if you're manually handling the scheduling.
	 * 
	 * @returns {Promise<{ updatedShips: string[]; skippedShips: string[] } | void>}
	 */
	async updateGalaxypediaShips (): Promise<{ updatedShips: string[]; skippedShips: string[] } | void> {
		function nth(n: number){return["st","nd","rd"][((n+90)%100-10)%10-1]||"th"}
		console.log(`${nth(this.runcount + 1)} Ship Update Iteration`)

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
		this.runcount += 1
		const shipssuccessfullyupdated = updatedShips
		const shipsskipped = skippedShips
		updatedShips = []
		skippedShips = []
		return { 
			"updatedShips": shipssuccessfullyupdated,
			"skippedShips": shipsskipped
		}
	}

	/**
	 * Fetch the ship data from the Galaxy Info API
	 * 
	 * @returns {Promise<any>}
	 */
	async getShipsData(): Promise<any> {
		if (!process.env.GALAXY_INFO_API) throw new Error("Galaxy Info API Endpoint is not specified in .env")
		if (!process.env.GALAXY_INFO_TOKEN) throw new Error("Galaxy Info Token is not specified in .env")

		const response = await fetch( `${process.env.GALAXY_INFO_API.trim()}/api/v2/galaxypedia?token=${process.env.GALAXY_INFO_TOKEN}`)
		if (!response.ok) throw new Error("Galaxy Info seems to be down")

		// No clue what response is given and im too lazy to hard code the type definitions for it so im just gonna use any lol
		const galaxyInfoShips = await response.json()
		return galaxyInfoShips
	}

	/** 
	 * Fetch the list of ships from the Galaxypedia
	 * 
	 * @returns {Promise<string[]>}
	 */
	async getGalaxypediaShipList(): Promise<string[]> {
		const response = await fetch("https://robloxgalaxy.wiki/api.php?action=query&format=json&list=categorymembers&cmtitle=Category%3AShips&cmlimit=5000")
		if (!response.ok) throw new Error("Galaxypedia appears to be down.")

		const galaxypediaPageList: string[] = (await response.json()).query.categorymembers.map((page: any) => page.title)
		const shipsList: string[] = galaxypediaPageList.filter((pageName: any) => !pageName.startsWith("Category:"))

		return shipsList
	}

	/**
	 * Handles updating each ship
	 * 
	 * @returns {Promise<void>}
	 */
	async updateShips(): Promise<void> {
		for (const shipName of Object.keys(this.shipsData).sort()) {
			await this.handleShip(this.shipsData[shipName])
		}
		console.log(chalk.greenBright("Ships updated!"))
	}

	/** 
	 * Handle updating a ship
	 * 
	 * The Ship environment variable can be used here to update a specific ship
	 * 
	 * @param {any} ship - The data from the Galaxy Info API for the ship
	 * @returns {Promise<void>}
	 */
	async handleShip(ship: any): Promise<void> {
		if (process.env.SHIP && process.env.SHIP !== "" && ship.title !== process.env.SHIP) return
		try {
			console.log(`${chalk.yellow("Processing ")} ${chalk.cyanBright(ship.title)}...`)
			const steps = await this.updateShip(ship)

			// Grab the most recent edit made by the bot & send the revid to the discord webhook logger
			// Grab the name of the page, including handling mapping. But don't throw an error if the page isnt found. Instead just return undefined
			const pagename = await this.getShipPageName(ship, false)
			let latestrevision = undefined

			// If the page exists, get the latest revision. If not, just return latestrevision as undefined
			if (pagename) {
				latestrevision = (await this.getArticleRevisions(pagename)).reverse()
			}
			let revision = null

			// If the latest revision exists, filter it to only include the bot's edits. If it doesn't exist, just return revision as null
			if (latestrevision) {
				if (latestrevision[0].user && latestrevision[0].revid) {
					latestrevision = await latestrevision.filter((val: any) => val.user === process.env.MW_LOGIN)[0]
					if (latestrevision && latestrevision.user && latestrevision.revid) {
						const timestamp = new Date(latestrevision.timestamp)
						const rn = new Date()
						if (timestamp.getDate() === rn.getDate() && timestamp.getMonth() === rn.getMonth() && timestamp.getFullYear() === rn.getFullYear()) {
							revision = latestrevision
						}
					}
				}
			}

			if (!steps) throw new Error("No steps were returned from the update function")
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

	/**
	 * Update a ship
	 * 
	 * @param {any} ship - The data from the Galaxy Info API for the ship
	 * @returns {Promise<string[] | void>}
	 */
	async updateShip(ship: any): Promise<string[] | void> {
		// Setup performance logging
		const steps: string[] = []
		async function step (name: string, prom: Promise<any>) {
			const start = performance.now()
			const returned = await prom
			const end = performance.now()
			steps.push(`${name} ${(end - start).toFixed(2)}ms`)
			return returned
		}

		// Get the page name of the ship we wish to update
		const pageName = await step("getShipPageName", this.getShipPageName(ship))
		
		// Get the wikitext of the ship page after we have found what the page name is. If we can't find the page, we can't update it, so we throw an error.
		const oldWikitext = await step("getArticle", this.getArticle(pageName))
		if (!oldWikitext) throw new Error(`Wikitext for ${pageName} missing`)

		// Parse the wikitext into a data object (which is basically just the infobox with its data in a more readable way), merge the data object with the new data (the new data being data gathed from the API and parsed into the same format as the old data), and then format the data back into wikitext.
		const oldData = await step("parseWikiText", this.parseWikitext(oldWikitext))
		const newData = await step("mergeData", this.mergeData(oldData, ship))

		// Using the new data, format it into wikitext and compare it to the old wikitext. If they're the same, we don't need to update the page, so we throw an error saying they're up to date.
		// The end result should be a new wikitext that is different from the old wikitext with updated data.
		const newWikitext = await step("formatDataIntoWikitext", this.formatDataIntoWikitext(newData, oldWikitext))
		if (newWikitext === oldWikitext) {
			skippedShips.push(ship.title)
			throw new Error("Already up-to-date")
		}

		// If we're not in dryrun mode, we edit the page with the new wikitext and a brief summary.
		if (!dryrun) {
			try {
				await step("editArticle", this.editArticle(pageName, newWikitext, "Automatic Infobox Update", false))
				updatedShips.push(ship.title)
			} catch (err: any) {
				throw new Error("hi edit failed lol " + err.message)
			}
		}

		// We return the time it took to complete each step, so we can log it later if we're interested in performance monitoring.
		return steps
	}

	/**
	 * Get the page name of a ship
	 * 
	 * @param {any} ship - The data from the Galaxy Info API for the ship
	 * @param {any} [error] - Whether or not to throw an error if the page doesn't exist
	 * @returns {Promise<any>}
	 */
	async getShipPageName(ship: any, error?: boolean): Promise<any> {
		// Check if the ship is in the ship list. If it is, return the ship name.
		if (this.galaxypediaShipList.includes(ship.title)) return ship.title
		
		// If the ship isn't in the ship list, check if it's in the ship name map. If it is, return the mapped name.
		const mappedName: string = SHIP_NAME_MAP[ship.title]
		if (mappedName && this.galaxypediaShipList.includes(mappedName)) return mappedName

		// If the page doesn't exist in the ship list at all, but exists after searching the entire site, this could mean that it simply isn't in the Ships category. So we check if it's in the Main category, and if it is, we send a notification to the webhook to look into it.
		const resolveSuspicion = async (shipname: string) => {
			try {
				const page = await this.getArticle(shipname)
				if (page) {
					// If the runcount is a multiple of 5, log the suspicions to the discord webhook. Otherwise only log to the console
					if (this.runcount % 5 === 0) {
						this.logDiscord(`**${shipname}** is not in the Ships category, but is in the Main namespace. Please check if it should be in the Ships category.`)
                    } else {
						console.log(chalk.yellowBright("[?]"), chalk.cyanBright(shipname) + ": " + chalk.yellowBright(`${shipname} is not in the Ships category, but is in the Main namespace. Please check if it should be in the Ships category.`))
					}
				}
			} catch (err: any) {
				throw new Error(chalk.redBright("[!] ") + chalk.cyanBright(shipname) + ": " + chalk.redBright(`Error while resolving suspicion: ${err.message}`))
			}
		}


		// If error is false, we don't want to throw an error, we just want to return undefined. This is used when we're checking if a page exists, and we don't want to throw an error if it doesn't.
		if (error === false) {
			return undefined
		} else { 
			if (mappedName) {
				resolveSuspicion(mappedName)
			} else {
				resolveSuspicion(ship.title)
			}
			throw new Error(`Can't find the page name for ${ship.title}`)
		}
		
	}

	/**
	 * Parse the wikitext of a ship page into a data object
	 * 
	 * @param {any} wikitext - The wikitext of the ship page
	 * @returns {Promise<any>}
	 */
	async parseWikitext(wikitext: any): Promise<any> {
		// This function parses the wikitext into a data object, which is basically just the infobox with its data in a more readable way.

		// First we check if the wikitext has an infobox. If it doesn't, we throw an error.
		const matches = wikitext.match(this.SHIP_INFOBOX_REGEX)
		if (!matches) throw new Error("Could not find infobox!")

		// Then we parse the infobox into a data object using the wiki-text-parser library.
		const data = wikiTextParser.parseTemplate(matches[0]).namedParts

		// We then check if the data object has an image. If it does, we check if the image is a gallery. If it is, we replace the image with the gallery.
		if (data.image && data.image.startsWith("<gallery")) {
			const boo = wikitext.match(/<gallery.*?>.*?<\/gallery>/sg)
			if (boo) {
				data.image = boo[0]
			}
		}
		
		if (verbose) console.log("Ship Data Raw\n" + JSON.stringify(data, null, "\t"))
		return data
	}

	/**
	 * This function can be pretty confusing, so I'm going to give some clarification on what exactly it does.  
	 * First we initialize the data variable. This is what's going to hold our data.  
	 * So you can see we make a function called mergeObjectIn. For now, just ignore it. It will make sense in a second.  
	 * When this function is called, we give it two inputs (look up, you can see it being run with the arguments of (oldData, ship)). The first one is the old data, the next one is the new data that we obtain from the API.  
	 * You can see where we do the for loop where we basically iterate over the objects that we gather in the arguments of this function. Given that we've supplied the correct inputs. We should only be working with two objects.  
	 * The first pass will basically take the old data and input it into the data array. By doing this, we will have all the old data that the API doesn't supply for us. For example like the creator of a ship, something that editors will have manually added to the page.  
	 * By now, the data array will basically have all the old data that is currently present on the page.  
	 * Now what we do in the second pass is we take the data from the API. And we basically go ahead and add new data from the API or overwrite old data with the new data from the API, this is done using the line data[key] = obj[key].  
	 * So let's say that the API supplies us with the shield parameter, but the old data that's currently on the page doesn't have that. This way we will be adding that parameter to the data array. But let's say that the original page did have that parameter. What we will be doing is overwriting the old data with the data that we obtained with the API.  
	 * So yeah, that's basically an explanation of this function, because it's really confusing to understand what's going on here.
	 * 
	 * @summary Merge the data from the API with the data from the page
	 * @param {any[]} objects - The data object of the ship
	 * @returns {Promise<any>}
	 */
	async mergeData(...objects: any[]) {
		const data: any = {}
		function mergeObjectIn (obj: any) {
			for (const key of Object.keys(obj)) {
				// If the value of the parameter is an empty string, we don't want to include it in the final result
				if (obj[key] === "") continue

				data[key] = obj[key]
			}
		}

		// If a parameter is in the list of parameters to exempt but is supplied by the API, remove the parameter from the API data.
		// The reason for this is because exempting the parameters shouldnt result in KetchupBot outright deleting the parameter from the page. It should just not add it to the page if it doesn't already exist.
		for (const parameter of parameters_to_exempt) {
			if (parameter in objects[1]) delete objects[1][parameter]
			console.log("Not adding parameter " + parameter + " to the page because it is in the list of parameters to exempt.")
		}

		// First pass, we merge in the old data to the data array. Second pass, we merge in the new data to the data array. We are left with a json object that has all of the old data, but with the new data overwriting any parameters that need to be updated.
		for (const obj of objects) {
			mergeObjectIn(obj)
		}

		// Sort the data alphabetically, idk how it works but it works lol
		const sorted: any = {}
		const keys: string[] = Object.keys(data).sort((a, b) => a.localeCompare(b))
	
		for (const key of keys) {
			sorted[key] = data[key]
		}

		if (verbose) console.log("Ship Data Merged: ", { objects, sorted })
		return sorted
	}

	async formatDataIntoWikitext(data: any, oldWikitext: string) {
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

// Note to self: Improve documentation for this module
class TurretsUpdater {
	TURRET_TABLE_REGEX!: RegExp
	bot: any
	logChange!: (name: string, revision: { revid: string | number } | null) => void
	logDiscord!: (content: string) => void
	// eslint-disable-next-line @typescript-eslint/ban-types
	getArticle!: Function
	// eslint-disable-next-line @typescript-eslint/ban-types
	editArticle!: Function
	// eslint-disable-next-line @typescript-eslint/ban-types
	getArticleWikitext!: Function
	// eslint-disable-next-line @typescript-eslint/ban-types
	getArticleRevisions!: Function
	currentlyUpdating!: boolean
	async main (bot: any, logChange: (name: string, revision: { revid: string | number } | null) => void, logDiscord: (content: string) => void) {
		this.TURRET_TABLE_REGEX = /{\|\s*class="wikitable sortable".*?\|}/sig
		this.bot = bot
		this.logChange = logChange
		this.logDiscord = logDiscord
		this.getArticle = promisify(this.bot.getArticle.bind(this.bot))
		this.editArticle = promisify(this.bot.edit.bind(bot))
		this.getArticleWikitext = promisify(wikiTextParser.getArticle.bind(wikiTextParser))
		this.getArticleRevisions = promisify(this.bot.getArticleRevisions.bind(this.bot))
		
		cron.schedule("30 * * * *", () => this.updateGalaxypediaTurrets())
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
		if (!process.env.GALAXY_INFO_API) throw new Error("Galaxy Info API not specified in .env file")
		const response = await fetch(`${process.env.GALAXY_INFO_API.trim()}/api/v2/ships-turrets/raw`)
		if (!response.ok) throw new Error("Galaxy Info seems to be down - Turrets")
		const galaxyInfoTurrets = await response.json()
		return galaxyInfoTurrets.serializedTurrets
	}

	async updateTurrets(turretData: any) {
		const turretPageWikitext = await this.getArticleWikitext("Turrets")
		let cum = turretPageWikitext

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

/**
 * Init the NodeMW bot
 * 
 * @param {string} [protocol]
 * @param {string} [server]
 * @param {string} [path]
 * @returns {Promise<NodeMW>}
 */
export async function initBot(protocol?: string, server?: string, path?: string): Promise<NodeMW> {
	const bot = new NodeMW({
		protocol: protocol || "https",
		server: server || "robloxgalaxy.wiki",
		path: path || "/",
		debug: verbose
	})

	const logIn = promisify(bot.logIn.bind(bot)) as (username: string, password: string) => Promise<void>

	try {
		if (!process.env.MW_LOGIN || !process.env.MW_PASS) throw new Error("No login details specified")
		await logIn(process.env.MW_LOGIN, process.env.MW_PASS)
	} catch (error: any) {
		console.error(chalk.red("------------ LOGIN ERROR ------------\n") + error.stack)
		throw new Error("Login failed")
	}

	return bot
}

/** Initialize the logger functions */
export function initLoggers() {
	async function logChange(name: string, revision: { revid: string | number } | null) {
		if (dryrun) return
		if (!process.env.WEBHOOK) throw new Error("No webhook specified")

		await fetch(process.env.WEBHOOK, {
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
		if (!process.env.WEBHOOK) throw new Error("No webhook specified")

		await fetch(process.env.WEBHOOK, {
			method: "POST",
			headers: {
				"Content-Type": "application/json"
			},
			body: JSON.stringify({
				content: content.toString()
			})
		})
	}

	return { logChange, logDiscord }
}

async function printBanner() {
	console.log((await fs.readFile("banner.txt")).toString())
	console.log("Written by smallketchup82 with the help of yname\n---------------------------------")
}
if (require.main === module) {
	(async () => {
		await printBanner()
		
		if (dryrun) {
			console.log(`${chalk.red("[!]")} Dry run is enabled! Halting for 3 seconds, terminate program if unintentional.`)
			await new Promise(resolve => setTimeout(resolve, 3000))
		}

		const bot = await initBot()

		const { logChange, logDiscord } = await initLoggers()

		if (process.env.TURRETSONLY === "false") {
			const shipupdater = new ShipUpdater(bot, logChange, logDiscord)
			await shipupdater.startScheduler()
                        await shipupdater.updateGalaxypediaShips()
		}

		if (process.env.SHIPSONLY === "false") {
			const turretupdater = new TurretsUpdater()
			await turretupdater.main(bot, logChange, logDiscord)
		}
	})()
} else {
	(async () => {
		await printBanner()
		
		if (dryrun) {
			console.log(`${chalk.red("[!]")} Dry run is enabled! Terminate program if unintentional.`)
		}
	})()
}
