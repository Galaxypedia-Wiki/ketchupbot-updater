module.exports = {
	"env": {
		"node": true,
		"commonjs": true,
		"es6": true
	},
	"extends": ["eslint:recommended", "plugin:@typescript-eslint/recommended"],
	"parser": "@typescript-eslint/parser",
	"plugins": ["@typescript-eslint"],
	"root": true,
	"parserOptions": {
		"ecmaVersion": "latest"
	},
	"rules": {
		"quotes": [
			"error",
			"double"
		],
		"semi": [
			"error",
			"never"
		],
		"@typescript-eslint/no-explicit-any": "off"
	}
}
