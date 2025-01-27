const process = require("process");
const readFile = require("fs").readFileSync;
const { getDirectoryBuildVersion } = require("./version.cjs");

const packageJson = JSON.parse(readFile("package.json", "utf8"));
const expectedVersion = getDirectoryBuildVersion();

if (packageJson.version !== expectedVersion) {
    console.error("Version number does not match Directory.Build.props file, run 'npm run version' first.");

    process.exit(1);
}
