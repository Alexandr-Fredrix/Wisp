# Releases

Current releases are source previews, always marked as prereleases.

1. Update version.json, the C# project version, CHANGELOG.md and docs/releases/VERSION.md.
2. Run checks and tests; commit and push main.
3. Tag that commit: `git tag -a v0.1.0-alpha.1 -m "Initial source preview"`.
4. Push: `git push origin v0.1.0-alpha.1`.
5. Release workflow checks the version, packages tracked source files and publishes Wisp-source-VERSION.zip plus SHA256SUMS.txt.
6. Verify Actions and the downloaded archive. Never move a published tag; issue a new version.

The built-in GITHUB_TOKEN publishes releases. No personal token or game DLL upload is required. Stable tags are rejected while stage is source-preview.

## Before playable releases

Replace source-only publishing only after a real DLL builds and is tested against documented game/API versions. Publish Wisp files and properly licensed resources, not game assemblies. Include installation/removal instructions, checksums, compatibility and testing results (ordinary mode, Steel Soul, existing saves, controller, spoiler settings).

Submit a tested downloadable mod archive and accessible source to https://github.com/hk-modding/modlinks. Wisp has not yet been submitted.
