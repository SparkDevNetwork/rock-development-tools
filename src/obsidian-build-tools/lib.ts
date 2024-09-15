import { sync as globSync } from "glob";
import { statSync, readdirSync } from "fs";
import { mkdir, readFile, writeFile } from "fs/promises";
import * as path from "path";
import chokidar from "chokidar";
import { defineConfig as defineRollupConfig, OutputOptions, Plugin, rollup } from "rollup";
import vuePlugin from "rollup-plugin-vue";
import babelPlugin from "@rollup/plugin-babel";
import commonJsPlugin from "@rollup/plugin-commonjs";
import nodeResolvePlugin from "@rollup/plugin-node-resolve";
import postCssPlugin from "rollup-plugin-postcss";
import { terser as terserPlugin } from "rollup-plugin-terser";
import copyPlugin from "rollup-plugin-copy";
import cssnano from "cssnano";
import { cwd } from "process";
import babelPresetEnv from "@babel/preset-env";
import babelTypescript from "@babel/preset-typescript";
import { compileStylesheetFile, minifyString, StylesheetError, StylesheetOutput } from "./css";

// #region Interfaces

/**
 * The configuration options to use when generating the configuration object(s).
 */
export interface ConfigOptions {
    /**
     * If enabled the output file will be minified. Set to "auto" to use the
     * environment variable TODO to determine if minification should be used.
     */
    minify?: boolean | "auto";

    /**
     * The directory to copy the output file(s) to.
     */
    copy?: string;

    /**
     * The options specific to compiler TypeScript files.
     */
    script?: ScriptConfigOptions;
}

/**
 * The configuraiton options to use when generating script configuration
 * object(s). There are four build modes.
 *
 * Normal compiles the input file and
 * writes it to the output and keeps anything except partial files as external.
 *
 * The second mode is "lib" mode. This mode does the opposite, it bundles
 * everything into the output file.
 *
 * The third mode is "bundled". This will bundle everything in the same folder
 * or underneath the folder containing the input file. The input should point
 * to an index.ts file. This is used by the framework to build certain directories
 * as a single file rather than a bunch of separate micro files.
 *
 * The final mode is "nested". This is similar to "bundled" but it uses
 * automatically generated index files for each directory and then exports a
 * single object that contains all the files in that directory as child objects.
 * This is useless to blocks and plugin developers. It is used by the framework
 * to build special directories that are then handled by the loader.
 */
export interface ScriptConfigOptions {
    /**
     * If enabled the entire directory tree will be bundled into a single file.
     * The outputPath should specify a filename instead of a directory. This is
     * used by the internal build system for certain folders.
     */
    bundled?: boolean;

    /**
     * Similar to bundled, but the directory tree will be re-exported in a
     * nested format. Special option used by Enums and Utility folders of
     * framework.
     */
    nested?: boolean;

    /**
     * If enabled, all references will be compiled into a single library
     * including any node modules. Useful for adding references to external
     * libraries.
     */
    lib?: boolean;
}

/**
 * A bundle that describes a previous build operation.
 */
export interface Bundle {
    /** The primary source file that was built. */
    source: string;

    /** The destination file that was written. */
    destination: string;

    /** The files to be watched for changes to initiate a rebuild. */
    watchFiles: string[];

    /** Duration in milliseconds this build took. */
    duration: number;
}

/**
 * Defines a builder that will build and output a bundle.
 */
export interface BundleBuilder {
    /** The primary source file for this bundle. */
    readonly source: string;

    /** The function that will build the bundle. */
    build(): Promise<Bundle>;
}

/**
 * The internal configuration for a stylesheet builder.
 */
interface StylesheetConfiguration {
    /** The absolute path to the file that will be compiled. */
    source: string;

    /** The absolute path to the output file. */
    destination: string;

    /** If `true` then the output will be minified. */
    minify: boolean;

    /** If set then this contains the absolute path to copy the output to. */
    copy?: string;
}

/**
 * The internal configuration for an static file builder.
 */
interface StaticFileConfiguration {
    /** The absolute path to the file that will be compiled. */
    source: string;

    /** The absolute path to the output file. */
    destination: string;

    /** If set then this contains the absolute path to copy the output to. */
    copy?: string;
}

// #endregion

// #region Rollup Plugins

/**
 * Simple plugin that allows for virtual files to be resolved and loaded by
 * rollup. This is used by the fakeIndex process to bundle all files in a
 * directory tree.
 *
 * @param modules An object whose keys are filenames and values are the contents of the virtual files.
 */
