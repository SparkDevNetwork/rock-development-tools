import prompts from "prompts";
import fs from "fs";
import path from "path";
import { Options } from "./types";
import { generateProjects } from "./generator";

const supportedRockVersions = [
    "1.16.0"
];

/**
 * Gets the options that describe the plugin to be created.
 * 
 * @returns An object that contains all the options selected by the user.
 */
async function getOptions(): Promise<Options> {
    const possibleRockWebPaths = [
        "RockWeb",
        path.join("Rock", "RockWeb")
    ];
    let defaultRockWebPath = "";

    for (const p of possibleRockWebPaths) {
        if (fs.existsSync(path.join(p, "web.config"))) {
            defaultRockWebPath = p;
            break;
        }
    }

    const answers = await prompts([
        {
            type: "text",
            name: "organization",
            message: "Organization",
            initial: "Rock Solid Church Demo",
            validate: prev => !!prev
        },
        {
            type: "text",
            name: "orgCode",
            message: "Organization Code",
            initial: (_, vals) => `com.${(vals.organization as string).replace(/ /g, "").toLowerCase()}`,
            validate: prev => !!prev
        },
        {
            type: "text",
            name: "pluginName",
            message: "Plugin Name",
            validate: prev => !!prev
        },
        {
            type: "select",
            name: "rockVersion",
            message: "Target Rock version",
            choices: supportedRockVersions.map(v => {
                return {
                    title: "1.16.0",
                    value: "1.16.0"
                }
            })
        },
        {
            type: "text",
            name: "rockWebPath",
            message: "Path to RockWeb",
            initial: defaultRockWebPath,
            validate(prev) {
                if (typeof prev !== "string") {
                    return false;
                }
                else if (prev === "") {
                    return true;
                }
                else if (fs.existsSync(prev)) {
                    if (fs.existsSync(path.join(prev, "web.config"))) {
                        return true;
                    }
                    else {
                        return "That path does not appear to be a RockWeb path";
                    }
                }
                else {
                    return "Please enter a path to an existing folder";
                }
            }
        },
        {
            type: "confirm",
            name: "createObsidianProject",
            message: "Create Obsidian Project",
            initial: true
        },
        {
            type: (prev, ans) => ans.rockWebPath ? "confirm" : null,
            name: "copyCSharpToRockWeb",
            message: "Copy C# artifacts to RockWeb",
            initial: true
        },
        {
            type: (prev, ans) => ans.rockWebPath ? "confirm" : null,
            name: "copyObsidianToRockWeb",
            message: "Copy Obsidian artifacts to RockWeb",
            initial: true
        }
    ], {
        onCancel() {
            process.exit(1);
        }
    });

    return {
        ...answers,
        rockWebPath: answers.rockWebPath.replace(/[\\/]/g, path.sep),
        pluginCode: answers.pluginName.replace(/ /g, "")
    };
}

function getTemplatesDir(): string {
    if (fs.existsSync(path.resolve(__dirname, "templates"))) {
        return path.resolve(__dirname, "templates");
    }
    else if (fs.existsSync(path.resolve(__dirname, "..", "templates"))) {
        return path.resolve(__dirname, "..", "templates");
    }
    else {
        throw new Error("Unable to find templates.");
    }
}

async function main(): Promise<void> {
    const options = await getOptions();

    generateProjects(getTemplatesDir(), options);
}

main();
