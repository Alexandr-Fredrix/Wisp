# Releases

Build locally against Hollow Knight 1.5.12620 and BepInEx 5.4.23.5 x64. Never upload game assemblies.

1. Update version.json, project metadata, CHANGELOG and docs/releases/VERSION.md.
2. Run check.py, Python tests, test-core.ps1 and build.ps1. Record the actual scope of in-game tests.
3. Run `python tools/package_mod.py`. The archive installs one DLL; other files are instructions, terms and build status.
4. Commit as Alex, push and create the version tag for that exact commit.
5. Create a GitHub release, add its notes, ZIP and .sha256 file, then publish. Mark experimental alpha builds as Pre-release and keep unverified behavior explicit. Use Latest only when publishing a regular release after runtime validation.
6. Verify release checks the tag, archive checksum and single-DLL layout after publication; it does not compile game adapters or certify runtime compatibility.

Never silently replace a published binary or move its tag. Publish a new version for fixes. Wisp is not listed in Lumafly/Modlinks; the standard Modding API is a separate target.
