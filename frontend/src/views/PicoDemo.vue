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
      This page uses Pico's classless stylesheet, scoped to just this
      component. No other page in the app is affected by it.
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
// No script logic needed — this page exists purely to compare Pico's
// default styling against the rest of the app's custom CSS.
</script>

<style scoped>
/* Pico's classless stylesheet, imported ONLY inside this component's
   scoped style block. It styles raw semantic HTML (header, nav, article,
   button, form, input) automatically, but — because Vue's `scoped`
   attribute adds a unique data attribute to every element in this
   template — it cannot leak out and affect any other component in the
   app, unlike a global import in main.ts would. */
@import '@picocss/pico/css/pico.classless.min.css';

.pico-demo {
  padding: 24px;
  max-width: 640px;
  margin: 0 auto;
}
</style>
