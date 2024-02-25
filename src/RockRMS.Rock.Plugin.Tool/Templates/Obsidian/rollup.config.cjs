const path = require("path");
const { defineConfigs } = require("@rockrms/obsidian-build-tools");

const workspacePath = path.resolve(__dirname);
const srcPath = path.join(workspacePath, "src");
const outPath = path.join(workspacePath, "dist");
const pluginsPath = path.join(workspacePath, "{{ RockWebPath | replace:'\\','/' }}", "Plugins", "{{ OrganizationCode }}", "{{ PluginCode }}");

const configs = [
    ...defineConfigs(srcPath, outPath, {
        {% if Copy != true %}// {% endif %}copy: pluginsPath
    })
];

module.exports = configs;
