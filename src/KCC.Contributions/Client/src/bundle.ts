export const manifests: Array<UmbExtensionManifest> = [
  {
    type: 'dashboard',
    alias: 'KCC.Dashboard.Contributions',
    name: 'KCC Contributions Dashboard',
    element: () => import('./contributions-dashboard.element.js'),
    // Above Umbraco's own content dashboards, so the Content section opens on this one.
    weight: 100,
    meta: {
      label: 'Contributions',
      pathname: 'contributions',
    },
    conditions: [
      { alias: 'Umb.Condition.SectionAlias', match: 'Umb.Section.Content' },
      { alias: 'Umb.Condition.CurrentUser.IsAdmin' },
    ],
  },
]
