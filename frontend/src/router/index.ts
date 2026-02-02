import { useAuthStore } from '../stores/authStore.ts';
import { createRouter, createWebHistory } from 'vue-router';
import { sanitizeUrlParams } from '@/utility-tools/routeUtils.ts';


const requiredAuthorization: Array<any> =
[
  { path: "/profile", name : "profile", component: () => import(`../views/Profile.vue`), meta: {requiresAuth: true} },
  { path: "/dashboard", name : "dashboard", component: () => import(`../views/Index.vue`) }//, meta: {requiresAuth: true} }
  
]

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    { path: "/", name : "index", component: () => import(`../views/Index.vue`) },
    { path: "/logout", name : "logout", component: () => import(`../views/Index.vue`) },
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

    } catch (err) {
      
      // if parsing fails, continue to route and log
      // eslint-disable-next-line no-console
      console.error('Failed to parse user from query', err);
    }
  }
  const urlParameters: Array<string> = [];
  const urlSearchParams = new URLSearchParams(window.location.search);

  urlSearchParams.forEach((value, key) => urlParameters.push(key));
  if (urlParameters && urlParameters.length > 0) sanitizeUrlParams(urlParameters);
  // navigate to same path without query params

  if (to.meta.requiresAuth && authStore.isAuthenticated) next();
  else if (to.name === 'logout') authStore.logout();
  else next('/');
})

export default router