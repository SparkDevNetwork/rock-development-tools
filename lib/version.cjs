const readFile = require("fs").readFileSync;

/**
 * Gets the version number from the Directory.Build.props file.
 * 
 * @returns {string} The version number from the Directory.Build.props file.
 */
function getDirectoryBuildVersion() {
    const directoryBuildXml = readFile("../../Directory.Build.props", "utf8");
    const match = /<Version>(\d+.*)<\/Version>/.exec(directoryBuildXml);

    if (!match) {
        throw new Error("Could not find version number in Directory.Build.props");
    }

    return match[1];
}

module.exports = { getDirectoryBuildVersion };
