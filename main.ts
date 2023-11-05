import simpleGit, { CleanOptions, SimpleGitProgressEvent } from "simple-git";
import { IndeterminateBar, ProgressBar, logSuccess } from "./progress";
import { exec } from "child_process";
import prompts from "prompts";
import path from "path";
import fs from "fs";
import { resolveProjectReferencePath } from "typescript";
import { execute } from "./process";

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
            bar.message = `Downloading Rock [${ev.stage === "remote:" ? "remote" : ev.stage}]`;
            lastStage = ev.stage;
            bar.setTotal(ev.total);
        }

        bar.update(ev.processed);
    }

    await simpleGit({ progress }).clone("https://github.com/SparkDevNetwork/Rock", rockPath, [
        "--depth",
        "1",
        "--branch",
        `${version.prefix}-${version.major}.${version.minor}.${version.patch}`
    ]);

    bar.stop();
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

    versions.sort(rockVersionBranchSorter);

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

async function main(): Promise<void> {
    const rockVersion = await selectRockVersion();
    const rockPath = path.resolve(path.join(process.cwd(), "build", "Rock"));

    if (await checkRockVersion(rockVersion)) {
        await resetRockBranch();
        logSuccess("Existing Rock version");
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
}

main();
