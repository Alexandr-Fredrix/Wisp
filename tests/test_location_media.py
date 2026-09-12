import json
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[1]

class LocationMediaTests(unittest.TestCase):
    def setUp(self):
        self.media = json.loads((ROOT/'content/step-media.json').read_text(encoding='utf-8'))
        self.steps = {s['id']: s for c in json.loads((ROOT/'content/route-pdf.json').read_text(encoding='utf-8')) for s in c['steps']}

    def test_every_concrete_route_target_has_location_picture(self):
        # These tasks concern whole-save statistics or starting a save, not a location.
        abstract = {'complete-100', 'complete-112', 'speed-10', 'speed-5', 'speed-100', 'steel-start', 'steel-ending', 'steel-100'}
        for sid, step in self.steps.items():
            if not step.get('referenceOnly') and sid not in abstract:
                self.assertTrue(step.get('collection') or any(a.get('wide') for a in self.media.get(sid, [])), sid)

    def test_embedded_images_exist_and_have_captions(self):
        for sid, images in self.media.items():
            self.assertIn(sid.split('/')[-1], self.steps)
            for image in images:
                # A blank caption was explicitly supplied in the flower-delivery edit.
                self.assertTrue(image['label'].strip() or (sid == '112/pdf-a12/delicate-flower' and any(h in image['url'] for h in ['3c5a6f2508678964','cf3abe06e3','a82b285f54','720c1a93fc'])), sid)
                if image['url'].startswith('embedded:'):
                    self.assertTrue((ROOT/'content'/image['url'][9:]).is_file(), image['url'])

    def test_collection_and_mushroom_maps_complete(self):
        collections = json.loads((ROOT/'content/collections.json').read_text(encoding='utf-8'))
        grubs = next(g for g in collections if g['id'] == 'grubs')['items']
        self.assertEqual(len(grubs), 46)
        self.assertEqual(sum('/Grub_' in a['url'] for item in grubs for a in item['maps']), 46)
        for n in range(1, 8):
            sid = f'mushroom-{n}-guide'
            self.assertTrue(self.steps[sid]['referenceOnly'])
            self.assertEqual(len(self.media[sid]), 2)

if __name__ == '__main__':
    unittest.main()
