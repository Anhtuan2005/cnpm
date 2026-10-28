import { createRequire } from 'node:module';

// Specs live outside the frontend package. Resolve its locked test runner here
// so a clean clone works and a root node_modules cannot load a second instance.
const requireFrontend = createRequire(new URL('../../../src/frontend/package.json', import.meta.url));
export const { test, expect } = requireFrontend('@playwright/test');
