import { css, customElement, html, nothing, state } from '@umbraco-cms/backoffice/external/lit'
import { UMB_EDIT_DOCUMENT_WORKSPACE_PATH_PATTERN } from '@umbraco-cms/backoffice/document'
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element'
import { UMB_NOTIFICATION_CONTEXT } from '@umbraco-cms/backoffice/notification'
import { UmbTextStyles } from '@umbraco-cms/backoffice/style'
import { approveMember, getWaiting, type Waiting, type WaitingMember } from './api.js'
import { formatDateTime } from './format.js'

@customElement('kcc-waiting-tab')
export class KccWaitingTabElement extends UmbLitElement {
  @state()
  private waiting?: Waiting

  @state()
  private approving?: string

  @state()
  private loadFailed = false

  override connectedCallback() {
    super.connectedCallback()
    void this.#load()
  }

  async #load() {
    const waiting = await getWaiting(this)
    if (waiting) {
      this.waiting = waiting
    }
    this.loadFailed = waiting === undefined
  }

  async #approve(member: WaitingMember) {
    this.approving = member.key
    try {
      if (await approveMember(this, member.key)) {
        const notifications = await this.getContext(UMB_NOTIFICATION_CONTEXT)
        notifications?.peek('positive', { data: { message: `${member.userName} can now sign in.` } })
      }
      // tryExecute is silent about a 404, so the reload is what drops a member approved elsewhere.
      await this.#load()
    } finally {
      this.approving = undefined
    }
  }

  override render() {
    if (!this.waiting) {
      return this.loadFailed
        ? html`<uui-box><p class="empty">The list could not be loaded.</p></uui-box>`
        : html`<uui-loader></uui-loader>`
    }

    return html`
      <uui-box headline="Members waiting for approval">
        ${this.waiting.members.length === 0 ? html`<p class="empty">No one is waiting.</p>` : this.#renderMembers()}
      </uui-box>
      <uui-box headline="Recipes and variants waiting to be published">
        ${this.waiting.drafts.length === 0 ? html`<p class="empty">Nothing is waiting.</p>` : this.#renderDrafts()}
      </uui-box>
    `
  }

  #renderMembers() {
    return html`
      <uui-table aria-label="Members waiting for approval">
        <uui-table-head>
          <uui-table-head-cell>Name</uui-table-head-cell>
          <uui-table-head-cell>Username</uui-table-head-cell>
          <uui-table-head-cell>Email</uui-table-head-cell>
          <uui-table-head-cell>Registered</uui-table-head-cell>
          <uui-table-head-cell></uui-table-head-cell>
        </uui-table-head>
        ${this.waiting!.members.map(
          (member) => html`
            <uui-table-row data-member=${member.userName}>
              <uui-table-cell>${member.name}</uui-table-cell>
              <uui-table-cell>${member.userName}</uui-table-cell>
              <uui-table-cell>${member.email}</uui-table-cell>
              <uui-table-cell>${formatDateTime(member.registered)}</uui-table-cell>
              <uui-table-cell>
                <uui-button
                  look="primary"
                  color="positive"
                  label="Approve"
                  .state=${this.approving === member.key ? 'waiting' : undefined}
                  ?disabled=${this.approving !== undefined}
                  @click=${() => this.#approve(member)}></uui-button>
              </uui-table-cell>
            </uui-table-row>
          `,
        )}
      </uui-table>
    `
  }

  #renderDrafts() {
    return html`
      <uui-table aria-label="Recipes and variants waiting to be published">
        <uui-table-head>
          <uui-table-head-cell>Name</uui-table-head-cell>
          <uui-table-head-cell>Type</uui-table-head-cell>
          <uui-table-head-cell>Submitted by</uui-table-head-cell>
          <uui-table-head-cell>Created</uui-table-head-cell>
        </uui-table-head>
        ${this.waiting!.drafts.map(
          (draft) => html`
            <uui-table-row data-draft=${draft.name}>
              <uui-table-cell>
                <a href=${UMB_EDIT_DOCUMENT_WORKSPACE_PATH_PATTERN.generateAbsolute({ unique: draft.key })}>${draft.name}</a>
                ${draft.kind === 'variant' && draft.recipeName ? html`<small>of ${draft.recipeName}</small>` : nothing}
              </uui-table-cell>
              <uui-table-cell>${draft.kind === 'recipe' ? 'Recipe' : 'Variant'}</uui-table-cell>
              <uui-table-cell>${draft.authorName ?? '—'}</uui-table-cell>
              <uui-table-cell>${formatDateTime(draft.created)}</uui-table-cell>
            </uui-table-row>
          `,
        )}
      </uui-table>
    `
  }

  static override readonly styles = [
    UmbTextStyles,
    css`
      :host {
        display: grid;
        gap: var(--uui-size-layout-1);
      }

      .empty {
        color: var(--uui-color-text-alt);
      }

      small {
        display: block;
        color: var(--uui-color-text-alt);
      }
    `,
  ]
}

export default KccWaitingTabElement

declare global {
  interface HTMLElementTagNameMap {
    'kcc-waiting-tab': KccWaitingTabElement
  }
}
