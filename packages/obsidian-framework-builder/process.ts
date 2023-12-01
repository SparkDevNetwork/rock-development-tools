import { exec } from "child_process";

export function execute(command: string, cwd: string): Promise<number> {
    return new Promise<number>((resolve, reject) => {
        const child = exec(command, {
            cwd
        });

        child.on("error", (err) => {
            reject(err);
        });

        child.on("exit", code => {
            if (code !== null) {
                resolve(code);
            }
            else {
                reject(new Error("Unknown exit code from child process."));
            }
        });
    });

}
