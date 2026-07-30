import { execSync } from 'child_process';

async function globalTeardown() {
  console.log('Tearing down Docker Compose stack...');
  try {
    execSync('docker compose down -v', { cwd: process.cwd(), stdio: 'inherit' });
  } catch (err) {
    console.error('Docker compose down failed:', err);
  }
}

export default globalTeardown;
