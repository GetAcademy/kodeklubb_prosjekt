<template>
  <div class="pico-demo">
    <header>
      <nav>
        <ul><li><strong>GET Academy</strong></li></ul>
        <ul><li><router-link to="/">Dashboard</router-link></li></ul>
      </nav>
    </header>

    <h2>Pico CSS demo (scoped)</h2>
    <p>
      This page uses Pico's classless stylesheet, imported only inside
      this component's own script. No other page in the app is affected
      by it.
    </p>

    <article>
      <h3>Sample team card</h3>
      <p>Team seeded for local development.</p>
      <button>Open</button>
    </article>

    <form @submit.prevent>
      <label>
        Team name
        <input type="text" placeholder="Enter a team name" />
      </label>
      <button type="submit">Create team</button>
    </form>
  </div>
</template>

<script setup lang="ts">
// Imported here (a JS-level import, not a CSS @import inside a scoped
// <style> block) because this component is lazy-loaded by the router
// (() => import('../views/PicoDemo.vue')). Vite bundles this CSS into
// that component's own dynamic chunk, so it's only ever injected into
// the page when this specific route is visited — unlike the earlier
// main.ts import, which loaded it globally for every page from the
// moment the app started.
import '@picocss/pico/css/pico.classless.min.css';
</script>

<style scoped>
.pico-demo {
  padding: 24px;
  max-width: 640px;
  margin: 0 auto;
}
</style>
