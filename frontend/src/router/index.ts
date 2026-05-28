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
  { path: "/teams/:teamId/edit", name: "Rediger team", component: () => import(`../views/teams/EditTeam.vue`), meta: {requiresAuth: true, isTeam: false, isHidden: true} },
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

      // Redirect to main page after successful login
      return next({ path: '/discover', query: {} });

    } catch (err) {
      console.error('Failed to parse user from query', err);
    }
  }

  // Redirect unauthenticated users away from protected routes
  if (to.meta.requiresAuth && !authStore.isAuthenticated) {
    return next({ path: '/' });
  }

  next();
});

export default router;