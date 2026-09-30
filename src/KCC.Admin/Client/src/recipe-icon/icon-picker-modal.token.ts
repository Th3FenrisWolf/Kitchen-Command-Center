import { UmbModalToken } from '@umbraco-cms/backoffice/modal'

export interface KccIconPickerModalValue {
  icon: string
}

export const KCC_ICON_PICKER_MODAL = new UmbModalToken<object, KccIconPickerModalValue>('KCC.Modal.RecipeIconPicker', {
  modal: { type: 'sidebar', size: 'medium' },
})
