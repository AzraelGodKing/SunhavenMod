/**
 * Cloudflare Pages Function: POST /api/feedback  |  GET /api/feedback
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
 * Logs: one JSON object per line. Never logs tokens or user-submitted text.
 */

const rateWindowMsDefault = 10 * 60 * 1000;
const rateLimitDefault = 5;
const rateBuckets = new Map();

const LOG_SERVICE = "sunhaven-website";
const LOG_COMPONENT = "api.feedback";

function truncate(str, maxLen) {
  const s = String(str ?? "");
  if (s.length <= maxLen) return s;
  return `${s.slice(0, maxLen)}…`;
}

function feedbackLog(level, fields) {
  const line = JSON.stringify({
    ts: new Date().toISOString(),
    level,
    service: LOG_SERVICE,
    component: LOG_COMPONENT,
    ...fields,
  });
  if (level === "error") console.error(line);
  else if (level === "warn") console.warn(line);
  else console.log(line);
}

function summarizeGraphQLErrors(errors) {
  if (!Array.isArray(errors)) return null;
  return errors.map((e) => ({
    message: typeof e?.message === "string" ? truncate(e.message, 500) : null,
    code: e?.extensions?.code ?? e?.extensions?.type ?? null,
    path: e?.path ?? null,
  }));
}

