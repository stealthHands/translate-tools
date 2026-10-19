import { createRequire } from 'node:module';
import { mkdir, readdir, copyFile } from 'node:fs/promises';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
const require = createRequire(import.meta.url);
const entry = require.resolve('@huggingface/transformers');
const dist = dirname(entry);
const ortDist = dirname(require.resolve('onnxruntime-web'));
const vendor = join(here, 'vendor');
await mkdir(vendor, { recursive: true });
const transformerFiles = await readdir(dist);
const ortFiles = await readdir(ortDist);
const selected = [
  ...transformerFiles.filter(name => name === 'transformers.min.js' || name.endsWith('.wasm') || name.endsWith('.mjs')).map(name => [dist, name]),
  ...ortFiles.filter(name => /^ort-wasm.*\.(wasm|mjs)$/.test(name)).map(name => [ortDist, name])
];
if (!selected.some(([, name]) => name === 'transformers.min.js')) throw new Error(`transformers.min.js missing from ${dist}`);
if (!selected.some(([, name]) => name === 'ort-wasm-simd-threaded.wasm')) throw new Error(`ONNX Runtime Web WASM file missing from ${ortDist}`);
for (const [source, name] of selected) await copyFile(join(source, name), join(vendor, name));
console.log(`Staged ${selected.length} browser runtime files into ${vendor}`);
