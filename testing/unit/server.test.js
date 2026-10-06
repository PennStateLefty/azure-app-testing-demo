import { after, before, test } from 'node:test';
import assert from 'node:assert/strict';
import { createServer } from '../../src/backend/server.js';

let server;
let baseURL;

before(async () => {
  server = createServer();
  await new Promise((resolve) => server.listen(0, '127.0.0.1', resolve));
  baseURL = `http://127.0.0.1:${server.address().port}`;
});

after(async () => {
  await new Promise((resolve, reject) => server.close((error) => error ? reject(error) : resolve()));
});

test('health endpoint reports readiness', async () => {
  const response = await fetch(`${baseURL}/api/health`);
  assert.equal(response.status, 200);
  assert.deepEqual(await response.json(), { status: 'ok' });
});

test('catalog has stable IDs and positive prices', async () => {
  const response = await fetch(`${baseURL}/api/products?demo=true`);
  assert.equal(response.status, 200);
  const products = await response.json();
  assert.equal(products.length, 3);
  assert.equal(new Set(products.map((product) => product.id)).size, 3);
  for (const product of products) {
    assert.equal(typeof product.name, 'string');
    assert.ok(product.price > 0);
  }
});

test('serves only allowlisted frontend assets with correct content types', async () => {
  for (const [path, type] of [['/', 'text/html'], ['/app.js', 'text/javascript'], ['/styles.css', 'text/css']]) {
    const response = await fetch(`${baseURL}${path}`);
    assert.equal(response.status, 200);
    assert.ok(response.headers.get('content-type').startsWith(type));
    assert.equal(response.headers.get('x-content-type-options'), 'nosniff');
    assert.ok((await response.text()).length > 0);
  }
});

test('unknown routes and internal files are not exposed', async () => {
  for (const path of ['/missing', '/server.js', '/package.json', '/%2e%2e/backend/server.js', '/toString']) {
    const response = await fetch(`${baseURL}${path}`);
    assert.equal(response.status, 404);
    assert.deepEqual(await response.json(), { error: 'Not found' });
  }
});

test('unsupported methods cannot change the catalog', async () => {
  const response = await fetch(`${baseURL}/api/products`, { method: 'POST' });
  assert.equal(response.status, 405);
  assert.equal(response.headers.get('allow'), 'GET');
});
