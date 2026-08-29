"""User-authorized alpha-only cleanup, 2026-08-28. Never redraw or change RGB.

Stage first, inspect dark/light previews, then apply with --apply. Component IDs
refer only to the immutable source hashes in ld-residue-inventory.json.
"""
import argparse
import hashlib
import json
import shutil
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw
sys.path.insert(0, str(Path.home() / '.codex/tmp/girls-art-deps'))
import cv2

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Docs/ArtReview/AlphaRepair'
# Individually reviewed negative-space components, not a global white removal.
CONFIG = {
    'LD2': {'components': [10, 11, 13, 14, 15], 'edges': [(380, 35, 835, 1230)]},
    'LD10': {'components': [29, 30, 33, 34, 38, 44], 'edges': [(490, 40, 750, 275)]},
    'LD14': {'components': [154], 'edges': [(290, 40, 720, 445)]},
    'LD16': {'components': [10, 22, 23], 'edges': [(365, 0, 960, 255), (365, 300, 485, 470), (840, 260, 965, 480)]},
    'LD20': {'components': [630], 'threshold': 232, 'spread': 5, 'edges': [(110, 570, 300, 755), (110, 790, 300, 990), (880, 625, 1155, 905)], 'smooth_spikes': True},
    'LD24': {'components': [], 'edges': [(195, 285, 440, 610), (200, 610, 310, 735), (795, 445, 1160, 1190), (410, 840, 545, 1020)], 'small_edge_components': True},
    'lv01-a': {'components': [76, 79, 80, 81, 92, 95], 'edges': [(360, 30, 525, 212)]},
    'lv01-b': {'components': [141, 150, 164, 171, 184, 288, 313], 'edges': [(380, 30, 535, 216)]},
    'lv04-a': {'components': [37, 59, 62, 63, 68, 122, 134, 166], 'edges': [(395, 25, 750, 235), (635, 245, 750, 395)]},
    'rose-oath': {'components': [193, 284], 'threshold': 232, 'spread': 5, 'edges': []},
}


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def stage(key, item):
    config = CONFIG[key]
    original = OUT / 'Originals' / (key + '.png')
    if not original.exists():
        source = ROOT / item['path']
        assert digest(source) == item['sha256'], f'Unexpected source change: {key}'
        shutil.copy2(source, original)
    assert digest(original) == item['sha256'], key
    source = np.array(Image.open(original).convert('RGBA'))
    rgb = source[:, :, :3].astype(np.int16)
    lo, spread = rgb.min(2), rgb.max(2) - rgb.min(2)
    alpha = source[:, :, 3]
    candidate = (lo >= config.get('threshold', 215)) & (spread <= config.get('spread', 24)) & (alpha > 0)
    _, labels, stats, _ = cv2.connectedComponentsWithStats(candidate.astype('uint8'), 8)
    removed = np.zeros(alpha.shape, dtype=bool)
    selected = []
    for index in config['components']:
        assert 0 < index < len(stats), (key, index)
        x, y, w, h, area = map(int, stats[index])
        assert area < 6000, f'Unexpectedly large component: {key}/{index}'
        removed |= labels == index
        selected.append({'component': index, 'xywh': [x, y, w, h], 'area': area})
    # Limit expansion around white-haired characters so their fine strands survive.
    pale_hair=key in ('LD20','rose-oath')
    halo = (cv2.dilate(removed.astype('uint8'), np.ones((3 if pale_hair else 5,)*2, np.uint8)) > 0) & ~removed & (lo >= (215 if pale_hair else 150)) & (spread <= (8 if pale_hair else 35)) & (alpha > 0)
    edge_regions = np.zeros(alpha.shape, dtype=bool)
    for x0, y0, x1, y1 in config['edges']:
        edge_regions[y0:y1, x0:x1] = True
    near_clear = cv2.dilate((alpha == 0).astype('uint8'), np.ones((5, 5), np.uint8)) > 0
    fringe = edge_regions & near_clear & (lo >= 215) & (spread <= 24) & (alpha > 0)
    if config.get('small_edge_components'):
        for index, (_, _, _, _, area) in enumerate(stats[1:], 1):
            if area <= 150:
                component = labels == index
                if np.all(edge_regions[component]):
                    removed |= component
    result = source.copy()
    result[:, :, 3][removed | fringe | halo] = 0
    # Remove tiny detached matte fragments created at the inspected outer edges.
    foreground=(result[:,:,3]>0).astype('uint8')
    _, islands, island_stats, _=cv2.connectedComponentsWithStats(foreground,8)
    for index,(_,_,_,_,area) in enumerate(island_stats[1:],1):
        if area<=12:
            part=islands==index
            if np.all(edge_regions[part]) and np.all(lo[part]>=150) and np.all(spread[part]<=40):
                result[:,:,3][part]=0
    if config.get('smooth_spikes'):
        opened=cv2.morphologyEx(foreground,cv2.MORPH_OPEN,cv2.getStructuringElement(cv2.MORPH_ELLIPSE,(3,3)))
        spikes=(foreground>opened)&edge_regions&near_clear&(lo>=180)&(spread<=35)
        result[:,:,3][spikes]=0
    changed = result[:, :, 3] != alpha
    assert np.array_equal(result[:, :, :3], source[:, :, :3]), key
    assert np.all(result[:, :, 3] <= alpha), key
    assert 0 < changed.sum() < alpha.size * .02, key
    clean = OUT / 'Cleaned' / (key + '.png')
    Image.fromarray(result).save(clean)
    Image.fromarray((changed * 255).astype('uint8')).save(OUT / 'Masks' / (key + '.png'))
    for color, label in [('#182631', 'dark'), ('#e2d8c8', 'light')]:
        preview = Image.new('RGBA', (source.shape[1] * 2, source.shape[0] + 28), color)
        preview.alpha_composite(Image.fromarray(source), (0, 28))
        preview.alpha_composite(Image.fromarray(result), (source.shape[1], 28))
        draw = ImageDraw.Draw(preview)
        draw.text((12, 8), key + ' BEFORE', fill='#ffffff' if label == 'dark' else '#182631')
        draw.text((source.shape[1] + 12, 8), key + ' ALPHA ONLY', fill='#ffffff' if label == 'dark' else '#182631')
        preview.convert('RGB').save(OUT / f'{key}-{label}.jpg', quality=95)
    return {'id': key, 'path': item['path'], 'beforeSha256': item['sha256'], 'afterSha256': digest(clean), 'rgbUnchanged': True, 'changedAlphaPixels': int(changed.sum()), 'selectedComponents': selected, 'applied': False}


def main():
    args = argparse.ArgumentParser()
    args.add_argument('--apply', action='store_true')
    options = args.parse_args()
    for folder in ('Originals', 'Cleaned', 'Masks'):
        (OUT / folder).mkdir(parents=True, exist_ok=True)
    inventory = json.loads((ROOT / 'Docs/ArtReview/ld-residue-inventory.json').read_text('utf-8'))['items']
    records = [stage(key, inventory[key]) for key in CONFIG]
    if options.apply:
        for record in records:
            target = ROOT / record['path']
            assert digest(target) in (record['beforeSha256'], record['afterSha256']), f'Asset changed externally: {target}'
        for record in records:
            shutil.copy2(OUT / 'Cleaned' / (record['id'] + '.png'), ROOT / record['path'])
            record['applied'] = True
    (OUT / 'repair-report.json').write_text(json.dumps(records, ensure_ascii=False, indent=2), encoding='utf-8')
    print(json.dumps([{'id': r['id'], 'alphaPixels': r['changedAlphaPixels'], 'rgbUnchanged': r['rgbUnchanged'], 'applied': r['applied']} for r in records]))


if __name__ == '__main__':
    main()
