// Pass the bundled node_modules directory as the first argument.
// The PNG files are local QA previews; use the SVG files in the report.
const path = require('node:path');
const sharp = require(path.join(process.argv[2], 'sharp'));

(async () => {
  for (const name of ['reversi-object', 'save-load-sequence']) {
    await sharp(path.join(__dirname, name + '.svg')).png().toFile(path.join(__dirname, name + '.png'));
    console.log('Rendered ' + name + '.png');
  }
})().catch(error => { console.error(error.message); process.exitCode = 1; });
