import { request } from '@playwright/test';

async function globalSetup() {
  const baseURL = 'http://localhost:4200';
  const context = await request.newContext({ baseURL });
  let attempts = 0;
  const maxAttempts = 60;

  while (attempts < maxAttempts) {
    try {
      const response = await context.get('/api/genres', { timeout: 5000 });
      if (response.ok()) {
        await context.dispose();
        return;
      }
    } catch {
    }
    attempts++;
    await new Promise((resolve) => setTimeout(resolve, 1000));
  }

  await context.dispose();
  throw new Error('API did not become ready in time for e2e tests');
}

export default globalSetup;
