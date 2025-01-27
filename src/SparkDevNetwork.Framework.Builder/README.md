# Process to build binary RockWeb package

We do not currently have this automated, but here is the current process to build a binary package. This package is uploaded to azure so that the dev tool can download pre-built Rock versions for the environment.

1. Clone Rock repo and switch to the release/hotfix branch to be built.
2. Build the solution.
3. Create the zip file from a WSL shell with this command:

```shell
zip -r Rock-1.16.6-rc.1.zip RockWeb -x "RockWeb/Bin/*.refresh" -x "**/.gitignore" -x "RockWeb/App_Data/Avatar/*" -x "RockWeb/App_Data/Cache/*" -x "RockWeb/App_Data/ChromeEngine/*" -x "RockWeb/App_Data/Logs/*" -x "RockWeb/App_Data/Uploads/*" -x "RockWeb/Themes/**/*.css" -x "RockWeb/packages.config" -x "RockWeb/Settings.StyleCop" -x "RockWeb/tsconfig.json" -x "RockWeb/web.ConnectionStrings.config"
```

4. The zip file can then be uploaded to the blob storage under developer/environments
