#!/bin/sh
# Rebuild the report:  sh docs/elaborat/build.sh
cd "$(dirname "$0")"
pandoc elaborat.md -o elaborat.docx --resource-path=. --reference-doc=reference.docx
pandoc elaborat.md -o elaborat.pdf  --resource-path=. --pdf-engine=xelatex -H pdf-header.tex \
  -V mainfont="Arial" -V monofont="Menlo" -V geometry:a4paper,margin=2cm -V fontsize=10pt -V colorlinks=true
