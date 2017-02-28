#16x16 has to be done manually
sizesNoAA="20 32"
sizesAA="40 44 46 50 64 128 150 256 512"
names=""
for i in $sizesNoAA 
do 
    convert +antialias -background none plug.svg -filter Lanczos -resize ${i}x${i} png8:icon_${i}x${i}.png
    names=$names" icon_"${i}x${i}".png" 
done

for i in $sizesAA
do 
    convert +antialias -background none plug.svg -resize ${i}x${i} icon_${i}x${i}.png
    names=$names" icon_"${i}x${i}".png" 
done

names=$names" icon_16x16.png"

convert $names Tray.ico
