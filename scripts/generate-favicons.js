const sharp = require('sharp');
const fs = require('fs');
const path = require('path');

const IMG = path.join(__dirname, '../src/TaxiBlitz.Web/wwwroot/img');
const ROOT = path.join(__dirname, '../src/TaxiBlitz.Web/wwwroot');

const DARK_SVG  = path.join(IMG, 'favicon-dark.svg');
const LIGHT_SVG = path.join(IMG, 'favicon-light.svg');

async function rasterise(svgPath, px) {
  return sharp(svgPath, { density: Math.ceil(72 * px / 100) + 1 })
    .resize(px, px)
    .png({ compressionLevel: 9 })
    .toBuffer();
}

function buildIco(images) {
  const HEADER = 6, DIR = 16;
  const hdr = Buffer.alloc(HEADER);
  hdr.writeUInt16LE(0, 0);
  hdr.writeUInt16LE(1, 2);
  hdr.writeUInt16LE(images.length, 4);

  const dirs = [];
  let offset = HEADER + DIR * images.length;
  for (const { buf, size } of images) {
    const d = Buffer.alloc(DIR);
    d.writeUInt8(size >= 256 ? 0 : size, 0);
    d.writeUInt8(size >= 256 ? 0 : size, 1);
    d.writeUInt8(0, 2);
    d.writeUInt8(0, 3);
    d.writeUInt16LE(1, 4);
    d.writeUInt16LE(32, 6);
    d.writeUInt32LE(buf.length, 8);
    d.writeUInt32LE(offset, 12);
    offset += buf.length;
    dirs.push(d);
  }
  return Buffer.concat([hdr, ...dirs, ...images.map(i => i.buf)]);
}

async function main() {
  console.log('Generating TaxiBlitz favicons…\n');

  const [d16, d32, d48, d512] = await Promise.all([
    rasterise(DARK_SVG,  16),
    rasterise(DARK_SVG,  32),
    rasterise(DARK_SVG,  48),
    rasterise(DARK_SVG, 512),
  ]);
  const [l512] = await Promise.all([
    rasterise(LIGHT_SVG, 512),
  ]);

  const writes = [
    [path.join(IMG,  'favicon-16.png'),       d16],
    [path.join(IMG,  'favicon-32.png'),       d32],
    [path.join(IMG,  'favicon-48.png'),       d48],
    [path.join(IMG,  'favicon-512.png'),      d512],
    [path.join(IMG,  'favicon-light-512.png'),l512],
    [path.join(ROOT, 'favicon.ico'),          buildIco([{ buf: d16, size: 16 }, { buf: d32, size: 32 }])],
  ];

  for (const [filePath, data] of writes) {
    fs.writeFileSync(filePath, data);
    console.log(`  ✓  ${path.relative(path.join(__dirname, '..'), filePath)}`);
  }

  console.log('\nAll done.');
}

main().catch(e => { console.error(e.message); process.exit(1); });
