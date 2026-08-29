from pathlib import Path
from PIL import Image,ImageDraw
import re,json
root=Path.cwd(); sprites={}; guidpath={}
for p in (root/'Assets').rglob('*.png.meta'):
 m=re.search(r'^guid: (\w+)',p.read_text(encoding='utf-8'),re.M)
 if m: guidpath[m[1]]=p.with_suffix('')
scene=(root/'Assets/Scenes/Ingame.unity').read_text(encoding='utf-8')
used={guid:guidpath[guid] for guid in re.findall(r'm_Sprite: \{fileID: \d+, guid: (\w+)',scene) if guid in guidpath and '/Characters/' not in str(guidpath[guid]).replace('\\','/') and '닌자' not in str(guidpath[guid])}
items=list(used.items())
for start in range(0,len(items),24):
 sheet=Image.new('RGB',(1200,800),'#30313d'); draw=ImageDraw.Draw(sheet)
 for j,(guid,p) in enumerate(items[start:start+24]):
  im=Image.open(p).convert('RGBA'); im.thumbnail((184,158)); x=j%6*200; y=j//6*200
  sheet.paste(im,(x+(200-im.width)//2,y+(165-im.height)//2),im);draw.text((x+4,y+166),f'{start+j}: {p.stem}',fill='white')
 sheet.save(root/f'Docs/UIRefresh/existing-{start//24}.png')
(root/'Docs/UIRefresh/existing-resources.json').write_text(json.dumps([{'index':i,'guid':g,'path':p.relative_to(root).as_posix()} for i,(g,p) in enumerate(items)],ensure_ascii=False,indent=2),encoding='utf-8')
print(len(items),'scene textures')
