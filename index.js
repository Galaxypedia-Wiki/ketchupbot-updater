const WikiTextParser = require('parse-wikitext');
const wikiTextParser = new WikiTextParser('robloxgalaxy.wiki');
const fetch = require('node-fetch');
var nodemw = require('nodemw');
const fs = require('fs');
const chalk = require('chalk');
const { promisify } = require('util');

// Settings
const verbose = process.env.verbose || false;
const regex = /{{\s*Ship[ _]Infobox[ _]Template.*?}}/si;
var interactive = false;
const disc_webhook = 'https://discord.com/api/webhooks/989011701619892285/224ie95dP5VRPH7eNHH56HzMN6iTc5i_tj7bI7_V33ACHhc5TFx8B8zr7lPlOUk1YuA8';

var bot = new nodemw({
	protocol: 'https',
	server: 'robloxgalaxy.wiki',
	path: '/',
	debug: verbose,
});

const getArticle = promisify(bot.getArticle.bind(bot));
const editArticle = promisify(bot.edit.bind(bot));
const getArticleWikitext = promisify(wikiTextParser.getArticle.bind(wikiTextParser));

// Fetch all ship data
async function getAllShipData() {
	const response = await fetch('https://galaxy.wingysam.xyz/api/v2/galaxypedia?token=aanLgKS6HW7fuZVM7khbFf8UfSV6knjT');

	if (!response.ok) {
		return null;
	}

	const data = await response.json();

	return data;
}

// Get an individual ships data
async function getShipData(shipname) {
	var shipdata;

	try {
		shipdata = await getAllShipData();
	} catch {
		console.error(chalk.red('Seems like the host is down, trying to fetch from cache.'));

		if (fs.existsSync('cache.json')) {
			shipdata = JSON.parse(fs.readFileSync('cachejson'));
		} else {
			console.error(chalk.red('There is no cache file, unable to update!'));
			return 'HostDown';
		}
	}

	return shipdata[shipname];
}

async function processShip(shipname, wikitext) {
	const shipdata = await getShipData(shipname);
	const matches = wikitext.match(regex);
	if (!matches) {
		console.log('No infobox found!');
		return 23; // No infoboxes found
	}

	const data = wikiTextParser.parseTemplate(matches[0]).namedParts;
	if (data.image1 && data.image1.startsWith('<gallery')) data.image1 = wikitext.match(/<gallery.*?>.*?<\/gallery>/sg)[0];

	if (verbose) console.log('Ship Data Raw\n' + JSON.stringify(data, null, '\t'));

	for (const dataval of Object.keys(shipdata)) {
		data[dataval] = shipdata[dataval];
	}

	Object.keys(data).forEach(key => { // Remove blank keys
		if (data[key] === '') delete data[key];
	});

	if (verbose) console.log('Ship Data Collected from API\n' + JSON.stringify(shipdata, null, '\t'));
	if (verbose) console.log('Ship Data Merged\n' + JSON.stringify(data, null, '\t'));

	const newCode = wikitext.replace(regex, '{{Ship Infobox Template\n|' + Object.entries(data).map(([key, val]) => {
		if (key.includes('turret')) {
			return `${key} = ${val.toString().replaceAll('\n', '\n\n')}`;
		} else {
			return `${key} = ${val}`;
		}
	}).join('\n|') + '\n}}');
	if (newCode === wikitext) {
		console.log(chalk.green(`${shipname} is up to date!`));
		return 22; // Up to date
	}

	if (verbose) {
		console.log(chalk.blueBright('------------ OLD PAGE WIKITEXT ------------'));
		console.log(wikitext);
		console.log(chalk.blueBright('------------ NEW PAGE WIKITEXT ------------'));
		console.log(newCode);
	}

	console.log(chalk.yellow('Processed ') + chalk.cyanBright(`${shipname}!`));

	return newCode;
}

async function update(ship) {
	const article = await getArticleWikitext(ship);

	const processed = await processShip(ship, article);

	if (processed == 23 || processed == 22) return;

	await editArticle(ship, processed, 'Automatic Infobox Update', false);

	console.log(chalk.green(`Updated ${ship}!`));
	fetch(disc_webhook, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({
		'content': `Updated ${ship}!`,
	}) });

	return processed;
}

bot.logIn('Ketchupbot101', 'Small-Bot123', async (err) => {
	if (err) return err;

	const thankuyname = await getAllShipData();

	// At every hour, cache the ship info to use in case it is down. But also run it once during startup.
	try {
		fs.writeFileSync('cache.json', JSON.stringify(thankuyname, null, '\t'));
		console.log(chalk.blueBright('Cached ship info'));
	} catch {
		console.log(chalk.red('Seems like the host is down!'));
	}
	setInterval(async () => {
		const rn = new Date().getMinutes();

		if (rn === '00') {
			try {
				fs.writeFileSync('cache.json', JSON.stringify(await getAllShipData(), null, '\t'));
				console.log(chalk.blueBright('Cached ship info'));
			} catch {
				console.log(chalk.red('Seems like the host is down!'));
			}
		}

	}, 1000);

	const testarray = ['Bonehawk', 'Stormbringer', 'Leviathan', 'Frostpocalypse', 'Jackal'];

	for (const ship of testarray) {
		const article = await getArticle(ship);
		if (article) {
			console.log(chalk.yellow(`${ship} exists!`));
			await update(ship);
		} else {
			console.log(chalk.yellow(`${ship} does not exist!`));
		}
	}
});