function virtual(modules: Record<string, string>): Plugin {
    const resolvedIds = new Map();

    Object.keys(modules).forEach(id => {
        resolvedIds.set(path.resolve(id), modules[id]);
    });

    return {
        name: "virtual",

        resolveId(id, importer) {
            if (id in modules) {
                return id;
            }

            if (importer) {
                const resolved = path.resolve(path.dirname(importer), id);

                if (resolvedIds.has(resolved)) {
                    return resolved;
                }
            }
            else {
                const resolved = path.resolve(cwd(), id);

                if (resolvedIds.has(resolved)) {
                    return resolved;
                }
            }
        },

        load(id) {
            if (id in modules) {
                return modules[id];
            }
            else {
                return resolvedIds.get(id);
            }
        }
    };
}

// #endregion

// #region Functions

/**
 * Scans a directory tree and creates virtual index files for use in the virtual
 * plugin. These can then be compiled without having to keep an index.ts file
 * up to date by hand.
 *
 * @param indexes An object to store the virtual index file contents into.
 * @param sourcePath The source path to use when generating virtual indexes to be compiled.
 *
 * @returns The index filename to be used for this directory.
 */
function createVirtualNestedIndex(indexes: Record<string, string>, sourcePath: string): string {
    const entries = readdirSync(sourcePath);
    let indexContent = "";

    entries.forEach(f => {
        const filePath = path.join(sourcePath, f);

        if (statSync(filePath).isDirectory()) {
            indexContent += `export * as ${f} from "./${f}/virtual-index.js";\n`;
            createVirtualNestedIndex(indexes, filePath);
        }
        else if (f.endsWith(".ts")) {
            indexContent += `export * as ${f.split(".")[0]} from "./${f.split(".")[0]}";\n`;
        }
    });

    const virtualIndexPath = path.join(sourcePath, "virtual-index.js").replace(/\\/g, "/");
    indexes[virtualIndexPath] = indexContent;

    return virtualIndexPath;
}

/**
 * Clears the screen and returns the cursor to the top left.
 */
export function clearScreen(): void {
    process.stdout.write("\u001bc");
}

/**
 * Wraps the text in ANSI sequence to make the foreground color red.
 * 
 * @param text The text that should be made red.
 * 
 * @returns A new string with the text wrapped in ANSI sequences.
 */
export function red(text: string): string {
    return `\u001b[31m${text}\u001b[39m`;
}

/**
 * Wraps the text in ANSI sequence to make the foreground color green.
 * 
 * @param text The text that should be made green.
 * 
 * @returns A new string with the text wrapped in ANSI sequences.
 */
export function green(text: string): string {
    return `\u001b[32m${text}\u001b[39m`;
}

/**
 * Wraps the text in ANSI sequence to make the foreground color yellow.
 * 
 * @param text The text that should be made yellow.
 * 
 * @returns A new string with the text wrapped in ANSI sequences.
 */
export function yellow(text: string): string {
    return `\u001b[33m${text}\u001b[39m`;
}

/**
 * Wraps the text in ANSI sequence to make the foreground color dim.
 * 
 * @param text The text that should be made dim.
 * 
 * @returns A new string with the text wrapped in ANSI sequences.
 */
export function dim(text: string): string {
    return `\u001b[2m${text}\u001b[22m`;
}

/**
 * Defines the builders for all the valid source files, including those
 * in sub directories, for a given directory.
 *
 * @param sourcePath The base path to use when searching for files to compile.
 * @param outputPath The base output path to use when writing the compiled
 * files. The relative paths to the source files will be maintained when
 * compiled to this location.
 * @param options The options that define how the files are compiled.
 *
 * @returns An array of {@link BundleBuilder} objects.
 */
export function defineBuilders(sourcePath: string, outputPath: string, options: ConfigOptions): BundleBuilder[] {
    return [
        ...defineScriptBuilders(sourcePath, outputPath, options),
        ...defineStylesheetBuilders(sourcePath, outputPath, options),
        ...defineStaticFileBuilders(sourcePath, outputPath, options)
    ];
}

// #endregion

// #region Script Builders

/**
 * Defines the builders for all the TypeScript and Obsidian files,
 * including those in sub directories, for a given directory. Any filename
 * ending with .lib.ts will be automatically compiled with ConfigOptions.lib
 * option enabled.
 *
 * @param sourcePath The base path to use when searching for files to compile.
 * @param outputPath The base output path to use when writing the compiled
 * files. The relative paths to the source files will be maintained when
 * compiled to this location.
 * @param options The options to pass to {@link defineScriptFileBuilder}.
 *
 * @returns An array of {@link BundleBuilder} objects.
 */
