// @ts-check

import eslint from "@eslint/js";
import tseslint from "typescript-eslint";
import prettierConfig from "eslint-config-prettier";

export default tseslint.config(
    eslint.configs.recommended,
    ...tseslint.configs.stylisticTypeChecked,
    ...tseslint.configs.recommendedTypeChecked,
    ...tseslint.configs.strictTypeChecked,
    prettierConfig,
    {
        ignores: ["eslint.config.js"],
        languageOptions: {
            parserOptions: {
                project: "./tsconfig.json",
                tsconfigRootDir: import.meta.dirname,
            },
        },
        rules: {
            "@typescript-eslint/semi": ["error", "always"],
            "@typescript-eslint/naming-convention": [
                "error",
                {
                    selector: "default",
                    format: null,
                },
                {
                    selector: "class",
                    format: ["PascalCase"],
                },
                {
                    selector: "method",
                    format: ["camelCase"],
                },
                {
                    selector: "variable",
                    modifiers: ["const"],
                    format: ["UPPER_CASE"],
                },
                {
                    selector: "variable",
                    format: ["snake_case"],
                },
                {
                    selector: "classProperty",
                    format: ["UPPER_CASE"],
                },
                {
                    selector: "parameter",
                    format: ["camelCase"], 
                }
            ],
        },
    }
);
