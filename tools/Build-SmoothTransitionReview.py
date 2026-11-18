"""Bruno-only local candidate build. Never changes production app/art inputs.

Run with an existing authoring Python with Pillow; normal app builds do not use it.
"""
import hashlib
import json
import shutil
import subprocess
import sys
from pathlib import Path
from PIL import Image

root = Path(__file__).resolve().parent.parent
revision = sys.argv[1] if len(sys.argv)>1 else 'v2'
assert revision in ['v2','v3'], 'Unsupported review revision'
config = json.loads((root/('planning/smooth-transitions-'+revision+'.json')).read_text(encoding='utf-8'))
work = root/('local-data/smooth-transitions-'+revision)
for folder in ['app','art/frames','build','test-exes']:
    (work/folder).mkdir(parents=True,exist_ok=True)
for source in (root/'app').glob('*'):
    if source.is_file(): shutil.copyfile(source,work/'app'/source.name)
for source in (root/'art/frames').glob('*.png'):shutil.copyfile(source,work/'art/frames'/source.name)
baseline = json.loads((root/'art/manifest.json').read_text(encoding='utf-8'))
candidate = json.loads(json.dumps(baseline))
for asset in config['assets']:
    source=root/asset['file'];image=Image.open(source)
    assert image.mode=='RGBA',asset['id']
    bounds=image.getchannel('A').point(lambda a:255 if a>16 else 0).getbbox()
    assert bounds and 0<bounds[0]<bounds[2]<image.width and 0<bounds[1]<bounds[3]<image.height,asset['id']
    x,y=asset['contactLandmark']
    assert abs(y-asset['supportY'])<=.5 and image.getpixel((x,y))[3]>16,asset['id']+' body contact'
    assert bounds[3]>asset['supportY'],asset['id']+' paw overhang'
    shutil.copyfile(source,work/'art/frames'/(asset['id']+'.png'))
    candidate['frames'][asset['id']]=dict(file='frames/'+asset['id']+'.png',sha256=hashlib.sha256(source.read_bytes()).hexdigest(),width=image.width,height=image.height,bounds=list(bounds),scale=asset['scale'],supportX=asset['supportX'],supportY=asset['supportY'],floorContact=asset['floorContact'],draft=False,precomposited=False)
for name,keys,seconds in [
    ('sit-to-lay',['sit-lay-a','sit-lay-1','sit-lay-2','sit-lay-b','sit-lay-3'],[.06,.06,.07,.07,.12]),
    ('lay-to-sit',['sit-lay-3','sit-lay-b','sit-lay-2','sit-lay-1','sit-lay-a'],[.12,.07,.07,.06,.06]),
    ('lay-to-sleep',['lay-sleep-a','lay-sleep-1','lay-sleep-b','lay-sleep-2','lay-sleep-3'],[.08,.08,.08,.08,.16]),
    ('sleep-to-lay',['lay-sleep-3','lay-sleep-2','lay-sleep-b','lay-sleep-1','lay-sleep-a'],[.16,.08,.08,.08,.08])]:
    if revision=='v3' and name in ['sit-to-lay','lay-to-sit']:
        keys=['sit-lay-c' if key=='sit-lay-1' else key for key in keys]
    candidate['clips'][name]['frames']=keys;candidate['clips'][name]['seconds']=seconds
(work/'art/manifest.json').write_text(json.dumps(candidate,indent=2)+'\n',encoding='utf-8')
(work/'art/baseline.json').write_bytes((root/'art/manifest.json').read_bytes())
program=work/'app/Program.cs';text=program.read_text(encoding='utf-8').replace('BrunoCompanionDevV1','BrunoCompanionTransitionReviewDev'+revision.upper())
text=text.replace('Bruno Companion Development','Bruno Transition Review').replace('0.1.2.0','0.1.3.0')
text=text.replace('using(Assets art=new Assets())','using(Assets art=new Assets())').replace('if(args.Length==2&&args[0]=="--render-contact")','if(args.Length==2&&args[0]=="--smooth-review"){SmoothTransitionReview.Run(art,args[1]);return 0;}\n                    if(args.Length==2&&args[0]=="--render-contact")')
program.write_text(text,encoding='utf-8')
checks=work/'app/Checks.cs';checks.write_text(checks.read_text(encoding='utf-8').replace('frames==21','frames=='+str(len(candidate['frames']))),encoding='utf-8')
shutil.copyfile(root/'tools/SmoothTransitionReview.cs',work/'app/SmoothTransitionReview.cs')
compiler=Path('C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe')
exe=root/('test-exes/Bruno-TransitionReview-'+revision+'.exe')
exe.parent.mkdir(parents=True,exist_ok=True)
args=[str(compiler),'/nologo','/target:winexe','/platform:x64','/optimize+','/define:BRUNO_DEV','/main:Bruno.Program','/r:System.Drawing.dll','/r:System.Windows.Forms.dll','/r:System.Web.Extensions.dll','/out:'+str(exe),'/win32manifest:'+str(work/'app/app.manifest'),'/resource:'+str(work/'art/manifest.json')+',Bruno.Manifest','/resource:'+str(work/'art/baseline.json')+',Bruno.BaselineManifest']
if 'previousReviewManifest' in config:
    args.append('/resource:'+str(root/config['previousReviewManifest'])+',Bruno.PreviousReviewManifest')
for name,info in candidate['frames'].items():args.append('/resource:'+str(work/'art'/info['file'])+',Bruno.Art.'+name)
args += [str(p) for p in (work/'app').glob('*.cs')]
subprocess.run(args,check=True)
output=root/('build/smooth-transitions-'+revision);output.mkdir(parents=True,exist_ok=True)
for command,target in [('--self-test',output/'self-test.json'),('--test-desktop',output/'desktop-test.json'),('--smooth-review',output)]:
    subprocess.run([str(exe),command,str(target)],check=True,creationflags=subprocess.CREATE_NO_WINDOW)
for mode in ['cycle','context']:
    delays=json.loads((output/mode/'delays.json').read_text())
    for color in ['light','dark']:
        command="& '"+str(root/'tools/SharedWrite-AnimatedGif.ps1').replace("'","''")+"' -FramesDirectory '"+str(output/mode/(color+'-frames')).replace("'","''")+"' -OutputPath '"+str(output/mode/(color+'.gif')).replace("'","''")+"' -Delays @("+','.join(map(str,delays))+")"
        subprocess.run(['pwsh','-NoProfile','-Command',command],check=True,creationflags=subprocess.CREATE_NO_WINDOW)
print('Review ready:',exe)
print('Preview:',output/'cycle/light.gif')
