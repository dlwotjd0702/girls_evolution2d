"""Snapshot actual Addressables, verify original preservation, and build paired visual QA sheets."""
import hashlib
import json
import re
import sys
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
DOC = ROOT / 'Docs/ArtReview'
LEVELS = [2, 6, 7, 10, 11, 12, 13, 14, 15, 16, 17, 19, 20, 23, 24]

def inventory():
    guids = {}
    for meta in (ROOT/'Assets').rglob('*.png.meta'):
        match = re.search(r'^guid: (\w+)', meta.read_text(encoding='utf-8'), re.M)
        if match:
            guids[match[1]] = meta.with_suffix('')
    items = {}
    for group in (ROOT/'Assets/AddressableAssetsData/AssetGroups').glob('*.asset'):
        for entry in re.split(r'\n  - m_GUID: ', group.read_text(encoding='utf-8'))[1:]:
            guid = entry.splitlines()[0].strip()
            address = re.search(r'    m_Address: "?(\d+)"?\s*\n', entry)
            quality = re.search(r'    - (SD|LD)\s*\n', entry)
            if not address or not quality:
                continue
            key = quality[1] + address[1]
            assert key not in items, f'Duplicate address {key}'
            path = guids[guid]
            im = Image.open(path).convert('RGBA')
            items[key] = {'path': path.relative_to(ROOT).as_posix(), 'guid': guid,
                          'sha256': hashlib.sha256(path.read_bytes()).hexdigest(),
                          'size': list(im.size), 'alpha': list(im.getchannel('A').getextrema())}
    assert len(items) == 50, f'Expected 50 SD/LD entries, found {len(items)}'
    return items

def sheets(items):
    for first in range(1, 25, 4):
        out = Image.new('RGB', (1200, 840), '#303642')
        draw = ImageDraw.Draw(out)
        for offset in range(4):
            level = first + offset
            col, row = offset % 2, offset // 2
            for i, quality in enumerate(['SD', 'LD']):
                x, y = col*600+i*300, row*420
                im = Image.open(ROOT/items[f'{quality}{level}']['path']).convert('RGBA')
                im.thumbnail((278, 374), Image.Resampling.LANCZOS)
                out.paste(im, (x+(300-im.width)//2, y+32+(374-im.height)//2), im)
                draw.text((x+12, y+10), f'Lv.{level} {quality}' + (' REDRAW' if quality == 'LD' and level in LEVELS else ''), fill='white')
        out.save(DOC/f'final-pairs-{first:02}-{first+3:02}.png')

if __name__ == '__main__':
    items = inventory()
    if '--before' in sys.argv:
        path = DOC/'before-sprites.json'
        if not path.exists():
            path.write_text(json.dumps(items, ensure_ascii=False, indent=2), encoding='utf-8')
        print('Captured 50 active sprites without modifying assets.')
    else:
        before = json.loads((DOC/'before-sprites.json').read_text(encoding='utf-8'))
        for key, record in items.items():
            if key in ('SD8', 'SD9'):
                original = ROOT/f'Assets/닌자SD2/{key[2:]}.png'
                assert record['sha256'] == hashlib.sha256(original.read_bytes()).hexdigest(), key
                assert record['path'] == before[key]['path'] and record['guid'] == before[key]['guid'], key
            elif key.startswith('SD') or int(key[2:]) not in LEVELS:
                assert record == before[key], f'Unrequested original changed: {key}'
            else:
                assert record['path'] == f'Assets/Art/Characters/LD/{key[2:]}.png', key
                assert record['alpha'] == [0, 255], key
        (DOC/'final-sprites.json').write_text(json.dumps(items, ensure_ascii=False, indent=2), encoding='utf-8')
        sheets(items)
        print('PASS: 50 unique addresses; 15 new LD with alpha; SD8/9 content corrected; 23 other SD and 10 other LD unchanged.')
