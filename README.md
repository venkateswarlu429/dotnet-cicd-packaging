# dotnet-cicd-packaging

A purpose-built sample demonstrating **.NET build, test, pack, and publish
workflows** — the same delivery flow expressed twice, so the two pipelines
can be compared side by side:

- **GitHub Actions** (`.github/workflows/ci.yml`) — build, test, pack, and
  publish to **GitHub Packages**.
- **Azure DevOps** (`azure-pipelines.yml`) — the same build, test, pack,
  and publish flow against an **Azure Artifacts** feed.

The app itself is deliberately tiny: a .NET 8 Web API with a health endpoint
and a version endpoint, plus a small xUnit test project. The pipelines are
the point, not the app.

> Scope note: this is a sample built to demonstrate .NET build/packaging
> workflows. It is not a claim of .NET production experience.

## Layout

```
src/
  SampleService/
    SampleService.csproj   # net8.0 Web API, packable (IsPackable)
    Program.cs             # /health and /version endpoints
tests/
  SampleService.Tests/
    SampleService.Tests.csproj
    ApiTests.cs            # health, version, unknown-route tests
.github/
  workflows/
    ci.yml                 # GitHub Actions: build → test → pack → publish
azure-pipelines.yml        # Azure DevOps: build → test → pack → publish
```

## Endpoints

| Endpoint   | Result                                          |
|------------|-------------------------------------------------|
| `GET /health`  | `200` with `{"status":"healthy"}`           |
| `GET /version` | `200` with `{"version":"<informational version>"}` |

The version comes from the assembly informational version, which the
pipelines set from the release tag (see below). Local runs without a
version stamp report `dev`.

## How versioning works

Semantic versions come from **git tags**. Pushing a tag like `v1.2.3`
produces package version `1.2.3`.

| Build trigger              | Package version            | Publish? |
|----------------------------|----------------------------|----------|
| Tag `vX.Y.Z`               | `X.Y.Z`                    | Yes      |
| Push to `main` (no tag)    | `0.1.0-ci.<build number>` | No (pack only) |
| Pull request               | `0.1.0-ci.<build number>` | No — PRs build and test but **never publish** |

The version is passed to `dotnet pack` as `PackageVersion`; nothing in the
source tree hard-codes a release number.

## How the two pipelines map to each other

| Step | GitHub Actions (`ci.yml`) | Azure DevOps (`azure-pipelines.yml`) |
|------|---------------------------|--------------------------------------|
| SDK  | `actions/setup-dotnet` (8.x) | `UseDotNet@2` (8.x) |
| Restore | `dotnet restore` | `DotNetCoreCLI@2` restore |
| Build | `dotnet build --no-restore -c Release` | `DotNetCoreCLI@2` build |
| Test | `dotnet test --no-build -c Release` | `DotNetCoreCLI@2` test |
| Pack | `dotnet pack --no-build -c Release -p:PackageVersion=<version>` | `DotNetCoreCLI@2` pack |
| Publish | `dotnet nuget push` to GitHub Packages, tag builds only | `dotnet nuget push` to Azure Artifacts, tag builds only |

Both pipelines gate publishing on a tag build, and both pack the same
`SampleService` project with the same versioning rule, so a release tag
produces the same package version from either system.

## Azure Artifacts feed setup (placeholders only)

No real organisation, project, or feed is referenced anywhere in this
repo. To run the Azure pipeline against your own feed:

1. Create an Azure Artifacts feed in your own organisation/project.
2. In `azure-pipelines.yml`, replace these placeholders:
   - `<YOUR-ORG>` — your Azure DevOps organisation
   - `<YOUR-PROJECT>` — your Azure DevOps project
   - `<YOUR-FEED>` — your feed name
3. Give the pipeline's build service account (or a service connection)
   permission to push to that feed. The pipeline authenticates with the
   built-in `$(System.AccessToken)`; no personal access token is stored
   in this repo.
4. Push a tag (`git tag v0.1.0 && git push origin v0.1.0`) to produce
   and publish a versioned package.

GitHub Packages needs no setup beyond the repo itself: the workflow
authenticates with the automatic `GITHUB_TOKEN`, scoped to packages
write on tag builds only.

## Run it locally

```bash
dotnet run --project src/SampleService
# GET http://localhost:5000/health
# GET http://localhost:5000/version
dotnet test
```

Requires .NET 8 SDK. No external services, databases, or credentials.

## Validation

- `dotnet build -c Release` passes with 0 warnings and 0 errors.
- `dotnet pack` produces a real package
  (`SampleService.0.1.0-ci.1.nupkg` was created during validation).
- The built app was run and its endpoints verified live with curl:
  `GET /health` returns `200 {"status":"healthy"}`, `GET /version`
  returns `200` with a version string, and an unknown route returns
  `404` — exactly the three behaviors the xUnit tests assert.
- **xUnit runner not executed here:** the three tests in
  `tests/SampleService.Tests/ApiTests.cs` are authored but were not run
  to green in this environment — the vstest testhost process failed to
  connect twice (timeouts at 90s and again at 300s), an environment
  limitation, not a code failure. They are not claimed as passing.
- Both pipeline files were parsed as YAML and checked for structure
  (jobs/steps, tag-only publish conditions, PRs excluded from publish).
- **Not validated live:** no package was published to GitHub Packages or
  to Azure Artifacts from this repo, and the Azure pipeline was not
  executed against a real Azure DevOps organisation — the feed URL and
  organisation in `azure-pipelines.yml` are placeholders, so the publish
  step there is documented, not demonstrated.

## License

MIT — © 2026 Venkateswarlu Maddula.
