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
 *
 * Logs: one JSON object per line (search in Workers Logs for `event` values
 * like `feedback.linear_failed`). Never logs tokens or user-submitted text.
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

/**
 * @param {"debug"|"info"|"warn"|"error"} level
 * @param {Record<string, unknown>} fields
 */
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

function bad(message, status = 400) {
  return new Response(JSON.stringify({ ok: false, error: message }), {
    status,
    headers: { "content-type": "application/json; charset=utf-8" },
  });
}

/** @param {Record<string, unknown>} [detail] */
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

export async function onRequestPost(context) {
  const { request, env } = context;
  if (!env.LINEAR_API_TOKEN || !env.LINEAR_TEAM_ID) {
    feedbackLog("warn", {
      event: "feedback.config_missing",
      missing: [
        !env.LINEAR_API_TOKEN ? "LINEAR_API_TOKEN" : null,
        !env.LINEAR_TEAM_ID ? "LINEAR_TEAM_ID" : null,
      ].filter(Boolean),
      ...requestDiag(request, env),
    });
    return bad("Server is not configured for feedback submission.", 500);
  }

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

  // Honeypot field must stay empty.
  const honeypot = sanitizeText(body?.website, 120);
  if (honeypot) {
    feedbackLog("warn", {
      event: "feedback.spam_honeypot",
      ...requestDiag(request, env),
    });
    return bad("Spam detected.");
  }

  const type = sanitizeText(body?.type, 12).toLowerCase();
  if (type !== "bug" && type !== "feature") {
    feedbackLog("warn", {
      event: "feedback.invalid_type",
      submittedType: truncate(type, 32) || null,
      ...requestDiag(request, env),
    });
    return bad("Invalid feedback type.");
  }

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

  const name = sanitizeText(body?.name, 120);
  const title = sanitizeText(body?.title, 160);
  const description = sanitizeText(body?.description, 4000);

  if (!name || !title || !description) {
    feedbackLog("warn", {
      event: "feedback.validation_missing_fields",
      hasName: Boolean(name),
      hasTitle: Boolean(title),
      hasDescription: Boolean(description),
      ...requestDiag(request, env, { feedbackType: type }),
    });
    return bad("Missing required fields.");
  }

  const issueTitle = `[${type === "bug" ? "Bug" : "Feature"}] ${title}`;
  const bugLabelId = sanitizeText(env.LINEAR_BUG_LABEL_ID, 120);
  const featureLabelId = sanitizeText(env.LINEAR_FEATURE_LABEL_ID, 120);
  const labelId = type === "bug" ? bugLabelId : featureLabelId;
  const labelIds = labelId ? [labelId] : [];
  const issueDescription =
    `Submitted from website feedback form.\n` +
    asList("Type", type) +
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
      feedbackLog("error", {
        event: "feedback.linear_failed",
        httpStatusReturned: 502,
        ...requestDiag(request, env, {
          feedbackType: type,
          labelIdsCount: labelIds.length,
        }),
        ...(err.detail && typeof err.detail === "object" ? err.detail : {}),
      });
      return bad("Failed to create Linear issue", 502);
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
    return bad("Failed to create Linear issue", 502);
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
