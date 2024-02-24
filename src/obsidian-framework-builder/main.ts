import simpleGit, { CleanOptions, SimpleGitProgressEvent } from "simple-git";
import { IndeterminateBar, ProgressBar, logSuccess } from "./progress";
import prompts from "prompts";
import path from "path";
import fs from "fs";
import { glob } from "glob";
import { execute } from "./process";
import { PackageJson } from "type-fest";
import { msbuild, nuget } from "./vs";
import { Liquid } from "liquidjs";
import semver, { SemVer } from "semver";

const buildPath = path.resolve(__dirname, ".staging");
const templatesPath = path.resolve(__dirname, "templates");
const distPath = path.resolve(__dirname, "dist");
const engine = new Liquid();

type RockVersionBranch = {
    commit: string;

    tag: string;

    semver: SemVer;
};

function ensureDirectory(directoryPath: string): void {
    if (!fs.existsSync(directoryPath)) {
        fs.mkdirSync(directoryPath, { recursive: true });
    }
}

async function cleanBuild(): Promise<void> {
    await fs.promises.rm(buildPath, { recursive: true, force: true });
}

async function downloadRock(version: RockVersionBranch): Promise<void> {
    const rockPath = path.resolve(path.join(buildPath, "Rock"));

    ensureDirectory(buildPath);

    let bar: ProgressBar = new ProgressBar(1, "Downloading Rock");
    let lastStage = "";

    function progress(ev: SimpleGitProgressEvent): void {
        if (ev.stage !== lastStage) {
            lastStage = ev.stage;
            bar.setTotal(ev.total, ev.stage === "remote:" ? "remote" : ev.stage);
        }

        bar.update(ev.processed);
    }

    await simpleGit({ progress }).clone("https://github.com/SparkDevNetwork/Rock", rockPath, [
        "--depth",
        "1",
        "--branch",
        version.tag
    ]);

    bar.success();
}

function splitVersionSuffix(suffix: string): string {
    const components: string[] = [];
    let component = "";

    for (let i = 0; i < suffix.length; i++) {
        const chunk = suffix.substring(i, i + 1);

        if (chunk === "." || chunk === "-") {
            if (component.length > 0) {
                components.push(component);
            }
            component = "";
        }
        else if (component.length === 0) {
            component = chunk;
        }
        else {
            if (/^[0-9]/.test(component) === /^[0-9]/.test(chunk)) {
                component = `${component}${chunk}`;
            }
            else {
                components.push(component);
                component = chunk;
            }
        }
    }

    if (component.length > 0) {
        components.push(component);
    }

    return components.join(".");
}

async function selectRockVersion(): Promise<RockVersionBranch> {
    const remoteData = await simpleGit().listRemote([
        "https://github.com/SparkDevNetwork/Rock",
        "refs/tags/[0-9]*.*"
    ]);

    const versions: RockVersionBranch[] = [];
    const regex = /([a-f0-9]+)\s+refs\/tags\/((\d+)\.(\d+)\.(\d+)([\.-].*)?)/gmi;
    let match = regex.exec(remoteData);
    while (match) {
        // Skip older versions that are not supported.
        if (parseInt(match[3]) < 1 || (parseInt(match[3]) === 1 && parseInt(match[4]) < 16)) {
            match = regex.exec(remoteData);
            continue;
        }

        const versionString = match[6] === undefined
            ? `${match[3]}.${match[4]}.${match[5]}`
            : `${match[3]}.${match[4]}.${match[5]}-rc.${splitVersionSuffix(match[6])}`;

        const ver = semver.parse(versionString);

        if (!ver) {
            match = regex.exec(remoteData);
            continue;
        }

        const version: RockVersionBranch = {
            commit: match[1],
            tag: match[2],
            semver: ver
        };

        console.log(version);
        versions.push(version);

        match = regex.exec(remoteData);
    }

    if (versions.length === 0) {
        console.error("No release or hotfix branches found.");
        process.exit(1);
    }

    versions.sort((a, b) => semver.compareBuild(a.semver, b.semver)).reverse();

    const answers = await prompts([
        {
            type: "select",
            name: "version",
            message: "Build which version of Rock",
            choices: versions.map(v => ({
                title: v.semver.version,
                value: v
            }))
        },
        {
            type: "text",
            name: "prerelease",
            message: "Pre-release suffix",
            initial(prev, values) { return values.version.semver.prerelease.join("."); }
        }
    ]);

    process.stdout.write("\n");

    if (!answers.version) {
        process.exit(1);
    }

    const selectedVersion: RockVersionBranch = answers.version;
    let finalVersionString = `${selectedVersion.semver.major}.${selectedVersion.semver.minor}.${selectedVersion.semver.patch}`;

    if (answers.prerelease !== "") {
        const tmpVersion = `0.0.0-${answers.prerelease}`;
        finalVersionString = `${finalVersionString}-${semver.prerelease(tmpVersion)?.join(".")}`;
    }

    const finalSemVer = semver.parse(finalVersionString);

    if (!finalSemVer) {
        process.stderr.write(`Unable to parse final version number '${finalVersionString}'.\n`);
        process.exit(1);
    }

    const finalVersion: RockVersionBranch = {
        commit: answers.version.commit,
        tag: answers.version.tag,
        semver: finalSemVer
    };

    return finalVersion;
}

