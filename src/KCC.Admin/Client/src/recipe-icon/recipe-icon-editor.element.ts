import { css, customElement, html, nothing, property, state } from '@umbraco-cms/backoffice/external/lit'
import { UmbChangeEvent } from '@umbraco-cms/backoffice/event'
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element'
import { umbOpenModal } from '@umbraco-cms/backoffice/modal'
import { UMB_PROPERTY_DATASET_CONTEXT } from '@umbraco-cms/backoffice/property'
import type { UmbPropertyEditorUiElement } from '@umbraco-cms/backoffice/property-editor'
import { UmbTextStyles } from '@umbraco-cms/backoffice/style'
import { UmbFormControlMixin } from '@umbraco-cms/backoffice/validation'
import { suggestRecipeIcon } from '../api.js'
import { fontAwesomeStyles, registerFontAwesome } from './font-awesome.js'
import { KCC_ICON_PICKER_MODAL } from './icon-picker-modal.token.js'
import { iconLabel } from './icons.js'

@customElement('kcc-recipe-icon-editor')
export class KccRecipeIconEditorElement
  extends UmbFormControlMixin<string | undefined, typeof UmbLitElement, undefined>(UmbLitElement)
  implements UmbPropertyEditorUiElement
{
  @property({ type: Boolean, reflect: true })
  readonly = false

  @property({ type: Boolean })
  mandatory = false

  @state()
  private suggesting = false

  #dataset?: typeof UMB_PROPERTY_DATASET_CONTEXT.TYPE

  constructor() {
    super()
    this.consumeContext(UMB_PROPERTY_DATASET_CONTEXT, (context) => (this.#dataset = context))
    this.addValidator('valueMissing', () => 'Choose an icon.', () => this.mandatory && !this.value)
  }

  override connectedCallback() {
    super.connectedCallback()
    registerFontAwesome()
  }

  async #choose() {
    const chosen = await umbOpenModal(this, KCC_ICON_PICKER_MODAL, { value: { icon: this.value ?? '' } }).catch(() => undefined)
    if (chosen?.icon) {
      this.#set(chosen.icon)
    }
  }

  // Sends the name and description as they are in the editor, saved or not.
  async #suggest() {
    this.suggesting = true
    try {
      const properties = (await this.#dataset?.getProperties()) ?? []
      const description = properties.find((property) => property.alias === 'description')?.value
      const icon = await suggestRecipeIcon(this, this.#dataset?.getName() ?? '', typeof description === 'string' ? description : '')
      if (icon) {
        this.#set(icon)
      }
    } finally {
      this.suggesting = false
    }
  }

  #set(icon: string) {
    this.value = icon
    this.pristine = false
    this.dispatchEvent(new UmbChangeEvent())
  }

  override render() {
    return html`
      <div class="field">
        ${this.value
          ? html`<span class="preview"><i class=${this.value}></i></span><span class="label">${iconLabel(this.value)}</span>`
          : html`<span class="empty">No icon chosen</span>`}
        ${this.readonly
          ? nothing
          : html`
              <uui-button look="secondary" label="Select icon" @click=${this.#choose}></uui-button>
              <uui-button
                look="secondary"
                label="Suggest with AI"
                .state=${this.suggesting ? 'waiting' : undefined}
                ?disabled=${this.suggesting}
                @click=${this.#suggest}></uui-button>
            `}
      </div>
    `
  }

  static override readonly styles = [
    UmbTextStyles,
    fontAwesomeStyles,
    css`
      .field {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        gap: var(--uui-size-space-3);
      }

      .preview {
        display: inline-flex;
        align-items: center;
        justify-content: center;
        width: var(--uui-size-12);
        height: var(--uui-size-12);
        font-size: 1.75rem;
        border: 1px solid var(--uui-color-border);
        border-radius: var(--uui-border-radius);
      }

      .empty {
        color: var(--uui-color-text-alt);
      }
    `,
  ]
}

export default KccRecipeIconEditorElement

declare global {
  interface HTMLElementTagNameMap {
    'kcc-recipe-icon-editor': KccRecipeIconEditorElement
  }
}
