"""Read-only comparison of editorial blocks. Never apply or regenerate user edits."""
import argparse,difflib,json,pathlib,re

def compare(document):
    document=pathlib.Path(document)
    baseline=json.loads((document.parent/'Wisp-материалы/baseline/manifest.json').read_text(encoding='utf-8'))
    text=document.read_text(encoding='utf-8')
    pieces=re.split(r'<!-- wisp:(.*?) -->',text)
    blocks={};duplicates=[]
    for marker,body in zip(pieces[1::2],pieces[2::2]):
        if marker in blocks: duplicates.append(marker)
        blocks[marker]=body
    original_path=document.parent/'Wisp-материалы/baseline/original-review.md'
    originals={}
    if original_path.exists():
        parts=re.split(r'<!-- wisp:(.*?) -->',original_path.read_text(encoding='utf-8'))
        originals=dict(zip(parts[1::2],parts[2::2]))
    changes=[];current_bodies={}
    for item in baseline['occurrences']:
        marker=item['marker'];block=blocks.get(marker)
        if block is None: changes.append({'marker':marker,'change':'missing marker; review deletion manually'});continue
        if marker in originals and block!=originals[marker]:
            changes.append({'marker':marker,'change':'block changed (including notes, warnings, images and metadata)','diff':'\n'.join(difflib.unified_diff(originals[marker].splitlines(),block.splitlines(),lineterm=''))})
        match=re.search(r'<!-- body:start -->\n(.*?)\n<!-- body:end -->',block,re.S)
        if not match: changes.append({'marker':marker,'change':'body markers changed; manual review required'});continue
        body=match[1];current_bodies.setdefault(item['step']['id'],[]).append((marker,body))
        title=re.search(r'^### \d+\. (.*)$',block,re.M)
        if body!=item['step']['body'] or not title or title[1]!=item['step']['title']:
            changes.append({'marker':marker,'change':'instruction/title changed','diff':'\n'.join(difflib.unified_diff(item['step']['body'].splitlines(),body.splitlines(),lineterm=''))})
        notes=re.search(r'#### Ваша проверка\n(.*?)(?:\n---|\Z)',block,re.S)
        if notes:
            remainder=re.sub(r'- \[[ xX]\] Проверено|\*\*(Что исправить|Чего не хватает|Добавить или заменить изображение):\*\*','',notes[1]).strip()
            if remainder: changes.append({'marker':marker,'change':'editorial notes','notes':remainder})
    expected={x['marker'] for x in baseline['occurrences']}
    conflicts=[]
    for sid,values in current_bodies.items():
        old={x['marker']:x['step']['body'] for x in baseline['occurrences'] if x['step']['id']==sid}
        altered=[(m,b) for m,b in values if b!=old[m]]
        if altered and len(values)>1: conflicts.append({'id':sid,'review_shared_occurrences':[m for m,b in values]})
    return {'changes':changes,'duplicate_markers':duplicates,'new_markers':sorted(set(blocks)-expected),'order_changed':list(blocks)!=[x['marker'] for x in baseline['occurrences']],'shared_ids_to_review':conflicts,'note':'Read-only aid. Every block difference requires review; no edits are applied.'}

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('document');a=p.parse_args();print(json.dumps(compare(a.document),ensure_ascii=False,indent=2))
