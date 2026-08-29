"""Install the two reviewed visual corrections without changing sprite GUIDs.

New skin production deliberately refuses overwrites. This separate, narrow
path requires both old and new hashes, immutable backups and visual approval.
"""
import argparse
import json
import shutil
from pathlib import Path

import prepare_skin_batch as stage


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--apply', action='store_true')
    parser.add_argument('--batch', choices=['17','19'], default='17')
    args = parser.parse_args()
    stage.OUT = stage.DOC/('SkinBatch'+args.batch)
    for folder in ('Source', 'Cutouts', 'Ready', 'ReplacedOriginals'):
        (stage.OUT/folder).mkdir(parents=True, exist_ok=True)
    manifest = json.loads((stage.DOC/('skin-batch-'+args.batch+'.json')).read_text('utf-8'))
    allowed = {('lv05-b','LD'), ('moon-patrol','SD')} if args.batch=='17' else {('lv01-a','SD'), ('lv01-b','SD')}
    assert {(i['id'],i['kind']) for i in manifest['items']} == allowed
    records = []
    destinations = []
    for item in manifest['items']:
        key = item['id']+'-'+item['kind']
        target = stage.ROOT/'Assets/Art/Characters/Skins'/(key+'.png')
        original = stage.OUT/'ReplacedOriginals'/(key+'.png')
        assert stage.sha(Path(str(target)+'.meta')) == item['metaSha256']
        if not original.exists():
            assert stage.sha(target) == item['beforeSha256']
            shutil.copy2(target, original)
        assert stage.sha(original) == item['beforeSha256']
        record = stage.stage(item)
        assert stage.sha(target) in (item['beforeSha256'], record['preparedSha256'])
        record.update(path=target.relative_to(stage.ROOT).as_posix(),
                      originalPath=original.relative_to(stage.ROOT).as_posix(),
                      beforeSha256=item['beforeSha256'],
                      afterSha256=record['preparedSha256'],
                      metaSha256=item['metaSha256'],
                      reason=item.get('reviewNotes',''),applied=args.apply)
        records.append(record)
        destinations.append((stage.ROOT/record['preparedPath'],target))
    if args.apply:
        assert all(r['approved'] and r['reason'] for r in records)
        assert all(i.get('alphaProbes') for i in manifest['items'])
        for prepared,target in destinations:
            shutil.copy2(prepared,target)
        (stage.OUT/'replacements.json').write_text(json.dumps(records,indent=2),'utf-8')
    (stage.OUT/'report.json').write_text(json.dumps(records,indent=2),'utf-8')
    print('Applied' if args.apply else 'Staged',len(records),'reviewed corrections; IDs/meta unchanged')


if __name__ == '__main__':
    main()