function sanitizeText(value, maxLen) {
  const text = String(value ?? "")
    .replace(/[\u0000-\u0008\u000B\u000C\u000E-\u001F\u007F]/g, "")
    .trim();
  return text.slice(0, maxLen);
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

function jsonResponse(body, status = 200) {
  return new Response(JSON.stringify(body), {
    status,
    headers: { "content-type": "application/json; charset=utf-8" },
  });
}

function bad(message, status = 400, detail = null) {
  const body = { ok: false, error: message };
  if (detail) body.detail = detail;
  return jsonResponse(body, status);
}

function throwLinearUpstream(detail = {}) {
  const err = new Error("Failed to create Linear issue");
  err.name = "FeedbackLinearUpstreamError";
  err.detail = detail;
  throw err;
}

function requestDiag(request, env, extra = {}) {
  return {
    clientIp: getClientIp(request),
    cfRay: request.headers.get("cf-ray") || null,
    linearTokenConfigured: Boolean(env.LINEAR_API_TOKEN),
    linearTeamIdConfigured: Boolean(env.LINEAR_TEAM_ID),
    ...extra,
  };
}

export async function onRequestGet(context) {
  const { request, env } = context;
  const diag = requestDiag(request, env);
  feedbackLog("info", { event: "feedback.health_check", ...diag });
  return jsonResponse({
    ok: true,
    configured: Boolean(env.LINEAR_API_TOKEN && env.LINEAR_TEAM_ID),
    linearTokenConfigured: Boolean(env.LINEAR_API_TOKEN),
    linearTeamIdConfigured: Boolean(env.LINEAR_TEAM_ID),
  });
}

export async function onRequestPost(context) {
  const { request, env } = context;

  // 1) Config check
  if (!env.LINEAR_API_TOKEN || !env.LINEAR_TEAM_ID) {
    feedbackLog("warn", {
      event: "feedback.config_missing",
      missing: [
        !env.LINEAR_API_TOKEN ? "LINEAR_API_TOKEN" : null,
        !env.LINEAR_TEAM_ID ? "LINEAR_TEAM_ID" : null,
      ].filter(Boolean),
      ...requestDiag(request, env),
    });
    return bad(
      "Server is not configured for feedback submission. The admin needs to set LINEAR_API_TOKEN and LINEAR_TEAM_ID.",
      500,
      { reason: "missing_env" }
    );
  }

  // 2) Parse body
  let body;
  try {
    body = await request.json();
  } catch {
    feedbackLog("warn", {
      event: "feedback.request_invalid_json",
      ...requestDiag(request, env),
    });
    return bad("Invalid JSON body.");
  }

  // 3) Honeypot
  const honeypot = sanitizeText(body?.website, 120);
  if (honeypot) {
    feedbackLog("warn", {
      event: "feedback.spam_honeypot",
      ...requestDiag(request, env),
    });
    return bad("Spam detected.");
  }

  // 4) Type validation
  const type = sanitizeText(body?.type, 12).toLowerCase();
  if (type !== "bug" && type !== "feature") {
    feedbackLog("warn", {
      event: "feedback.invalid_type",
      submittedType: truncate(type, 32) || null,
      ...requestDiag(request, env),
    });
    return bad("Invalid feedback type. Must be 'bug' or 'feature'.");
  }

  // 5) Rate limit
  const windowSec = Number(env.FEEDBACK_RATE_WINDOW_SECONDS || 600);
  const maxPerWindow = Number(env.FEEDBACK_RATE_MAX || rateLimitDefault);
  const windowMs = Number.isFinite(windowSec) ? Math.max(30, windowSec) * 1000 : rateWindowMsDefault;
  const allowed = checkRateLimit(getClientIp(request), windowMs, maxPerWindow);
  if (!allowed) {
    feedbackLog("warn", {
      event: "feedback.rate_limited",
      ...requestDiag(request, env, { feedbackType: type }),
    });
    return bad("Too many submissions. Please try again later.", 429);
  }

  // 6) Field validation
  const name = sanitizeText(body?.name, 120);
  const title = sanitizeText(body?.title, 160);
  const description = sanitizeText(body?.description, 4000);
  const mod = sanitizeText(body?.mod, 120);
  const priority = sanitizeText(body?.priority, 12).toLowerCase();

  if (!name || !title || !description) {
    feedbackLog("warn", {
      event: "feedback.validation_missing_fields",
      hasName: Boolean(name),
      hasTitle: Boolean(title),
      hasDescription: Boolean(description),
      ...requestDiag(request, env, { feedbackType: type }),
    });
    return bad("Missing required fields: name, title, and description are required.");
  }

  // 7) Build Linear payload
  const issueTitle = `[${type === "bug" ? "Bug" : "Feature"}] ${title}`;
  const bugLabelId = sanitizeText(env.LINEAR_BUG_LABEL_ID, 120);
  const featureLabelId = sanitizeText(env.LINEAR_FEATURE_LABEL_ID, 120);
  const labelId = type === "bug" ? bugLabelId : featureLabelId;
  const labelIds = labelId ? [labelId] : [];

  const issueDescription =
    `Submitted from website ticket desk.\n` +
    asList("Type", type) +
    (mod ? asList("Related Mod", mod) : "") +
    (priority ? asList("Priority", priority) : "") +
    asList("Name", name) +
    asList("Description", description);

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

  const linearInput = {
    teamId: env.LINEAR_TEAM_ID,
    title: issueTitle,
    description: issueDescription,
  };
  if (labelIds.length) {
    linearInput.labelIds = labelIds;
  }

  // 8) Call Linear
  let issueCreate;
  try {
    const linearResp = await fetch("https://api.linear.app/graphql", {
      method: "POST",
      headers: {
        Authorization: `Bearer ${env.LINEAR_API_TOKEN}`,
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        query: mutation,
        variables: { input: linearInput },
      }),
    });

    const httpStatus = linearResp.status;
    const rawText = await linearResp.text();

    let parsed = null;
    let parseError = null;
    if (rawText) {
      try {
        parsed = JSON.parse(rawText);
      } catch (e) {
        parseError = e instanceof Error ? e.message : String(e);
      }
    }

    if (!linearResp.ok) {
      throwLinearUpstream({
        failureReason: "linear_http_not_ok",
        linearHttpStatus: httpStatus,
        linearBodyPreview: truncate(rawText, 1200),
      });
    }
    if (parseError !== null || parsed === null) {
      if (!rawText) {
        throwLinearUpstream({
          failureReason: "linear_empty_body",
          linearHttpStatus: httpStatus,
        });
      }
      throwLinearUpstream({
        failureReason: "linear_body_not_json",
        linearHttpStatus: httpStatus,
        jsonParseError: parseError,
        linearBodyPreview: truncate(rawText, 800),
      });
    }
    if (Array.isArray(parsed.errors) && parsed.errors.length) {
      throwLinearUpstream({
        failureReason: "linear_graphql_errors",
        linearHttpStatus: httpStatus,
        graphqlErrors: summarizeGraphQLErrors(parsed.errors),
      });
    }

    issueCreate = parsed?.data?.issueCreate;
    if (!issueCreate?.success) {
      throwLinearUpstream({
        failureReason: "issue_create_not_successful",
        linearHttpStatus: httpStatus,
        issueCreateSuccess: issueCreate?.success ?? null,
        issueCreateHasIssue: Boolean(issueCreate?.issue),
        graphqlErrors: summarizeGraphQLErrors(parsed.errors),
      });
    }
  } catch (err) {
    if (err && err.name === "FeedbackLinearUpstreamError") {
      const detail = err.detail && typeof err.detail === "object" ? err.detail : {};
      feedbackLog("error", {
        event: "feedback.linear_failed",
        httpStatusReturned: 502,
        ...requestDiag(request, env, {
          feedbackType: type,
          labelIdsCount: labelIds.length,
        }),
        ...detail,
      });
      return bad(
        "Linear returned an error. This usually means the API token is invalid, the team ID is wrong, or the token lacks issue-create permissions.",
        502,
        { linearReason: detail.failureReason || "unknown" }
      );
    }
    feedbackLog("error", {
      event: "feedback.linear_failed",
      failureReason: "linear_fetch_exception",
      httpStatusReturned: 502,
      exceptionName: err?.name ?? null,
      exceptionMessage: err instanceof Error ? truncate(err.message, 500) : truncate(String(err), 500),
      ...requestDiag(request, env, {
        feedbackType: type,
        labelIdsCount: labelIds.length,
      }),
    });
    return bad(
      "Could not reach Linear. This may be a temporary network issue. Please try again in a moment.",
      502
    );
  }

  feedbackLog("info", {
    event: "feedback.linear_issue_created",
    issueIdentifier: issueCreate.issue?.identifier ?? null,
    issueId: issueCreate.issue?.id ?? null,
    ...requestDiag(request, env, {
      feedbackType: type,
      labelIdsCount: labelIds.length,
    }),
  });

  return jsonResponse({
    ok: true,
    id: issueCreate.issue?.id || null,
    identifier: issueCreate.issue?.identifier || null,
    url: issueCreate.issue?.url || null,
  });
}
