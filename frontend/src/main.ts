import { createApp } from 'vue';
import { createPinia } from 'pinia';

import App from './App.vue';
import { auth0 } from './auth0';
import router from './router';
import './assets/sass/index.sass';
import { useAuthStore } from './stores/authStore';
import { useTagsStore } from './stores/tagsStore';

const app = createApp(App)

app.use(createPinia())
app.use(router)
app.use(auth0)

app.mount('#app')

// Tags are Kodeklubb data, so they are loaded only after Auth0 is ready
// and the user is logged in. Fire-and-forget: it never blocks startup.
// Components that need tags still call tagsStore.ensureLoaded() themselves.
const authStore = useAuthStore();
authStore.whenReady().then(() => {
    if (authStore.isAuthenticated) {
        useTagsStore().ensureLoaded().catch(() => {
            // The error is stored in tagsStore.error for the UI to show.
        });
    }
});