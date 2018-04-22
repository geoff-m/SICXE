#!/bin/bash
FILE=sicasm/bin/Release/sicasm.exe
if [ -f $FILE ]; then
	mono ${FILE} $1
else
	echo "File ${FILE} does not exist! Did you run the makefile?"
fi
