import json
import pathlib
import re
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
CYRILLIC = re.compile('[А-Яа-яЁё]')

class LocalizationTests(unittest.TestCase):
    def setUp(self):
        self.translations = self.read('english.json')

    def read(self, name):
        return json.loads((ROOT / 'content' / name).read_text(encoding='utf-8'))

    def translated(self, value):
        if CYRILLIC.search(value):
            self.assertIn(value, self.translations, value[:140])
            self.assertTrue(self.translations[value].strip())
            self.assertFalse(CYRILLIC.search(self.translations[value]), value[:140])

    def test_every_displayed_route_and_reference_is_translated(self):
        for chapter in self.read('route-pdf.json'):
            self.translated(chapter['title'])
            for step in chapter['steps']:
                if step.get('referenceOnly') and not step['id'].endswith('-guide'):
                    continue
                for key in ['title', 'body', 'warning']:
                    self.translated(step.get(key, ''))
        for chapter in self.read('route.json'):
            self.translated(chapter['title'])

    def test_enemies_achievements_and_image_captions(self):
        for enemy in self.read('enemies.json'):
            for key in ['name', 'warning', 'beforeInfection', 'afterInfection']:
                self.translated(enemy.get(key, ''))
        for caption in self.read('achievements.json').values():
            self.translated(caption['title'])
            self.translated(caption['description'])
        for images in self.read('step-media.json').values():
            for image in images:
                self.translated(image['label'])

    def test_ui_literals(self):
        lexer = re.compile(r'//[^\n]*|/\*[\s\S]*?\*/|@"(?:[^"]|"")*"|"(?:\\.|[^"\\])*"')
        for path in (ROOT / 'src/Wisp/UI').glob('*.cs'):
            if path.name == 'BundledFont.cs':
                continue
            for match in lexer.finditer(path.read_text(encoding='utf-8')):
                if not match[0].startswith('"'):
                    continue
                value = json.loads(match[0])
                if value != 'Язык: Русский':
                    self.translated(value)

    def test_translation_does_not_replace_progress_identifiers(self):
        for chapter in self.read('route-pdf.json'):
            self.assertNotIn(chapter['id'], self.translations)
            for step in chapter['steps']:
                self.assertNotIn(step['id'], self.translations)
                for rule in step.get('conditions', []):
                    self.assertNotIn(rule.get('field', ''), self.translations)
        self.assertEqual(set(self.read('regions-en.json')), set(self.read('regions.json')))

    def test_atlas_is_available_in_both_languages_without_network(self):
        from PIL import Image
        for name in ['regions.json', 'regions-en.json']:
            maps = self.read(name)
            self.assertEqual(len(maps), 15)
            for url in maps.values():
                self.assertTrue(url.startswith('embedded:'), url)
                with Image.open(ROOT / 'content' / url[9:]) as image:
                    image.verify()

    def test_reviewed_translation_details_do_not_regress(self):
        audit = json.loads((ROOT / 'tests/fixtures/ui-review-2026-09-13.json').read_text(encoding='utf-8'))
        for change in audit['translationCorrections']:
            self.assertEqual(self.translations[change['ru']], change['after'], change['id'])
        for chapter in self.read('route-pdf.json'):
            for step in chapter['steps']:
                body = self.translations.get(step['body'], step['body'])
                self.assertNotIn('Collections → Stag Stations', body)
                self.assertNotIn('Collections → Cartographer', body)
