/* ===================================================================
   Hub ticket desk — POST /api/feedback (Worker)
   API origin is public, not a secret. Override with:
     - html[data-feedback-api-origin]
     - ?api=https://your-worker.example
     - localStorage sunhavenmod-feedback-api-origin
   =================================================================== */

(function () {
    var NAME_MAX = 120;
    var TITLE_MAX = 160;
    var DESC_MAX = 4000;
    var TYPE_MAX = 12;
    var MOD_MAX = 120;
    var HONEYPOT_MAX = 120;

    function sanitizeText(value, maxLen) {
        var text = String(value == null ? "" : value)
            .replace(/[\u0000-\u0008\u000B\u000C\u000E-\u001F\u007F]/g, "")
            .trim();
        return text.slice(0, maxLen);
    }

    function resolveApiOrigin() {
        var params = new URLSearchParams(window.location.search);
        var fromQuery = params.get("api");
        if (fromQuery) return String(fromQuery).replace(/\/$/, "");
        try {
            var stored = localStorage.getItem("sunhavenmod-feedback-api-origin");
            if (stored) return String(stored).replace(/\/$/, "");
        } catch (err) {
            /* ignore */
        }
        var attr = document.documentElement.getAttribute("data-feedback-api-origin") || "";
        return attr.replace(/\/$/, "");
    }

    function setNotice(el, message, status) {
        if (!el) return;
        el.textContent = message || "";
        if (status) el.setAttribute("data-status", status);
        else el.removeAttribute("data-status");
    }

    function populateMods(select, versions, preselect) {
        if (!select) return;
        var names = Object.keys(versions || {})
            .map(function (key) { return versions[key] && versions[key].name; })
            .filter(Boolean)
            .sort(function (a, b) { return a.localeCompare(b); });
        names.forEach(function (name) {
            var opt = document.createElement("option");
            opt.value = name;
            opt.textContent = name;
            if (preselect && preselect === name) opt.selected = true;
            select.appendChild(opt);
        });
    }

    document.addEventListener("DOMContentLoaded", function () {
        var form = document.getElementById("feedbackForm");
        if (!form) return;

        var typeEl = form.querySelector('[name="type"]');
        var nameEl = form.querySelector('[name="name"]');
        var titleEl = form.querySelector('[name="title"]');
        var descEl = form.querySelector('[name="description"]');
        var modEl = form.querySelector('[name="mod"]');
        var websiteEl = form.querySelector('[name="website"]');
        var submitEl = form.querySelector('[type="submit"]');
        var noticeEl = document.getElementById("feedbackNotice");
        var preselect = new URLSearchParams(window.location.search).get("mod") || "";

        fetch("versions.json")
            .then(function (res) { return res.ok ? res.json() : {}; })
            .then(function (versions) { populateMods(modEl, versions, preselect); })
            .catch(function () { /* dropdown stays at Any / not sure */ });

        form.addEventListener("submit", function (event) {
            event.preventDefault();
            setNotice(noticeEl, "", "");

            var honeypot = sanitizeText(websiteEl && websiteEl.value, HONEYPOT_MAX);
            if (honeypot) {
                setNotice(noticeEl, "Spam detected.", "error");
                return;
            }

            var type = sanitizeText(typeEl && typeEl.value, TYPE_MAX).toLowerCase();
            if (type !== "bug" && type !== "feature") {
                setNotice(noticeEl, "Invalid feedback type. Must be 'bug' or 'feature'.", "error");
                return;
            }

            var name = sanitizeText(nameEl && nameEl.value, NAME_MAX);
            var title = sanitizeText(titleEl && titleEl.value, TITLE_MAX);
            var description = sanitizeText(descEl && descEl.value, DESC_MAX);
            var mod = sanitizeText(modEl && modEl.value, MOD_MAX);

            if (!name || !title || !description) {
                setNotice(noticeEl, "Missing required fields: name, title, and description are required.", "error");
                return;
            }

            var origin = resolveApiOrigin();
            if (!origin) {
                setNotice(noticeEl, "Feedback API origin is not configured on this page.", "error");
                return;
            }

            var payload = {
                type: type,
                name: name,
                title: title,
                description: description,
                website: "",
            };
            if (mod) payload.mod = mod;

            if (submitEl) submitEl.disabled = true;
            setNotice(noticeEl, "Submitting…", "");

            fetch(origin + "/api/feedback", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(payload),
            })
                .then(function (res) {
                    return res.json().catch(function () { return {}; }).then(function (body) {
                        return { res: res, body: body };
                    });
                })
                .then(function (result) {
                    if (!result.res.ok) {
                        throw new Error(result.body && result.body.error ? result.body.error : "Request failed (" + result.res.status + ")");
                    }
                    form.reset();
                    if (typeEl) typeEl.value = "bug";
                    setNotice(noticeEl, "Thanks! Your feedback was submitted successfully.", "success");
                })
                .catch(function (err) {
                    setNotice(
                        noticeEl,
                        err instanceof Error ? err.message : "Could not submit feedback right now.",
                        "error"
                    );
                })
                .then(function () {
                    if (submitEl) submitEl.disabled = false;
                });
        });
    });
})();
