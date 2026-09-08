"""Validate project metadata without game assemblies."""
import json
import pathlib
import re
import xml.etree.ElementTree as ET

ROOT = pathlib.Path(__file__).resolve().parents[1]

def metadata():
    data = json.loads((ROOT / "version.json").read_text(encoding="utf-8"))
    version = data["version"]
    if not re.fullmatch(r"0\.\d+\.\d+-(alpha|beta|rc)\.\d+", version):
        raise ValueError("Source previews require a 0.x prerelease version")
    if data["stage"] not in {"source-preview", "development-alpha"}:
        raise ValueError("Unknown development stage")
    return data

def validate():
    version = metadata()["version"]
    for name in ["README.md", "README.en.md", "LICENSE", "THIRD_PARTY_NOTICES.md",
                 "CONTRIBUTING.md", "SECURITY.md", "docs/RELEASING.md",
                 f"docs/releases/{version}.md"]:
        if not (ROOT / name).is_file():
            raise ValueError(f"Missing file: {name}")
    project = ET.parse(ROOT / "src/Wisp/Wisp.csproj")
    if project.findtext(".//Version") != version:
        raise ValueError("C# project version differs from version.json")
    if version not in (ROOT / "CHANGELOG.md").read_text(encoding="utf-8"):
        raise ValueError("Version missing from changelog")
    return version

if __name__ == "__main__":
    print("Validated Wisp", validate())
