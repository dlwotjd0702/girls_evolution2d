"""Verify the complete 96-pair catalog; visual approvals remain separate evidence."""
import hashlib
import json
import re
from collections import Counter
from datetime import datetime
from pathlib import Path

import numpy as np
from PIL import Image

from verify_skin_batch import load_alpha_revisions, assert_preserved

ROOT = Path(__file__).resolve().parents[2]
DOC = ROOT / 'Docs/UIRefresh'
SAVE_SHA = '55531c17baf405c188da20129310fd65007e2b6c2d8757611fefdb0d9771f408'


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    plan = json.loads((DOC/'skins-96-plan.json').read_text('utf-8'))['skins']
    assert len(plan) == 96
    for field in ('id', 'koreanName', 'englishName'):
        assert len({p[field] for p in plan}) == 96, field
    assert all(p['qualityStatus'] == 'approved' for p in plan)
    for level in range(1, 25):
        assert sorted(p['slot'] for p in plan if p['level'] == level) == [0, 1, 2, 3]
    assert all(1 <= p['level'] <= 24 for p in plan)

    scene_path = ROOT/'Assets/Scenes/Ingame.unity'
    scene = scene_path.read_text('utf-8')
    block = scene.split('  skins:\n', 1)[1].split('\n---', 1)[0]
    records = dict(re.findall(r'^  - id: (\S+)\n(.*?)(?=^  - id: |\Z)', block, re.M | re.S))
    assert set(records) == {p['id'] for p in plan}
    assert 'Readability Preview' not in scene
    before = DOC/'FinalSkinReview/Ingame-before-final-refresh.unity.txt'
    assert before.read_bytes() == scene_path.read_bytes(), 'Final refresh changed scene contents'

    assets = []
    for item in plan:
        record = records[item['id']]
        assert int(re.search(r'^    level: (\d+)$', record, re.M)[1]) == item['level']
        for prop in ('koreanName', 'englishName'):
            value = re.search(r'^    '+prop+r': (.*)$', record, re.M)[1]
            if value.startswith('"'):
                value = json.loads(value)
            assert value == item[prop], (item['id'], prop)
        for kind in ('SD', 'LD'):
            relative = item['expected'+kind.title()+'Path']
            path = ROOT/relative
            img = Image.open(path)
            assert img.mode == 'RGBA', relative
            alpha = np.asarray(img)[:, :, 3]
            assert not any((alpha[0].any(), alpha[-1].any(), alpha[:, 0].any(), alpha[:, -1].any())), relative
            assert .03 < (alpha > 0).mean() < .8, relative
            assert np.count_nonzero((alpha > 0) & (alpha < 255)) > 0, relative
            assert img.width == img.height and img.width <= (512 if kind == 'SD' else 1024), relative
            meta = Path(str(path)+'.meta').read_text('utf-8')
            guid = re.search(r'^guid: (\w+)$', meta, re.M)[1]
            assert re.search(r'^    '+kind.lower()+r': \{fileID: 21300000, guid: '+guid+r', type: 3\}$', record, re.M), relative
            for key, value in [('spriteMode', 1), ('alphaIsTransparency', 1), ('textureType', 8), ('enableMipMap', 0)]:
                assert re.search(r'^\s+'+key+': '+str(value)+'$', meta, re.M), (relative, key)
            assets.append({'id': item['id'], 'level': item['level'], 'slot': item['slot'], 'variant': kind,
                           'path': relative, 'sha256': sha(path), 'metaSha256': sha(Path(str(path)+'.meta')),
                           'guid': guid, 'dimensions': list(img.size),
                           'transparentPixels': int(np.count_nonzero(alpha == 0)),
                           'antialiasedPixels': int(np.count_nonzero((alpha > 0) & (alpha < 255)))})
    assert len(assets) == len({a['sha256'] for a in assets}) == len({a['guid'] for a in assets}) == 192
    expected_paths = {a['path'] for a in assets}
    assert {p.relative_to(ROOT).as_posix() for p in (ROOT/'Assets/Art/Characters/Skins').glob('*.png')} == expected_paths

    initial = json.loads((DOC/'ExistingSkinReview/initial-review.json').read_text('utf-8'))['records']
    assert len(initial) == 20 and all(r['status'] == 'approved' for r in initial)
    for record in initial:
        for kind, digest in record['finalSha256'].items():
            assert sha(ROOT/f"Assets/Art/Characters/Skins/{record['id']}-{kind}.png") == digest
    checks = []
    revisions = load_alpha_revisions(checks)
    protected_count = 0
    for snapshot in [DOC/'SkinBatch02/protected-hashes.json', DOC/'SkinBatch16/protected-hashes.json', ROOT/'Docs/ArtReview/AlphaRepair/protected-hashes.json']:
        for path, digest in json.loads(snapshot.read_text('utf-8')).items():
            assert_preserved(path, digest, revisions)
            protected_count += 1
    save = ROOT/'EditorSaves/save.json'
    assert sha(save) == SAVE_SHA
    reports = []
    for number in range(1, 17):
        path = DOC/f'SkinBatch{number:02}/verification.json'
        result = json.loads(path.read_text('utf-8'))
        assert result['status'] == 'PASS'
        reports.append({'batch': number, 'checks': len(result['checks']), 'sha256': sha(path)})
    install = (DOC/'skin-install-status.txt').read_text('utf-8')
    assert 'Registered pairs: 96\n' in install and 'Unregistered pairs: 0\n' in install
    assert 'Existing file pairs awaiting visual approval/reinspection: 0\n' in install
    regression = ROOT/'Logs/progression-checks.txt'
    assert regression.read_text('utf-8').startswith('PASS: 1049 checks\n')
    assert regression.stat().st_mtime >= max((ROOT/a['path']).stat().st_mtime for a in assets)
    (DOC/'skin-192-regression-checks.txt').write_bytes(regression.read_bytes())
    result = {'status': 'PASS', 'checkedAt': datetime.now().astimezone().isoformat(), 'levels': 24,
              'skinsPerLevel': 4, 'skinPairs': 96, 'imageAssets': 192, 'visuallyApprovedPairs': 96,
              'registeredPairs': 96, 'baseAppearanceIncluded': False, 'skipLevelsFrom': 25,
              'actualPlayerSaveSha256': sha(save), 'finalRefreshSceneUnchanged': True, 'previewFixtures': 0,
              'protectedHashAssertions': protected_count, 'validatedExistingRevisions': len(checks),
              'unityEditModeChecks': 1049, 'unityRegressionModifiedAt': datetime.fromtimestamp(regression.stat().st_mtime).astimezone().isoformat(),
              'dimensionCounts': dict(Counter(f"{a['variant']} {a['dimensions'][0]}x{a['dimensions'][1]}" for a in assets)),
              'batchVerification': reports, 'assets': assets,
              'limitations': 'Visual review is recorded separately from pixel checks. Legacy LD canvas dimensions are preserved. Play Mode, device builds, advertising and purchases were not run.'}
    (DOC/'skin-192-completion.json').write_text(json.dumps(result, ensure_ascii=False, indent=2)+'\n', 'utf-8')
    print(f"PASS: 96 approved pairs, 192 unique transparent sprites, 24 complete levels, 1049 Unity checks; {protected_count} preserved-hash assertions")


if __name__ == '__main__':
    main()
