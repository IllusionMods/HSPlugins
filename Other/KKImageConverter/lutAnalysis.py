import matplotlib.pyplot as plt
import numpy as np
from PIL import Image

def getFromLut(im, size, r, g, b):
    x = b * size + r
    y = size - 1 - g
    return im.getpixel((x, y))

im = Image.open('./CC2.png').convert("RGB")

fig, ax = plt.subplots()

#size = int(im.height)

#redI = 23
#greenI = 23
#blueI = 23
#xRange = range(0, 256, int(256 / size))


#rValues = []
#gValues = []
#bValues = []
#for i in range(size):
#    r, g, b = getFromLut(im, size, i, greenI, blueI)
#    rValues.append(r)
#    r, g, b = getFromLut(im, size, redI, i, blueI)
#    gValues.append(g)
#    r, g, b = getFromLut(im, size, redI, greenI, i)
#    bValues.append(b)
#ax.plot(xRange, rValues, color="red")  # Plot some data on the axes.
#ax.plot(xRange, gValues, color="green")  # Plot some data on the axes.
#ax.plot(xRange, bValues, color="blue")  # Plot some data on the axes.


#for rI in range(0, size, size - 1):
#    values = []
#    for bI in range(size):
#        r, g, b = getFromLut(im, size, rI, 31, bI)
#        values.append(b)
#    ax.plot(xRange, values, color="red")

rValues = []
gValues = []
bValues = []

rValuesC = []
gValuesC = []
bValuesC = []

xValues = []
for y in range(1080):
    r, g, b = im.getpixel((160, y)); #R
    rValues.append(r)
    r, g, b = im.getpixel((480, y)); #G
    gValues.append(g)
    r, g, b = im.getpixel((800, y)); #B
    bValues.append(b)

    r, g, b = im.getpixel((1120, y)); #RC
    rValuesC.append(r)
    r, g, b = im.getpixel((1440, y)); #GC
    gValuesC.append(g)
    r, g, b = im.getpixel((1760, y)); #BC
    bValuesC.append(b)
    xValues.append(y * 256.0 / 1080)
#ax.plot(xValues, list(reversed(rValues)), color="red")  # Plot some data on the axes.
#ax.plot(xValues, list(reversed(gValues)), color="green")  # Plot some data on the axes.
#ax.plot(xValues, list(reversed(bValues)), color="blue")  # Plot some data on the axes.

ax.plot(xValues, list(reversed(rValuesC)), color="#ff474c")  # Plot some data on the axes.
ax.plot(xValues, list(reversed(gValuesC)), color="#96f97b")  # Plot some data on the axes.
ax.plot(xValues, list(reversed(bValuesC)), color="#95d0fc")  # Plot some data on the axes.


major_ticks = np.arange(0, 256, 16)
minor_ticks = np.arange(0, 256, 4)

ax.set_xticks(major_ticks)
ax.set_xticks(minor_ticks, minor=True)
ax.set_yticks(major_ticks)
ax.set_yticks(minor_ticks, minor=True)

ax.grid()
plt.show()
