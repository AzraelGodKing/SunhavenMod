import { createRouter, createWebHistory } from "vue-router";

const HomeView = () => import("../views/HomeView.vue");
const DownloadsView = () => import("../views/DownloadsView.vue");
const FeedbackView = () => import("../views/FeedbackView.vue");
const ModView = () => import("../views/ModView.vue");
const NotFoundView = () => import("../views/NotFoundView.vue");

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: "/", name: "home", component: HomeView, meta: { title: "SunhavenMod" } },
    {
      path: "/downloads",
      name: "downloads",
      component: DownloadsView,
      meta: { title: "Downloads · SunhavenMod" },
    },
    {
      path: "/feedback",
      name: "feedback",
      component: FeedbackView,
      meta: { title: "Feedback · SunhavenMod" },
    },
    {
      path: "/mods/:modKey",
      name: "mod",
      component: ModView,
      props: true,
      meta: { title: "Mod · SunhavenMod" },
    },
    // Legacy static HTML paths
    { path: "/index.html", redirect: "/" },
    { path: "/downloads.html", redirect: "/downloads" },
    { path: "/feedback.html", redirect: "/feedback" },
    {
      path: "/mods/:modKey.html",
      redirect: (to) => `/mods/${to.params.modKey}`,
    },
    { path: "/:pathMatch(.*)*", name: "not-found", component: NotFoundView },
  ],
  scrollBehavior(to, _from, saved) {
    if (saved) return saved;
    if (to.hash) return { el: to.hash, behavior: "smooth" };
    return { top: 0 };
  },
});

router.afterEach((to) => {
  const base = to.meta?.title || "SunhavenMod";
  document.title = base;
});

export default router;
