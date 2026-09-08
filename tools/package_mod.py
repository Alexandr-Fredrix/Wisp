"""Prepare a clearly labelled local development build, never publish a release."""
import argparse
import hashlib
import json
import pathlib
import zipfile
from check import ROOT, validate

def package(dll):
    version = validate()
    dll = pathlib.Path(dll).resolve()
    if dll.name != 'Wisp.dll' or dll.read_bytes()[:2] != b'MZ':
        raise ValueError('Expected a compiled Wisp.dll')
    output = ROOT / 'dist'
    output.mkdir(exist_ok=True)
    target = output / f'Wisp-{version}.zip'
    notice = {
        'version': version,
        'target_game': '1.5.12620',
        'loader': 'BepInEx 5.4.23.5 x64',
        'runtime_tested': False,
        'status': 'Development build. Complete docs/TESTING.md before public binary release.',
        'dll_sha256': hashlib.sha256(dll.read_bytes()).hexdigest(),
    }
    with zipfile.ZipFile(target, 'w', zipfile.ZIP_DEFLATED) as archive:
        archive.write(dll, 'BepInEx/plugins/Wisp/Wisp.dll')
        for name in ['LICENSE', 'THIRD_PARTY_NOTICES.md', 'docs/INSTALL.md']:
            archive.write(ROOT / name, pathlib.Path(name).name)
        archive.writestr('BUILD-STATUS.json', json.dumps(notice, indent=2) + '\n')
    (output / (target.name + '.sha256')).write_text(hashlib.sha256(target.read_bytes()).hexdigest() + '  ' + target.name + '\n', encoding='utf-8')
    print(target)
    return target

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--dll', default=str(ROOT / 'artifacts/Wisp/Wisp.dll'))
    package(parser.parse_args().dll)
