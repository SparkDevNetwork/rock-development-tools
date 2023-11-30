import simpleGit, { CleanOptions, SimpleGitProgressEvent } from "simple-git";
import { IndeterminateBar, ProgressBar, logSuccess } from "./progress";
import prompts from "prompts";
import path from "path";
import fs from "fs";
import { glob } from "glob";
import { execute } from "./process";
import { PackageJson } from "type-fest";

type RockVersionBranch = {
    prefix: string;

    major: number;

    minor: number;

    patch: number;
};

function rockVersionBranchSorter(a: RockVersionBranch, b: RockVersionBranch): number {
    if (a.major < b.major) {
        return -1;
    }
    else if (a.major > b.major) {
        return 1;
    }
    else {
        if (a.minor < b.minor) {
            return -1;
        }
        else if (a.minor > b.minor) {
            return 1;
        }
        else {
            if (a.patch < b.patch) {
                return -1;
            }
            else if (a.patch > b.patch) {
                return 1;
            }
            else {
                return 0;
            }
        }
    }
}

async function cleanBuild(): Promise<void> {
    const buildPath = path.resolve(path.join(process.cwd(), "build"));

    await fs.promises.rm(buildPath, { recursive: true, force: true });
}

async function downloadRock(version: RockVersionBranch): Promise<void> {
    const buildPath = path.resolve(path.join(process.cwd(), "build"));
    const rockPath = path.resolve(path.join(buildPath, "Rock"));

    if (!fs.existsSync(buildPath)) {
        fs.mkdirSync(buildPath);
    }

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
        `${version.prefix}-${version.major}.${version.minor}.${version.patch}`
    ]);

    bar.success();
}

async function selectRockVersion(): Promise<RockVersionBranch> {
    const remoteData = await simpleGit().listRemote([
        "https://github.com/SparkDevNetwork/Rock",
        "refs/heads/release-*",
        "refs/heads/hotfix-*"
    ]);

    const versions: RockVersionBranch[] = [];
    const regex = /refs\/heads\/(hotfix|release)\-(\d+)\.(\d+)\.(\d+)/gmi;
    let match = regex.exec(remoteData);
    while (match) {
        const version: RockVersionBranch = {
            prefix: match[1],
            major: parseInt(match[2]),
            minor: parseInt(match[3]),
            patch: parseInt(match[4])
        };

        if (version.major > 1 || (version.major === 1 && version.minor >= 16)) {
            versions.push(version);
        }

        match = regex.exec(remoteData);
    }

    if (versions.length === 0) {
        console.error("No release or hotfix branches found.");
        process.exit(1);
    }

    versions.sort(rockVersionBranchSorter).reverse();

    const answers = await prompts([
        {
            type: "select",
            name: "version",
            message: "Build which version of Rock?",
            choices: versions.map(v => ({
                title: `${v.major}.${v.minor}.${v.patch}`,
                value: v
            }))
        }
    ]);

    process.stdout.write("\n");

    return answers.version;
}

async function checkRockVersion(version: RockVersionBranch): Promise<boolean> {
    const buildPath = path.resolve(path.join(process.cwd(), "build"));
    const rockPath = path.resolve(path.join(buildPath, "Rock"));

    if (!fs.existsSync(rockPath)) {
        return false;
    }

    const localBranchResponse = await simpleGit(rockPath).branchLocal();

    return localBranchResponse.current === `${version.prefix}-${version.major}.${version.minor}.${version.patch}`;
}

async function resetRockBranch(): Promise<void> {
    const buildPath = path.resolve(path.join(process.cwd(), "build"));
    const rockPath = path.resolve(path.join(buildPath, "Rock"));

    await simpleGit(rockPath)
        .clean(CleanOptions.FORCE)
        .checkout(".");

    await fs.promises.rm(path.join(rockPath, "Rock.JavaScript.Obsidian", "dist"), {
        recursive: true,
        force: true
    });
}

async function buildObsidian(version: RockVersionBranch): Promise<void> {
    const rockPath = path.resolve(path.join(process.cwd(), "build", "Rock"));
    const obsidianPath = path.join(rockPath, "Rock.JavaScript.Obsidian");

    let indeterminateBar = new IndeterminateBar("Building Rock.JavaScript.Obsidian");
    indeterminateBar.start();

    let statusCode = await execute("npm ci", obsidianPath);
    if (statusCode !== 0) {
        indeterminateBar.fail();
        process.exit(1);
    }

    statusCode = await execute("npm run build-framework", obsidianPath);
    if (statusCode !== 0) {
        indeterminateBar.fail();
        process.exit(1);
    }

    indeterminateBar.success();
}

async function prepareObsidianPackage(version: RockVersionBranch): Promise<void> {
    const frameworkBuildPath = path.join(process.cwd(),
        "build",
        "Rock",
        "Rock.JavaScript.Obsidian",
        "dist",
        "Framework");
    const frameworkPath = path.join(process.cwd(),
        "build",
        "Rock",
        "Rock.JavaScript.Obsidian",
        "Framework");

    const stagingPath = path.join(process.cwd(),
        "build",
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

        if (!fs.existsSync(path.dirname(dest))) {
            await fs.promises.mkdir(path.dirname(dest), { recursive: true });
        }

        await fs.promises.copyFile(src, dest);

        bar.increment();
    }

    // Read the Vue version from the rock project.
    const obsidianPackagePath = path.join(process.cwd(), "build", "Rock", "Rock.JavaScript.Obsidian", "package.json");
    const obsidianPackage = JSON.parse(await fs.promises.readFile(obsidianPackagePath, { encoding: "utf-8" })) as PackageJson;
    const vueVersion = obsidianPackage.dependencies!["vue"]

    // Create the package.json file.
    const templatePath = path.join(process.cwd(), "templates");
    const templateJson = await fs.promises.readFile(path.join(templatePath, "rock-obsidian-framework.json"), {
        encoding: "utf-8"
    });
    const template = JSON.parse(templateJson) as PackageJson;

    template.version = `${version.minor}.${version.patch}.0`;
    template.peerDependencies ??= {};
    template.peerDependencies["vue"] = vueVersion;

    await fs.promises.writeFile(path.join(stagingPath, "package.json"), JSON.stringify(template, undefined, 4));

    // Copy additional template files that don't need translation.
    await fs.promises.copyFile(path.join(templatePath, "tsconfig.base.json"), path.join(stagingPath, "tsconfig.base.json"));

    bar.success();
}

async function createObsidianPackage(): Promise<void> {
    const stagingPath = path.join(process.cwd(),
        "build",
        "rock-obsidian-framework");

    const bar = new IndeterminateBar("Packing rock-obsidian-framework");
    const statusCode = await execute("npm pack", stagingPath);

    if (statusCode !== 0) {
        bar.fail();
        process.exit(1);
    }

    bar.success();
}

async function main(): Promise<void> {
    const rockVersion = await selectRockVersion();
    const rockPath = path.resolve(path.join(process.cwd(), "build", "Rock"));

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

    await buildObsidian(rockVersion);
    await prepareObsidianPackage(rockVersion);
    await createObsidianPackage();
}

main();
