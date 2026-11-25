"""Local artwork review only; does not modify production frames or enable gaze."""
from pathlib import Path
import json, hashlib, math, shutil, subprocess
import numpy as np
import cv2
from PIL import Image

ROOT=Path(__file__).resolve().parent.parent
WORK=ROOT/'local-data/clock-look-review-v1'
OUTPUT=ROOT/'build/clock-look-review-v1'
HOURS=[12,1,2,3,4,5,6,7,8,9,10,11]
WORK.mkdir(parents=True,exist_ok=True);OUTPUT.mkdir(parents=True,exist_ok=True)
baseline=json.loads((WORK/'production-baseline.json').read_text())
assert json.loads((ROOT/'art/manifest.json').read_text())==baseline,'Production changed since review started'
candidate=json.loads(json.dumps(baseline));records=[]
sift=cv2.SIFT_create(nfeatures=5000)
def digest(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def features(rgba,start):
    mask=(rgba[:,:,3]>220).astype(np.uint8)*255;mask[:start,:]=0
    mask=cv2.erode(mask,np.ones((5,5),np.uint8))
    return sift.detectAndCompute(cv2.cvtColor(rgba[:,:,:3],cv2.COLOR_RGB2GRAY),mask)
def underside(rgba,roi):
    x0,y0,x1,y1=roi;values=[]
    for x in range(max(0,x0),min(rgba.shape[1],x1)):
        ys=np.flatnonzero(rgba[max(0,y0):min(rgba.shape[0],y1),x,3]>64)
        if len(ys):values.append(int(ys[-1])+max(0,y0))
    assert len(values)>8,'No reliable body-contact silhouette'
    return float(np.quantile(values,.9))
for pose in ['sit','lay']:
    base=baseline['frames'][pose];original=np.array(Image.open(ROOT/'art'/base['file']).convert('RGBA'))
    start=650 if pose=='sit' else 610
    kb,db=features(original,start)
    roi=[450,1180,650,1300] if pose=='sit' else [240,780,305,960]
    body_bottom=underside(original,roi)
    for hour in HOURS:
        key='clock-'+pose+'-h'+str(hour).zfill(2)
        folder=ROOT/'draft-assets/clock-looks-v1'/(pose+'-h'+str(hour).zfill(2))
        source=folder/'source-v1.png'
        image=Image.open(source);assert image.mode=='RGBA',key
        rgba=np.array(image);bounds=image.getchannel('A').point(lambda a:255 if a>16 else 0).getbbox()
        assert bounds and 0<bounds[0]<bounds[2]<image.width and 0<bounds[1]<bounds[3]<image.height,key+' clipped'
        kg,dg=features(rgba,int(start*image.height/original.shape[0]))
        matches=cv2.BFMatcher().knnMatch(db,dg,k=2)
        pairs=[a for a,b in matches if a.distance<.72*b.distance]
        assert len(pairs)>=12,key+' insufficient stable body landmarks'
        src=np.float32([kb[p.queryIdx].pt for p in pairs]);dst=np.float32([kg[p.trainIdx].pt for p in pairs])
        matrix,inlier=cv2.estimateAffinePartial2D(src,dst,method=cv2.RANSAC,ransacReprojThreshold=3,maxIters=5000)
        assert matrix is not None,key
        selected=inlier.ravel().astype(bool);count=int(selected.sum());assert count>=10,key+' poor body registration'
        src=src[selected];dst=dst[selected]
        rotation=math.degrees(math.atan2(matrix[1,0],matrix[0,0]))
        assert abs(rotation)<1.5,key+' body rotated'
        scale=math.hypot(matrix[0,0],matrix[1,0])
        translation=np.median(dst-src*scale,axis=0)
        residual=np.linalg.norm(dst-(src*scale+translation),axis=1)
        tx,ty=map(float,translation)
        mapped=[int(round(roi[0]*scale+tx)),int(round(roi[1]*scale+ty)),int(round(roi[2]*scale+tx)),int(round(roi[3]*scale+ty))]
        contact_adjustment=underside(rgba,mapped)-(body_bottom*scale+ty)
        assert abs(contact_adjustment)<9,key+' body underside drift requires visual correction'
        support_y=base['supportY']*scale+ty+contact_adjustment
        assert bounds[3]>support_y,key+' paw overhang missing'
        candidate['frames'][key]=dict(file=str(source.relative_to(ROOT)).replace('\\','/'),sha256=digest(source),width=image.width,height=image.height,bounds=list(bounds),scale=base['scale']/scale,supportX=base['supportX']*scale+tx,supportY=support_y,floorContact=base['floorContact'],draft=True,precomposited=False)
        records.append(dict(id=key,pose=pose,hour=hour,source=str(source.relative_to(ROOT)).replace('\\','/'),sha256=digest(source),bodyFeatureInliers=count,registrationScale=scale,registrationTranslation=[tx,ty],bodyRotationDiagnostic=rotation,medianBodyResidual=float(np.median(residual)),contactAdjustment=contact_adjustment,bodyRoi=roi,registration=candidate['frames'][key]))
(WORK/'manifest.json').write_text(json.dumps(candidate,indent=2)+'\n')
record={'date':'2026-11-24','status':'Candidate sprites; owner approval before mouse-follow integration','scope':'12 clock directions in both awake poses; no sleeping looks; separate exact neutral endpoints','clockOrder':HOURS,'screenMapping':{'12':'up','3':'viewer right','6':'down','9':'viewer left'},'method':'Whole-pose imagegen drawings, uniform scale/translation registered using lower-body landmarks. Each drawing keeps its own pixels. Body-contact silhouette measured separately. No pasted head or crossfade.','productionFramesPreserved':len(baseline['frames']),'candidates':records}
(ROOT/'planning/clock-look-review-v1.json').write_text(json.dumps(record,indent=2)+'\n')
compiler=Path('C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe');exe=WORK/'Bruno-ClockLookReview-v1.exe'
args=[str(compiler),'/nologo','/target:winexe','/platform:x64','/optimize+','/main:Bruno.ClockLookReview','/r:System.Drawing.dll','/r:System.Web.Extensions.dll','/out:'+str(exe),'/resource:'+str(WORK/'manifest.json')+',Bruno.Manifest']
for key,f in candidate['frames'].items():
    source=ROOT/f['file'] if key.startswith('clock-') else ROOT/'art'/f['file']
    assert digest(source)==f['sha256'],key
    args.append('/resource:'+str(source)+',Bruno.Art.'+key)
args += [str(ROOT/p) for p in ['app/Assets.cs','app/Placement.cs','app/SharedSpriteSupport.cs','tools/ClockLookReview.cs']]
subprocess.run(args,check=True)
subprocess.run([str(exe),str(OUTPUT)],check=True,creationflags=subprocess.CREATE_NO_WINDOW)
delays=json.loads((OUTPUT/'delays.json').read_text())
shell=Path('C:/Windows/System32/WindowsPowerShell/v1.0/powershell.exe')
for color in ['light','dark']:
    cmd="& '"+str(ROOT/'tools/SharedWrite-AnimatedGif.ps1').replace("'","''")+"' -FramesDirectory '"+str(OUTPUT/(color+'-frames')).replace("'","''")+"' -OutputPath '"+str(OUTPUT/(color+'.gif')).replace("'","''")+"' -Delays @("+','.join(map(str,delays))+")"
    subprocess.run([str(shell),'-NoProfile','-ExecutionPolicy','Bypass','-Command',cmd],check=True,creationflags=subprocess.CREATE_NO_WINDOW)
    gif=Image.open(OUTPUT/(color+'.gif'));actual=[]
    for i in range(gif.n_frames):gif.seek(i);actual.append(gif.info['duration'])
    assert actual==[d*10 for d in delays] and gif.info['loop']==0
assert json.loads((ROOT/'art/manifest.json').read_text())==baseline
print('Review ready:',OUTPUT,'with',len(records),'clock looks. Production unchanged.')
