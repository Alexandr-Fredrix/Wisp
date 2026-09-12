import hashlib
import json
import pathlib
import re
import unittest
from PIL import Image

ROOT = pathlib.Path(__file__).resolve().parents[1]

def read(path):
    return json.loads((ROOT / path).read_text(encoding='utf-8'))

def occurrences(route):
    return {c['goal']+'/'+c['id']+'/'+s['id']: s for c in route for s in c['steps']}

class CollectionsTransferTests(unittest.TestCase):
    def setUp(self):
        self.audit = read('tests/fixtures/studio-transfer-2026-09-13.json')
        self.route = read('content/route-pdf.json')
        self.groups = read('content/collections.json')
        self.media = read('content/step-media.json')

    def test_all_playable_ids_conditions_and_choices_survive(self):
        old = {k:s for k,s in occurrences(self.audit['routeBefore']).items() if not s.get('referenceOnly')}
        new = {k:s for k,s in occurrences(self.route).items() if not s.get('referenceOnly')}
        self.assertEqual(len(old), 167)
        self.assertEqual(old.keys(), new.keys())
        for key in old:
            for field in ['id', 'conditions', 'warning', 'spoiler', 'mapChapter']:
                self.assertEqual(old[key].get(field), new[key].get(field), (key, field))

    def test_current_route_matches_reviewed_transfer_and_catalogue_move(self):
        correction = read('tests/fixtures/ui-review-2026-09-13.json')
        self.assertEqual(self.route, correction['routeAfter'])
        before = occurrences(self.audit['routeAfterCollections'])
        after = occurrences(self.route)
        changed = {k for k in before if before[k] != after[k]}
        self.assertEqual(changed, set(correction['restoredRouteOccurrences']))
        self.assertEqual(len(self.audit['changedOccurrences']), 13)
        self.assertEqual(len(self.audit['removedCollectionChapters']), 9)
        for c in self.route:
            self.assertFalse(re.search(r'-ref-(17|19|20)$', c['id']))
        first = self.route[0]['steps']
        self.assertEqual([s['id'] for s in first[:3]], ['pdf-a1-guide', 'movement', 'first-charm'])

    def test_exact_occurrence_galleries_and_empty_deletions_survive(self):
        for key, images in self.audit['mediaOverrides'].items():
            if key.endswith('/all-charms'):
                continue  # Its edited map now belongs to the charm card below.
            expected = json.loads(json.dumps(images))
            for image in expected:
                if re.fullmatch(r'Снимок экрана 2026-09-13 010(?:614|634|647)\.png', image['label']):
                    image['label'] = ''
            self.assertEqual(self.media[key], expected, key)
        for n in range(1,5):
            self.assertEqual(self.media[f'112/pdf-a14/pantheon-{n}'], [])
        self.assertEqual(self.media['speed/pdf-b1/well'], [])
        self.assertTrue(self.media['well'])
        flower = self.media['112/pdf-a12/delicate-flower']
        self.assertEqual(sum('studio-' in i['url'] and any(h in i['url'] for h in ['3c5a6f2508','cf3abe06e3','a82b285f54','720c1a93fc']) for i in flower),4)

    def test_collection_completeness_and_local_images(self):
        self.assertEqual({g['id']:len(g['items']) for g in self.groups},
                         {'charms':40,'grubs':46,'masks':16,'vessels':9})
        ids = [i['id'] for g in self.groups for i in g['items']]
        self.assertEqual(len(ids), len(set(ids)))
        self.assertEqual(set(i['charmId'] for i in self.groups[0]['items']),set(range(1,41)))
        urls = set()
        for group in self.groups:
            for item in group['items']:
                self.assertTrue(item['title'] and item['location'] and item['maps'])
                self.assertTrue(item['charmId'] or item['icon'])
                self.assertEqual(bool(item['effect']), group['id']=='charms')
                urls.update(a['url'] for a in item['maps'])
                if item['icon']: urls.add(item['icon'])
        urls.update(a['url'] for images in self.audit['mediaOverrides'].values() for a in images)
        for url in urls:
            self.assertTrue(url.startswith('embedded:'), url)
            path = ROOT/'content'/url[9:]
            with Image.open(path) as im: im.verify()
            if path.name.startswith('studio-'):
                self.assertEqual(hashlib.sha256(path.read_bytes()).hexdigest(),path.stem[7:])
        hive = next(i for i in self.groups[0]['items'] if i['id']=='charm-29')
        self.assertIn('af7854c0bb8687', hive['maps'][0]['url'])

    def test_translated_cards_and_no_return_of_global_catalogue_galleries(self):
        translations=read('content/english.json')
        def visit(value):
            if isinstance(value,dict):
                for v in value.values(): visit(v)
            elif isinstance(value,list):
                for v in value: visit(v)
            elif isinstance(value,str) and re.search('[А-Яа-яЁё]',value):
                self.assertIn(value,translations,value[:100])
                self.assertFalse(re.search('[А-Яа-яЁё]', translations[value]),value[:100])
        visit(self.groups)
        for c in self.route:
            for s in c['steps']:
                if s.get('collection'):
                    self.assertIn(s['collection'], {g['id'] for g in self.groups})
                    self.assertNotIn(s['id'], self.media)

if __name__ == '__main__':
    unittest.main()
