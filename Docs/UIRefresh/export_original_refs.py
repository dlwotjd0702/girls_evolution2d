"""Read initial texture references only; never reset scene/gameplay changes."""
import re,json,subprocess
from pathlib import Path
rows=[];colours=[]
for path in ['Assets/Scenes/Ingame.unity','Assets/Prefabs/SummonCell.prefab','Assets/Prefabs/EncyclopediaSlot.prefab']:
    raw=subprocess.check_output(['git','show','HEAD:'+path]).decode('utf8')
    blocks={int(m[2]):m[3] for m in re.finditer(r'--- !u!(\d+) &(\d+)\n(.*?)(?=\n--- !u!|\Z)',raw,re.S)}
    for oid,block in blocks.items():
        colour=re.search(r'^  m_Color: \{r: ([\d.e+-]+), g: ([\d.e+-]+), b: ([\d.e+-]+), a: ([\d.e+-]+)\}',block,re.M)
        if colour:colours.append(dict(asset=path,objectId=oid,**dict(zip('rgba',map(float,colour.groups())))))
        for m in re.finditer(r'^  (\w+): \{fileID: (-?\d+), guid: ([a-f0-9]+), type: \d+\}',block,re.M):
            rows.append(dict(asset=path,objectId=oid,property=m[1],fileId=int(m[2]),guid=m[3]))
Path('Docs/UIRefresh/original-refs.json').write_text(json.dumps({'entries':rows,'colours':colours},ensure_ascii=False,indent=2),encoding='utf8')
print(len(rows),'reference records exported from HEAD; only current experimental UI references will be restored.')
