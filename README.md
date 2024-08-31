# About

This repository contains a number of tools and projects to help with development of [Rock RMS](https://www.rockrms.com).

This is primarily to help with plugin developers, but not necessarily restricted to them.

# Tips

When using the framework builder, if you get an error from git about filenames being too long, you might need to run this command and then delete the `.staging` directory.

```shell
git config --global core.longpaths true
```

# TODO

* Logic to add existing plugin to environment with "existing" command.
* Merge env update command from rock/plugin into one command.
    * Add --no-git parameter to disable git changes.
    * Add --no-rock parameter to disable Rock installation changes (except for WebForms junctions).
    * Add --no-plugins parameter to disable plugin installation changes.
* Add --no-git parameter to disable git checks in env status
