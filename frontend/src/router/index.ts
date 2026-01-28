import { createRouter, createWebHistory } from 'vue-router'

const views: string = '../views';

const requiredAuthorization: Array<any> =
[
  { path: "/profile", name : "profile", component: () => import(`${views}/Profile.vue`) },//, meta: {requiresAuth: true} }
  { path: "/dashboard", name : "dashboard", component: () => import(`${views}/Dashboard.vue`) }//, meta: {requiresAuth: true} }
  
]

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    { path: "/", name : "index", component: () => import(`${views}/indexView.vue`) },
    ...requiredAuthorization
  ],
})

router.beforeEach((to, from, next) => {
  const isAuthenticated: boolean = !!localStorage.getItem('DiscordID');

  if (to.meta.requiresAuth && !isAuthenticated) next({path:'/'}) 
  else next();
})

export default router