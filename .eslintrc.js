module.exports = {
	"env": {
		"node": true,
		"commonjs": true,
		"es6": true
	},
	"parser": "@typescript-eslint/parser",
	"plugins": [
		"@typescript-eslint"
	],
	"root": true,
	"extends": [
		"eslint:recommended",
		"plugin:@typescript-eslint/recommended-type-checked",
		"plugin:@typescript-eslint/stylistic-type-checked"
	],
	"parserOptions": {
		"ecmaVersion": "latest",
		"project": true,
		"tsconfigRootDir": __dirname
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
		"@typescript-eslint/no-non-null-assertion": "off",
		"@typescript-eslint/no-explicit-any": "off"
	}
}