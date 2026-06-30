// Pulls the structured run data TUnit's HTML reporter embeds as
// <script id="test-data" data-compressed="gzip">base64</script>.
import { gunzipSync, inflateSync } from "node:zlib";

// Returns the parsed test-data object, or null if absent/unreadable.
export function extractTunitData(html) {
  const match = html.match(/<script id="test-data"([^>]*)>([\s\S]*?)<\/script>/);
  if (!match) return null;
  const compression = (match[1].match(/data-compressed="([^"]*)"/) || [])[1];
  const payload = match[2].trim();
  try {
    if (compression) {
      const buf = Buffer.from(payload, "base64");
      const text =
        compression === "gzip" ? gunzipSync(buf).toString("utf8")
        : compression === "deflate" ? inflateSync(buf).toString("utf8")
        : null;
      return text == null ? null : JSON.parse(text);
    }
    return JSON.parse(payload);
  } catch {
    return null;
  }
}
