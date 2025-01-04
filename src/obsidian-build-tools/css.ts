import { readFile } from "fs/promises";
import { dirname, extname } from "path";
import { fileURLToPath } from "url";
import postcss from "postcss";
import less from "less";
import sass from "sass";
import whitespace from "postcss-normalize-whitespace";

/**
 * An error reported by the bundling system.
 */
export class StylesheetError extends Error {
    /** The filename that caused the error. */
    public readonly filename: string;

    /** The line number that caused the error. */
    public readonly line: number;

    /** The column in the line that caused the error. */
    public readonly column: number;

    /**
     * Creates a new instance of {@link StylesheetError}.
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
 * The output from a stylesheet compile operation.
 */
export interface StylesheetOutput {
    /** The CSS from the operation. */
    css: string;

    /** The input files that should be watched to trigger a rebuild. */
    watchFiles: string[];
}

/**
 * Converts any file URLs to local file paths.
 * 
 * @param urls A list of {@link URL} objects to be converted to local paths.
 * 
 * @returns An array of local paths that represent the urls.
 */
function urlsToLocalFiles(urls: URL[]): string[] {
    const files: string[] = [];

    for (const url of urls) {
        if (url.protocol === "file:") {
            files.push(fileURLToPath(url));
        }
    }

    return files;
}

/**
 * Minifies the CSS string.
 * 
 * @param source The source CSS content to minify.
 * 
 * @returns A string containing the minified content.
 */
export async function minifyString(source: string): Promise<string> {
    const processor = postcss([whitespace()]);

    const result = await processor.process(source, { from: undefined });

    return result.css;
}

/**
 * Compiles a LESS file into standard CSS.
 * 
 * @param sourcePath The path the the source file to compile.
 * 
 * @returns An instance of {@link StylehseetOutput} that describes the compiled CSS.
 */
export async function compileLessFile(sourcePath: string): Promise<StylesheetOutput> {
    const content = await readFile(sourcePath, { encoding: "utf-8" });

    const paths: string[] = [dirname(sourcePath)];

    return new Promise<StylesheetOutput>((resolve, reject) => {
        less.render(content,
            {
                paths
            },
            (error, output) => {
                if (error) {
                    reject(new StylesheetError(error.message, error.filename, error.line, error.column));
                }
                else if (output) {
                    resolve({
                        css: output.css,
                        watchFiles: [sourcePath, ...output.imports]
                    });
                }
                else {
                    reject(new StylesheetError("No output generated", "unknown", 0, 0));
                }
            });
    });
}

/**
 * Compiles a SASS or SCSS file into standard CSS.
 * 
 * @param sourcePath The path the the source file to compile.
 * 
 * @returns An instance of {@link StylehseetOutput} that describes the compiled CSS.
 */
export function compileSassFile(sourcePath: string): Promise<StylesheetOutput> {
    try {
        const result = sass.compile(sourcePath);

        return Promise.resolve<StylesheetOutput>({
            css: result.css,
            watchFiles: urlsToLocalFiles(result.loadedUrls)
        });
    }
    catch (error) {
        if (error instanceof sass.Exception) {
            throw new StylesheetError(error.sassMessage,
                error.span.url ? fileURLToPath(error.span.url) : "unknown",
                error.span.start.line,
                error.span.start.column);
        }
        else {
            throw error;
        }
    }
}

/**
 * Compiles a CSS, LESS, SCSS or SASS file into standard CSS.
 * 
 * @param sourcePath The source file to be compiled.
 * 
 * @returns An instance of {@link StylehseetOutput} that describes the compiled CSS.
 */
export async function compileStylesheetFile(sourcePath: string): Promise<StylesheetOutput> {
    if (sourcePath.endsWith(".css")) {
        const content = await readFile(sourcePath, { encoding: "utf-8" });

        return {
            css: content,
            watchFiles: [sourcePath]
        };
    }
    else if (sourcePath.endsWith(".less")) {
        return compileLessFile(sourcePath);
    }
    else if (sourcePath.endsWith(".sass")) {
        return compileSassFile(sourcePath);
    }
    else if (sourcePath.endsWith(".scss")) {
        return compileSassFile(sourcePath);
    }
    else {
        throw new Error(`Cannot compile unknown file type ${extname(sourcePath)}`)
    }
}
