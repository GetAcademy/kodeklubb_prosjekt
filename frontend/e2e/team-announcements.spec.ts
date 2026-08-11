import { test, expect } from '@playwright/test';

test('team admin can create, edit, and delete a team announcement', async ({ page }) => {
  page.on('console', msg => console.log('BROWSER LOG:', msg.text()));
  page.on('requestfailed', req => console.log('REQUEST FAILED:', req.url(), '-', req.failure()?.errorText));
  page.on('response', res => {
    if (res.url().includes('/api/')) console.log('API RESPONSE:', res.status(), res.url());
  });

  const teamId = '22222222-2222-2222-2222-222222222222';
  const user = {
    id: '1485954601365671966',
    username: 'swatisonawane_58285',
    email: 'test@example.com',
    flags: 0,
    banner: '',
    locale: 'nb',
    avatar: '',
    verified: true,
    banner_color: '',
    accent_color: 0,
    mfa_enabled: false,
    premium_type: 0,
    public_flags: 0,
    discriminator: '0001'
  };

  const title = `Playwright announcement ${Date.now()}`;
  const updatedTitle = `${title} (updated)`;
  const body = 'This announcement was created by an automated test.';
  const updatedBody = 'This announcement was updated by an automated test.';

  await page.addInitScript((user) => {
    window.localStorage.setItem('user_token', 'test-token');
    window.localStorage.setItem('user_data', JSON.stringify(user));
  }, user);

  await page.goto(`/teams/${teamId}/news`, { waitUntil: 'networkidle' });
  await expect(page.getByRole('heading', { name: 'Team News' })).toBeVisible();
  await expect(page.locator('section.create-news')).toBeVisible();

  await page.getByLabel('Tittel').fill(title);
  await page.getByLabel('Innhold').fill(body);

 const [createResponse] = await Promise.all([
  page.waitForResponse(res => res.url().includes('/announcements') && res.request().method() === 'POST'),
  page.getByRole('button', { name: 'Publiser' }).click(),
]);
const created = await createResponse.json();
console.log('CREATED RESPONSE BODY:', JSON.stringify(created));
const announcementId = created.Id ?? created.id;
console.log('EXTRACTED ID:', announcementId);

const announcementCard = page.getByTestId(`announcement-${announcementId}`);
await expect(announcementCard).toBeVisible();

  await announcementCard.getByRole('button', { name: 'Rediger' }).click();
  await expect(announcementCard.locator('input')).toBeVisible();
  await announcementCard.locator('input').fill(updatedTitle);
  await announcementCard.locator('textarea').fill(updatedBody);
  await announcementCard.getByRole('button', { name: 'Lagre' }).click();

  await expect(announcementCard.locator('h2')).toHaveText(updatedTitle);
  await expect(announcementCard).toContainText(updatedBody);

  page.on('dialog', async (dialog) => {
    await dialog.accept();
  });

  await announcementCard.getByRole('button', { name: 'Slett' }).click();
  await expect(page.getByTestId(`announcement-${announcementId}`)).toHaveCount(0);
});