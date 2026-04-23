/**
 * Cloudflare Pages Function: POST /api/feedback
 *
 * Required env vars (Pages project):
 * - LINEAR_API_TOKEN
 * - LINEAR_TEAM_ID
 *
 * Optional:
 * - FEEDBACK_RATE_WINDOW_SECONDS (default 600)
 * - FEEDBACK_RATE_MAX (default 5)
 * - LINEAR_BUG_LABEL_ID
 * - LINEAR_FEATURE_LABEL_ID
 *
 * Note:
 * - CLOUDFLARE_API_TOKEN is for deployment/auth tooling and is not used by
 *   this runtime feedback endpoint.
 */

const rateWindowMsDefault = 10 * 60 * 1000;
const rateLimitDefault = 5;
const rateBuckets = new Map();

function sanitizeText(value, maxLen) {
  const text = String(value ?? "")
    .replace(/[\u0000-\u0008\u000B\u000C\u000E-\u001F\u007F]/g, "")
    .trim();
  return text.slice(0, maxLen);
}

function isEmail(value) {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value);
}

function asList(label, value) {
  if (!value) return "";
  return `\n**${label}**\n${value}\n`;
}

function getClientIp(request) {
  return (
    request.headers.get("CF-Connecting-IP") ||
    request.headers.get("x-forwarded-for")?.split(",")[0]?.trim() ||
    "unknown"
  );
}

function checkRateLimit(ip, windowMs, maxPerWindow) {
  const now = Date.now();
  const prev = rateBuckets.get(ip) || [];
  const recent = prev.filter((ts) => now - ts < windowMs);
  if (recent.length >= maxPerWindow) return false;
  recent.push(now);
  rateBuckets.set(ip, recent);
  return true;
}

function bad(message, status = 400) {
  return new Response(JSON.stringify({ ok: false, error: message }), {
    status,
    headers: { "content-type": "application/json; charset=utf-8" },
  });
}

export async function onRequestPost(context) {
  const { request, env } = context;
  if (!env.LINEAR_API_TOKEN || !env.LINEAR_TEAM_ID) {
    return bad("Server is not configured for feedback submission.", 500);
  }

  let body;
  try {
    body = await request.json();
  } catch {
    return bad("Invalid JSON body.");
  }

  const type = sanitizeText(body?.type, 12).toLowerCase();
  if (type !== "bug" && type !== "feature") {
    return bad("Invalid feedback type.");
  }

  // Honeypot field must stay empty.
  const honeypot = sanitizeText(body?.website, 120);
  if (honeypot) {
    return bad("Spam detected.");
  }

  const windowSec = Number(env.FEEDBACK_RATE_WINDOW_SECONDS || 600);
  const maxPerWindow = Number(env.FEEDBACK_RATE_MAX || rateLimitDefault);
  const windowMs = Number.isFinite(windowSec) ? Math.max(30, windowSec) * 1000 : rateWindowMsDefault;
  const allowed = checkRateLimit(getClientIp(request), windowMs, maxPerWindow);
  if (!allowed) {
    return bad("Too many submissions. Please try again later.", 429);
  }

  const name = sanitizeText(body?.name, 120);
  const email = sanitizeText(body?.email, 160);
  const title = sanitizeText(body?.title, 160);
  const description = sanitizeText(body?.description, 4000);
  const pageUrl = sanitizeText(body?.pageUrl, 1000);
  const steps = sanitizeText(body?.steps, 4000);
  const expected = sanitizeText(body?.expected, 3000);
  const actual = sanitizeText(body?.actual, 3000);
  const problem = sanitizeText(body?.problem, 3000);
  const outcome = sanitizeText(body?.outcome, 3000);

  if (!name || !email || !title || !description) {
    return bad("Missing required fields.");
  }
  if (!isEmail(email)) {
    return bad("Email is invalid.");
  }

  const issueTitle = `[${type === "bug" ? "Bug" : "Feature"}] ${title}`;
  const bugLabelId = sanitizeText(env.LINEAR_BUG_LABEL_ID, 120);
  const featureLabelId = sanitizeText(env.LINEAR_FEATURE_LABEL_ID, 120);
  const labelId =
    type === "bug" ? bugLabelId || null : featureLabelId || null;
  const labelIds = labelId ? [labelId] : [];
  const issueDescription =
    `Submitted from website feedback form.\n` +
    asList("Type", type) +
    asList("Name", name) +
    asList("Email", email) +
    asList("Page URL", pageUrl || "not provided") +
    asList("Description", description) +
    (type === "bug"
      ? `${asList("Steps to reproduce", steps)}${asList("Expected behavior", expected)}${asList("Actual behavior", actual)}`
      : `${asList("Problem to solve", problem)}${asList("Desired outcome", outcome)}`);

  const mutation = `
    mutation CreateIssue($input: IssueCreateInput!) {
      issueCreate(input: $input) {
        success
        issue {
          id
          identifier
          url
        }
      }
    }
  `;

  let linearJson;
  try {
    const linearResp = await fetch("https://api.linear.app/graphql", {
      method: "POST",
      headers: {
        Authorization: `Bearer ${env.LINEAR_API_TOKEN}`,
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        query: mutation,
        variables: {
          input: {
            teamId: env.LINEAR_TEAM_ID,
            title: issueTitle,
            description: issueDescription,
            ...(labelIds.length ? { labelIds } : {}),
          },
        },
      }),
    });

    const rawText = await linearResp.text();
    try {
      linearJson = rawText ? JSON.parse(rawText) : {};
    } catch {
      linearJson = { raw: rawText || null };
    }

    if (!linearResp.ok || linearJson?.errors?.length) {
      console.error("Linear API error:", linearJson);
      throw new Error("Failed to create Linear issue");
    }
  } catch (err) {
    const msg = err instanceof Error ? err.message : "Could not create issue in Linear.";
    return bad(msg, 502);
  }

  const issueCreate = linearJson?.data?.issueCreate;
  if (!issueCreate?.success) {
    return bad("Failed to create Linear issue", 502);
  }

  return new Response(
    JSON.stringify({
      ok: true,
      id: issueCreate.issue?.id || null,
      identifier: issueCreate.issue?.identifier || null,
      url: issueCreate.issue?.url || null,
    }),
    {
      status: 200,
      headers: { "content-type": "application/json; charset=utf-8" },
    }
  );
}
