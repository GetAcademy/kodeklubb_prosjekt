
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
    { path: "/auth-test", name: "AuthTest", component: () => import(`../views/AuthTest.vue`) },
    ...requiredAuthorization
  ]});

router.beforeEach(async (to) => {
  const authStore = useAuthStore();

  // Wait until Auth0 has finished loading (and handled ?code=&state= after login),
  // and the Kodeklubb user is fetched. Otherwise every reload looks "logged out".
  await authStore.whenReady();

  if (to.meta?.requiresAuth && !authStore.isAuthenticated) {
    return { path: '/' };
  }
});

export default router;