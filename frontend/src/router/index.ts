import { useAuthStore } from '../stores/authStore.ts';
import { createRouter, createWebHistory } from 'vue-router';

const profileRoutes: Array<Record<string, any>> = 
[
  { path: "/profile", name : "min-side", component: () => import(`../views/profile/Profile.vue`), meta: {requiresAuth: true} },
  { path: "/profile/edit", name : "ModifyProfile", component: () => import(`../views/profile/EditProfile.vue`), meta: {requiresAuth: true, isHidden: true} },
];

const teamRoutes: Array<Record<string, any>> = 
[
  { path: "/teams/:id", name : "medlemmer", component: () => import(`../views/profile/Profile.vue`), meta: {requiresAuth: true} },
  { path: "/teams/:id/members", name : "medlemmer", component: () => import(`../views/profile/Profile.vue`), meta: {requiresAuth: true} },
  { path: "/teams/:id/news", name : "aktuelt", component: () => import(`../views/profile/Profile.vue`), meta: {requiresAuth: true} },
  { path: "/teams/:id/description", name : "Om gruppen", component: () => import(`../views/profile/Profile.vue`), meta: {requiresAuth: true} },
];

const requiredAuthorization: Array<any> =
[
  ...teamRoutes,
  ...profileRoutes,
  { path: "/discover", name : "Utforsk grupper", component: () => import(`../views/profile/Profile.vue`), meta: {requiresAuth: true} },
  { path: "/logout", name : "logout", component: () => import(`../views/Index.vue`), meta: {requiresAuth: true} },
];

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    { path: "/", name : "index", component: () => import(`../views/Index.vue`) },
    ...requiredAuthorization
  ]});

router.beforeEach((to, from, next) =>
{
  const authStore = useAuthStore();
  const token: string = (to.query as any).token;
  const userEncoded: string = (to.query as any).user;

  if (token && userEncoded)
  {
    try {
      const user = JSON.parse(decodeURIComponent(userEncoded));

      authStore.setUser(user);
      authStore.setToken(token);

    } catch (err) {console.error('Failed to parse user from query', err);}
  }
router.afterEach((to) => {
  if (Object.keys(to.query).length > 0) router.replace({ path: to.path,  query: {}, hash: to.hash});

  // Save user to database
  next();
  });
});
export default router;