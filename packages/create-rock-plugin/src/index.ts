import prompts from "prompts";

async function main(): Promise<void> {
    const answers = await prompts([
        {
            type: "select",
            name: "version",
            message: "Target Rock version",
            choices: [{
                title: "1.16.0",
                value: "1.16.0"
            }]
        }
    ]);

    if (Object.keys(answers).length === 0) {
        process.exit(1);
    }

    console.log(answers);
}

main();
