import axios from 'axios';
import { useAuthStore } from '../stores/authStore.ts';
import { createRouter, createWebHistory } from 'vue-router';


const profileRoutes: Array<Record<string, any>> = 
[
  { path: "/profile", name : "min-side", component: () => import(`../views/profile/Profile.vue`), meta: {requiresAuth: true} },
  { path: "/profile/edit", name : "ModifyProfile", component: () => import(`../views/profile/EditProfile.vue`), meta: {requiresAuth: true, isHidden: true} },
];

const teamRoutes: Array<Record<string, any>> = 
[
  { path: "/teams/:teamId", name : "Team Portal", component: () => import(`../views/teams/TeamDashboard.vue`), meta: {requiresAuth: true, isTeam: true} },
  { path: "/teams/:id/members", name : "Medlemmer", component: () => import(`../views/teams/Members.vue`), meta: {requiresAuth: true, isTeam: true} },
  { path: "/teams/:id/news", name : "Aktuelt", component: () => import(`../views/teams/News.vue`), meta: {requiresAuth: true, isTeam: true} },
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

    } catch (err) {console.error('Failed to parse user from query', err);}
  }
  next();
});

router.afterEach((to) => {
  if (Object.keys(to.query).length > 0) router.replace({ path: to.path,  query: {}, hash: to.hash});
  // Save user to database
});
router.afterEach((to) => { if (Object.keys(to.query).length > 0) router.replace({ path: to.path,  query: {}, hash: to.hash});});
export default router;