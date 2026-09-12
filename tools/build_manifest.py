"""Bind a local DLL to the exact source and resource inputs, including uncommitted work."""
import argparse,hashlib,json,pathlib,subprocess
from check import ROOT,validate

def digest(path):
    h=hashlib.sha256()
    with pathlib.Path(path).open('rb') as f:
        for block in iter(lambda:f.read(1024*1024),b''): h.update(block)
    return h.hexdigest()

def inputs():
    paths=[ROOT/'version.json']
    paths.extend(ROOT/name for name in ['LICENSE','THIRD_PARTY_NOTICES.md','docs/INSTALL.md'] if (ROOT/name).is_file())
    for directory in ['src','content','tools','tests']:
        paths.extend(p for p in (ROOT/directory).rglob('*') if p.is_file() and not {'obj','bin','__pycache__'}&set(p.relative_to(ROOT).parts))
    return {p.relative_to(ROOT).as_posix():digest(p) for p in sorted(paths)}

def capture(directory):
    directory=pathlib.Path(directory);dll=directory/'Wisp.dll'
    commit=subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT,text=True).strip()
    manifest={'version':validate(),'dll_sha256':digest(dll),'base_commit':commit,'inputs':inputs(),'runtime_tested':False}
    (directory/'build-manifest.json').write_text(json.dumps(manifest,indent=2,sort_keys=True)+'\n',encoding='utf-8')
    return manifest

def verify(directory):
    directory=pathlib.Path(directory)
    manifest=json.loads((directory/'build-manifest.json').read_text(encoding='utf-8'))
    if manifest['version']!=validate() or manifest['dll_sha256']!=digest(directory/'Wisp.dll') or manifest['inputs']!=inputs():
        raise ValueError('DLL/source mismatch: rebuild and rerun validation before packaging')
    return manifest

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('directory');p.add_argument('--capture',action='store_true');a=p.parse_args()
    (capture if a.capture else verify)(a.directory)
    print('Build manifest verified' if not a.capture else 'Build manifest recorded')
