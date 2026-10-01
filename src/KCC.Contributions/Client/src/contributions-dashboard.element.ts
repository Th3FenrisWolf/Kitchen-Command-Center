import { css, customElement, html, state } from '@umbraco-cms/backoffice/external/lit'
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element'
import { UmbTextStyles } from '@umbraco-cms/backoffice/style'
import './entries-tab.element.js'
import './waiting-tab.element.js'

type Tab = 'waiting' | 'reviews' | 'notes'

const tabs: Array<{ tab: Tab; label: string }> = [
  { tab: 'waiting', label: 'Waiting' },
  { tab: 'reviews', label: 'Reviews' },
  { tab: 'notes', label: 'Cook notes' },
]

@customElement('kcc-contributions-dashboard')
export class KccContributionsDashboardElement extends UmbLitElement {
  @state()
  private tab: Tab = 'waiting'

  override render() {
    return html`
      <uui-tab-group>
        ${tabs.map(
          ({ tab, label }) =>
            html`<uui-tab label=${label} ?active=${this.tab === tab} @click=${() => (this.tab = tab)}>${label}</uui-tab>`,
        )}
      </uui-tab-group>
      <div class="panel">${this.#renderTab()}</div>
    `
  }

  #renderTab() {
    switch (this.tab) {
      case 'reviews':
        return html`<kcc-entries-tab kind="reviews"></kcc-entries-tab>`
      case 'notes':
        return html`<kcc-entries-tab kind="notes"></kcc-entries-tab>`
      default:
        return html`<kcc-waiting-tab></kcc-waiting-tab>`
    }
  }

  static override readonly styles = [
    UmbTextStyles,
    css`
      :host {
        display: block;
        padding: var(--uui-size-layout-1);
      }

      .panel {
        margin-top: var(--uui-size-layout-1);
      }
    `,
  ]
}

export default KccContributionsDashboardElement
