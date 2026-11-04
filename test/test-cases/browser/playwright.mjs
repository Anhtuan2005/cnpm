import { createRequire } from 'node:module';

// Specs live outside the frontend package. Resolve its locked test runner here
// so a clean clone works and a root node_modules cannot load a second instance.
const requireFrontend = createRequire(new URL('../../../src/frontend/package.json', import.meta.url));
const runner = requireFrontend('@playwright/test');
export const expect = runner.expect;
export const test = runner.test.extend({
  context: async ({ context, baseURL }, use) => {
    if (process.env.TECHVORA_SMOKE_LIVE_ASSETS !== 'true') {
      const appOrigin = new URL(baseURL).origin;
      // Optional photo/font/icon CDNs must not make business tests wait for
      // network availability. Address API fixtures remain scoped to each spec.
      await context.route('**/*', async route => {
        const request = route.request();
        const url = new URL(request.url());
        const external = /^https?:$/.test(url.protocol) && url.origin !== appOrigin;
        if (external && request.resourceType() === 'image') {
          return route.fulfill({ contentType: 'image/svg+xml', body: '<svg xmlns="http://www.w3.org/2000/svg" width="200" height="200"><rect width="200" height="200" fill="#eee"/></svg>' });
        }
        if (external && ['font', 'stylesheet', 'script'].includes(request.resourceType())) return route.abort();
        return route.continue();
      });
    }
    await use(context);
  }
});
