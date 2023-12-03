import * as esbuild from "esbuild";

await esbuild.build({
    bundle: true,
    entryPoints: ['src/index.ts'],
    external: [],
    outfile: 'index.cjs',
    format: 'cjs',
    platform: 'node',
    target: 'node18',

    plugins: [
    ]
});
