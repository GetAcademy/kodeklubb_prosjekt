import { useAuthStore } from '../stores/authStore.ts';
import { createRouter, createWebHistory } from 'vue-router';
import { sanitizeUrlParams } from '@/utility-tools/routeUtils.ts';


const requiredAuthorization: Array<any> =
[
  { path: "/profile", name : "min-side", component: () => import(`../views/Profile.vue`), meta: {requiresAuth: true} },
  { path: "/discover-teams", name : "utforsk-teams", component: () => import(`../views/Profile.vue`), meta: {requiresAuth: true} },
  { path: "/teams/my-team", name : "teams", component: () => import(`../views/Profile.vue`), meta: {requiresAuth: true} },
  { path: "/logout", name : "logout", component: () => import(`../views/Index.vue`), meta: {requiresAuth: true} },
]

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    { path: "/", name : "index", component: () => import(`../views/Index.vue`) },
    ...requiredAuthorization
  ],
})
router.beforeEach((to, from, next) => {

  const authStore = useAuthStore();
  const token: string = (to.query as any).token;
  const userEncoded: string = (to.query as any).user;

  if (token && userEncoded) {
    try {
      const user = JSON.parse(decodeURIComponent(userEncoded));

      authStore.setUser(user);
      authStore.setToken(token);

    } catch (err) {console.error('Failed to parse user from query', err);}
  }
  const urlParameters: Array<string> = [];
  const urlSearchParams = new URLSearchParams(window.location.search);

  urlSearchParams.forEach((value, key) => urlParameters.push(key));
  if (urlParameters && urlParameters.length > 0) sanitizeUrlParams(urlParameters);

  if (to.meta.requiresAuth && authStore.isAuthenticated) next();
  else if (to.name === 'logout') authStore.logout();
  else next('/');
})

export default router