<script setup>
import { useAmbient } from "../composables/useAmbient.js";

defineProps({
  variant: { type: String, default: "landing" },
});

const { ambientEnabled } = useAmbient();
</script>

<template>
  <div
    v-if="ambientEnabled"
    class="ambient"
    :class="'ambient-' + variant"
    aria-hidden="true"
  >
    <span class="ambient-orb ambient-orb-a"></span>
    <span class="ambient-orb ambient-orb-b"></span>
    <span class="ambient-orb ambient-orb-c"></span>
    <span class="ambient-grain"></span>
  </div>
</template>
