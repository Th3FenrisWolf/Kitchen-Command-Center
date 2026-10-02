import { css, customElement, html, repeat, state } from '@umbraco-cms/backoffice/external/lit'
import { UmbModalBaseElement } from '@umbraco-cms/backoffice/modal'
import { UmbTextStyles } from '@umbraco-cms/backoffice/style'
import { getRecipeIcons } from '../api.js'
import { fontAwesomeStyles, registerFontAwesome } from './font-awesome.js'
import type { KccIconPickerModalValue } from './icon-picker-modal.token.js'
import { filterIcons, iconLabel } from './icons.js'

@customElement('kcc-icon-picker-modal')
export class KccIconPickerModalElement extends UmbModalBaseElement<object, KccIconPickerModalValue> {
  @state()
  private icons: string[] = []

  @state()
  private search = ''

  override connectedCallback() {
    super.connectedCallback()
    registerFontAwesome()
    void getRecipeIcons(this).then((icons) => (this.icons = icons))
  }

  override render() {
    return html`
      <umb-body-layout headline="Select an icon">
        <uui-box>
          <uui-input
            type="search"
            label="Search icons"
            placeholder="Search icons"
            .value=${this.search}
            @input=${(event: Event) => (this.search = (event.target as HTMLInputElement).value)}></uui-input>
          <div class="grid">
            ${repeat(
              filterIcons(this.icons, this.search),
              (icon) => icon,
              (icon) => html`
                <button
                  type="button"
                  class=${icon === this.value?.icon ? 'icon selected' : 'icon'}
                  title=${icon}
                  aria-pressed=${icon === this.value?.icon ? 'true' : 'false'}
                  @click=${() => this.updateValue({ icon })}>
                  <i class=${icon}></i>
                  <span>${iconLabel(icon)}</span>
                </button>
              `,
            )}
          </div>
        </uui-box>
        <uui-button slot="actions" label="Cancel" @click=${() => this._rejectModal()}></uui-button>
        <uui-button
          slot="actions"
          look="primary"
          color="positive"
          label="Select icon"
          ?disabled=${!this.value?.icon}
          @click=${() => this._submitModal()}></uui-button>
      </umb-body-layout>
    `
  }

  static override readonly styles = [
    UmbTextStyles,
    fontAwesomeStyles,
    css`
      uui-input {
        width: 100%;
        margin-bottom: var(--uui-size-space-4);
      }

      .grid {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(6rem, 1fr));
        gap: var(--uui-size-space-3);
      }

      .icon {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        gap: var(--uui-size-space-2);
        min-height: 6rem;
        padding: var(--uui-size-space-3);
        border: 1px solid var(--uui-color-border);
        border-radius: var(--uui-border-radius);
        background: var(--uui-color-surface);
        color: var(--uui-color-text);
        font: inherit;
        cursor: pointer;
      }

      .icon:hover {
        background: var(--uui-color-surface-emphasis);
      }

      .icon:focus-visible {
        outline: 2px solid var(--uui-color-focus);
      }

      .icon.selected {
        border-color: var(--uui-color-selected);
        box-shadow: inset 0 0 0 1px var(--uui-color-selected);
      }

      .icon i {
        font-size: 1.75rem;
      }

      .icon span {
        font-size: var(--uui-type-small-size);
        text-align: center;
        word-break: break-word;
      }
    `,
  ]
}

export default KccIconPickerModalElement
