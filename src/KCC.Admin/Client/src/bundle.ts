export const manifests: Array<UmbExtensionManifest> = [
  {
    type: 'propertyEditorUi',
    alias: 'KCC.PropertyEditorUi.Ingredients',
    name: 'KCC Ingredients Property Editor UI',
    element: () => import('./json-array/ingredients-editor.element.js'),
    meta: {
      label: 'KCC Ingredients',
      icon: 'icon-bulleted-list',
      group: 'Recipes',
      propertyEditorSchemaAlias: 'Umbraco.TextArea',
      supportsReadOnly: true,
    },
  },
  {
    type: 'propertyEditorUi',
    alias: 'KCC.PropertyEditorUi.Instructions',
    name: 'KCC Instructions Property Editor UI',
    element: () => import('./json-array/instructions-editor.element.js'),
    meta: {
      label: 'KCC Instructions',
      icon: 'icon-ordered-list',
      group: 'Recipes',
      propertyEditorSchemaAlias: 'Umbraco.TextArea',
      supportsReadOnly: true,
    },
  },
  {
    type: 'propertyEditorUi',
    alias: 'KCC.PropertyEditorUi.RecipeIcon',
    name: 'KCC Recipe Icon Property Editor UI',
    element: () => import('./recipe-icon/recipe-icon-editor.element.js'),
    meta: {
      label: 'KCC Recipe Icon',
      icon: 'icon-picture',
      group: 'Recipes',
      propertyEditorSchemaAlias: 'Umbraco.TextBox',
      supportsReadOnly: true,
    },
  },
  {
    type: 'modal',
    alias: 'KCC.Modal.RecipeIconPicker',
    name: 'KCC Recipe Icon Picker Modal',
    element: () => import('./recipe-icon/icon-picker-modal.element.js'),
  },
]
