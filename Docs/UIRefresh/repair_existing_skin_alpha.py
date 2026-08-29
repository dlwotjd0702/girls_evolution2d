"""Reviewed alpha-only repairs for installed sprites; never resize or alter RGB."""
import argparse
import hashlib
import json
import shutil
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw
sys.path.insert(0, str(Path.home()/'.codex/tmp/girls-art-deps'))
import cv2

ROOT = Path(__file__).resolve().parents[2]
DOC = ROOT/'Docs/UIRefresh'


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--batch', required=True)
    parser.add_argument('--apply', action='store_true')
    args = parser.parse_args()
    assert len(args.batch) == 2 and all(c in '0123456789' for c in args.batch)
    manifest = json.loads((DOC/f'existing-alpha-{args.batch}.json').read_text('utf-8'))
    out = DOC/f'SkinBatch{args.batch}'/'ExistingAlpha'
    for folder in ('Originals', 'Cleaned', 'Masks'):
        (out/folder).mkdir(parents=True, exist_ok=True)
    records = []
    for item in manifest['items']:
        key, target = item['key'], ROOT/item['path']
        original = out/'Originals'/f'{key}.png'
        if not original.exists():
            assert sha(target) == item['beforeSha256'], key
            shutil.copy2(target, original)
        assert sha(original) == item['beforeSha256'], key
        source = np.array(Image.open(original).convert('RGBA'))
        rgb = source[:, :, :3].astype('int16')
        lo, spread, alpha = rgb.min(2), rgb.max(2)-rgb.min(2), source[:, :, 3]
        candidate = (lo >= item.get('threshold', 215)) & (spread <= item.get('spread', 24)) & (alpha > 0)
        _, labels, stats, _ = cv2.connectedComponentsWithStats(candidate.astype('uint8'), 8)
        selected = np.isin(labels, item['components'])
        for index in item['components']:
            assert 0 < index < len(stats) and stats[index, 4] < alpha.size*.03, (key, index)
        halo = (cv2.dilate(selected.astype('uint8'), np.ones((3, 3), 'uint8')) > 0) & (lo >= 145) & (spread <= 40)
        regions = np.zeros(alpha.shape, bool)
        for x0, y0, x1, y1 in item.get('edgeRegions', []):
            assert 0 <= x0 < x1 <= alpha.shape[1] and 0 <= y0 < y1 <= alpha.shape[0]
            regions[y0:y1, x0:x1] = True
        near_clear = cv2.dilate((alpha == 0).astype('uint8'), np.ones((3, 3), 'uint8')) > 0
        fringe = regions & near_clear & (lo >= 150) & (spread <= 40)
        result = source.copy()
        result[:, :, 3][selected | halo | fringe] = 0
        # Fade the immediate edge only inside reviewed matte regions.
        if item.get('edgeInsetPixels'):
            inset = item['edgeInsetPixels']
            assert 0 < inset <= 1
            distance = cv2.distanceTransform((result[:, :, 3] > 0).astype('uint8'), cv2.DIST_L2, 5)
            edge_alpha = np.rint(np.clip(distance-inset, 0, 1)*255).astype('uint8')
            result[:, :, 3][regions] = np.minimum(result[:, :, 3], edge_alpha)[regions]
        assert np.array_equal(source[:, :, :3], result[:, :, :3])
        assert np.all(result[:, :, 3] <= alpha)
        changed = result[:, :, 3] != alpha
        assert changed.sum() < alpha.size*.05, key
        probes = item.get('alphaProbes')
        if probes:
            assert probes.get('clear') and probes.get('opaque'), key
            for x, y in probes['clear']:
                assert result[y, x, 3] == 0, (key, 'background residue', x, y)
            for x, y in probes['opaque']:
                assert result[y, x, 3] >= 250, (key, 'lost foreground', x, y)
        cleaned = out/'Cleaned'/f'{key}.png'
        Image.fromarray(result).save(cleaned)
        Image.fromarray((changed*255).astype('uint8')).save(out/'Masks'/f'{key}.png')
        candidates = []
        overlay = Image.new('RGBA', (alpha.shape[1], alpha.shape[0]), '#23313e')
        overlay.alpha_composite(Image.fromarray(result))
        draw = ImageDraw.Draw(overlay)
        for index, (x, y, w, h, area) in enumerate(stats[1:], 1):
            if area < 3:
                continue
            candidates.append({'component':index, 'xywh':list(map(int,(x,y,w,h))), 'area':int(area), 'removed':index in item['components']})
            if area >= 10 and index not in item['components']:
                draw.rectangle((int(x),int(y),int(x+w),int(y+h)), outline='#20ff80', width=1)
                draw.text((int(x),int(y)),str(index),fill='#20ff80',stroke_width=1,stroke_fill='black')
        overlay.convert('RGB').save(out/f'{key}-holes.jpg',quality=95)
        for name, color in [('dark','#182631'),('light','#e2d8c8')]:
            preview = Image.new('RGBA',(alpha.shape[1],alpha.shape[0]),color)
            preview.alpha_composite(Image.fromarray(result))
            preview.convert('RGB').save(out/f'{key}-{name}.jpg',quality=95)
        records.append({'key':key,'path':item['path'],'beforeSha256':sha(original),'afterSha256':sha(cleaned),'originalPath':str(original.relative_to(ROOT)),'cleanedPath':str(cleaned.relative_to(ROOT)),'rgbUnchanged':True,'changedAlphaPixels':int(changed.sum()),'candidates':candidates,'approved':item.get('approved',False),'applied':False})
        if probes:
            records[-1]['alphaProbes'] = probes
    if args.apply:
        assert all(r['approved'] and r['changedAlphaPixels'] > 0 for r in records)
        assert all(sha(ROOT/r['path']) in (r['beforeSha256'],r['afterSha256']) for r in records), 'Source changed externally'
        for r in records:
            shutil.copy2(ROOT/r['cleanedPath'],ROOT/r['path'])
            r['applied'] = True
    (out/'report.json').write_text(json.dumps(records,indent=2),encoding='utf-8')
    print(json.dumps([{'key':r['key'],'changedAlphaPixels':r['changedAlphaPixels'],'applied':r['applied']} for r in records]))


if __name__ == '__main__':
    main()