async function checkRockVersion(version: RockVersionBranch): Promise<boolean> {
    const rockPath = path.resolve(path.join(buildPath, "Rock"));

    if (!fs.existsSync(rockPath)) {
        return false;
    }

    const currentCommit = await simpleGit(rockPath).revparse("HEAD");

    return currentCommit === version.commit;
}

async function resetRockBranch(): Promise<void> {
    const rockPath = path.resolve(path.join(buildPath, "Rock"));

    await simpleGit(rockPath)
        .clean(CleanOptions.FORCE)
        .checkout(".");

    await fs.promises.rm(path.join(rockPath, "Rock.JavaScript.Obsidian", "dist"), {
        recursive: true,
        force: true
    });
}

async function buildProject(project: string): Promise<void> {
    const rockPath = path.resolve(path.join(buildPath, "Rock"));
    const projectPath = path.join(rockPath, project);

    const indeterminateBar = new IndeterminateBar(`Building ${project}`);
    indeterminateBar.start();

    const result = await msbuild([`${project}.csproj`, "/p:Configuration=Release", "/nr:false"], { cwd: projectPath });

    if (!result) {
        indeterminateBar.fail();
        process.exit(1);
    }

    indeterminateBar.success();
}

async function buildProjects(projects: string[]): Promise<void> {
    const rockPath = path.resolve(path.join(buildPath, "Rock"));

    const indeterminateBar = new IndeterminateBar("Restoring NuGet packages");
    indeterminateBar.start();

    const status = await nuget(["restore", "Rock.sln"], { cwd: rockPath });

    if (!status) {
        indeterminateBar.fail();
        process.exit(1);
    }

    indeterminateBar.success();

    for (const project of projects) {
        await buildProject(project);
    }
}

async function buildObsidian(): Promise<void> {
    const rockPath = path.resolve(path.join(buildPath, "Rock"));
    const obsidianPath = path.join(rockPath, "Rock.JavaScript.Obsidian");

    const indeterminateBar = new IndeterminateBar("Building Rock.JavaScript.Obsidian");
    indeterminateBar.start();

    let result = await execute("npm ci", obsidianPath);
    if (result.exitCode !== 0) {
        indeterminateBar.fail();
        process.exit(1);
    }

    result = await execute("npm run build-framework", obsidianPath);
    if (result.exitCode !== 0) {
        indeterminateBar.fail();
        process.exit(1);
    }

    indeterminateBar.success();
}

