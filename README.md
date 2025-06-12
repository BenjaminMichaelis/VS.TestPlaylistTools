# NuGet Library

A base template for NuGet Library Packages, with unit tests, and github actions

## Startup Steps

- [ ] Search for all CHANGEME words, most are in the Directory.Build.props file and the deploy workflow
- [ ] Make sure the project compiles properly
- [ ] Add NUGET_API_KEY with your NUGET api key to the GitHub repository to be able to deploy packages to Nuget.org

## Features

## Template

Create a new app in your current directory by running.

```cli
 dotnet new bmichaelis.nuget
```

### Generating Releases

1. Create a release on GitHub
2. Add your [Nuget.org](https://www.nuget.org/account/apikeys) API key to the GitHub repository secrets as `NUGET_API_KEY` with push scope permissions
3. Tag the release with a tag following a v*.*.*format, likely with*.*.* following a [Semver](https://semver.org/) format. ex: v1.0.0
