# About

This repository contains a number of tools and projects to help with development of [Rock RMS](https://www.rockrms.com).

This is primarily to help with plugin developers, but not necessarily restricted to them.

# TODO

* Logic to add existing plugin to environment with "existing" command.
* Copy build task should probably ignore all DLLs except primary and additional specified. Too many to safely ignore I think.
* C# project creation, also ask if it will contain REST apis. If so include:
  * Microsoft.AspNet.WebApi.Core Version="5.2.3"
  * Newtonsoft.Json Version="13.0.1" (Only required to fix build warnings as above will only include v6.0.0)
  * Alternative, maybe have Rock(.Common?) reference Newtonsoft.Json in nuget.
  * Include `<Reference Include="System.Web" />`
  * Include `<Reference Include="System.Web.Extensions" />`
