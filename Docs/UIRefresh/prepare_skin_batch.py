"""Stage selected generated skins; never overwrite an installed sprite.

Local background extraction was explicitly authorized by the user. The raw
generation is immutable; only alpha changes before fitting a new sprite canvas.
Review the hole overlays and dark/light previews before marking a pair approved.
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
DOC = ROOT / 'Docs/UIRefresh'
OUT = DOC / 'SkinBatch01'


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def stage(item):
    key = item['id'] + '-' + item['kind']
    source = Path(item['source'])
    backup = OUT / 'Source' / (key + '.png')
    if not backup.exists():
        shutil.copy2(source, backup)
    assert sha(source) == sha(backup), key
    original = Image.open(backup)
    image = original.convert('RGBA')
    if item['half'] is not None:
        half = item['half']
        image = image.crop((half * image.width // 2, 0, (half+1) * image.width // 2, image.height))
    rgba = np.array(image)
    rgb = rgba[:, :, :3].astype('int16')
    lo, spread = rgb.min(2), rgb.max(2)-rgb.min(2)
    candidate = (lo >= item['threshold']) & (spread <= item['spread']) & (rgba[:, :, 3] > 0)
    _, labels, stats, _ = cv2.connectedComponentsWithStats(candidate.astype('uint8'), 8)
    outer = set(np.unique(np.concatenate((labels[0], labels[-1], labels[:, 0], labels[:, -1])))) - {0}
    selected = outer | set(item['holes'])
    for index in item['holes']:
        # Long ponytails can enclose a larger reviewed negative-space region.
        limit = .05 if index in item.get('reviewedLargeHoles', []) else .03
        assert 0 < index < len(stats) and stats[index, 4] < image.width * image.height * limit, (key, index)
    bg = np.isin(labels, list(selected)) | (rgba[:, :, 3] == 0)
    # Only neutral pale matte immediately adjacent to a confirmed background.
    fringe_min, fringe_spread, fringe_kernel = item.get('matteFringe', [180, 25, 5])
    assert 150 <= fringe_min <= 255 and 0 <= fringe_spread <= 80 and fringe_kernel in (3, 5, 7), key
    fringe = (cv2.dilate(bg.astype('uint8'), np.ones((fringe_kernel, fringe_kernel), 'uint8')) > 0) & (lo >= fringe_min) & (spread <= fringe_spread)
    clean = rgba.copy()
    clean[:, :, 3][bg | fringe] = 0
    # Reviewed low-contrast residue inside a specific negative-space region.
    # This is never a global gray/white removal; bounds are recorded per image.
    for region in item.get('matteRegions', []):
        x0, y0, x1, y1, minimum, maximum_spread = region
        assert 0 <= x0 < x1 <= image.width and 0 <= y0 < y1 <= image.height, key
        assert (x1-x0)*(y1-y0) < image.width*image.height*.01, key
        part = (lo[y0:y1, x0:x1] >= minimum) & (spread[y0:y1, x0:x1] <= maximum_spread)
        clean[y0:y1, x0:x1, 3][part] = 0
    # Local shadow cleanup must connect to already-confirmed empty background.
    # Closed highlights inside toes, eyes or white garments remain untouched.
    for region in item.get('connectedMatteRegions', []):
        x0, y0, x1, y1, minimum, maximum_spread = region
        assert 0 <= x0 < x1 <= image.width and 0 <= y0 < y1 <= image.height, key
        assert (x1-x0)*(y1-y0) < image.width*image.height*.01, key
        part = (lo[y0:y1, x0:x1] >= minimum) & (spread[y0:y1, x0:x1] <= maximum_spread)
        _, local_labels = cv2.connectedComponents(part.astype('uint8'), 8)
        empty = clean[y0:y1, x0:x1, 3] == 0
        touching = cv2.dilate(empty.astype('uint8'), np.ones((3, 3), 'uint8')) > 0
        connected = set(np.unique(local_labels[touching & part])) - {0}
        clean[y0:y1, x0:x1, 3][np.isin(local_labels, list(connected))] = 0
    # Explicitly reviewed detached matte specks only; never remove all small
    # components (which could include an intended sparkle or loose hair strand).
    if item.get('removeIslands'):
        _, islands, island_stats, _ = cv2.connectedComponentsWithStats((clean[:, :, 3] > 0).astype('uint8'), 8)
        for index in item['removeIslands']:
            assert 0 < index < len(island_stats) and island_stats[index, 4] <= 12, (key, index)
            clean[:, :, 3][islands == index] = 0
    # Some legacy sheets place a long LD blade across the vertical midpoint.
    # A reviewed seam through empty space separates figures without truncating
    # weapons or hair. This changes alpha only, never paints/reconstructs art.
    if item.get('keepPolygon'):
        points = item['keepPolygon']
        assert len(points) >= 4 and all(0 <= x < image.width and 0 <= y < image.height for x, y in points), key
        region = Image.new('L', image.size)
        ImageDraw.Draw(region).polygon([tuple(p) for p in points], fill=255)
        keep = np.array(region) > 0
        rim = keep & ~(cv2.erode(keep.astype('uint8'), np.ones((3, 3), 'uint8'), borderType=cv2.BORDER_CONSTANT, borderValue=0) > 0)
        assert not clean[:, :, 3][rim].any(), (key, 'Figure separation seam intersects artwork')
        clean[:, :, 3][~keep] = 0
    # Optional subpixel matte trim for reviewed LD edges. At source resolution
    # this fades only the outermost pixel; RGB and interior highlights stay intact.
    inset = item.get('edgeInsetPixels', 0)
    if inset:
        assert 0 < inset <= 1, key
        distance = cv2.distanceTransform((clean[:, :, 3] > 0).astype('uint8'), cv2.DIST_L2, 5)
        edge_alpha = np.rint(np.clip(distance - inset, 0, 1) * 255).astype('uint8')
        clean[:, :, 3] = np.minimum(clean[:, :, 3], edge_alpha)
    assert np.array_equal(clean[:, :, :3], rgba[:, :, :3]), key
    cut = Image.fromarray(clean)
    cut.save(OUT / 'Cutouts' / (key + '.png'))
    holes = []
    preview = Image.new('RGBA', image.size, '#23313e')
    preview.alpha_composite(cut)
    draw = ImageDraw.Draw(preview)
    for index, (x, y, w, h, area) in enumerate(stats[1:], 1):
        if index in outer or area < 3:
            continue
        holes.append({'component': index, 'xywh': [int(x), int(y), int(w), int(h)], 'area': int(area), 'removed': index in selected})
        if index not in selected and area >= 10:
            draw.rectangle((int(x), int(y), int(x+w), int(y+h)), outline='#20ff80', width=1)
            draw.text((int(x), int(y)), str(index), fill='#20ff80', stroke_width=1, stroke_fill='black')
    preview.convert('RGB').save(OUT / (key + '-holes.jpg'), quality=95)
    bbox = cut.getbbox()
    assert bbox and bbox[0] > 0 and bbox[1] > 0 and bbox[2] < cut.width and bbox[3] < cut.height, key
    crop = cut.crop(bbox)
    size = max(crop.size)
    margin = round(size * .045)
    canvas = Image.new('RGBA', (size + 2 * margin,) * 2)
    canvas.alpha_composite(crop, ((canvas.width-crop.width)//2, margin))
    target_size = 512 if item['kind'] == 'SD' else 1024
    canvas = canvas.resize((target_size,) * 2, Image.Resampling.LANCZOS)
    final = OUT / 'Ready' / (key + '.png')
    canvas.save(final)
    for label, color in [('dark', '#182631'), ('light', '#e2d8c8')]:
        preview = Image.new('RGBA', canvas.size, color)
        preview.alpha_composite(canvas)
        preview.convert('RGB').save(OUT / (key + '-' + label + '.jpg'), quality=95)
    return {'id': item['id'], 'kind': item['kind'], 'source': str(backup.relative_to(ROOT)), 'sourceSha256': sha(source), 'sourceMode': original.mode, 'alphaOnlyBeforeCanvasFit': True, 'holes': holes, 'bbox': bbox, 'preparedPath': str(final.relative_to(ROOT)), 'preparedSha256': sha(final), 'approved': item.get('approved', False)}


def main():
    global OUT
    parser = argparse.ArgumentParser()
    parser.add_argument('--apply', action='store_true')
    parser.add_argument('--batch', default='01', help='Two-digit batch number; defaults to the original batch')
    args = parser.parse_args()
    if len(args.batch) != 2 or any(c not in '0123456789' for c in args.batch):
        parser.error('--batch must contain exactly two ASCII digits')
    OUT = DOC / ('SkinBatch' + args.batch)
    for folder in ('Source', 'Cutouts', 'Ready'):
        (OUT/folder).mkdir(parents=True, exist_ok=True)
    manifest = json.loads((DOC/('skin-batch-' + args.batch + '.json')).read_text('utf-8'))
    keys = [(item['id'], item['kind']) for item in manifest['items']]
    assert len(keys) == len(set(keys)) and all(kind in ('SD', 'LD') for _, kind in keys), 'Duplicate or invalid output variants'
    assert all({kind for skin, kind in keys if skin == skin_id} == {'SD', 'LD'} for skin_id, _ in keys), 'Each skin needs both variants'
    records = [stage(item) for item in manifest['items']]
    if args.apply:
        assert all(r['approved'] for r in records), 'Visual approval required for all outputs.'
        destinations = [(ROOT/r['preparedPath'], ROOT/'Assets/Art/Characters/Skins'/f"{r['id']}-{r['kind']}.png") for r in records]
        assert all(not target.exists() or sha(source) == sha(target) for source, target in destinations), 'Refusing to overwrite an existing sprite.'
        for source, target in destinations:
            if not target.exists():
                shutil.copy2(source, target)
    (OUT/'report.json').write_text(json.dumps(records, indent=2), 'utf-8')
    print(json.dumps([{'id': r['id'], 'kind': r['kind'], 'sourceMode': r['sourceMode'], 'candidates': len(r['holes']), 'approved': r['approved']} for r in records]))


if __name__ == '__main__':
    main()
