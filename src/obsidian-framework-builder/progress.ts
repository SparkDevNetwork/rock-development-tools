export class ProgressBar {
    private stage: string | undefined;

    private message: string;

    private value: number;
    private totalNumber: number;

    constructor(totalNumber: number, message: string) {
        // this.singleBar = new SingleBar({
        //     format: `{title} [\u001b[38;5;208m{bar}\u001b[0m] {percentage}%`
        // }, Presets.shades_classic);

        this.message = message;
        this.value = 0;
        this.totalNumber = totalNumber;
    }

    public update(value: number): void {
        if (this.value !== value) {
            const lastPercent = Math.floor(this.value / this.totalNumber * 100);
            this.value = value;

            if (Math.floor(this.value / this.totalNumber * 100) !== lastPercent) {
                this.updateBar();
            }
        }
    }

    public increment(): void {
        this.update(this.value + 1);
    }

    public setTotal(totalNumber: number, stage?: string): void {
        this.totalNumber = totalNumber;
        this.stage = stage;

        this.updateBar();
    }

    public success(): void {
        process.stdout.write("\u001b[2K\r");
        logSuccess(this.message);
    }

    public fail(): void {
        process.stdout.write("\u001b[2K\r");
        logFail(this.message);
    }

    private updateBar(): void {
        const msg = this.stage ? `${this.message} [${this.stage}]` : this.message;
        const percent = Math.max(0, Math.min(100, Math.floor(this.value / this.totalNumber * 100)));
        const bar = "\u2588".repeat(Math.floor(percent * 40 / 100));
        const fill = " ".repeat(40 - bar.length);

        process.stdout?.write(`\u001b[2K\r${msg} [\u001b[38;5;208m${bar}\u001b[0m${fill}] ${percent}%`);
    }
}

export class IndeterminateBar {
    private tickPosition: number;
    private message: string;
    private timer?: NodeJS.Timeout;

    private static readonly barString: string = "\u2592\u2593\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2593\u2592";
    private static readonly filledBarWidth: number = this.barString.length;
    private static readonly totalBarWidth: number = 40;
    private static readonly tickDuration: number = 50;

    constructor(message: string) {
        this.tickPosition = -IndeterminateBar.filledBarWidth;
        this.message = message;
    }

    public start(): void {
        if (!this.timer) {
            this.timer = setInterval(() => this.tick(), IndeterminateBar.tickDuration);
        }
    }

    public stop(): void {
        if (this.timer) {
            clearInterval(this.timer);
            this.timer = undefined;

            process.stdout.write("\n");
        }
    }

    private tick(): void {
        // Bar size needs to account for being off screen to left and right.
        let barSize = IndeterminateBar.filledBarWidth;

        if (this.tickPosition < 0) {
            barSize -= Math.abs(this.tickPosition);
        }
        else if (this.tickPosition > (IndeterminateBar.totalBarWidth - IndeterminateBar.filledBarWidth)) {
            barSize = Math.max(0, IndeterminateBar.totalBarWidth - this.tickPosition);
        }

        const preBar = " ".repeat(Math.max(0, Math.min(IndeterminateBar.totalBarWidth, this.tickPosition)));
        const barStrStart = Math.abs(Math.min(0, this.tickPosition));
        let barStr = IndeterminateBar.barString.substring(barStrStart, barStrStart + barSize);
        const postBar = " ".repeat(IndeterminateBar.totalBarWidth - barSize - preBar.length);

        process.stdout.write(`\r${this.message} [${preBar}\u001b[38;5;208m${barStr}\u001b[0m${postBar}]`);

        this.tickPosition++;
        if (this.tickPosition >= IndeterminateBar.totalBarWidth + IndeterminateBar.filledBarWidth) {
            this.tickPosition = -IndeterminateBar.filledBarWidth;
        }
    }

    public success(): void {
        const padLength = (1 + IndeterminateBar.totalBarWidth + 1);
        const pad = " ".repeat(padLength);

        process.stdout.write(`\r${this.message} \u001b[38;5;10m\u2714\u001b[0m${pad}`);

        this.stop();
    }

    public fail(): void {
        const padLength = (1 + IndeterminateBar.totalBarWidth + 1);
        const pad = " ".repeat(padLength);

        process.stdout.write(`\r${this.message} \u001b[38;5;9m\u2715\u001b[0m${pad}`);

        this.stop();
    }
}


export function logSuccess(message: string): void {
    process.stdout.write(`${message} \u001b[38;5;10m\u2714\u001b[0m\n`);
}

export function logFail(message: string): void {
    process.stdout.write(`${message} \u001b[38;5;9m\u2715\u001b[0m\n`);
}
