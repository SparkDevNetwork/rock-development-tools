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
