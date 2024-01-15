module.exports = {
    root: true,
    parser: "vue-eslint-parser",
    plugins: [
        "@typescript-eslint",
    ],
    extends: [
        "eslint:recommended",
        "plugin:@typescript-eslint/recommended",
        "@blueboxmoon/eslint-config-rock-recommended"
    ],
    env: {
        browser: true,
        amd: true
    },
    parserOptions: {
        parser: "@typescript-eslint/parser",
        ecmaVersion: 6,
        sourceType: "module"
    }
};
