"""Lossless PNG candidates with decoded-pixel verification; originals stay in editorial snapshot."""
import argparse, hashlib, io, json, pathlib
from PIL import Image, PngImagePlugin
ROOT=pathlib.Path(__file__).resolve().parents[1]

def run(apply=False):
    output=ROOT/'artifacts/image-comparison';output.mkdir(parents=True,exist_ok=True)
    records=[]
    for path in sorted((ROOT/'content').rglob('*.png')):
        if path.name in ['frame.png','divider.png']: continue
        original=path.read_bytes()
        with Image.open(io.BytesIO(original)) as im:
            im.load()
            if im.is_animated: continue
            metadata=PngImagePlugin.PngInfo()
            for key,value in im.info.items():
                if isinstance(value,str): metadata.add_text(key,value)
            kwargs={key:im.info[key] for key in ['icc_profile','transparency','dpi','exif'] if key in im.info}
            # Preserve color-space chunks that Pillow does not round-trip automatically.
            offset=8
            while offset+12<=len(original):
                length=int.from_bytes(original[offset:offset+4],'big');kind=original[offset+4:offset+8]
                if kind in [b'gAMA',b'cHRM',b'sRGB',b'sBIT']: metadata.add(kind,original[offset+8:offset+8+length])
                offset+=length+12
            buffer=io.BytesIO();im.save(buffer,format='PNG',optimize=True,compress_level=9,pnginfo=metadata,**kwargs)
            candidate=buffer.getvalue()
            with Image.open(io.BytesIO(candidate)) as check:
                assert check.size==im.size and check.convert('RGBA').tobytes()==im.convert('RGBA').tobytes(),path
            smaller=len(candidate)<len(original)
            records.append({'file':path.relative_to(ROOT).as_posix(),'before':len(original),'after':min(len(original),len(candidate)),'identical_pixels':True,'original_sha256':hashlib.sha256(original).hexdigest()})
            if apply and smaller: path.write_bytes(candidate)
    (output/'lossless.json').write_text(json.dumps(records,indent=2),encoding='utf-8')
    # Lossy examples are review candidates only, never substituted into content.
    names=['Screenshot_HK_The_Hunter_01.png','Screenshot_HK_Dreamers_08.png','Mapshot_HK_Cornifer_01.png']
    lossy=[]
    for name in names:
        path=ROOT/'content/locations'/name
        with Image.open(path) as im:
            target=output/(path.stem+'-quality92.jpg');im.convert('RGB').save(target,quality=92,subsampling=0,optimize=True)
        lossy.append({'file':name,'original_bytes':path.stat().st_size,'candidate_bytes':target.stat().st_size,'candidate':target.name,'applied':False})
    (output/'lossy-candidates.json').write_text(json.dumps(lossy,indent=2),encoding='utf-8')
    print('PNG files verified:',len(records),'bytes before:',sum(x['before'] for x in records),'after:',sum(x['after'] for x in records),'applied:',apply)

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--apply',action='store_true');run(p.parse_args().apply)
