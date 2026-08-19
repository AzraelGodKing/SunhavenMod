<script setup>
import { formatInt, formatTotalWithUnique } from "../composables/format.js";

defineProps({
  siteTotal: { type: Object, required: true },
  totalMods: { type: Number, required: true },
});
</script>

<template>
  <div class="totals" aria-label="Suite download totals">
    <div class="total">
      <span class="total-label">Guild total</span>
      <span class="total-value">{{ formatInt(siteTotal.grand_total) }}</span>
    </div>
    <div class="total">
      <span class="total-label">Thunderstore</span>
      <span class="total-value">{{ formatInt(siteTotal.thunderstore) }}</span>
    </div>
    <div class="total">
      <span class="total-label">Nexus total (unique)</span>
      <span class="total-value">{{
        formatTotalWithUnique(siteTotal.nexus_total, siteTotal.nexus_unique)
      }}</span>
    </div>
    <div class="total">
      <span class="total-label">Published mods</span>
      <span class="total-value">{{ formatInt(totalMods) }}</span>
    </div>
  </div>
</template>
