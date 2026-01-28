<template>
  <div class="dashboard">
    <h1>Dashboard</h1>
    <div v-if="userData">
      <h2>Welcome, {{ userData.username }}!</h2>
      <pre>{{ JSON.stringify(userData, null, 2) }}</pre>
    </div>
    <div v-else>
      <p>Loading user data...</p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';

const userData = ref<any>(null);

onMounted(() => {
  console.log('Dashboard mounted');
  console.log('Full URL:', window.location.href);
  
  // Check if user data is in URL (after Discord redirect)
  const urlParams = new URLSearchParams(window.location.search);
  const token = urlParams.get('token');
  const userDataEncoded = urlParams.get('user');
  const code = urlParams.get('code');
  
  console.log('Token from URL:', token);
  console.log('User data from URL:', userDataEncoded);
  
  if (token && userDataEncoded) {
    try {
      const parsedUserData = JSON.parse(decodeURIComponent(userDataEncoded));
      console.log('Discord User Information:', parsedUserData);
      userData.value = parsedUserData;
      localStorage.setItem('user_token', token);
      localStorage.setItem('user_data', JSON.stringify(parsedUserData));
      
      // Clean up URL
      window.history.replaceState({}, document.title, window.location.pathname);
    } catch (error) {
      console.error('Error parsing user data:', error);
    }
  } else if (code) {
    const backendBaseUrl = import.meta.env.VITE_C_SERVER;
    window.location.href = `${backendBaseUrl}/auth/discord/callback?code=${encodeURIComponent(code)}`;
  } else {
    // Check if user is already logged in
    const storedUserData = localStorage.getItem('user_data');
    if (storedUserData) {
      try {
        userData.value = JSON.parse(storedUserData);
        console.log('Logged in user from localStorage:', userData.value);
      } catch (error) {
        console.error('Error parsing stored user data:', error);
      }
    }
  }
});
</script>

<style scoped>
.dashboard {
  padding: 2rem;
}

pre {
  background: #f5f5f5;
  padding: 1rem;
  border-radius: 4px;
  overflow-x: auto;
}
</style>