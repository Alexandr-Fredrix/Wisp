"""Package tracked project source only; never an installable game mod."""
import hashlib
import pathlib
import subprocess
import zipfile
from check import ROOT, validate

FORBIDDEN = {".dll", ".pdb", ".dat", ".pem", ".key"}
def eligible(name):
    path = pathlib.PurePosixPath(name)
    return (not any(part in {"bin", "obj", "dist", ".git", "HollowKnightManaged"}
                    for part in path.parts)
            and path.suffix.lower() not in FORBIDDEN
            and not path.name.startswith(".env")
            and not path.name.endswith(".modded.json"))

def package():
    version = validate()
    names = subprocess.check_output(["git", "-c", "safe.directory=" + str(ROOT), "ls-files", "-z"], cwd=ROOT).decode().split("\0")
    output = ROOT / "dist"
    output.mkdir(exist_ok=True)
    archive = output / f"Wisp-source-{version}.zip"
    with zipfile.ZipFile(archive, "w", zipfile.ZIP_DEFLATED) as bundle:
        for name in sorted(filter(None, names)):
            if not eligible(name):
                raise ValueError(f"Forbidden tracked file: {name}")
            info = zipfile.ZipInfo("Wisp/" + name, date_time=(2026, 1, 1, 0, 0, 0))
            info.compress_type = zipfile.ZIP_DEFLATED
            info.external_attr = 0o100644 << 16
            bundle.writestr(info, (ROOT / name).read_bytes())
    digest = hashlib.sha256(archive.read_bytes()).hexdigest()
    (output / "SHA256SUMS.txt").write_text(f"{digest}  {archive.name}\n", encoding="utf-8")
    print(archive.name, digest)
    return archive

if __name__ == "__main__":
    package()
