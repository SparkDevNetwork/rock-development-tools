const { getDirectoryBuildVersion } = require("./version.cjs");
const readFile = require("fs").readFileSync;
const writeFile = require("fs").writeFileSync;

const packageJson = JSON.parse(readFile("package.json", "utf8"));

packageJson.version = getDirectoryBuildVersion();

writeFile("package.json", JSON.stringify(packageJson, null, 4) + "\n");
