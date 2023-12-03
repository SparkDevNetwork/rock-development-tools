import { Options } from "./types";
import fs from "fs";
import path from "path";
import { Liquid } from "liquidjs";

const engine = new Liquid();

/**
 * Copies a template file to a destination after replacing any tokens.
 * 
 * @param source The source path components inside the templates directory.
 * @param destination The destination path components relative to current directory.
 * @param options The options provided by the user.
 */
function copyTemplate(source: string[], destination: string[], options: Options): void {
    const templatePath = path.join(...source);
    const rawContent = fs.readFileSync(templatePath, { encoding: "utf8" });
    const rockWebPathPrefix: string[] = [];

    // The RockWeb path is relative to the current directory. So we need to
    // adjust it to be relative to the sub-directory the indicated by the
    // destination parameter.
    for (let i = 1; i < destination.length; i++) {
        rockWebPathPrefix.push("..");
    }

    const content = engine.parseAndRenderSync(rawContent, options);
    
    fs.writeFileSync(path.join(...destination), content);
}

/**
 * Creates the C# project and all related files.
 * 
 * @param templatesDir The directory that contains all the templates.
 * @param directory The directory to use for the CSharp project.
 * @param options The options entered by the user.
 */
function createCSharpProject(templatesDir: string, directory: string, options: Options): void {
    const projectFilename = `${options.orgCode}.${options.pluginCode}.csproj`;

    fs.mkdirSync(directory);

    copyTemplate([templatesDir, "csharp", "project.csproj"], [directory, projectFilename], options);
    copyTemplate([templatesDir, "csharp", "class1.cs"], [directory, "Class1.cs"], options);
}

/**
 * Creates the Obsidian project and all related files.
 * 
 * @param templatesDir The directory that contains all the templates.
 * @param directory The directory to use for the Obsidian project.
 * @param options The options entered by the user.
 */
function createObsidianProject(templatesDir: string, directory: string, options: Options): void {
    const projectFilename = `${options.orgCode}.${options.pluginCode}.Obsidian.esproj`;

    fs.mkdirSync(directory);

    copyTemplate([templatesDir, "obsidian", "project.esproj"], [directory, projectFilename], options);
    copyTemplate([templatesDir, "obsidian", "package.json"], [directory, "package.json"], options);
}

/**
 * Generates all projects specified by the options.
 * 
 * @param templatesDir The directory that contains all the templates.
 * @param options The options entered by the user.
 */
export function generateProjects(templatesDir: string, options: Options): void {
    const csharpDirectory = `${options.orgCode}.${options.pluginCode}`;
    const obsidianDirectory = `${options.orgCode}.${options.pluginCode}.Obsidian`;

    if (fs.existsSync(csharpDirectory)) {
        process.stdout.write(`Directory ${csharpDirectory} already exists, aborting.\n`);
        process.exit(1);
    }

    if (options.createObsidianProject && fs.existsSync(obsidianDirectory)) {
        process.stdout.write(`Directory ${obsidianDirectory} already exists, aborting.\n`);
        process.exit(1);
    }

    createCSharpProject(templatesDir, csharpDirectory, options);
    
    if (options.createObsidianProject) {
        createObsidianProject(templatesDir, obsidianDirectory, options);
    }
}
