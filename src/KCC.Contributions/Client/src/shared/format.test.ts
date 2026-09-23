import { describe, expect, it } from "vitest";
import {
  appendPage,
  formatAverage,
  formatDateTime,
  starShapesFor,
  toRouterPath,
  totalPagesFor,
} from "./format";

describe("starShapesFor", () => {
  it("renders all empty stars when there is no average", () => {
    expect(starShapesFor(null)).toEqual(["empty", "empty", "empty", "empty", "empty"]);
    expect(starShapesFor(0)).toEqual(["empty", "empty", "empty", "empty", "empty"]);
  });

  it("rounds to the nearest half star", () => {
    expect(starShapesFor(2.25)).toEqual(["full", "full", "semi", "empty", "empty"]);
    expect(starShapesFor(4.3)).toEqual(["full", "full", "full", "full", "semi"]);
  });

  it("handles the boundaries", () => {
    expect(starShapesFor(0.5)).toEqual(["semi", "empty", "empty", "empty", "empty"]);
    expect(starShapesFor(4.75)).toEqual(["full", "full", "full", "full", "full"]);
    expect(starShapesFor(5)).toEqual(["full", "full", "full", "full", "full"]);
  });
});

describe("formatAverage", () => {
  it("formats to one decimal", () => {
    expect(formatAverage(4.25)).toBe("4.3");
    expect(formatAverage(3)).toBe("3.0");
  });

  it("returns empty for null", () => {
    expect(formatAverage(null)).toBe("");
  });
});

describe("totalPagesFor", () => {
  it("always reports at least one page", () => {
    expect(totalPagesFor(0, 20)).toBe(1);
  });

  it("rounds partial pages up", () => {
    expect(totalPagesFor(21, 20)).toBe(2);
    expect(totalPagesFor(40, 20)).toBe(2);
  });
});

describe("appendPage", () => {
  it("replaces on the first page", () => {
    expect(appendPage(["stale"], ["fresh"], 0)).toEqual(["fresh"]);
  });

  it("appends on later pages", () => {
    expect(appendPage(["first"], ["second"], 1)).toEqual(["first", "second"]);
  });
});

describe("formatDateTime", () => {
  it("returns empty for missing or invalid input", () => {
    expect(formatDateTime(null)).toBe("");
    expect(formatDateTime("not-a-date")).toBe("");
  });

  it("formats timezone-less timestamps as UTC instants", () => {
    const withMarker = formatDateTime("2026-08-12T15:30:00Z");
    const withoutMarker = formatDateTime("2026-08-12T15:30:00");
    expect(withoutMarker).toBe(withMarker);
    expect(withMarker).not.toBe("");
  });
});

describe("toRouterPath", () => {
  it("drops the administration prefix the router applies for itself", () => {
    expect(toRouterPath("/admin/webpages-1/en_1181/variant-contributions"))
      .toBe("/webpages-1/en_1181/variant-contributions");
  });

  it("leaves a path that carries no prefix alone", () => {
    expect(toRouterPath("/webpages-1/en_1181/variant-contributions"))
      .toBe("/webpages-1/en_1181/variant-contributions");
    expect(toRouterPath("/administration/overview")).toBe("/administration/overview");
  });
});
