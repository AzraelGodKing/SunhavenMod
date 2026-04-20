// Load this script with `defer` so rendering is never blocked.
// Cache file: docs/data/stats-cache.json → on GitHub Pages (publish /docs) public URL is /data/stats-cache.json at site root (no "/docs" segment).

function resolveStatsCacheUrl() {
  const scripts = document.getElementsByTagName("script");
  for (let i = scripts.length - 1; i >= 0; i--) {
    const s = scripts[i];
    if (!s.src) continue;
    const u = new URL(s.src, window.location.href);
    if (!/\/scripts\/stats-display\.js(\?|$)/.test(u.pathname)) continue;
    u.pathname = u.pathname.replace(/\/scripts\/stats-display\.js$/, "/data/stats-cache.json");
    return u.href;
  }

  // Local / preview: URL path still contains "/docs/" (e.g. Live Server opened from repo root)
  const m = window.location.pathname.match(/^(.*\/docs\/)/);
  if (m) {
    return new URL("data/stats-cache.json", window.location.origin + m[1]).href;
  }

  return new URL("data/stats-cache.json", window.location.href).href;
}

document.addEventListener("DOMContentLoaded", () => {
  const formatValue = (value) => {
    if (value == null) return "—";
    const num = Number(value);
    if (!Number.isFinite(num)) return "—";
    return num.toLocaleString();
  };

  const setText = (el, value) => {
    el.textContent = formatValue(value);
  };

  fetch(resolveStatsCacheUrl(), { cache: "no-cache" })
    .then((res) => {
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      return res.json();
    })
    .then((stats) => {
      const siteTotal = stats?.site_total || {};
      const mods = stats?.mods || {};

      document.querySelectorAll("[data-stats='total']").forEach((el) => {
        const field = el.getAttribute("data-field");
        switch (field) {
          case "grand_total":
            setText(el, siteTotal.grand_total);
            break;
          case "thunderstore_site_total":
            setText(el, siteTotal.thunderstore);
            break;
          case "nexus_site_total":
            setText(el, siteTotal.nexus_total);
            break;
          default:
            setText(el, null);
            break;
        }
      });

      document.querySelectorAll("[data-stats-mod]").forEach((el) => {
        const modId = el.getAttribute("data-stats-mod");
        const field = el.getAttribute("data-field");
        const mod = mods[modId] || {};
        const thunderstore = mod.thunderstore || {};
        const nexus = mod.nexus || {};

        switch (field) {
          case "combined_total":
            setText(el, mod.combined_total);
            break;
          case "thunderstore_downloads":
            setText(el, thunderstore.total_downloads);
            break;
          case "nexus_downloads":
            setText(el, nexus.total_downloads);
            break;
          case "nexus_unique":
            setText(el, nexus.unique_downloads);
            break;
          default:
            setText(el, null);
            break;
        }
      });
    })
    .catch(() => {
      // Enhancement-only: leave placeholder "—" values in place if fetch fails.
    });
});
