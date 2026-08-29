"""Render staged SD/LD assets on light and dark backgrounds for visual review."""
import argparse
import json
import math
from pathlib import Path

from PIL import Image, ImageDraw


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--batch', required=True)
    args = parser.parse_args()
    if len(args.batch) != 2 or any(c not in '0123456789' for c in args.batch):
        parser.error('--batch must contain exactly two ASCII digits')
    doc = Path(__file__).resolve().parent
    out = doc / ('SkinBatch' + args.batch)
    items = json.loads((doc / ('skin-batch-' + args.batch + '.json')).read_text('utf-8'))['items']
    assert items
    sheet = Image.new('RGB', (1280, math.ceil(len(items) / 4) * 650), '#14232d')
    draw = ImageDraw.Draw(sheet)
    for i, item in enumerate(items):
        key = item['id'] + '-' + item['kind']
        x, y = (i % 4) * 320, (i // 4) * 650
        sprite = Image.open(out / 'Ready' / (key + '.png')).convert('RGBA')
        sprite.thumbnail((320, 300), Image.Resampling.LANCZOS)
        for n, color in enumerate(['#182631', '#e2d8c8']):
            preview = Image.new('RGBA', (320, 300), color)
            preview.alpha_composite(sprite, ((320-sprite.width)//2, (300-sprite.height)//2))
            sheet.paste(preview.convert('RGB'), (x, y+25+n*300))
        draw.text((x+8, y+6), key, fill='white')
    target = out / 'review-sheet.jpg'
    sheet.save(target, quality=95)
    print(target)


if __name__ == '__main__':
    main()
