import { ref, watch } from "vue";

const STORAGE_KEY = "sunhavenmod-ambient-enabled";

function readStored() {
  try {
    return localStorage.getItem(STORAGE_KEY) !== "0";
  } catch {
    return true;
  }
}

const ambientEnabled = ref(readStored());

watch(ambientEnabled, (value) => {
  try {
    localStorage.setItem(STORAGE_KEY, value ? "1" : "0");
  } catch {
    // Ignore storage failures.
  }
});

export function useAmbient() {
  function toggleAmbient() {
    ambientEnabled.value = !ambientEnabled.value;
  }

  return { ambientEnabled, toggleAmbient };
}
