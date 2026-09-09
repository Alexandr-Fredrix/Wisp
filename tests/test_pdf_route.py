import json
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[1]

class PdfRouteTests(unittest.TestCase):
    def setUp(self):
        self.chapters = json.loads((ROOT / 'content/route-pdf.json').read_text(encoding='utf-8'))

    def test_all_pdf_stages_and_three_goals(self):
        ids = [c['id'] for c in self.chapters]
        self.assertEqual(len(ids), len(set(ids)))
        self.assertEqual({c['goal'] for c in self.chapters}, {'112', 'speed', 'steel'})
        for n in range(1, 16):
            self.assertIn('pdf-a' + str(n), ids)
        for c in self.chapters:
            self.assertTrue(c['steps'])
            self.assertTrue(c['steps'][0]['referenceOnly'])
            for s in c['steps']:
                self.assertTrue(s['title'] and s['body'])
                if s.get('referenceOnly'):
                    self.assertEqual(s['conditions'], [])

    def test_irreversible_choices_stay_on_their_save_path(self):
        goals = {g: [s['id'] for c in self.chapters if c['goal'] == g for s in c['steps']] for g in ('112','speed','steel')}
        a = goals['112']
        self.assertLess(a.index('ending-a'), a.index('void-choice'))
        self.assertLess(a.index('pantheon-5'), a.index('grey-prince'))
        self.assertLess(a.index('hunter-lightseeds'), a.index('broken-vessel'))
        for x in ('neglect','smith-kill','grimm-banish'):
            self.assertNotIn(x, a)
            self.assertIn(x, goals['speed'])
        self.assertIn('steel-100', goals['steel'])
        self.assertNotIn('grimm-ritual', goals['steel'])

    def test_gardens_and_return_after_credits(self):
        a10 = next(c for c in self.chapters if c['id'] == 'pdf-a10')
        self.assertIn('Сады королевы', a10['title'])
        self.assertTrue(any(s['id']=='pdf-love-key' and s['mapChapter']=='queens-gardens' for s in a10['steps']))
        self.assertFalse(any(s['id']=='resume-save' for c in self.chapters for s in c['steps']))

if __name__ == '__main__':
    unittest.main()
