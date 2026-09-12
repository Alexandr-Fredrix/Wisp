import json,pathlib,sys,tempfile,unittest
from unittest.mock import patch
sys.path.insert(0,str(pathlib.Path(__file__).resolve().parents[1]/'tools'))
import build_manifest

class BuildManifestTests(unittest.TestCase):
    def test_stale_dll_and_changed_source_are_rejected(self):
        with tempfile.TemporaryDirectory() as d:
            root=pathlib.Path(d)
            for name in ['src','content','tools','tests']: (root/name).mkdir()
            (root/'version.json').write_text('{}')
            (root/'src/test.cs').write_text('before')
            output=root/'artifacts';output.mkdir();(output/'Wisp.dll').write_bytes(b'MZtest')
            with patch.object(build_manifest,'ROOT',root),patch.object(build_manifest,'validate',return_value='1.0.1'):
                manifest={'version':'1.0.1','dll_sha256':build_manifest.digest(output/'Wisp.dll'),'inputs':build_manifest.inputs()}
                (output/'build-manifest.json').write_text(json.dumps(manifest))
                self.assertEqual(build_manifest.verify(output)['version'],'1.0.1')
                (root/'src/test.cs').write_text('after')
                with self.assertRaises(ValueError): build_manifest.verify(output)
                (root/'src/test.cs').write_text('before');(output/'Wisp.dll').write_bytes(b'MZchanged')
                with self.assertRaises(ValueError): build_manifest.verify(output)

    def test_new_untracked_source_is_included(self):
        with tempfile.TemporaryDirectory() as d:
            root=pathlib.Path(d);(root/'version.json').write_text('{}');(root/'src').mkdir();(root/'src/new.cs').write_text('new')
            with patch.object(build_manifest,'ROOT',root): self.assertIn('src/new.cs',build_manifest.inputs())