export function defineScriptBuilders(sourcePath: string, outputPath: string, options: ConfigOptions): BundleBuilder[] {
    options = options || {};

    const files = globSync(sourcePath.replace(/\\/g, "/") + "/**/*.@(ts|obs)")
        .map(f => path.normalize(f).substring(sourcePath.length + 1))
        .filter(f => !f.endsWith(".d.ts") && !f.endsWith(".partial.ts") && !f.endsWith(".partial.obs"));

    return files.map(file => {
        const fileOptions = Object.assign({}, options);
        let outFile = file;

        fileOptions.script ??= {};

        // If the caller requested a copy operation, append the path to the
        // source file to the copy destination. If sourcePath is "/src" and
        // outputPath is "/dist" and file is "/src/a/b/c.js" then the new
        // copy path becomes "/dist/a/b".
        if (fileOptions.copy) {
            fileOptions.copy = path.join(fileOptions.copy, path.dirname(file));
        }

        // If the filename indicates it should be compiled as a library then
        // enable that option in the file options.
        if (file.endsWith(".lib.ts") || file.endsWith(".lib.obs")) {
            fileOptions.script.lib = true;
        }

        // Fix extension names for the output file.
        if (outFile.endsWith(".obs")) {
            outFile = `${outFile}.js`;
        }
        else if (outFile.endsWith(".ts")) {
            outFile = outFile.replace(/\.ts$/, ".js");
        }

        return defineScriptFileBuilder(path.join(sourcePath, file), path.join(outputPath, outFile), fileOptions);
    });
}

/**
 * Defines the builder object for a single script file.
 *
 * @param input The path to the input file or directory to be built.
 * @param output The path to the output file or directory to write compiled files.
 * @param options The configuration options to use when compiling the source.
 *
 * @returns A {@link BundleBuilder} object.
 */
export function defineScriptFileBuilder(input: string, output: string, options: ConfigOptions): BundleBuilder {
    let virtualPlugin: Plugin | undefined = undefined;
    let inputFile = input;
    const absoluteSrcPath = path.resolve(input);

    options = options || {};

    // If they requested the special nested structure, we need to generate
    // a special index file that exports all the files and folders
    // recursively.
    if (options.script?.nested) {
        const virtualData = {};
        inputFile = createVirtualNestedIndex(virtualData, input);
        virtualPlugin = virtual(virtualData);
    }

    // Not really needed, but makes the build log much cleaner.
    inputFile = path.relative(cwd(), inputFile).replace(/\\/g, "/");

    const config = defineRollupConfig({
        input: inputFile,

        output: {
            format: "system",
            file: output,
            sourcemap: true
        },

        external: (target, source) => {
            // Check if this is a primary bundle.
            if (source === undefined) {
                return false;
            }

            // If we are building a library, always bundle all externals.
            if (options?.script?.lib) {
                // Except these special cases that are handled by our global
                // imports and need to be standard.
                if (target === "vue") {
                    return true;
                }

                return false;
            }

            // Check if it is a reference to a partial file, which is included.
            if (target.endsWith(".partial") || target.endsWith(".partial.ts") || target.endsWith(".partial.obs")) {
                return false;
            }

            // Always include vue extracted files.
            if (target.includes("?vue&type")) {
                return false;
            }

            // Always keep the CSS style inejector internal.
            if (target.includes("style-inject.es.js")) {
                return false;
            }

            // If we are building a bundled file then include any relative imports.
            if (options?.script?.bundled || options?.script?.nested) {
                if (target.startsWith("./") || target.startsWith(absoluteSrcPath)) {
                    return false;
                }
            }

            return true;
        },

        plugins: [
            virtualPlugin,

            nodeResolvePlugin({
                browser: true,
                extensions: [".js", ".ts"]
            }),

            commonJsPlugin(),

            vuePlugin({
                include: [/\.obs$/i],
                preprocessStyles: true
            }),

            postCssPlugin({
                plugins: [
                    cssnano()
                ]
            }),

            babelPlugin({
                babelHelpers: "bundled",
                presets: [
                    [
                        babelPresetEnv,
                        {
                            targets: "edge >= 13, chrome >= 50, chromeandroid >= 50, firefox >= 53, safari >= 10, ios >= 10"
                        }
                    ],
                    babelTypescript
                ],
                extensions: [".js", ".jsx", ".ts", ".tsx", ".obs"],
                comments: false,
                sourceMaps: true
            })
        ]
    });

    // If they requested minification, then do so.
    if (options.minify) {
        config.plugins!.push(terserPlugin());
    }

    // If they wanted to copy it, do that after the bundle is closed.
    if (options.copy) {
        const copySource = path.relative(cwd(), output).replace(/\\/g, "/");
        const copyDestination = path.relative(cwd(), options.copy).replace(/\\/g, "/");

        config.plugins!.push(copyPlugin({
            targets: [
                {
                    src: copySource,
                    dest: copyDestination,
                },
                {
                    src: `${copySource}.map`,
                    dest: copyDestination,
                }
            ],
            hook: "closeBundle"
        }));
    }

    return {
        source: config.input as string,
        async build(): Promise<Bundle> {
            const start = Date.now();
            const bundle = await rollup(config);

            let output: OutputOptions | undefined = Array.isArray(config.output) ? config.output[0] : config.output;
            if (output) {
                await bundle.write(output);
            }

            await bundle.close();

            const duration = Math.floor((Date.now() - start) / 1000);

            return {
                source: config.input as string,
                destination: output?.file ?? "none",
                duration,
                watchFiles: bundle.watchFiles
            };
        }
    };
}

