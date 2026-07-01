import { describe, it, expect } from "vitest";
import { gzipSync } from "node:zlib";
import { extractTunitData } from "../../../scripts/extract.mjs";

function report(data: unknown, compressed = true): string {
  const json = JSON.stringify(data);
  const body = compressed
    ? `<script id="test-data" type="text/plain" data-compressed="gzip">${gzipSync(Buffer.from(json, "utf8")).toString("base64")}</script>`
    : `<script id="test-data" type="text/plain">${json}</script>`;
  return `<!DOCTYPE html><html><body>${body}</body></html>`;
}

describe("extractTunitData", () => {
  it("extracts gzip + base64 embedded data", () => {
    const data = { assemblyName: "KCC.UnitTests", summary: { total: 3 }, groups: [] };
    expect(extractTunitData(report(data))).toEqual(data);
  });

  it("extracts uncompressed embedded JSON", () => {
    const data = { assemblyName: "X" };
    expect(extractTunitData(report(data, false))).toEqual(data);
  });

  it("returns null when the script tag is absent", () => {
    expect(extractTunitData("<html><body>nope</body></html>")).toBeNull();
  });

  it("returns null on a corrupt payload", () => {
    expect(
      extractTunitData(`<script id="test-data" data-compressed="gzip">not-base64!!!</script>`),
    ).toBeNull();
  });
});
