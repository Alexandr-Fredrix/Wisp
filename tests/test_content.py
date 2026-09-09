import json
import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]

class ContentTests(unittest.TestCase):
    def setUp(self):
        self.route = json.loads((ROOT / 'content/route.json').read_text(encoding='utf-8'))
        self.enemies = json.loads((ROOT / 'content/enemies.json').read_text(encoding='utf-8'))

    def test_stable_ids_and_complete_steps(self):
        ids = [s['id'] for c in self.route for s in c['steps']]
        self.assertEqual(len(ids), len(set(ids)))
        self.assertEqual(len({c['id'] for c in self.route}), len(self.route))
        for chapter in self.route:
            self.assertTrue(chapter['steps'])
            for step in chapter['steps']:
                self.assertTrue(step['title'] and step['body'])
                for rule in step['conditions']:
                    self.assertIn(rule['kind'], ('bool', 'int'))
                    self.assertGreaterEqual(rule['minimum'], 1)

    def test_reminders_precede_irreversible_events(self):
        steps = [s['id'] for c in self.route for s in c['steps']]
        for reminder, trigger in [('hunter-lightseeds','broken-vessel'), ('hunter-grimm-nightmare','grimm-banish'), ('hunter-journal-start','zote-choice')]:
            self.assertLess(steps.index(reminder), steps.index(trigger))

    def test_enemy_catalog_has_regions_and_only_reference_links(self):
        self.assertEqual(len(self.enemies), len({e['id'] for e in self.enemies}))
        for enemy in self.enemies:
            self.assertTrue(enemy['regions'], enemy['id'])
            self.assertTrue(enemy['source'].startswith('https://hollowknight.wiki/w/'))
            self.assertNotIn('portrait', enemy)
            self.assertNotIn('maps', enemy)

    def test_mutually_exclusive_choices_are_never_autocompleted(self):
        manual = {'smith-choice','smith-kill','smith-spare','grimm-choice','grimm-banish','grimm-ritual','zote-choice','neglect'}
        for step in [s for c in self.route for s in c['steps']]:
            if step['id'] in manual:
                self.assertEqual(step['conditions'], [])

    def test_regions_do_not_include_nearby_entrances(self):
        enemies = {e['id']: e for e in self.enemies}
        guard = enemies['Husk_Guard']['regions']
        self.assertIn('Forgotten Crossroads', guard)
        self.assertIn('Infected Crossroads', guard)
        self.assertNotIn('Crystal Peak', guard)
        self.assertNotIn('Fog Canyon', guard)
        self.assertIn("Queen's Gardens", enemies['Mossy_Vagabond']['regions'])
        self.assertNotIn('Greenpath', enemies['Mossy_Vagabond']['regions'])
        self.assertIn('The Abyss', enemies['Shadow_Creeper_(Hollow_Knight)']['regions'])
        self.assertIn('Deepnest', enemies['Zote']['regions'])

if __name__ == '__main__':
    unittest.main()
