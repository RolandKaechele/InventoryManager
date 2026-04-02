const fs   = require('fs');
const path = require('path');

const assetsBase = path.join(__dirname, 'Assets');

const folders = [
    path.join(assetsBase, 'Items'),
    path.join(assetsBase, 'Resources', 'Items'),
    path.join(assetsBase, 'Resources', 'Items', 'Icons'),
    path.join(assetsBase, 'Scripts'),
];

folders.forEach(dir => {
    if (!fs.existsSync(dir)) {
        fs.mkdirSync(dir, { recursive: true });
        console.log(`Created: ${dir}`);
    } else {
        console.log(`Exists:  ${dir}`);
    }
});
