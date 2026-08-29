"""Pixel/asset checks complement visual QA; they do not certify drawing quality."""
import argparse
import hashlib
import json
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
DOC = ROOT / 'Docs/UIRefresh'
ALPHA = ROOT / 'Docs/ArtReview/AlphaRepair'


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def load_alpha_revisions(checks):
    revisions = {}
    applied = []
    for report in sorted(DOC.glob('SkinBatch*/ExistingAlpha/report.json')):
        for record in json.loads(report.read_text('utf-8')):
            if not record.get('applied'):
                continue
            assert record['approved'], record['key']
            before_path, after_path = ROOT/record['originalPath'], ROOT/record['cleanedPath']
            assert sha(before_path) == record['beforeSha256']
            assert sha(after_path) == record['afterSha256']
            before, after = np.array(Image.open(before_path).convert('RGBA')), np.array(Image.open(after_path).convert('RGBA'))
            assert before.shape == after.shape and np.array_equal(before[:, :, :3], after[:, :, :3])
            assert np.all(after[:, :, 3] <= before[:, :, 3])
            assert np.count_nonzero(after[:, :, 3] != before[:, :, 3]) == record['changedAlphaPixels'] > 0
            probes = record.get('alphaProbes')
            if probes:
                assert probes.get('clear') and probes.get('opaque'), record['key']
                for x, y in probes['clear']:
                    assert after[y, x, 3] == 0, (record['key'], 'background residue', x, y)
                for x, y in probes['opaque']:
                    assert after[y, x, 3] >= 250, (record['key'], 'lost foreground', x, y)
            key = (Path(record['path']).as_posix(), record['beforeSha256'])
            assert key not in revisions, 'Ambiguous alpha revision'
            revisions[key] = record['afterSha256']
            applied.append(record)
            checks.append('Reviewed existing sprite alpha repair preserves RGB and dimensions: '+record['key'])
    # A visual correction is different from alpha cleanup. Validate its complete
    # source/cutout/approval chain before allowing a protected hash to advance.
    allowed_corrections = {'17': {('lv05-b','LD'), ('moon-patrol','SD')},
                          '19': {('lv01-a','SD'), ('lv01-b','SD')}}
    for correction_batch, allowed in allowed_corrections.items():
        correction_report = DOC/f'SkinBatch{correction_batch}/replacements.json'
        if not correction_report.exists():
            continue
        manifest = json.loads((DOC/f'skin-batch-{correction_batch}.json').read_text('utf-8'))
        items = {(i['id'], i['kind']): i for i in manifest['items']}
        for record in json.loads(correction_report.read_text('utf-8')):
            assert record['approved'] and record['applied'] and record['reason']
            item = items[(record['id'], record['kind'])]
            assert item['approved'] and item['reviewNotes'] and item['prompt']
            assert (record['id'], record['kind']) in allowed
            assert sha(ROOT/record['originalPath']) == record['beforeSha256']
            assert sha(ROOT/record['preparedPath']) == record['afterSha256']
            assert sha(ROOT/record['source']) == record['sourceSha256']
            assert sha(ROOT/(record['path']+'.meta')) == record['metaSha256']
            source = np.array(Image.open(ROOT/record['source']).convert('RGBA'))
            cut = np.array(Image.open(DOC/f'SkinBatch{correction_batch}/Cutouts'/f"{record['id']}-{record['kind']}.png"))
            assert np.array_equal(source[:,:,:3], cut[:,:,:3])
            assert np.all(cut[:,:,3] <= source[:,:,3])
            for x,y in item['alphaProbes']['clear']:
                assert cut[y,x,3] == 0
            for x,y in item['alphaProbes']['opaque']:
                assert cut[y,x,3] >= 250
            expected = 512 if record['kind']=='SD' else 1024
            final = Image.open(ROOT/record['preparedPath'])
            assert final.mode == 'RGBA' and final.size == (expected,expected)
            alpha = np.array(final)[:,:,3]
            assert not any((alpha[0].any(),alpha[-1].any(),alpha[:,0].any(),alpha[:,-1].any()))
            key = (Path(record['path']).as_posix(),record['beforeSha256'])
            assert key not in revisions, 'Ambiguous sprite revision'
            revisions[key] = record['afterSha256']
            applied.append(record)
            checks.append('Reviewed appearance correction with preserved sprite GUID: '+record['id'])
    for record in applied:
        assert_preserved(record['path'], record['afterSha256'], revisions)
    return revisions


