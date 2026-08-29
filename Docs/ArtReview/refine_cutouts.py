"""Local background extraction authorized by the user, 2026-08-28.

No image generation. Sources stay in Redraws; output is staged for visual QA.
Dependencies: Pillow, numpy, scipy, opencv-python-headless.
"""
import json
from pathlib import Path
import cv2
import numpy as np
from PIL import Image, ImageDraw, ImageOps
from scipy import ndimage

ROOT = Path(__file__).resolve().parents[2]
DOC = ROOT / 'Docs/ArtReview'
OUT = DOC / 'Cutouts'
OUT.mkdir(exist_ok=True)
# Enclosed checkerboard holes selected from numbered QA overlays, not white details.
HOLES = {
    2: [2, 10, 11, 19],
    6: [8, 10, 24, 33, 34, 45, 82, 89],
    7: [36, 41, 43],
    10: [2, 3, 6, 10, 17, 25, 30, 38, 44, 64],
    11: [6, 16, 20, 22, 38, 67, 83, 92, 130, 137, 138, 157],
    12: [15, 30, 31, 51, 62, 109, 113, 116, 126, 130, 134, 158],
    13: [22, 32],
    14: [13, 36, 27, 186],
    15: [4, 9, 10, 11, 12, 14, 15, 17, 18, 19, 26, 27, 31],
    16: [2, 3, 6, 8, 9, 11, 12, 30, 73, 78, 80, 116],
    17: [8, 14, 21, 25, 30, 31, 62],
    19: [108],
    20: [309, 347, 432, 434, 440, 499, 546, 472, 550, 563, 586, 588, 737, 842, 844, 845, 850, 889, 891, 921],
    23: [121, 242, 246, 271, 281, 322],
    24: [7, 9, 10, 14, 16, 24, 26, 30, 31, 33, 34, 35, 36, 37, 40, 45, 47, 49, 56, 60, 62, 64, 67, 71, 73, 74],
}

def extract(path):
    rgb = np.array(Image.open(path).convert('RGB'))
    lo, hi = rgb.min(axis=2), rgb.max(axis=2)
    border = np.concatenate((lo[:12].ravel(), lo[-12:].ravel(), lo[:, :12].ravel(), lo[:, -12:].ravel()))
    threshold = max(220, int(np.percentile(border, 0.2)) - 4)
    candidate = ((hi.astype(int) - lo <= 5) & (lo >= threshold)).astype('uint8')
    count, labels, stats, centers = cv2.connectedComponentsWithStats(candidate, 8)
    outside = set(np.unique(np.concatenate((labels[0], labels[-1], labels[:, 0], labels[:, -1])))) - {0}
    level = int(path.stem.split('-')[0])
    removed = outside | set(HOLES.get(level, []))
    background = np.isin(labels, list(removed))
    # Compression leaves near-neutral white specks just outside the contour.
    # Limit this cleanup to a two-pixel boundary band, never the white armor interior.
    boundary = ndimage.binary_dilation(background, iterations=2)
    background |= boundary & (hi.astype(int) - lo <= 22) & (lo >= 216)
    hard = ~background
    # Recover edge colors from the nearest opaque interior rather than leaving white fringes.
    core = ndimage.binary_erosion(hard, iterations=2)
    _, indices = ndimage.distance_transform_edt(~core, return_indices=True)
    nearest = rgb[tuple(indices)].astype(float)
    edge = hard & ~core
    b = 249.0
    numerator = ((rgb.astype(float) - b) * (nearest - b)).sum(axis=2)
    denominator = ((nearest - b) ** 2).sum(axis=2)
    coverage = np.clip(numerator / np.maximum(denominator, 1), 0, 1)
    alpha = hard.astype(float)
    alpha[edge] = coverage[edge]
    clean = rgb.copy()
    clean[edge] = nearest[edge].astype('uint8')
    rgba = np.dstack((clean, (alpha * 255).round().astype('uint8')))
    rgba[rgba[:, :, 3] == 0, :3] = 0
    output = Image.fromarray(rgba)
    output.save(OUT / f'{level}.png')
    preview = Image.new('RGBA', output.size, '#343b4b')
    preview.alpha_composite(output)
    d = ImageDraw.Draw(preview)
    enclosed = []
    for i in range(1, count):
        x, y, w, h, area = stats[i]
        if i in outside or area < 20:
            continue
        enclosed.append({'id': i, 'xywh': [int(x), int(y), int(w), int(h)], 'area': int(area)})
        if i not in removed:
            d.rectangle((int(x), int(y), int(x+w), int(y+h)), outline='#00ff78', width=2)
            d.text((int(x), int(y)), str(i), fill='#00ff78', stroke_width=1, stroke_fill='black')
    preview.convert('RGB').save(OUT / f'{level}-holes.jpg')
    return {'level': level, 'threshold': threshold, 'source': str(path.relative_to(ROOT)),
            'transparentFraction': float((rgba[:, :, 3] == 0).mean()), 'holes': enclosed}

def sheet(paths, target, cell=(350, 420), columns=4, background='#343b4b'):
    out = Image.new('RGB', (columns*cell[0], ((len(paths)+columns-1)//columns)*cell[1]), background)
    d = ImageDraw.Draw(out)
    for i, path in enumerate(paths):
        im = Image.open(path).convert('RGBA')
        im.thumbnail((cell[0]-16, cell[1]-35), Image.Resampling.LANCZOS)
        x,y=(i%columns)*cell[0],(i//columns)*cell[1]
        out.paste(im, (x+(cell[0]-im.width)//2, y+30), im)
        d.text((x+10,y+8), path.stem, fill='white')
    out.save(target)

if __name__ == '__main__':
    paths = sorted((DOC/'Redraws').glob('*-LD.png'), key=lambda p:int(p.stem.split('-')[0]))
    records = [extract(p) for p in paths]
    (OUT/'alpha-report.json').write_text(json.dumps(records, indent=2), encoding='utf-8')
    sheet([OUT/f'{r["level"]}.png' for r in records], DOC/'cutouts-review.png')
    print(json.dumps([{'level':r['level'], 'holes':len(r['holes']), 'alpha0':r['transparentFraction']} for r in records]))
