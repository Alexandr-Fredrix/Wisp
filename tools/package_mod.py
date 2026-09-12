"""Package the compiled mod and checksums; publishing is a separate action."""
import argparse
import hashlib
import json
import pathlib
import zipfile
from check import ROOT, validate
from build_manifest import verify, digest

def package(dll):
    version = validate()
    dll = pathlib.Path(dll).resolve()
    if dll.name != 'Wisp.dll' or dll.read_bytes()[:2] != b'MZ':
        raise ValueError('Expected a compiled Wisp.dll')
    manifest = verify(dll.parent)
    evidence = json.loads((dll.parent / 'validation.json').read_text(encoding='utf-8'))
    if evidence['manifest_sha256'] != digest(dll.parent / 'build-manifest.json') or evidence['dll_sha256'] != manifest['dll_sha256'] or len(evidence['checks']) != 6 or any(c['exit_code'] != 0 for c in evidence['checks']):
        raise ValueError('Missing or stale validation; run validate_candidate.py')
    for i, check in enumerate(evidence['checks'], 1):
        if check['log_sha256'] != digest(dll.parent / f'check-{i}.log'):
            raise ValueError('Validation log mismatch')
    output = ROOT / 'dist'
    output.mkdir(exist_ok=True)
    target = output / f'Wisp-{version}.zip'
    notice = {
        'version': version,
        'target_game': '1.5.12620',
        'loader': 'BepInEx 5.4.23.5 x64',
        'runtime_tested': False,
        'status': 'Pre-release 1.0.1. Version 1.0.0 remains the main release. Automated checks recorded in VALIDATION.json; game and controller testing pending.',
        'base_commit': manifest['base_commit'],
        'languages': ['ru', 'en'],
        'dll_sha256': hashlib.sha256(dll.read_bytes()).hexdigest(),
    }
    with zipfile.ZipFile(target, 'w', zipfile.ZIP_DEFLATED) as archive:
        archive.write(dll, 'BepInEx/plugins/Wisp/Wisp.dll')
        for name in ['LICENSE', 'THIRD_PARTY_NOTICES.md', 'docs/INSTALL.md']:
            archive.write(ROOT / name, pathlib.Path(name).name)
        archive.writestr('BUILD-STATUS.json', json.dumps(notice, indent=2) + '\n')
        archive.writestr('VALIDATION.json', json.dumps(evidence, indent=2) + '\n')
        archive.writestr('BUILD-MANIFEST.json', json.dumps(manifest, indent=2) + '\n')
    (output / (target.name + '.sha256')).write_text(hashlib.sha256(target.read_bytes()).hexdigest() + '  ' + target.name + '\n', encoding='utf-8')
    print(target)
    return target

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--dll', default=str(ROOT / 'artifacts/Wisp/Wisp.dll'))
    package(parser.parse_args().dll)