async function prepareObsidianPackage(version: RockVersionBranch): Promise<void> {
    const frameworkBuildPath = path.join(buildPath,
        "Rock",
        "Rock.JavaScript.Obsidian",
        "dist",
        "Framework");
    const frameworkPath = path.join(buildPath,
        "Rock",
        "Rock.JavaScript.Obsidian",
        "Framework");

    const stagingPath = path.join(buildPath,
        "rock-obsidian-framework");

    // Remove the old staging path and create it as empty.
    await fs.promises.rm(stagingPath, {
        recursive: true,
        force: true
    });
    await fs.promises.mkdir(stagingPath, { recursive: true });

    const bar = new ProgressBar(1, "Preparing to package rock-obsidian-framework");

    let files: { src: string, target: string }[] = [];

    // Get the built files, except the Libs files since those are internal
    // to Rock and should not be used.
    files = files.concat(
        (await glob("**/*.d.ts", { cwd: frameworkBuildPath }))
            .filter(f => !f.startsWith(`Libs${path.sep}`))
            .map(f => {
                return {
                    src: path.join(frameworkBuildPath, f),
                    target: f
                };
            })
    );

    // Get the Types and ViewModels files that aren't copied into the
    // distribution folder but should be part of the package.
    files = files.concat(
        (await glob(["ViewModels/**/*.d.ts", "Types/**/*.d.ts"], { cwd: frameworkPath }))
            .map(f => {
                return {
                    src: path.join(frameworkPath, f),
                    target: f
                };
            })
    );

    if (files.length === 0) {
        bar.fail();
        process.stderr?.write("No files were found, perhaps the build failed.\n");
        process.exit(1);
    }

    bar.setTotal(files.length);

    for (const file of files) {
        const src = file.src;
        const dest = path.join(stagingPath, "types", file.target);

        ensureDirectory(path.dirname(dest));

        await fs.promises.copyFile(src, dest);

        bar.increment();
    }

    // Read the Vue version from the rock project.
    const obsidianPackagePath = path.join(buildPath, "Rock", "Rock.JavaScript.Obsidian", "package.json");
    const obsidianPackage = JSON.parse(await fs.promises.readFile(obsidianPackagePath, { encoding: "utf-8" })) as PackageJson;
    const vueVersion = obsidianPackage.dependencies!["vue"]

    // Get the version number to write.
    const packageVersion = version.semver.version;

    // Create the package.json file.
    const templateJson = await fs.promises.readFile(path.join(templatesPath, "rock-obsidian-framework.json"), {
        encoding: "utf-8"
    });
    const template = JSON.parse(templateJson) as PackageJson;

    template.version = packageVersion;
    template.peerDependencies ??= {};
    template.peerDependencies["vue"] = vueVersion;

    await fs.promises.writeFile(path.join(stagingPath, "package.json"), JSON.stringify(template, undefined, 4));

    // Copy additional template files that don't need translation.
    await fs.promises.copyFile(path.join(templatesPath, "tsconfig.base.json"), path.join(stagingPath, "tsconfig.base.json"));

    bar.success();
}

async function copyTextTemplate(source: string, destination: string, version: RockVersionBranch): Promise<void> {
    const rawText = await fs.promises.readFile(source, "utf8");
    let rockVersion = version.semver.version;

    const text = await engine.parseAndRender(rawText, { rockVersion });

    await fs.promises.writeFile(destination, text);
}

async function prepareNugetPackages(version: RockVersionBranch, projects: string[]): Promise<void> {
    await fs.promises.copyFile(path.join(templatesPath, "Icon.png"), path.join(buildPath, "Icon.png"));
    await fs.promises.copyFile(path.join(templatesPath, "LICENSE.md"), path.join(buildPath, "LICENSE.md"));

    for (const project of projects) {
        await copyTextTemplate(path.join(templatesPath, `${project}.nuspec`), path.join(buildPath, `${project}.nuspec`), version);
    }
}

async function createObsidianPackage(): Promise<void> {
    const stagingPath = path.join(buildPath,
        "rock-obsidian-framework");

    ensureDirectory(distPath);

    const bar = new IndeterminateBar("Packing rock-obsidian-framework");
    bar.start();

    const distRelPath = path.relative(stagingPath, distPath);
    const result = await execute(`npm pack --pack-destination "${distRelPath}"`, stagingPath);

    if (result.exitCode !== 0) {
        bar.fail();
        process.exit(1);
    }

    bar.success();
}

async function createNugetPackage(project: string): Promise<void> {
    const bar = new IndeterminateBar(`Packing ${project}`);

    bar.start();

    const result = await nuget([
        "pack",
        `${project}.nuspec`,
        "-OutputDirectory",
        distPath
    ], { cwd: buildPath });

    if (!result) {
        bar.fail();
        process.exit(1);
    }

    bar.success();
}

async function createNugetPackages(projects: string[]): Promise<void> {
    for (const project of projects) {
        await createNugetPackage(project);
    }
}

async function main(): Promise<void> {
    const rockVersion = await selectRockVersion();
    const rockPath = path.resolve(path.join(buildPath, "Rock"));

    if (await checkRockVersion(rockVersion)) {
        await resetRockBranch();
        logSuccess("Existing Rock download check");
    }
    else {
        if (fs.existsSync(rockPath)) {
            const cleanupBar = new IndeterminateBar("Cleaning up previous build");
            cleanupBar.start();
            await cleanBuild();
            cleanupBar.success();
        }

        await downloadRock(rockVersion);
    }

    await buildProjects(["Rock.Enums", "Rock.ViewModels", "Rock.Common", "Rock.Lava.Shared", "Rock"]);
    await buildObsidian();

    await prepareNugetPackages(rockVersion, ["Rock.Enums", "Rock.ViewModels", "Rock.Common", "Rock.Lava.Shared", "Rock"]);
    await prepareObsidianPackage(rockVersion);

    await createNugetPackages(["Rock.Enums", "Rock.ViewModels", "Rock.Common", "Rock.Lava.Shared", "Rock"]);
    await createObsidianPackage();
}

main();
