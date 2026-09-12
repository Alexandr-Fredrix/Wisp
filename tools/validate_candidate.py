"""Run real checks and attest only to their completed outcomes, never to gameplay."""
import argparse,json,pathlib,subprocess,sys
from build_manifest import ROOT,verify,digest

def run(pwsh,game,api,directory):
    directory=pathlib.Path(directory).resolve();dll=directory/'Wisp.dll'
    manifest=verify(directory)
    commands=[
        [sys.executable,'-B','tools/check.py'],
        [sys.executable,'-B','-m','unittest','discover','-s','tests','-v'],
        [pwsh,'-NoProfile','-File','tools/test-core.ps1'],
        [pwsh,'-NoProfile','-File','tools/test-media.ps1'],
        [pwsh,'-NoProfile','-File','tools/test-localization.ps1','-Dll',str(dll)],
        [pwsh,'-NoProfile','-File','tools/test-adapter.ps1','-Dll',str(dll),'-HollowKnightRefs',game,'-BepInExRefs',api],
    ]
    records=[]
    for index,command in enumerate(commands):
        result=subprocess.run(command,cwd=ROOT,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True,encoding='utf-8',errors='replace')
        (directory/f'check-{index+1}.log').write_text(result.stdout,encoding='utf-8')
        print(result.stdout)
        if result.returncode: raise RuntimeError('Validation failed: '+command[2])
        records.append({'check':command[2:] if command[1]=='-B' else command[3:4],'exit_code':result.returncode,'log_sha256':digest(directory/f'check-{index+1}.log')})
    verify(directory)
    evidence={'version':manifest['version'],'dll_sha256':manifest['dll_sha256'],'manifest_sha256':digest(directory/'build-manifest.json'),'checks':records,'runtime_tested':False}
    (directory/'validation.json').write_text(json.dumps(evidence,indent=2)+'\n',encoding='utf-8')
    print('Candidate checks passed; runtime validation remains pending.')

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--pwsh',default='pwsh');p.add_argument('--game',required=True);p.add_argument('--api',required=True);p.add_argument('--directory',default=str(ROOT/'artifacts/Wisp'));a=p.parse_args();run(a.pwsh,a.game,a.api,a.directory)
