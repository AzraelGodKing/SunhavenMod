export function formatInt(value) {
  if (value == null) return "—";
  const n = Number(value);
  if (!Number.isFinite(n)) return "—";
  return n.toLocaleString();
}

export function formatWhen(iso) {
  if (!iso) return "unknown";
  try {
    return new Date(iso).toLocaleString();
  } catch {
    return String(iso);
  }
}

export function formatTotalWithUnique(total, unique) {
  const totalText = formatInt(total);
  const uniqueText = formatInt(unique);
  if (totalText === "—" && uniqueText === "—") return "—";
  return `${totalText} (${uniqueText})`;
}

export function laneClass(lane) {
  if (!lane) return "lane-generic";
  return `lane-${String(lane).toLowerCase().replace(/[^a-z0-9]+/g, "-")}`;
}

export function prefersReducedMotion() {
  return (
    typeof window !== "undefined" &&
    typeof window.matchMedia === "function" &&
    window.matchMedia("(prefers-reduced-motion: reduce)").matches
  );
}
