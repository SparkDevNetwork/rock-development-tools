import { exec } from "child_process";

export type ProcessStatus = {
    exitCode: number;

    stdout: string;
};

export function execute(command: string, cwd: string): Promise<ProcessStatus> {
    return new Promise<ProcessStatus>((resolve, reject) => {
        const buffers: string[] = [];

        const child = exec(command, {
            cwd
        });

        child.stdout?.on("data", chunk => {
            buffers.push(chunk);
        });

        child.on("error", (err) => {
            reject(err);
        });

        child.on("exit", code => {
            if (code !== null) {
                resolve({
                    exitCode: code,
                    stdout: buffers.join("")
                });
            }
            else {
                reject(new Error("Unknown exit code from child process."));
            }
        });
    });
}
