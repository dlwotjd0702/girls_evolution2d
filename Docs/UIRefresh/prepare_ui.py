"""Pack generated UI artwork; retain RGBA and remove only exterior white on panel atlas."""
from pathlib import Path
import json, shutil
from PIL import Image
import numpy as np
import cv2

ROOT=Path(__file__).resolve().parents[2]
DOC=ROOT/'Docs/UIRefresh'
OUT=ROOT/'Assets/Art/UI/Refresh'
for name,source_name,columns in [('surfaces','surfaces-v3',3),('icons-main','icons-main-v2',4),('icons-settings','icons-settings-v2',4)]:
    source=json.loads((DOC/'UIV2'/f'{source_name}.json').read_text())['source']
    im=Image.open(source).convert('RGBA')
    result=Image.new('RGBA',(columns*384,columns*384))
    for i in range(columns*columns):
        c,r=i%columns,i//columns
        tile=im.crop((c*im.width//columns,r*im.height//columns,(c+1)*im.width//columns,(r+1)*im.height//columns))
        if name=='surfaces':
            a=np.array(tile);rgb=a[:,:,:3];lo=rgb.min(2);hi=rgb.max(2)
            candidate=((lo>=228)&(hi.astype(int)-lo<=12)).astype('uint8')
            count,labels,stats,centers=cv2.connectedComponentsWithStats(candidate,8)
            outer=set(np.unique(np.concatenate([labels[0],labels[-1],labels[:,0],labels[:,-1]])))-{0}
            a[np.isin(labels,list(outer)),3]=0
            tile=Image.fromarray(a)
        # Remove uneven generated margins without changing any painted shape.
        tile=tile.crop(tile.getbbox());tile.thumbnail((376,376),Image.Resampling.LANCZOS)
        result.alpha_composite(tile,(c*384+(384-tile.width)//2,r*384+(384-tile.height)//2))
    old=OUT/f'{name}.png';backup=DOC/'RejectedV1'/old.name;backup.parent.mkdir(parents=True,exist_ok=True)
    if old.exists() and not backup.exists():shutil.copy2(old,backup)
    result.save(old)
    print(name,result.size)
