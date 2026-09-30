import { css, customElement, html } from '@umbraco-cms/backoffice/external/lit'
import { isInstructionValid, stampSteps } from './helpers.js'
import { KccJsonArrayEditorElement } from './json-array-editor.element.js'
import type { Instruction } from './types.js'

@customElement('kcc-instructions-editor')
export class KccInstructionsEditorElement extends KccJsonArrayEditorElement<Instruction> {
  protected override readonly labels = {
    add: 'Add step',
    empty: 'No steps yet.',
    invalid: 'Every step needs a description.',
    missing: 'Add at least one step.',
  }

  protected override readonly summaryTag = 'ol'

  protected override newItem(): Instruction {
    return { step: 0, text: '' }
  }

  protected override isItemValid(item: Instruction): boolean {
    return isInstructionValid(item)
  }

  protected override prepare(items: Instruction[]): Instruction[] {
    return stampSteps(items)
  }

  protected override renderSummary(item: Instruction): string {
    return item.text
  }

  protected override renderFields(item: Instruction, index: number, change: (next: Instruction) => void) {
    return html`
      <div class="instruction">
        <span class="step">${index + 1}</span>
        <uui-textarea
          label="Step ${index + 1}"
          placeholder="Step description"
          auto-height
          .value=${item.text}
          @input=${(event: Event) => change({ ...item, text: (event.target as HTMLTextAreaElement).value })}></uui-textarea>
      </div>
    `
  }

  static override readonly styles = [
    ...KccJsonArrayEditorElement.styles,
    css`
      .instruction {
        display: grid;
        grid-template-columns: 2rem 1fr;
        gap: var(--uui-size-space-3);
        align-items: start;
      }

      .step {
        font-weight: 700;
        line-height: var(--uui-size-11);
        text-align: center;
      }
    `,
  ]
}

export default KccInstructionsEditorElement

declare global {
  interface HTMLElementTagNameMap {
    'kcc-instructions-editor': KccInstructionsEditorElement
  }
}
