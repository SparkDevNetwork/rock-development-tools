# Development

## Testing the tool

Simplest way to test the tool is to use the --environment argument to specify
the environment directory.

Another way is to install the tool locally. This can be done with the following.

```console
$ dotnet pack
$ dotnet tool install --global --add-source bin\Release --no-cache --prerelease SparkDevNetwork.Rock.DevTool
You can invoke the tool using the following command: rock-dev-tool
Tool 'sparkdevnetwork.rock.devtool' (version '1.0.0-rc.1') was successfully installed.

$ dotnet tool uninstall --global SparkDevNetwork.Rock.DevTool
Tool 'sparkdevnetwork.rock.devtool' (version '1.0.0-rc.1') was successfully uninstalled.
```
