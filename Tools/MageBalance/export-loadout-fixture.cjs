'use strict';
// Feed the actual browser table into the real-tML integration checks; no duplicate stat table.
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const html = fs.readFileSync(path.join(__dirname, 'index.html'), 'utf8');
const ctx = { module: { exports: {} } };
vm.runInNewContext(html.match(/<script id="balance-engine">([\s\S]*?)<\/script>/)[1], ctx);
const { armors, accessories } = ctx.module.exports;
fs.writeFileSync(process.argv[2], JSON.stringify({ armors, accessories }, null, 2));
