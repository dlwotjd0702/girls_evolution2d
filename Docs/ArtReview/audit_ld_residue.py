"""Read-only LD alpha QA. Flags candidates, never treats white clothing as background.

No source pixel or importer is modified. Coordinates use the source PNG, top-left origin.
Candidate counts are NOT a pass/fail quality score; visual review is required.
"""
import hashlib
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image

sys.path.insert(0, str(Path.home() / '.codex/tmp/girls-art-deps'))
import cv2

ROOT = Path(__file__).resolve().parents[2]
DOC = ROOT / 'Docs/ArtReview'


def main():
    inventory = json.loads((DOC / 'ld-residue-inventory.json').read_text('utf-8'))
    findings = []
    for key, item in inventory['items'].items():
        path = ROOT / item['path']
        assert hashlib.sha256(path.read_bytes()).hexdigest() == item['sha256'], key
        pixels = np.asarray(Image.open(path).convert('RGBA'))
        rgb, alpha = pixels[:, :, :3].astype(np.int16), pixels[:, :, 3]
        near_edge = cv2.dilate((alpha <= 16).astype(np.uint8), np.ones((7, 7), np.uint8)) > 0
        white = (rgb.min(axis=2) >= 238) & ((rgb.max(axis=2) - rgb.min(axis=2)) <= 16) & (alpha >= 128)
        _, labels, stats, _ = cv2.connectedComponentsWithStats(white.astype(np.uint8), 8)
        candidates = []
        for index, (x, y, w, h, area) in enumerate(stats[1:], 1):
            if area < 4:
                continue
            edge_pixels = int(np.count_nonzero(near_edge[y:y+h, x:x+w] & (labels[y:y+h, x:x+w] == index)))
            if edge_pixels < 3:
                continue
            candidates.append({'xywh': [int(x), int(y), int(w), int(h)], 'white_pixels': int(area), 'near_alpha_edge_pixels': edge_pixels})
        findings.append({'id': key, **item, 'candidate_count': len(candidates), 'near_white_edge_pixels': int(np.count_nonzero(white & near_edge)), 'candidates': sorted(candidates, key=lambda c: c['near_alpha_edge_pixels'], reverse=True)})
        assert hashlib.sha256(path.read_bytes()).hexdigest() == item['sha256'], key
    report = {'note': 'Read-only triage of 44 current LD sources. White pixels can be legitimate highlights, silver trim, hair or clothing. Never globally erase these candidates. No image passed as fringe-free on this heuristic.', 'images': findings}
    (DOC / 'ld-residue-candidates.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
    print(f'PASS: {len(findings)} LD images analyzed without changing source files.')
    for item in sorted(findings, key=lambda i: i['near_white_edge_pixels'], reverse=True)[:12]:
        print(item['id'], 'near-white edge pixels:', item['near_white_edge_pixels'], 'candidate components:', item['candidate_count'])


if __name__ == '__main__':
    main()
