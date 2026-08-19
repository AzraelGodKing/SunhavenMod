<script setup>
import { reactive, ref } from "vue";
import AmbientLayer from "../components/AmbientLayer.vue";

const form = reactive({
  type: "bug",
  name: "",
  title: "",
  description: "",
  website: "",
});
const formStatus = ref("idle");
const formNotice = ref("");

function resetForm() {
  form.name = "";
  form.title = "";
  form.description = "";
  form.website = "";
}

async function submitFeedback() {
  formNotice.value = "";
  if (![form.name, form.title, form.description].every((v) => String(v || "").trim())) {
    formStatus.value = "error";
    formNotice.value = "Please complete name, title, and description.";
    return;
  }
  formStatus.value = "submitting";
  try {
    const res = await fetch("/api/feedback", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(form),
    });
    const payload = await res.json().catch(() => ({}));
    if (!res.ok) throw new Error(payload?.error || `Request failed (${res.status})`);
    formStatus.value = "success";
    formNotice.value = "Thanks! Your feedback was submitted successfully.";
    resetForm();
  } catch (err) {
    formStatus.value = "error";
    formNotice.value =
      err instanceof Error ? err.message : "Could not submit feedback right now.";
  }
}
</script>

<template>
  <div class="page page-feedback">
    <AmbientLayer variant="downloads" />

    <header class="page-header">
      <div class="shell">
        <p class="brand-mark">SunhavenMod</p>
        <h1>Report a bug / request a feature</h1>
        <p class="lead">
          Share feedback without leaving the site. Submissions go to our tracker in the
          background.
        </p>
      </div>
    </header>

    <main id="main" class="shell stack">
      <section class="feedback-panel">
        <p class="kicker">Feedback desk</p>
        <h2>Send it from here</h2>
        <form class="feedback-form" novalidate @submit.prevent="submitFeedback">
          <label class="field">
            <span>Type</span>
            <select v-model="form.type" :disabled="formStatus === 'submitting'">
              <option value="bug">Bug</option>
              <option value="feature">Feature</option>
            </select>
          </label>
          <label class="field">
            <span>Name</span>
            <input
              v-model.trim="form.name"
              autocomplete="name"
              required
              maxlength="120"
              :disabled="formStatus === 'submitting'"
            />
          </label>
          <label class="field">
            <span>Title</span>
            <input
              v-model.trim="form.title"
              required
              maxlength="160"
              :disabled="formStatus === 'submitting'"
            />
          </label>
          <label class="field">
            <span>Description</span>
            <textarea
              v-model.trim="form.description"
              rows="5"
              required
              maxlength="4000"
              :disabled="formStatus === 'submitting'"
            ></textarea>
          </label>
          <label class="honeypot" aria-hidden="true">
            <span>Leave this field empty</span>
            <input v-model="form.website" tabindex="-1" autocomplete="off" />
          </label>
          <div class="feedback-actions">
            <button
              class="btn btn-primary"
              type="submit"
              :disabled="formStatus === 'submitting'"
            >
              {{ formStatus === "submitting" ? "Submitting…" : "Submit feedback" }}
            </button>
            <p v-if="formNotice" class="feedback-inline" role="status" aria-live="polite">
              {{ formNotice }}
            </p>
          </div>
        </form>
      </section>
    </main>
  </div>
</template>
