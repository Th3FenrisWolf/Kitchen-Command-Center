import { css, customElement, html, state } from '@umbraco-cms/backoffice/external/lit'
import { getRecipeUnits } from '../api.js'
import { formatIngredientSummary, isIngredientValid, normalizeIngredient } from './helpers.js'
import { KccJsonArrayEditorElement } from './json-array-editor.element.js'
import type { Ingredient } from './types.js'

@customElement('kcc-ingredients-editor')
export class KccIngredientsEditorElement extends KccJsonArrayEditorElement<Ingredient> {
  protected override readonly labels = {
    add: 'Add ingredient',
    empty: 'No ingredients yet.',
    invalid: 'Every ingredient needs a name.',
    missing: 'Add at least one ingredient.',
  }

  protected override readonly summaryTag = 'ul'

  @state()
  private units: string[] = []

  override connectedCallback() {
    super.connectedCallback()
    void getRecipeUnits(this).then((units) => (this.units = units))
  }

  protected override newItem(): Ingredient {
    return { name: '', quantity: null, unit: '', isEyeballed: false }
  }

  protected override isItemValid(item: Ingredient): boolean {
    return isIngredientValid(item)
  }

  protected override prepare(items: Ingredient[]): Ingredient[] {
    return items.map(normalizeIngredient)
  }

  protected override renderSummary(item: Ingredient): string {
    return formatIngredientSummary(item)
  }

  // The unit is a plain input: a datalist must sit in the same shadow root as the input that lists it, and
  // uui-input keeps its input in a shadow root of its own.
  protected override renderFields(item: Ingredient, index: number, change: (next: Ingredient) => void) {
    return html`
      <div class="ingredient">
        <uui-input
          class="name"
          label="Ingredient ${index + 1} name"
          placeholder="Ingredient name"
          .value=${item.name}
          @input=${(event: Event) => change({ ...item, name: (event.target as HTMLInputElement).value })}></uui-input>
        <uui-input
          class="quantity"
          type="number"
          min="0"
          step="any"
          label="Ingredient ${index + 1} quantity"
          placeholder="Qty"
          ?disabled=${item.isEyeballed}
          .value=${item.quantity == null ? '' : String(item.quantity)}
          @input=${(event: Event) => {
            const raw = (event.target as HTMLInputElement).value
            change({ ...item, quantity: raw === '' ? null : Number(raw) })
          }}></uui-input>
        <input
          class="unit"
          list="units"
          aria-label="Ingredient ${index + 1} unit"
          placeholder="Unit"
          ?disabled=${item.isEyeballed}
          .value=${item.unit}
          @input=${(event: Event) => change({ ...item, unit: (event.target as HTMLInputElement).value })} />
        <uui-checkbox
          label="Eyeballed"
          ?checked=${item.isEyeballed}
          @change=${(event: Event) =>
            change(
              (event.target as HTMLInputElement).checked
                ? { ...item, isEyeballed: true, quantity: null, unit: '' }
                : { ...item, isEyeballed: false },
            )}></uui-checkbox>
      </div>
    `
  }

  override render() {
    return html`${super.render()}
      <datalist id="units">${this.units.map((unit) => html`<option value=${unit}></option>`)}</datalist>`
  }

  static override readonly styles = [
    ...KccJsonArrayEditorElement.styles,
    css`
      .ingredient {
        display: grid;
        grid-template-columns: minmax(10rem, 2fr) minmax(4rem, 0.6fr) minmax(6rem, 1fr) auto;
        gap: var(--uui-size-space-3);
        align-items: center;
      }

      .unit {
        box-sizing: border-box;
        height: var(--uui-size-11);
        padding: 0 var(--uui-size-space-3);
        border: 1px solid var(--uui-color-border);
        border-radius: var(--uui-border-radius);
        background: var(--uui-color-surface);
        color: var(--uui-color-text);
        font: inherit;
      }

      .unit:hover {
        border-color: var(--uui-color-border-emphasis);
      }

      .unit:focus {
        outline: calc(2px * var(--uui-show-focus-outline, 1)) solid var(--uui-color-focus);
      }

      .unit:disabled {
        background: var(--uui-color-disabled);
        color: var(--uui-color-disabled-contrast);
        border-color: var(--uui-color-disabled-standalone);
      }
    `,
  ]
}

export default KccIngredientsEditorElement

declare global {
  interface HTMLElementTagNameMap {
    'kcc-ingredients-editor': KccIngredientsEditorElement
  }
}
