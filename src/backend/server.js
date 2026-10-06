import { createServer as createHttpServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import { resolve } from 'node:path';

const products = [
  { id: 1, name: 'Demo notebook', price: 12 },
  { id: 2, name: 'Demo mug', price: 18 },
  { id: 3, name: 'Demo backpack', price: 45 },
];

const assets = {
  '/': ['index.html', 'text/html; charset=utf-8'],
  '/app.js': ['app.js', 'text/javascript; charset=utf-8'],
  '/styles.css': ['styles.css', 'text/css; charset=utf-8'],
};

export function createServer() {
  return createHttpServer(async (request, response) => {
    response.setHeader('X-Content-Type-Options', 'nosniff');
    response.setHeader('Content-Security-Policy', "default-src 'self'; frame-ancestors 'none'");
    const sendJson = (status, body) => {
      response.writeHead(status, { 'Content-Type': 'application/json; charset=utf-8' });
      response.end(JSON.stringify(body));
    };

    if (request.method !== 'GET') {
      response.setHeader('Allow', 'GET');
      sendJson(405, { error: 'Method not allowed' });
      return;
    }

    const pathname = request.url.split('?')[0];
    if (pathname === '/api/health') {
      sendJson(200, { status: 'ok' });
    } else if (pathname === '/api/products') {
      sendJson(200, products);
    } else if (Object.hasOwn(assets, pathname)) {
      const [filename, contentType] = assets[pathname];
      try {
        const body = await readFile(new URL(`../frontend/${filename}`, import.meta.url));
        response.writeHead(200, { 'Content-Type': contentType });
        response.end(body);
      } catch {
        sendJson(500, { error: 'Unable to load page' });
      }
    } else {
      sendJson(404, { error: 'Not found' });
    }
  });
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  const port = Number(process.env.PORT || 3000);
  createServer().listen(port, '0.0.0.0', () => {
    console.log(`Demo listening on port ${port}`);
  });
}
