'use strict';
const fs = require('fs');
const path = require('path');
const src = path.join(__dirname, '..', 'Images');
const dst = path.join(__dirname, '..', 'public', 'images');
if (!fs.existsSync(src)) {
  console.warn('sync-login-images: Images folder not found, skipping.');
  process.exit(0);
}
fs.mkdirSync(dst, { recursive: true });
for (const name of fs.readdirSync(src)) {
  const from = path.join(src, name);
  const to = path.join(dst, name);
  fs.cpSync(from, to, { recursive: true });
}
console.log('sync-login-images: copied Images/* -> public/images/');
