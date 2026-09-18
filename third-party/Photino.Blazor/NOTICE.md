# Photino.Blazor — vendored third-party code

**This directory is not original SharpDisk code.** It is a fork of Photino.Blazor,
copied into the repository so the Blazor host can be debugged and patched locally.

| | |
|---|---|
| Upstream project | [tryphotino/photino.Blazor](https://github.com/tryphotino/photino.Blazor) |
| Copyright | TryPhotino |
| License | Apache License 2.0 — full text in [`LICENSE`](LICENSE) |
| Forked from commit | `4164eedbe5f7cb46ae83fbc3bd1ed8c459620ec7` (2025-01-23, the tip of `master`) |
| Corresponds to release | Photino.Blazor 4.0.13 |
| Vendored on | 2026-09-18 |

Upstream's own README is kept as [`README.upstream.md`](README.upstream.md).

## Changes made to the upstream code

Apache-2.0 §4(b) requires that modified files say so. The list below is the whole of it —
keep it current when you touch anything here.

### `Photino.Blazor.csproj` — replaced

The upstream project file was not copied; this one was written from scratch. Differences:

- `TargetFrameworks` `net8.0;net9.0` → single `TargetFramework` `net10.0`.
- `Microsoft.AspNetCore.Components.WebView` `8.0.12` / `9.0.1` → `10.0.12`.
- `Microsoft.Extensions.Logging.Console` `8.0.1` / `9.0.1` → `10.0.12`.
- `Photino.NET` stays at `4.0.16` — upstream's latest, and it already pulls the current
  native layer (`Photino.Native` 4.0.22).
- NuGet packaging removed (`PackageId`, `PackageIcon`, the `SetPackageVersion` target,
  the `.nuspec`): this builds as a plain project reference, it is not republished.
- `ImplicitUsings` and `Nullable` explicitly disabled, matching upstream's effective
  settings — the vendored source is not nullable-clean.

### `*.cs` — unmodified

Every `.cs` file is byte-for-byte upstream as of the commit above. **When you do change
one, put a note at the top of that file** saying what was changed and why, and add a line
here. That keeps "what is ours" and "what is theirs" separable, which matters both for the
license and for diffing against upstream later.

## Files not copied

Samples, the upstream solution, CI pipelines, `nuget.config` and packaging assets were
left behind — re-clone upstream if you need them.