def assert_preserved(path, digest, revisions):
    """A protected hash advances only through validated, approved revision records."""
    normalized = Path(path).as_posix()
    actual = sha(ROOT/path)
    visited = set()
    while digest != actual:
        key = (normalized, digest)
        assert key in revisions and key not in visited, path
        visited.add(key)
        digest = revisions[key]
    return bool(visited)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--batch', default='01')
    args = parser.parse_args()
    if len(args.batch) != 2 or any(c not in '0123456789' for c in args.batch):
        parser.error('--batch must contain exactly two ASCII digits')
    batch_dir = DOC / ('SkinBatch' + args.batch)
    checks = []
    revisions = load_alpha_revisions(checks)
    for record in json.loads((ALPHA/'repair-report.json').read_text('utf-8')):
        target = ROOT/record['path']
        before = np.array(Image.open(ALPHA/'Originals'/(record['id']+'.png')).convert('RGBA'))
        cleaned = ALPHA/'Cleaned'/(record['id']+'.png')
        after = np.array(Image.open(cleaned).convert('RGBA'))
        assert record['applied'] and sha(cleaned) == record['afterSha256']
        assert_preserved(record['path'], record['afterSha256'], revisions)
        assert before.shape == after.shape and np.array_equal(before[:, :, :3], after[:, :, :3])
        assert np.all(after[:, :, 3] <= before[:, :, 3])
        assert np.count_nonzero(before[:, :, 3] != after[:, :, 3]) == record['changedAlphaPixels']
        checks.append('Existing art RGB/dimensions preserved; only reviewed alpha changed: '+record['id'])
    protected = json.loads((ALPHA/'protected-hashes.json').read_text('utf-8'))
    for path, digest in protected.items():
        assert sha(ROOT/path) == digest, path
    checks.append('All 44 existing LD meta files and the player save are byte-identical to the pre-cleanup snapshot')
    batch = json.loads((DOC/('skin-batch-' + args.batch + '.json')).read_text('utf-8'))
    reports = json.loads((batch_dir/'report.json').read_text('utf-8'))
    assert reports and len(batch['items']) == len(reports), 'Incomplete batch report'
    snapshot = batch_dir / 'protected-hashes.json'
    if snapshot.exists():
        batch_protected = json.loads(snapshot.read_text('utf-8'))
        repaired = sum(assert_preserved(path, digest, revisions) for path, digest in batch_protected.items())
        checks.append(f'{len(batch_protected)-repaired} pre-batch art/save files are unchanged; {repaired} have validated, individually approved revisions')
    for item, record in zip(batch['items'], reports):
        assert (item['id'], item['kind']) == (record['id'], record['kind']) and record['approved']
        key = item['id']+'-'+item['kind']
        original = Image.open(ROOT/record['source']).convert('RGBA')
        assert sha(ROOT/record['source']) == record['sourceSha256'], key
        if item['half'] is not None:
            half = item['half']
            original = original.crop((half*original.width//2, 0, (half+1)*original.width//2, original.height))
        source = np.array(original)
        cut = np.array(Image.open(batch_dir/'Cutouts'/(key+'.png')))
        assert np.array_equal(source[:, :, :3], cut[:, :, :3]), key
        assert np.all(cut[:, :, 3] <= source[:, :, 3]), key
        probes = item.get('alphaProbes')
        if probes:
            assert probes.get('clear') and probes.get('opaque'), key
            for x, y in probes['clear']:
                assert cut[y, x, 3] == 0, (key, 'background residue', x, y)
            for x, y in probes['opaque']:
                assert cut[y, x, 3] >= 250, (key, 'lost foreground', x, y)
            checks.append('Reviewed internal gaps are transparent; face/clothing/highlights stay opaque: '+key)
        asset = ROOT/'Assets/Art/Characters/Skins'/(key+'.png')
        final = Image.open(asset)
        assert sha(asset) == record['preparedSha256'] and final.mode == 'RGBA', key
        assert final.size == ((512, 512) if item['kind']=='SD' else (1024, 1024)), key
        alpha = np.array(final)[:, :, 3]
        assert not any((alpha[0].any(),alpha[-1].any(),alpha[:,0].any(),alpha[:,-1].any())), key
        assert .03 < (alpha>0).mean() < .8 and np.count_nonzero((alpha>0)&(alpha<255))>0, key
        checks.append('Reviewed RGBA sprite, clear border, antialiased alpha and unchanged source colors: '+key)
    assert len({r['preparedSha256'] for r in reports}) == len(reports)
    checks.append(f'All {len(reports)} new SD/LD images are distinct')
    result = {'status':'PASS','checks':checks,'limitation':'Pixel checks do not replace SD/LD visual review. Alpha approval applies only to explicitly reviewed files; remaining registered art still needs individual review.'}
    (batch_dir/'verification.json').write_text(json.dumps(result,indent=2),'utf-8')
    print('PASS:',len(checks),'pixel/preservation checks')


if __name__=='__main__':
    main()
