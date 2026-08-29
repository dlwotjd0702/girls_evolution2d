"""Authorized local background extraction only; generated source files remain intact."""
from pathlib import Path
import json,sys
import numpy as np
from PIL import Image,ImageDraw
import cv2
from scipy import ndimage
ROOT=Path(__file__).resolve().parents[2]
DOC=ROOT/'Docs/UIRefresh'
SRC=Path('C:/Users/user/.codex/generated_images/01a044e4-89f3-7ba2-9267-efb2e89f6a90')
OUT=ROOT/'Assets/Art/Characters/Skins'
OUT.mkdir(parents=True,exist_ok=True)
ITEMS={'violet-festival':'06ea9e77','jade-garden':'e03170f1','moon-patrol':'8aeb9173','rose-oath':'840b0cfa'}
for record in sorted((DOC/'Generated').glob('*.json')):
 item=json.loads(record.read_text(encoding='utf8')); ITEMS[item['id']]=item['source']
HOLES={'violet-festival':[64,65,67,81,159,161], 'jade-garden':[21,101,131,135,155], 'moon-patrol':[2,51,44,80,206], 'rose-oath':[240,1012]}
if (DOC/'skin-holes.json').exists(): HOLES.update(json.loads((DOC/'skin-holes.json').read_text(encoding='utf8')))
report=[]
for name,prefix in ITEMS.items():
 source=Path(prefix) if prefix.endswith('.png') else next(SRC.glob('exec-'+prefix+'*.png'))
 rgb=np.array(Image.open(source).convert('RGB')); lo=rgb.min(2); hi=rgb.max(2)
 border=np.concatenate([lo[:12].ravel(),lo[-12:].ravel(),lo[:,:12].ravel(),lo[:,-12:].ravel()])
 threshold=max(220,int(np.percentile(border,.2))-4)
 candidate=((hi.astype(int)-lo<=5)&(lo>=threshold)).astype('uint8')
 count,labels,stats,centers=cv2.connectedComponentsWithStats(candidate,8)
 outer=set(np.unique(np.concatenate([labels[0],labels[-1],labels[:,0],labels[:,-1]])))-{0}
 removed=outer|set(HOLES.get(name,[])); bg=np.isin(labels,list(removed))
 bg|=ndimage.binary_dilation(bg,iterations=2)&(hi.astype(int)-lo<=22)&(lo>=216)
 hard=~bg; core=ndimage.binary_erosion(hard,iterations=2)
 _,ix=ndimage.distance_transform_edt(~core,return_indices=True)
 near=rgb[tuple(ix)].astype(float); edge=hard&~core; b=249.
 cov=np.clip(((rgb.astype(float)-b)*(near-b)).sum(2)/np.maximum(((near-b)**2).sum(2),1),0,1)
 alpha=hard.astype(float); alpha[edge]=cov[edge]; clean=rgb.copy();clean[edge]=near[edge].astype('uint8')
 rgba=np.dstack([clean,(alpha*255).round().astype('uint8')]);rgba[rgba[:,:,3]==0,:3]=0
 im=Image.fromarray(rgba); preview=Image.new('RGBA',im.size,'#343b4b');preview.alpha_composite(im);d=ImageDraw.Draw(preview)
 holes=[]
 for i in range(1,count):
  x,y,w,h,area=map(int,stats[i])
  if i in outer or area<30:continue
  holes.append({'id':i,'xywh':[x,y,w,h],'area':area})
  if i not in removed:d.rectangle((x,y,x+w,y+h),outline='#00ff78',width=2);d.text((x,y),str(i),fill='#00ff78',stroke_width=1,stroke_fill='black')
 preview.convert('RGB').save(DOC/(name+'-holes.jpg'))
 for j,kind in enumerate(['SD','LD']):
  piece=im.crop((j*im.width//2,0,(j+1)*im.width//2,im.height));box=piece.getbbox()
  if not box:raise ValueError(name)
  piece=piece.crop(box); canvas=Image.new('RGBA',(max(piece.width,piece.height)+80,)*2)
  canvas.alpha_composite(piece,((canvas.width-piece.width)//2,(canvas.height-piece.height)//2));canvas.thumbnail((512 if kind=='SD' else 1024,)*2,Image.Resampling.LANCZOS);canvas.save(OUT/(name+'-'+kind+'.png'))
 report.append({'id':name,'source':str(source),'holes':holes,'threshold':threshold})
(DOC/'skin-alpha-report.json').write_text(json.dumps(report,indent=2))
sheet=Image.new('RGB',(1200,((len(ITEMS)+1)//2)*330),'#343b4b');d=ImageDraw.Draw(sheet)
for row,name in enumerate(ITEMS):
 for col,kind in enumerate(['SD','LD']):
  im=Image.open(OUT/(name+'-'+kind+'.png'));im.thumbnail((245,245));x=col*300+(row%2)*600;y=(row//2)*330
  sheet.paste(im,(x+(300-im.width)//2,y+50),im);d.text((x+15,y+15),name+' '+kind,fill='white')
sheet.save(DOC/'skins-review.png')
print('Prepared 8 cutouts; inspect hole overlays before accepting.')
