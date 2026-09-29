import { css, customElement, html, nothing, property, state } from '@umbraco-cms/backoffice/external/lit'
import { UMB_EDIT_DOCUMENT_WORKSPACE_PATH_PATTERN } from '@umbraco-cms/backoffice/document'
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element'
import { umbConfirmModal } from '@umbraco-cms/backoffice/modal'
import { UMB_NOTIFICATION_CONTEXT } from '@umbraco-cms/backoffice/notification'
import { UmbTextStyles } from '@umbraco-cms/backoffice/style'
import { deleteEntry, editEntry, getEntries, type Entry, type EntryKind, type EntryPage } from './api.js'
import { clampPage, formatDateTime, formatRating, maxTextLength, ratingSteps, totalPagesFor } from './format.js'

const pageSize = 20

interface Draft {
  id: number
  rating: number
  text: string
}

@customElement('kcc-entries-tab')
export class KccEntriesTabElement extends UmbLitElement {
  @property()
  kind: EntryKind = 'reviews'

  @state()
  private entries?: EntryPage

  @state()
  private page = 1

  @state()
  private draft?: Draft

  @state()
  private loadFailed = false

  get #isReviews() {
    return this.kind === 'reviews'
  }

  override connectedCallback() {
    super.connectedCallback()
    void this.#load()
  }

  async #load() {
    const page = this.page
    let entries = await getEntries(this, this.kind, page - 1, pageSize)
    // The API does not clamp the page, so one past the end comes back empty.
    if (entries?.items.length === 0 && entries.total > 0) {
      entries = await getEntries(this, this.kind, clampPage(page, entries.total, entries.pageSize) - 1, pageSize)
    }

    this.loadFailed = entries === undefined
    if (entries) {
      this.entries = entries
    }
    if (this.entries) {
      // The pager follows the list on screen, so a failed page change puts it back.
      this.page = this.entries.page + 1
    }
  }

  async #notify(message: string) {
    const notifications = await this.getContext(UMB_NOTIFICATION_CONTEXT)
    notifications?.peek('positive', { data: { message } })
  }

  async #save() {
    const draft = this.draft
    if (!draft || (!this.#isReviews && draft.text.trim() === '')) {
      return
    }

    const saved = await editEntry(this, this.kind, draft.id, this.#isReviews ? { rating: draft.rating, text: draft.text } : { text: draft.text })
    if (saved) {
      this.draft = undefined
      await this.#notify(this.#isReviews ? 'Review saved.' : 'Cook note saved.')
    }

    // tryExecute is silent about a 404, so an entry deleted elsewhere only shows as missing from the reload.
    await this.#load()
    if (this.draft?.id === draft.id && !this.entries?.items.some((entry) => entry.id === draft.id)) {
      this.draft = undefined
    }
  }

  async #delete(entry: Entry) {
    const noun = this.#isReviews ? 'review' : 'cook note'
    const confirmed = await umbConfirmModal(this, {
      headline: `Delete ${noun}?`,
      // A template, because umb-confirm-modal renders string content as raw HTML and the name is the member's own.
      content: html`This permanently removes the ${noun} by ${entry.memberName ?? 'a deleted member'}.`,
      color: 'danger',
      confirmLabel: 'Delete',
    }).then(
      () => true,
      () => false,
    )
    if (!confirmed) {
      return
    }

    if (await deleteEntry(this, this.kind, entry.id)) {
      await this.#notify(this.#isReviews ? 'Review deleted.' : 'Cook note deleted.')
    }
    await this.#load()
  }

  #goTo(page: number) {
    this.page = page
    void this.#load()
  }

  override render() {
    if (!this.entries) {
      return this.loadFailed
        ? html`<uui-box><p class="empty">The list could not be loaded.</p></uui-box>`
        : html`<uui-loader></uui-loader>`
    }

    if (this.entries.total === 0) {
      return html`<uui-box><p class="empty">${this.#isReviews ? 'No reviews yet.' : 'No cook notes yet.'}</p></uui-box>`
    }

    const pages = totalPagesFor(this.entries.total, this.entries.pageSize)
    return html`
      ${this.entries.items.map((entry) => this.#renderEntry(entry))}
      ${pages > 1
        ? html`<uui-pagination
            .current=${this.page}
            .total=${pages}
            @change=${(event: Event) => this.#goTo((event.target as HTMLElement & { current: number }).current)}></uui-pagination>`
        : nothing}
    `
  }

  #renderEntry(entry: Entry) {
    const editing = this.draft?.id === entry.id
    return html`
      <uui-box data-entry=${entry.id}>
        <div slot="headline" class="headline">
          <a href=${UMB_EDIT_DOCUMENT_WORKSPACE_PATH_PATTERN.generateAbsolute({ unique: entry.variantKey })}>
            ${entry.recipeName ? `${entry.recipeName} — ` : ''}${entry.variantName ?? 'Deleted variant'}
          </a>
        </div>
        <div slot="header-actions">
          ${editing
            ? nothing
            : html`
                <uui-button
                  label="Edit"
                  look="secondary"
                  @click=${() => (this.draft = { id: entry.id, rating: entry.rating ?? 5, text: entry.text ?? '' })}></uui-button>
                <uui-button label="Delete" look="secondary" color="danger" @click=${() => this.#delete(entry)}></uui-button>
              `}
        </div>
        <p class="meta">
          <strong>${entry.memberName ?? 'Deleted member'}</strong>
          ${this.#isReviews && entry.rating !== null ? html`<span>${formatRating(entry.rating)}</span>` : nothing}
          <span>${formatDateTime(entry.created)}</span>
        </p>
        ${editing ? this.#renderEditor() : html`<p class="text">${entry.text ?? ''}</p>`}
      </uui-box>
    `
  }

  #renderEditor() {
    const draft = this.draft!
    return html`
      <div class="editor">
        ${this.#isReviews
          ? html`
              <uui-select
                label="Rating"
                .options=${ratingSteps.map((step) => ({ name: formatRating(step), value: String(step), selected: step === draft.rating }))}
                @change=${(event: Event) => (this.draft = { ...draft, rating: Number((event.target as HTMLSelectElement).value) })}></uui-select>
            `
          : nothing}
        <uui-textarea
          label="Text"
          maxlength=${maxTextLength}
          auto-height
          .value=${draft.text}
          @input=${(event: Event) => (this.draft = { ...draft, text: (event.target as HTMLTextAreaElement).value })}></uui-textarea>
        <div class="actions">
          <uui-button label="Cancel" look="secondary" @click=${() => (this.draft = undefined)}></uui-button>
          <uui-button
            label="Save"
            look="primary"
            color="positive"
            ?disabled=${!this.#isReviews && draft.text.trim() === ''}
            @click=${this.#save}></uui-button>
        </div>
      </div>
    `
  }

  static override readonly styles = [
    UmbTextStyles,
    css`
      :host {
        display: grid;
        gap: var(--uui-size-space-4);
      }

      .meta {
        display: flex;
        flex-wrap: wrap;
        gap: var(--uui-size-space-4);
        margin: 0 0 var(--uui-size-space-3);
        color: var(--uui-color-text-alt);
      }

      .text {
        white-space: pre-wrap;
        margin: 0;
      }

      .editor {
        display: grid;
        gap: var(--uui-size-space-3);
      }

      .actions {
        display: flex;
        justify-content: flex-end;
        gap: var(--uui-size-space-3);
      }

      .empty {
        color: var(--uui-color-text-alt);
      }
    `,
  ]
}

export default KccEntriesTabElement

declare global {
  interface HTMLElementTagNameMap {
    'kcc-entries-tab': KccEntriesTabElement
  }
}
