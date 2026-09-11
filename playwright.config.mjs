import { defineConfig } from '@playwright/test';

export default defineConfig({
  testDir: './tests/ui',
  timeout: 120000,
  workers: 1,
  reporter: [
    ['list'],
    ['json', { outputFile: 'artifacts/verification/ui-tests.json' }],
  ],
  use: {
    baseURL: process.env.MEDULA_URL || 'http://localhost:5186',
    viewport: { width: 1440, height: 1000 },
    locale: 'tr-TR',
    trace: 'retain-on-failure',
  },
  outputDir: 'artifacts/ui-results',
});
