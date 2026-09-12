"""Create a portable editorial snapshot once. Never overwrite the owner's edits."""
import argparse
import hashlib
import json
import pathlib
import re
import shutil
from collections import Counter
from PIL import Image

ROOT = pathlib.Path(__file__).resolve().parents[1]

def export(destination, cache):
    destination, cache = pathlib.Path(destination), pathlib.Path(cache)
    document = destination / 'Wisp-прохождения-для-проверки.md'
    assets = destination / 'Wisp-материалы'
    if document.exists() or assets.exists():
        raise FileExistsError('Editorial snapshot already exists; choose a new destination. Never overwrite edits.')
    read = lambda name: json.loads((ROOT/'content'/name).read_text(encoding='utf-8'))
    chapters, media = read('route-pdf.json'), read('step-media.json')
    achievements, regions = read('achievements.json'), read('regions.json')
    geography = {c['id']: c['title'] for c in read('route.json')}
    geography.update({'abyss':'Бездна', 'fog-canyon':'Туманный каньон', 'queens-gardens':'Сады королевы'})
    mapping = dict(re.findall(r'\{ "([^"]+)", "([^"]+)" \}', (ROOT/'src/Wisp/Core/ProfileAchievements.cs').read_text(encoding='utf-8')))
    (assets/'images').mkdir(parents=True)
    (assets/'new').mkdir()
    (assets/'baseline').mkdir()
    for name in ['route-pdf.json','route.json','step-media.json','media.json','achievements.json','regions.json','regions-en.json','english.json']:
        shutil.copyfile(ROOT/'content'/name, assets/'baseline'/name)
    shutil.copyfile(ROOT/'src/Wisp/Core/ProfileAchievements.cs', assets/'baseline/ProfileAchievements.cs')
    files, occurrences = {}, []
    def image(source, label):
        if source.startswith('embedded:'):
            path = ROOT/'content'/source[9:]
        elif source.startswith('https:'):
            path = cache/(hashlib.sha256(source.encode()).hexdigest()+'.png')
        else:
            path = pathlib.Path(source)
        with Image.open(path) as im:
            im.load()
            suffix = '.jpg' if im.format == 'JPEG' else '.png'
            dimensions = list(im.size)
        digest = hashlib.sha256(path.read_bytes()).hexdigest()
        target = assets/'images'/(digest+suffix)
        if not target.exists(): shutil.copyfile(path, target)
        relative = target.relative_to(destination).as_posix()
        files[source] = {'file':relative, 'sha256':digest, 'dimensions':dimensions}
        return '!['+label.replace(']', '\\]')+']('+relative+')'
    def region_source(key):
        embedded = ROOT/'content/ui'/('region-'+key+'.png')
        local = cache/('region-'+key+'.png')
        if embedded.exists(): return str(embedded)
        if local.exists(): return str(local)
        return regions.get(key)
    shared = Counter(s['id'] for c in chapters for s in c['steps'])
    goals = {'112':'A — 112% и достижения', 'speed':'B — скорость и противоположные выборы', 'steel':'C — Стальная душа'}
    out = ['# Wisp — три прохождения для проверки', '',
           'Исходный снимок: 1.0.0, commit 7af903f. Это редакторский документ с полными спойлерами, а не игровой прогресс.', '',
           'Редактируйте инструкции прямо здесь. Замечания оставляйте под соответствующим пунктом. Новые картинки кладите в `Wisp-материалы/new/` и вставляйте обычной Markdown-ссылкой. Сохраняйте метки `wisp:`: по ним правки связываются с модом. Повторная выгрузка этот файл не заменяет.', '',
           'Все показываемые картинки локальные. Для переноса скопируйте этот файл вместе с папкой `Wisp-материалы`. Источники могут вести в интернет, но он не нужен для чтения документа.', '', '## Оглавление', '']
    for goal, title in goals.items():
        out += ['- ['+title+'](#goal-'+goal+')']
        out += ['  - ['+c['title']+'](#'+c['id']+')' for c in chapters if c['goal']==goal]
    for goal, title in goals.items():
        out += ['', '<a id="goal-'+goal+'"></a>', '# '+title, '']
        for c in [c for c in chapters if c['goal']==goal]:
            out += ['<a id="'+c['id']+'"></a>', '## '+c['title'], '', c.get('summary',''), '']
            for n, s in enumerate(c['steps'],1):
                marker = goal+'/'+c['id']+'/'+s['id']
                hidden = s.get('referenceOnly') and not s['id'].endswith('-guide')
                out += ['<!-- wisp:'+marker+' -->', '### '+str(n)+'. '+s['title'], '',
                        '**Метка:** `'+marker+'`', '', '**Прохождение / этап:** '+title+' / '+c['title'], '',
                        '**Видимость:** '+('Справочная запись — сейчас не показывается в интерфейсе' if hidden else 'Показывается в интерфейсе'), '',
                        '**Тип:** '+('Справочная информация, не входит в счётчик задач' if s.get('referenceOnly') else 'Задача'), '',
                        '**Спойлеры:** '+('Да' if s.get('spoiler') else 'Нет'), '']
                if shared[s['id']]>1: out += ['**Общий ID:** встречается в '+str(shared[s['id']])+' пунктах. Правки соседних прохождений проверяются отдельно.', '']
                out += ['#### Инструкция', '', '<!-- body:start -->', s['body'], '<!-- body:end -->', '', '#### Предупреждения', '', s.get('warning') or 'Нет.', '']
                key = mapping.get(s['id'])
                mode = 'Справочный пункт — не отмечается' if s.get('referenceOnly') else 'По достижениям игрового профиля' if key else 'По данным текущего сохранения; вручную, пока не подтверждено' if s.get('conditions') else 'Вручную'
                out += ['**Подтверждение:** '+mode+'.', '']
                if key:
                    a = achievements[key]
                    out += ['#### Достижение', '', '**'+a['title']+'**', '', a['description'], '', image('embedded:achievements/'+key+'.jpg',a['title']), '']
                region = s.get('mapChapter','')
                out += ['#### Область и карта', '', geography.get(region, region) if region else 'Отдельная область не назначена.', '']
                source = region_source(region) if region else None
                if source: out += [image(source,geography.get(region,region)), '']
                elif region: out += ['Региональная карта в доступных материалах отсутствует. Можно предложить её ниже.', '']
                out += ['#### Иллюстрации пункта', '']
                for a in media.get(s['id'],[]): out += ['**'+a['label']+'**', '', image(a['url'],a['label']), '']
                if not media.get(s['id']): out += ['Отдельных иллюстраций нет.', '']
                out += ['<details>', '<summary>Служебные данные — для переноса в мод</summary>', '', '```json', json.dumps({'goal':goal,'chapterId':c['id'],'stepId':s['id'],'mapChapter':region,'conditions':s.get('conditions',[]),'achievement':key,'source':s.get('source','')},ensure_ascii=False,indent=2), '```', '', '</details>', '',
                        '#### Ваша проверка', '', '- [ ] Проверено', '', '**Что исправить:**', '', '**Чего не хватает:**', '', '**Добавить или заменить изображение:**', '', '---', '']
                occurrences.append({'marker':marker,'chapter':c['id'],'goal':goal,'step':s,'hidden':bool(hidden)})
    document.write_text('\n'.join(out),encoding='utf-8')
    shutil.copyfile(document,assets/'baseline/original-review.md')
    manifest = {'source_commit':'7af903f','occurrences':occurrences,'images':files,'document_sha256':hashlib.sha256(document.read_bytes()).hexdigest()}
    (assets/'baseline/manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
    validate(document, manifest)
    print('Exported:',len(chapters),'chapters,',len(occurrences),'steps,',len(set(x['file'] for x in files.values())),'unique local images')

def validate(document, manifest):
    text = document.read_text(encoding='utf-8')
    markers = re.findall(r'<!-- wisp:(.*?) -->',text)
    assert markers == [x['marker'] for x in manifest['occurrences']]
    bodies = re.findall(r'<!-- body:start -->\n(.*?)\n<!-- body:end -->',text,re.S)
    assert bodies == [x['step']['body'] for x in manifest['occurrences']]
    assert len(markers)==len(set(markers))==300
    for item in manifest['images'].values():
        path=document.parent/item['file']
        assert hashlib.sha256(path.read_bytes()).hexdigest()==item['sha256']
    assert not re.search(r'!\[[^\n]*\]\(https?://',text)

if __name__=='__main__':
    parser=argparse.ArgumentParser(); parser.add_argument('--destination',required=True); parser.add_argument('--cache',required=True)
    args=parser.parse_args(); export(args.destination,args.cache)
