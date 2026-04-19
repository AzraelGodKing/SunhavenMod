// Load this script with `defer` so rendering is never blocked.
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

  fetch("/data/stats-cache.json", { cache: "no-cache" })
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
