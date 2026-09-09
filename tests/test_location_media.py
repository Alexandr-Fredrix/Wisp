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
                self.assertTrue(any(a.get('wide') for a in self.media.get(sid, [])), sid)

    def test_embedded_images_exist_and_have_captions(self):
        for sid, images in self.media.items():
            self.assertIn(sid, self.steps)
            for image in images:
                self.assertTrue(image['label'].strip(), sid)
                if image['url'].startswith('embedded:'):
                    self.assertTrue((ROOT/'content'/image['url'][9:]).is_file(), image['url'])

    def test_collection_and_mushroom_maps_complete(self):
        for sid in ['half-grubs', 'all-grubs']:
            self.assertEqual(sum('/Grub_' in i['url'] for i in self.media[sid]), 46)
        for n in range(1, 8):
            sid = f'mushroom-{n}-guide'
            self.assertTrue(self.steps[sid]['referenceOnly'])
            self.assertEqual(len(self.media[sid]), 2)

if __name__ == '__main__':
    unittest.main()
