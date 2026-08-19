<script setup>
import { RouterLink, useRoute } from "vue-router";
import { useAmbient } from "../composables/useAmbient.js";

const route = useRoute();
const { ambientEnabled, toggleAmbient } = useAmbient();
</script>

<template>
  <a class="skip-link" href="#main">Skip to content</a>
  <nav class="site-nav" aria-label="Primary">
    <div class="shell site-nav-inner">
      <RouterLink class="site-nav-brand" to="/" active-class="" exact-active-class="">
        SunhavenMod
      </RouterLink>
      <div class="site-nav-links">
        <RouterLink
          to="/"
          active-class=""
          exact-active-class="active"
          :class="{ active: route.name === 'home' }"
          >Home</RouterLink
        >
        <RouterLink
          to="/downloads"
          active-class="active"
          :class="{ active: route.name === 'downloads' }"
          >Downloads</RouterLink
        >
        <RouterLink
          to="/feedback"
          active-class="active"
          :class="{ active: route.name === 'feedback' }"
          >Feedback</RouterLink
        >
        <button class="ambient-toggle" type="button" @click="toggleAmbient">
          {{ ambientEnabled ? "Ambient: On" : "Ambient: Off" }}
        </button>
      </div>
    </div>
  </nav>
</template>
