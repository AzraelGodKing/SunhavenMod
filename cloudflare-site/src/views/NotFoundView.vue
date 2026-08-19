<script setup>
import { RouterLink } from "vue-router";
import AmbientLayer from "../components/AmbientLayer.vue";
</script>

<template>
  <div class="page page-404">
    <AmbientLayer variant="landing" />
    <main id="main" class="shell not-found">
      <p class="brand-mark">SunhavenMod</p>
      <h1>Path not found</h1>
      <p class="lead">That trail does not exist in this haven. Head back to home base.</p>
      <div class="cta-row">
        <RouterLink class="btn btn-primary" to="/">Return home</RouterLink>
        <RouterLink class="btn btn-ghost" to="/downloads">Downloads board</RouterLink>
      </div>
    </main>
  </div>
</template>
