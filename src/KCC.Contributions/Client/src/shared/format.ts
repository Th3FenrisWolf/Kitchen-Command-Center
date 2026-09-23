export type StarShape = "full" | "semi" | "empty";

export const starShapesFor = (average: number | null): StarShape[] => {
  if (average === null || average <= 0) {
    return Array<StarShape>(5).fill("empty");
  }

  const rounded = Math.min(5, Math.max(0, Math.round(average * 2) / 2));
  const full = Math.floor(rounded);
  const hasSemi = rounded - full === 0.5;

  return Array.from({ length: 5 }, (_, index) =>
    index < full ? "full" : index === full && hasSemi ? "semi" : "empty");
};

export const formatAverage = (average: number | null): string =>
  average === null ? "" : (Math.round(average * 10) / 10).toFixed(1);

export const totalPagesFor = (totalCount: number, pageSize: number): number =>
  Math.max(1, Math.ceil(Math.max(0, totalCount) / Math.max(1, pageSize)));

export function appendPage<T>(existing: T[], incoming: T[], page: number): T[] {
  return page === 0 ? incoming : [...existing, ...incoming];
}

export const formatDateTime = (iso: string | null): string => {
  if (!iso) {
    return "";
  }

  const normalized = /Z$|[+-]\d\d:\d\d$/.test(iso) ? iso : `${iso}Z`;
  const date = new Date(normalized);
  if (Number.isNaN(date.getTime())) {
    return "";
  }

  return new Intl.DateTimeFormat(undefined, { dateStyle: "medium", timeStyle: "short" }).format(date);
};

const adminPrefix = "/admin";

/**
 * A table row's link is rendered as a react-router NavLink, and `to` resolves against the router's
 * /admin basename — so an administration URL used there has the prefix applied twice. Plain anchors
 * elsewhere need it, which is why the server sends the full URL and only this one call site trims it.
 */
export const toRouterPath = (adminUrl: string): string =>
  adminUrl.startsWith(`${adminPrefix}/`) ? adminUrl.slice(adminPrefix.length) : adminUrl;
