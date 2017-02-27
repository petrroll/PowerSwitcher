#16x16 has to be done manually
sizes="20 32 44 50 64 128 150"
names=""
for i in $sizes 
do 
    convert +antialias -background none plug.svg -filter Lanczos -resize ${i}x${i} png8:icon_${i}x${i}.png
    names=$names" icon_"${i}x${i}".png" 
done

convert $names Tray.ico