// #endregion

// #region Stylesheet Builders

/**
 * Defines the configuration for all the stylesheet files that need to be
 * compiled, including those in sub directories, for a given directory. Any
 * filename ending with .css, .less, .scss or .sass will be included.
 * 
 * @param sourcePath The base path to use when searching for files to compile.
 * @param outputPath The base output path to use when writing the compiled
 * files. The relative paths to the source files will be maintained when
 * compiled to this location.
 * @param options The options that define how the files are compiled.
 * 
 * @returns An array of {@link BundleBuilder} objects.
 */
export function defineStylesheetBuilders(sourcePath: string, outputPath: string, options: ConfigOptions): BundleBuilder[] {
    options = options || {};

    const ignoredExtensions: string[] = [
        ".partial.css",
        ".partial.less",
        ".partial.sass",
        ".partial.scss"
    ];

    const files = globSync(sourcePath.replace(/\\/g, "/") + "/**/*.@(css|less|sass|scss)")
        .map(f => path.normalize(f).substring(sourcePath.length + 1))
        .filter(f => !ignoredExtensions.some(ext => f.endsWith(ext)));

    return files.map(file => {
        let outFile = file;

        if (outFile.endsWith(".less") || outFile.endsWith(".sass") || outFile.endsWith(".scss")) {
            outFile = `${outFile.substring(0, outFile.length - 4)}css`;
        }

        const configuration: StylesheetConfiguration = {
            source: path.join(sourcePath, file),
            destination: path.join(outputPath, outFile),
            minify: false
        };

        // If the caller requested a copy operation, append the path to the
        // source file to the copy destination. If sourcePath is "/src" and
        // outputPath is "/dist" and file is "/src/a/b/c.js" then the new
        // copy path becomes "/dist/a/b".
        if (options.copy) {
            configuration.copy = path.join(options.copy, path.dirname(file));
        }

        const builder: BundleBuilder = {
            source: configuration.source,
            build(): Promise<Bundle> {
                return buildStylesheet(configuration);
            }
        };

        return builder;
    });
}

/**
 * Builds a single stylesheet from the configuration.
 * 
 * @param configuration The configuration that defines the stylesheet to build.
 * 
 * @returns An instance of {@link Bundle} describing the output.
 */
export async function buildStylesheet(configuration: StylesheetConfiguration): Promise<Bundle> {
    const start = Date.now();
    let output: StylesheetOutput;

    try {
        output = await compileStylesheetFile(configuration.source);
    }
    catch (error) {
        if (error instanceof StylesheetError) {
            throw new BundleError(error.message, error.filename, error.line, error.column);
        }

        throw error;
    }

    let css = output.css;

    if (configuration.minify) {
        css = await minifyString(css);
    }

    await mkdir(path.dirname(configuration.destination), { recursive: true });
    await writeFile(configuration.destination, css);

    if (configuration.copy) {
        await mkdir(configuration.copy, { recursive: true });
        await writeFile(path.join(configuration.copy, path.basename(configuration.source)), css);
    }

    const duration = Math.floor((Date.now() - start) / 1000);

    return {
        source: configuration.source,
        destination: configuration.destination,
        duration,
        watchFiles: output.watchFiles
    };
}

// #endregion

// #region Static File Builders

