import path from "path";
import fs from "fs";
import { get } from "follow-redirects/https";
import { execFile } from "child_process";
import Stream from "stream";

/**
 * Gets the cache directory to use for storing downloads and other items that
 * will be needed between different runs.
 * 
 * @returns A string that represents the path to the cache directory.
 */
function getCacheDirectory(): string {
    const cacheDirectory = path.resolve(path.join(__dirname, ".cache"));

    if (!fs.existsSync(cacheDirectory)) {
        fs.mkdirSync(cacheDirectory);
    }

    return cacheDirectory;
}

/**
 * Downloads a URL into memory and returns a buffer to the response data.
 * 
 * @param url The URL to be downloaded.
 * 
 * @returns A buffer that represents the raw byte data returned in the body of the response.
 */
async function download(url: string): Promise<Buffer> {
    return new Promise((resolve, reject) => {
        const request = get(url, response => {
            if (response.statusCode !== 200) {
                return reject(new Error(`Status Code ${response.statusCode} received.`));
            }


            const body: Uint8Array[] = [];
            response.on("data", chunk => body.push(chunk));

            response.on("end", () => {
                try {
                    resolve(Buffer.concat(body));
                }
                catch (error) {
                    reject(error);
                }
            });
        });

        request.on("error", reject);
        request.end();
    });
}

/**
 * Downloads a URL into a file on disk.
 * 
 * @param url The URL to be downloaded.
 * @param path The path to save the downloaded file to.
 */
async function downloadFile(url: string, path: string): Promise<void> {
    if (!fs.existsSync(path)) {
        const buffer = await download(url);

        fs.writeFileSync(path, buffer);
    }
}

/**
 * Executes a program and returns the exit code after it completes.
 * 
 * @param executable The path to the file to be executed.
 * @param options Optional settings that configure how the file is executed.
 * 
 * @returns The exit code of the executable.
 */
async function execute(executable: string, options: { cwd?: string, arguments?: string[], stderr?: Stream.Writable | null, stdout?: Stream.Writable | null }): Promise<number> {
    return new Promise<number>((resolve, reject) => {
        const childProcess = execFile(executable, options?.arguments, {
            cwd: options.cwd
        });

        if (options?.stderr !== null) {
            childProcess.stderr?.pipe(options?.stderr ?? process.stderr);
        }
        if (options?.stdout !== null) {
            childProcess.stdout?.pipe(options?.stdout ?? process.stdout);
        }

        childProcess.on("exit", code => resolve(code ?? -1));
        childProcess.on("error", error => reject(error));
    });
}

/**
 * Gets the path and filename that can be used to execute msbuild.
 * 
 * @returns The path to the msbuild.exe executable.
 */
async function getMSBuildPath(): Promise<string> {
    const vswhereExecutable = path.join(getCacheDirectory(), "vswhere.exe");

    await downloadFile("https://github.com/microsoft/vswhere/releases/download/3.1.7/vswhere.exe", vswhereExecutable);

    const stdoutStream = new StreamWritableString();
    const returnValue = await execute(vswhereExecutable, {
        arguments: ["-latest", "-requires", "Microsoft.Component.MSBuild", "-find", "MSBuild\\**\\Bin\\MSBuild.exe"],
        stdout: stdoutStream
    });

    if (returnValue !== 0) {
        throw new Error("Unable to find Visual Studio installation.");
    }

    return stdoutStream.text().trim();
}

/**
 * Executes the msbuild process with the given arguments.
 * 
 * @param args The arguments to pass to msbuild.
 * @param options Additional options that describe how the process should be executed.
 * 
 * @returns `true` if the process was successful.
 */
export async function msbuild(args: string[], options?: { cwd?: string }): Promise<boolean> {
    const msbuildExecutable = await getMSBuildPath();
    const stdoutStream = new StreamWritableString();

    const exitCode = await execute(msbuildExecutable, {
        cwd: options?.cwd,
        arguments: args,
        stdout: stdoutStream,
        stderr: null
    });

    if (exitCode !== 0) {
        process.stdout.write(stdoutStream.text());
    }

    return exitCode === 0;
}

/**
 * Executes the nuget process with the given arguments.
 * 
 * @param args The arguments to pass to nuget.
 * @param options Additional options that describe how the process should be executed.
 * 
 * @returns `true` if the process was successful.
 */
export async function nuget(args: string[], options?: { cwd?: string }): Promise<boolean> {
    const nugetExecutable = path.join(getCacheDirectory(), "nuget.exe");
    const stdoutStream = new StreamWritableString();

    await downloadFile("https://dist.nuget.org/win-x86-commandline/latest/nuget.exe", nugetExecutable);

    const exitCode = await execute(nugetExecutable, {
        cwd: options?.cwd,
        arguments: args,
        stdout: stdoutStream,
        stderr: null
    });

    if (exitCode !== 0) {
        process.stdout.write(stdoutStream.text());
    }

    return exitCode === 0;
}

/**
 * Custom writable that allows us to capture standard output into a simple string.
 */
class StreamWritableString extends Stream.Writable {
    private data: Uint8Array[] = [];

    _write(chunk: any, encoding: BufferEncoding, callback: (error?: Error | null | undefined) => void): void {
        this.data.push(chunk);
        callback();
    }

    public text(): string {
        return Buffer.concat(this.data).toString("utf8");
    }
}
