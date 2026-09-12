"""Prove removal changes no visible instruction, condition, route order or media."""
import hashlib,json,pathlib,re,unittest
ROOT=pathlib.Path(__file__).resolve().parents[1]
def read(path):return json.loads((ROOT/path).read_text(encoding='utf-8'))
def digest(x):return hashlib.sha256(json.dumps(x,ensure_ascii=False,sort_keys=True,separators=(',',':')).encode()).hexdigest()
def norm(x):return re.sub(r'\s+',' ',x).strip()
class HiddenReferenceCleanupTests(unittest.TestCase):
 def setUp(self):
  self.route=read('tests/fixtures/studio-transfer-2026-09-13.json')['routeBefore'];self.audit=read('tests/fixtures/hidden-references-2026-09-12.json');self.media=read('content/step-media.json')
 def test_every_visible_field_and_order_is_preserved(self):
  expected=self.audit['chapters'];self.assertEqual([c['id'] for c in self.route],[c['chapter'] for c in expected])
  for c,old in zip(self.route,expected):
   self.assertEqual(digest(c['steps']),old['retained_sha256'])
   self.assertEqual(digest({k:v for k,v in c.items() if k!='steps'}),old['chapter_metadata_sha256'])
   self.assertTrue(all(not s.get('referenceOnly') or s['id'].endswith('-guide') for s in c['steps']))
  self.assertEqual({g:sum(len(c['steps']) for c in self.route if c['goal']==g) for g in ('112','speed','steel')},{'112':121,'speed':46,'steel':46})
 def test_each_removal_has_same_stage_coverage(self):
  self.assertEqual(len(self.audit['removed']),87)
  for r in self.audit['removed']:
   c=next(c for c in self.route if c['id']==r['chapter'] and c['goal']==r['goal']);lookup={s['id']:s for s in c['steps']}
   self.assertNotIn(r['id'],lookup);self.assertFalse(r['original']['conditions']);self.assertFalse(r['original']['warning']);self.assertFalse(self.media.get(r['id']))
   text=r['original']['body']
   if r['id']=='pdf-a4-instruction-6':
    self.assertTrue(text.endswith(' (Тень'));text=text.removesuffix(' (Тень');self.assertIn('После A5',lookup['pdf-shade-soul']['body'])
   self.assertTrue(any(norm(text) in norm(lookup[key]['body']) for key in r['coverage']))
 def test_collections_and_seven_mushroom_images_retained(self):
  for goal in ('112','speed','steel'):
   refs=[c for c in self.route if c['goal']==goal and '-ref-' in c['id']];self.assertEqual(len(refs),6)
   last=refs[-1]['steps'][0]['body']
   for n in range(1,8):self.assertIn(str(n)+'. ',last)
  for n in range(1,8):self.assertTrue(self.media.get(f'mushroom-{n}-guide'))
if __name__=='__main__':unittest.main()
