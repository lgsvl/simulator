import subprocess
import sys
import os
from PIL import Image

srcImg = sys.argv[1]
cols = int(sys.argv[2])
rows = int(sys.argv[3])

imgW = 4096
imgH = 4096

if os.path.exists(srcImg):
	img = Image.open(srcImg)
	imgW = int(img.size[0])
	imgH = int(img.size[1])
	print("Image size is: " + str(img.size))

dstDir = ""

if len(sys.argv) > 6:
	dstDir = sys.argv[6]
	
srcFileName = os.path.basename(srcImg)

if dstDir == None or dstDir == "":
	dstDir = r".\\" + srcImg + "_sliced"
	
if not os.path.exists(dstDir):
	os.makedirs(dstDir)

args = [r"C:\Program Files\ImageMagick-7.0.7-Q16\magick.exe", srcImg]			
		
outStr = dstDir + os.path.sep + srcFileName

blockX = imgW / cols
blockY = imgH / rows

for y in range(rows):
	for x in range(cols):
		subprocess.call(args + ["-crop", "%dx%d+%d+%d" % (blockX, blockY, x*blockX, y*blockY)] + [outStr + "_" + str(y * cols + x) + r".tif"])
		