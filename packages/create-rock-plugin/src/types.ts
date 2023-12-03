export type Options = {
    organization: string;

    orgCode: string;

    pluginName: string;

    pluginCode: string;

    rockVersion: string;

    rockWebPath: string;

    createCSharpProject: boolean;

    copyCSharpToRockWeb?: boolean;

    createObsidianProject: boolean;

    copyObsidianToRockWeb?: boolean;
};
