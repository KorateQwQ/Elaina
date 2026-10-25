import sys,struct,json,hashlib
from pathlib import Path
from PIL import Image
repo,out=map(Path,sys.argv[1:])
for label,name in [('wind','Mounts/Assets/BroomDashWind.png'),('broom','Mounts/扫帚_Back.png')]:
 im=Image.open(repo/name).convert('RGBA');data=bytearray(im.tobytes())
 # Mirror tML ImageIO.ToRaw: zero fully transparent pixels, preserve other RGB (no double premultiplication).
 for i in range(0,len(data),4):
  if data[i+3]==0:data[i:i+4]=bytes(4)
 (out/(label+'.rgba')).write_bytes(struct.pack('<ii',*im.size)+data)
 print(f'{label}: {im.size}, packaged PNG SHA256 {hashlib.sha256((repo/name).read_bytes()).hexdigest()}')
