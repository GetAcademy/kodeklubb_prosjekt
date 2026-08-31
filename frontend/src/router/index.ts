import axios from 'axios';
import { useAuthStore } from '../stores/authStore.ts';
import { createRouter, createWebHistory } from 'vue-router';


const profileRoutes: Array<Record<string, any>> =
[
  { path: "/profile", name : "min-side", component: () => import(`../views/profile/Profile.vue`), meta: {requiresAuth: true} },
  { path: "/profile/edit", name : "ModifyProfile", component: () => import(`../views/profile/EditProfile.vue`), meta: {requiresAuth: true, isHidden: true} },
  { path: "/profile/my-requests", name : "MyRequests", component: () => import(`../views/teams/MyRequests.vue`), meta: {requiresAuth: true} },
  { path: "/profile/add-tags", name: "LeggTilInteresser", component: () => import('../views/teams/AddTagsPage.vue'), meta: { requiresAuth: true, isHidden: true } },
];


const teamRoutes: Array<Record<string, any>> = 
[
  { path: "/teams/:teamId", name : "Team Info", component: () => import(`../views/teams/TeamDashboard.vue`), meta: {requiresAuth: true, isTeam: true} },
  { path: "/teams/:teamId/members", name : "Medlemmer", component: () => import(`../views/teams/Members.vue`), meta: {requiresAuth: true, isTeam: true} },
  { path: "/teams/:teamId/news", name : "Aktuelt", component: () => import(`../views/teams/News.vue`), meta: {requiresAuth: true, isTeam: true} },
  { path: "/teams/:teamId/add-tags", name : "LeggTilTags", component: () => import(`../views/teams/AddTagsPage.vue`), meta: {requiresAuth: true, isTeam: true} },
];

const requiredAuthorization: Array<any> =
[
  ...teamRoutes,
  ...profileRoutes,
  { path: "/discover", name : "Utforsk grupper", component: () => import(`../views/Discover.vue`), meta: {requiresAuth: true} },
  { path: "/logout", name : "logout", component: () => import(`../views/Index.vue`), meta: {requiresAuth: true} },
];

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    { path: "/", name : "index", component: () => import(`../views/Index.vue`) },
    { path: "/pico-demo", name: "PicoDemo", component: () => import(`../views/PicoDemo.vue`) },
    ...requiredAuthorization
  ]});

router.beforeEach((to, from, next) =>
{
  const authStore = useAuthStore();
  const token = (to.query as any).token as string | undefined;
  const userEncoded = (to.query as any).user as string | undefined;

  if (token && userEncoded)
  {
    try {
      const user = JSON.parse(decodeURIComponent(userEncoded));

      authStore.setUser(user);
      authStore.setToken(token);
      const targetPath = to.path === '/' ? '/profile' : to.path;
      return next({ path: targetPath, query: {}, hash: to.hash });
    }
    catch (err) {
      console.error('Failed to parse user from query', err);
    }
  }

  next();
});

router.afterEach((to) => {
  const hasAuthQuery = Boolean((to.query as any).token || (to.query as any).user);
  if (hasAuthQuery) {
    router.replace({ path: to.path, query: {}, hash: to.hash });
  }
});
export default router;
