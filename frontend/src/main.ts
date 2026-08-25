import { createApp } from 'vue';
import { createPinia } from 'pinia';

import App from './App.vue';
import router from './router';
import './assets/sass/index.sass';
import { useTagsStore } from './stores/tagsStore';

const app = createApp(App)

app.use(createPinia())
app.use(router)

app.mount('#app')

// Load the tag list once at startup. Safe to call before the user is
// logged in, and fire-and-forget so it never blocks app startup —
// components that need tags call tagsStore.ensureLoaded() themselves,
// which will simply resolve immediately once this finishes.
useTagsStore().ensureLoaded().catch(() => {
    // Errors are already captured in tagsStore.error for the UI to show;
    // nothing further to do here.
});
