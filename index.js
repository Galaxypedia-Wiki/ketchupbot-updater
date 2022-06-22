const WikiTextParser = require('parse-wikitext');
const wikiTextParser = new WikiTextParser('robloxgalaxy.wiki');
const fetch = require('node-fetch');
var nodemw = require('nodemw');
const fs = require('fs');
const chalk = require('chalk');

// Settings
const verbose = true;
const regex = /{{\s*Ship[ _]Infobox[ _]Template.*?}}/si;

var bot = new nodemw({
	protocol: 'https',
	server: 'robloxgalaxy.wiki',
	path: '/',
	debug: verbose,
});

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

	const newCode = wikitext.replace(regex, '{{Ship Infobox Template\n|' + Object.entries(data).map(([key, val]) => `${key} = ${val}`).join('\n|') + '\n}}');
	if (newCode === wikitext) {
		if (verbose) console.log(`${shipname} is up to date!`);
		return 22; // Up to date
	}

	if (verbose) {
		console.log(chalk.blueBright('------------ OLD PAGE WIKITEXT ------------'));
		console.log(wikitext);
		console.log(chalk.blueBright('------------ NEW PAGE WIKITEXT ------------'));
		console.log(newCode);
	}

	console.log(chalk.green('Successfully processed ') + chalk.cyanBright(`${shipname}!`));

	return newCode;
}

async function update(ship) {
	wikiTextParser.getArticle(ship, async function(err, data) {
		if (err) return console.error(err);
		const processed = await processShip(ship, data);

		bot.edit(ship, processed, 'Automatic Infobox Update', false, (err) => {
			if (err) throw 'Unknown error while trying to edit page';
		});

		return processed;
	});
}

const { promisify } = require('util');
const getArticle = promisify(bot.getArticle.bind(bot));

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


	for (const ship in thankuyname) {
		const article = await getArticle(ship);
		if (article) {
			console.log(`${ship} exists!`);
			await update(ship);
		} else {
			console.log(`${ship} does not exist!`);
		}
	}
});