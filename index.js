require('dotenv').config()

const WikiTextParser = require('parse-wikitext')
const wikiTextParser = new WikiTextParser('robloxgalaxy.wiki')
const fetch = require('node-fetch')
const NodeMW = require('nodemw')
const chalk = require('chalk')
const { promisify } = require('util')
const cron = require('node-cron')
const fs = require('fs/promises')
const { performance } = require('perf_hooks')

// Settings
const verbose = process.env.VERBOSE === 'true'
const SHIP_INFOBOX_TEMPLATE_REGEX = /{{\s*Ship[ _]Infobox[ _]Template.*?}}/si

const SHIP_NAME_MAP = {
  2018: '2018 Ship',
  yname: 'Yname (ship)'
}

class GalaxypediaUpdater {
  async main (username, password) {
    this.bot = new NodeMW({
      protocol: 'https',
      server: 'robloxgalaxy.wiki',
      path: '/',
      debug: verbose
    })

    this.getArticle = promisify(this.bot.getArticle.bind(this.bot))
    this.editArticle = promisify(this.bot.edit.bind(this.bot))
    this.getArticleWikitext = promisify(wikiTextParser.getArticle.bind(wikiTextParser))
    this.logIn = promisify(this.bot.logIn.bind(this.bot))

    await this.logIn(username, password)
    cron.schedule('0 * * * *', () => this.updateGalaxypediaShips())
    this.updateGalaxypediaShips()
  }

  async updateGalaxypediaShips () {
    try {
      if (this.currentlyUpdating) {
        console.log(`${chalk.redBright('[!]')} Already updating ships; not updating`)
        return
      }
      this.currentlyUpdating = true

      this.shipsData = await this.getShipsData()
      this.galaxypediaShipList = await this.getGalaxypediaShipList()
      await this.updateShips()
    } catch (error) {
      console.error(error)
    }

    this.currentlyUpdating = false
  }

  async getShipsData () {
    const response = await fetch(`https://galaxy.wingysam.xyz/api/v2/galaxypedia?token=${process.env.GALAXY_INFO_TOKEN}`)
    if (!response.ok) throw new Error('Galaxy Info seems to be down.')
    const galaxyInfoShips = await response.json()
    return galaxyInfoShips
  }

  async getGalaxypediaShipList () {
    const response = await fetch('https://robloxgalaxy.wiki/api.php?action=query&format=json&list=categorymembers&cmtitle=Category%3AShips&cmlimit=5000')
    if (!response.ok) throw new Error('Galaxypedia appears to be down.')

    const galaxypediaPageList = (await response.json()).query.categorymembers
      .map(page => page.title)
    const shipsList = galaxypediaPageList
      .filter(pageName => !pageName.startsWith('Category:'))

    return shipsList
  }

  async updateShips () {
    for (const shipName of Object.keys(this.shipsData).sort()) {
      await this.handleShip(this.shipsData[shipName])
    }
    console.log(chalk.greenBright('Complete!'))
  }

  async handleShip (ship) {
    if (process.env.SHIP && ship.title !== process.env.SHIP) return
    try {
      console.log(`${chalk.yellow('Processing ')} ${chalk.cyanBright(ship.title)}...`)
      const steps = await this.updateShip(ship)
      const perf = verbose ? ` perf: ${steps.join(', ')}` : ''
      console.log(`${chalk.green('Updated')} ${chalk.cyanBright(ship.title)}!` + perf)
      await this.logChange(ship.title)
    } catch (error) {
      console.log(`${chalk.red('[!]')} ${chalk.cyanBright(ship.title)}: ${chalk.red(error.message)}`)
    }
  }

  async updateShip (ship) {
    const steps = []
    async function step (name, prom) {
      const start = performance.now()
      const returned = await prom
      const end = performance.now()
      steps.push(`${name} ${(end - start).toFixed(2)}ms`)
      return returned
    }

    const pageName = await step('getShipPageName', this.getShipPageName(ship))
    const oldWikitext = await step('getArticle', this.getArticle(pageName))
    if (!oldWikitext) throw new Error(`Wikitext for ${pageName} missing`)

    const oldData = await step('parseWikiText', this.parseWikitext(oldWikitext))
    const newData = await step('mergeData', this.mergeData(oldData, ship))

    const newWikitext = await step('formatDataIntoWikitext', this.formatDataIntoWikitext(newData, oldWikitext))
    if (newWikitext === oldWikitext) throw new Error('Already up-to-date')

    await step('editArticle', this.editArticle(pageName, newWikitext, 'Automatic Infobox Update', false))
    return steps
  }

  async getShipPageName (ship) {
    if (this.galaxypediaShipList.includes(ship.title)) return ship.title
    const mappedName = SHIP_NAME_MAP[ship.title]
    if (mappedName && this.galaxypediaShipList.includes(mappedName)) return mappedName
    throw new Error(`Can't find page name for ${ship.title}`)
  }

  async parseWikitext (wikitext) {
    const matches = wikitext.match(SHIP_INFOBOX_TEMPLATE_REGEX)
    if (!matches) throw new Error('Could not find infobox wikitext')

    const data = wikiTextParser.parseTemplate(matches[0]).namedParts
    if (data.image1 && data.image1.startsWith('<gallery')) data.image1 = wikitext.match(/<gallery.*?>.*?<\/gallery>/sg)[0]

    if (verbose) console.log('Ship Data Raw\n' + JSON.stringify(data, null, '\t'))

    return data
  }

  async mergeData (...objects) {
    const data = {}
    function mergeObjectIn (obj) {
      for (const key of Object.keys(obj)) {
        if (obj[key] === '') continue
        data[key] = obj[key]
      }
    }

    for (const obj of objects) {
      mergeObjectIn(obj)
    }

    if (verbose) console.log('Ship Data Merged: ', { objects, data })
    return data
  }

  async formatDataIntoWikitext (data, oldWikitext) {
    const newWikitext = oldWikitext.replace(SHIP_INFOBOX_TEMPLATE_REGEX, '{{Ship Infobox Template\n|' + Object.entries(data).map(([key, val]) => `${key} = ${val}`).join('\n|') + '\n}}')

    if (verbose) {
      console.log(chalk.blueBright('------------ OLD PAGE WIKITEXT ------------'))
      console.log(oldWikitext)
      console.log(chalk.blueBright('------------ NEW PAGE WIKITEXT ------------'))
      console.log(newWikitext)
    }
    return newWikitext
  }

  async logChange (shipName) {
    await fetch(process.env.WEBHOOK, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        content: `Updated **${shipName}**!`
      })
    })
  }
}

;(async () => {
  console.log((await fs.readFile('banner.txt')).toString())
  const galaxypediaUpdater = new GalaxypediaUpdater()
  await galaxypediaUpdater.main(process.env.MW_LOGIN, process.env.MW_PASS)
})()
