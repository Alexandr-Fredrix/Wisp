import json
import pathlib
import unittest

ROOT=pathlib.Path(__file__).resolve().parents[1]
class HabitatMapTests(unittest.TestCase):
    def test_maps_preserved_and_metadata_valid(self):
        media=json.loads((ROOT/'content/media.json').read_text(encoding='utf-8'))
        enemies=json.loads((ROOT/'content/enemies.json').read_text(encoding='utf-8'))
        regions={r for e in enemies for r in e['regions']}
        self.assertEqual(sum(len(e['habitatMaps']) for e in media.values()),239)
        for entry in media.values():
            for m in entry['habitatMaps']:
                self.assertTrue(m['url'].startswith('https://cdn.wikimg.net/'))
                self.assertTrue(set(m['regions'])<=regions)
                self.assertIn(m['phase'],['unknown','both','before-infection','after-infection'])
                if not m['caption']: self.assertEqual(m['regions'],[])

    def test_greenpath_and_infected_records(self):
        media=json.loads((ROOT/'content/media.json').read_text(encoding='utf-8'))
        self.assertIn('Greenpath',media['Vengefly']['habitatMaps'][3]['regions'])
        enemies=json.loads((ROOT/'content/enemies.json').read_text(encoding='utf-8'))
        infected=[e['id'] for e in enemies if 'Infected Crossroads' in e['regions'] and 'Forgotten Crossroads' not in e['regions']]
        self.assertEqual(len(infected),5)
