<script setup>
import { RouterView } from "vue-router";
import SiteNav from "./components/SiteNav.vue";
import { useAmbient } from "./composables/useAmbient.js";

const { ambientEnabled } = useAmbient();
</script>

<template>
  <div class="app-root" :class="{ 'no-ambient': !ambientEnabled }">
    <SiteNav />
    <RouterView />
    <footer class="site-footer">
      <div class="shell">
        <p>
          SunhavenMod suite by
          <a href="https://github.com/AzraelGodKing" target="_blank" rel="noopener"
            >AzraelGodKing</a
          >
          ·
          <a href="https://azraelgodking.github.io/SunhavenMod/" target="_blank" rel="noopener"
            >Docs hub</a
          >
        </p>
      </div>
    </footer>
  </div>
</template>
