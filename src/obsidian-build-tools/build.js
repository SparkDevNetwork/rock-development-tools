#!/usr/bin/env node

const path = require("path");
const process = require("process");
const rollup = require("rollup");
const { defineConfigs } = require("./index");

function clearScreen() {
    process.stdout.write("\u001bc");
}

function red(text) {
    return `\u001b[31m${text}\u001b[39m`;
}

function green(text) {
    return `\u001b[32m${text}\u001b[39m`;
}

function dim(text) {
    return `\u001b[2m${text}\u001b[22m`;
}

function displayError(error) {
    let description = error.message || error;
    if (error.name) {
        description = `${error.name}: ${description}`;
    }

    if (error.plugin) {
        description = `(plugin ${error.plugin}) ${description}`;
    }

    process.stderr.write(`${red(`[!] ${description}`)}\n`); // red

    if (error.loc) {
        process.stderr.write(`${path.relative(process.cwd(), error.loc.file)} (${error.loc.line}:${error.loc.column})\n`);
    }

    if (error.stack) {
        process.stderr.write(`${dim(error.stack)}\n`);
    }
}

function configureWatcher(watcher) {
    watcher.on("event", ev => {
        if (ev.code === "START") {
            clearScreen();
            process.stdout.write("starting build.\n");
        }
        else if (ev.code === "END") {
            const date = new Date();
            const dateString = date
                .toISOString()
                .replace(/T/, " ")
                .replace(/\..+/, "");
            process.stdout.write(`\n[${dateString}] waiting for changes...\n`);
        }
        else if (ev.code === "BUNDLE_START") {
            process.stdout.write("\n");
        }
        else if (ev.code === "BUNDLE_END") {
            const source = path.relative(process.cwd(), ev.input);
            const dest = path.relative(process.cwd(), ev.output[0]);
            process.stdout.write(green(`${source} => ${dest} [${ev.duration}ms]`) + "\n");
            ev.result.close();
        }
        else if (ev.code === "ERROR") {
            displayError(ev.error);
        }
    });
}

async function build(options) {
    process.stdout.write(`Compiling source files [0/${options.length}]`);

    for (let i = 0; i < options.length; i++) {
        const optionsObj = options[i];

        try {
            const bundle = await rollup.rollup(optionsObj);
            await bundle.write(optionsObj.output);
            await bundle.close();
        }
        catch (error) {
            process.stdout.write("\n");
            displayError(error);

            process.exit(1);
        }

        process.stdout.write(`\rCompiling source files [${i + 1}/${options.length}]`);
    }

    process.stdout.write("\n");
}

const config = require(path.resolve(process.cwd(), "obsidian.config.json"));
const options = defineConfigs(path.resolve(process.cwd(), config.source), path.resolve(process.cwd(), "dist"), {
    copy: config.copyToPlugins ? config.pluginsPath : undefined
});

useWatch = process.argv.includes("--watch");

if (useWatch) {
    configureWatcher(rollup.watch(options));
}
else {
    build(options);
}
