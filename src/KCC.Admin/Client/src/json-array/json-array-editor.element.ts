import { css, html, nothing, repeat, property, state } from '@umbraco-cms/backoffice/external/lit'
import type { PropertyValues, TemplateResult } from '@umbraco-cms/backoffice/external/lit'
import { UmbChangeEvent } from '@umbraco-cms/backoffice/event'
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element'
import type { UmbPropertyEditorUiElement } from '@umbraco-cms/backoffice/property-editor'
import { UmbSorterController } from '@umbraco-cms/backoffice/sorter'
import { UmbTextStyles } from '@umbraco-cms/backoffice/style'
import { UmbFormControlMixin } from '@umbraco-cms/backoffice/validation'
import { reconcileIncomingValue, serializeItems } from './helpers.js'

interface Row<T> {
  id: string
  item: T
}

export interface JsonArrayLabels {
  add: string
  empty: string
  invalid: string
  missing: string
}

let nextRowId = 0

const toRows = <T>(items: T[]): Array<Row<T>> => items.map((item) => ({ id: `row-${nextRowId++}`, item }))

export abstract class KccJsonArrayEditorElement<T>
  extends UmbFormControlMixin<string | undefined, typeof UmbLitElement, undefined>(UmbLitElement)
  implements UmbPropertyEditorUiElement
{
  @property({ type: Boolean, reflect: true })
  readonly = false

  @property({ type: Boolean })
  mandatory = false

  @state()
  private rows: Array<Row<T>> = []

  @state()
  private parseFailed = false

  #lastEmitted: string | undefined

  #sorter = new UmbSorterController<Row<T>>(this, {
    getUniqueOfElement: (element) => element.dataset.rowId,
    getUniqueOfModel: (row) => row.id,
    itemSelector: '.row',
    containerSelector: '.rows',
    handleSelector: '.handle',
    onChange: ({ model }) => {
      this.rows = model
      this.#emit()
    },
  })

  protected abstract readonly labels: JsonArrayLabels

  protected abstract readonly summaryTag: 'ul' | 'ol'

  protected abstract newItem(): T

  protected abstract isItemValid(item: T): boolean

  protected abstract prepare(items: T[]): T[]

  protected abstract renderSummary(item: T): string

  protected abstract renderFields(item: T, index: number, change: (next: T) => void): TemplateResult

  constructor() {
    super()
    // An enabled sorter queues its container lookup when the host connects, and disable() does not cancel a queued
    // lookup. So it starts disabled, and willUpdate enables it whenever the list is editable.
    this.#sorter.disable()
    this.addValidator('valueMissing', () => this.labels.missing, () => this.mandatory && !this.parseFailed && this.rows.length === 0)
    this.addValidator('customError', () => this.labels.invalid, () => this.rows.some((row) => !this.isItemValid(row.item)))
  }

  override set value(value: string | undefined) {
    const reconciled = reconcileIncomingValue<T>(value, this.#lastEmitted)
    super.value = value
    if (reconciled) {
      this.#lastEmitted = value
      this.parseFailed = reconciled.error
      this.#setRows(toRows(reconciled.items))
    }
  }

  override get value(): string | undefined {
    return super.value
  }

  #setRows(rows: Array<Row<T>>) {
    this.rows = rows
    this.#sorter.setModel(rows)
  }

  #emit() {
    const value = serializeItems(this.prepare(this.rows.map((row) => row.item)))
    this.#lastEmitted = value
    this.value = value
    this.dispatchEvent(new UmbChangeEvent())
  }

  #change(id: string, item: T) {
    this.#setRows(this.rows.map((row) => (row.id === id ? { id, item } : row)))
    this.#emit()
  }

  #add() {
    this.#setRows([...this.rows, ...toRows([this.newItem()])])
    this.pristine = false
    this.#emit()
  }

  #remove(id: string) {
    this.#setRows(this.rows.filter((row) => row.id !== id))
    this.pristine = false
    this.#emit()
  }

  #startFresh() {
    this.parseFailed = false
    this.#setRows([])
    this.#emit()
  }

  get #editable() {
    return !this.parseFailed && !this.readonly
  }

  protected override willUpdate(changedProperties: PropertyValues) {
    super.willUpdate(changedProperties)
    if (this.#editable) {
      this.#sorter.enable()
    } else {
      this.#sorter.disable()
    }
  }

  override render() {
    return html`
      <div class="rows" ?hidden=${!this.#editable}>${this.#editable ? this.#renderRows() : nothing}</div>
      ${this.#renderBelowRows()}
    `
  }

  #renderBelowRows() {
    if (this.parseFailed) {
      return this.#renderUnreadable()
    }

    return this.readonly ? this.#renderSummary() : this.#renderAddButton()
  }

  #renderUnreadable() {
    return html`
      <div class="unreadable">
        <p>This field holds data that is not a JSON list, so it cannot be edited here. The stored value is below.</p>
        <pre>${this.value}</pre>
        ${this.readonly
          ? nothing
          : html`<uui-button look="secondary" color="danger" label="Start fresh (clears this field)" @click=${this.#startFresh}></uui-button>`}
      </div>
    `
  }

  #renderSummary() {
    if (this.rows.length === 0) {
      return html`<p class="empty">${this.labels.empty}</p>`
    }

    const items = this.rows.map((row) => html`<li>${this.renderSummary(row.item)}</li>`)
    return this.summaryTag === 'ol' ? html`<ol>${items}</ol>` : html`<ul>${items}</ul>`
  }

  #renderRows() {
    return repeat(
      this.rows,
      (row) => row.id,
      (row, index) => html`
        <div class="row ${this.isItemValid(row.item) ? '' : 'invalid'}" data-row-id=${row.id}>
          <uui-symbol-drag-handle class="handle" title="Drag to reorder"></uui-symbol-drag-handle>
          <div class="fields">${this.renderFields(row.item, index, (next) => this.#change(row.id, next))}</div>
          <uui-button compact label="Remove" look="outline" color="danger" @click=${() => this.#remove(row.id)}>
            <uui-icon name="icon-trash"></uui-icon>
          </uui-button>
        </div>
      `,
    )
  }

  #renderAddButton() {
    return html`<uui-button look="placeholder" label=${this.labels.add} @click=${this.#add}></uui-button>`
  }

  static override readonly styles = [
    UmbTextStyles,
    css`
      :host {
        display: block;
      }

      .rows {
        display: grid;
        gap: var(--uui-size-space-3);
        margin-bottom: var(--uui-size-space-3);
      }

      .rows[hidden] {
        display: none;
      }

      .row {
        display: grid;
        grid-template-columns: auto 1fr auto;
        gap: var(--uui-size-space-3);
        align-items: start;
        padding: var(--uui-size-space-3);
        border: 1px solid var(--uui-color-border);
        border-radius: var(--uui-border-radius);
        background: var(--uui-color-surface);
      }

      .row.invalid {
        border-color: var(--uui-color-invalid);
      }

      .handle {
        cursor: grab;
        align-self: center;
      }

      .--umb-sorter-placeholder {
        visibility: hidden;
      }

      uui-button[look='placeholder'] {
        width: 100%;
      }

      .unreadable pre {
        white-space: pre-wrap;
        word-break: break-all;
        background: var(--uui-color-surface-alt);
        padding: var(--uui-size-space-3);
        border-radius: var(--uui-border-radius);
      }

      .empty {
        color: var(--uui-color-text-alt);
      }
    `,
  ]
}