/**
 * Defines the configuration for all the static files that need to be
 * "compiled", including those in sub directories, for a given directory. Any
 * filename ending with .css, .less, .scss or .sass will be included.
 * 
 * @param sourcePath The base path to use when searching for files to compile.
 * @param outputPath The base output path to use when writing the compiled
 * files. The relative paths to the source files will be maintained when
 * compiled to this location.
 * @param options The options that define how the files are compiled.
 * 
 * @returns An array of {@link BundleBuilder} objects.
 */
export function defineStaticFileBuilders(sourcePath: string, outputPath: string, options: ConfigOptions): BundleBuilder[] {
    options = options || {};

    const extensions: string[] = [
        "jpg",
        "jpeg",
        "png",
        "apng",
        "gif",
        "svg",
        "webp",
        "lava",
    ];

    const files = globSync(sourcePath.replace(/\\/g, "/") + `/**/*.@(${extensions.join("|")})`)
        .map(f => path.normalize(f).substring(sourcePath.length + 1));

    return files.map(file => {
        let outFile = file;

        const configuration: StaticFileConfiguration = {
            source: path.join(sourcePath, file),
            destination: path.join(outputPath, outFile)
        };

        // If the caller requested a copy operation, append the path to the
        // source file to the copy destination. If sourcePath is "/src" and
        // outputPath is "/dist" and file is "/src/a/b/c.jpg" then the new
        // copy path becomes "/dist/a/b".
        if (options.copy) {
            configuration.copy = path.join(options.copy, path.dirname(file));
        }

        const builder: BundleBuilder = {
            source: configuration.source,
            build(): Promise<Bundle> {
                return buildStaticFile(configuration);
            }
        };

        return builder;
    });
}

/**
 * Builds a single static from the configuration.
 * 
 * @param configuration The configuration that defines the static file to copy.
 * 
 * @returns An instance of {@link Bundle} describing the output.
 */
export async function buildStaticFile(configuration: StaticFileConfiguration): Promise<Bundle> {
    const start = Date.now();
    let data: Buffer;

    try {
        data = await readFile(configuration.source);
    }
    catch (error) {
        if (error instanceof Error) {
            throw new BundleError(error.message, configuration.source, 0, 0);
        }

        throw error;
    }

    await mkdir(path.dirname(configuration.destination), { recursive: true });
    await writeFile(configuration.destination, data);

    if (configuration.copy) {
        await mkdir(configuration.copy, { recursive: true });
        await writeFile(path.join(configuration.copy, path.basename(configuration.source)), data);
    }

    const duration = Math.floor((Date.now() - start) / 1000);

    return {
        source: configuration.source,
        destination: configuration.destination,
        duration,
        watchFiles: [configuration.source]
    };
}

// #endregion

/**
 * An error reported by the bundling system.
 */
export class BundleError extends Error {
    /** The filename that caused the error. */
    public readonly filename: string;

    /** The line number that caused the error. */
    public readonly line: number;

    /** The column in the line that caused the error. */
    public readonly column: number;

    /**
     * Creates a new instance of {@link BundleError}.
     * 
     * @param message The message that describes the error.
     * @param filename The filename that caused the error.
     * @param line The line number that caused the error.
     * @param column The column in the line that caused the error.
     */
    public constructor(message: string, filename: string, line: number, column: number) {
        super(message);

        Object.setPrototypeOf(this, new.target.prototype);

        this.filename = filename;
        this.line = line;
        this.column = column;
    }
}

/**
 * Simple helper for watching file changes and triggering an event when one
 * of the watched files is changed.
 */
export class Watcher {
    private readonly watcher: chokidar.FSWatcher;

    private readonly callback: () => void;

    private watchedFiles: string[] = [];

    /** This will be set to `true` whenever a watched file has changed. */
    public dirty: boolean = false;

    /**
     * Creates a new instance of {@link Watcher}.
     * 
     * @param callback The function to call when any file has changed.
     */
    public constructor(callback: () => void) {
        this.callback = callback;
        this.watcher = chokidar.watch([]);
        this.watcher.on("change", () => {
            this.dirty = true;
            this.callback();
        });
    }

    /**
     * Updates the list of watched files by adding new watchers and removing old
     * watchers based on the list of files.
     * 
     * @param files The new list of files to be watched.
     */
    public updateWatchFiles(files: string[]): void {
        const filesToUnwatch = this.watchedFiles.filter(f => !files.includes(f));
        const filesToWatch = files.filter(f => !this.watchedFiles.includes(f));

        this.watcher.unwatch(filesToUnwatch);
        this.watcher.add(filesToWatch);
    }
}
