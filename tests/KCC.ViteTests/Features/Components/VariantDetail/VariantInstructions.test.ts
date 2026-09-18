import { renderSsr } from '../../../support/renderSsr'
import { describe, expect, it } from 'vitest'
import VariantInstructions from '~/Components/VariantDetail/VariantInstructions.vue'
import type { Instruction } from '~/Types/Recipe'

const instructions: Instruction[] = [
  { text: 'Brown the butter until it smells of hazelnuts.' },
  { text: 'Fold the flour through in three goes.' },
  { text: 'Rest the dough for twenty minutes.' },
]

const render = (over: Record<string, unknown> = {}) => renderSsr(VariantInstructions, { instructions, ...over })

const tagWith = (html: string, needle: string) => html.match(new RegExp(`<[a-z0-9]+[^>]*${needle}[^>]*>`))?.[0] ?? ''

describe('VariantInstructions sheet', () => {
  it('is a peach-washed sheet on the sixth tear, labelled with the numbered list', async () => {
    const html = await render()

    expect(tagWith(html, 'kcc-slip')).toContain('kcc-slip kcc-tear-6')
    expect(html).toContain('<span class="kcc-label"><i class="fa-duotone fa-list-ol" aria-hidden="true"></i>')
    expect(html).toMatch(/kcc-label[^>]*>.*?Instructions/s)
    expect(html).not.toMatch(/sk-[a-z]/)
  })

  it('pools its one wash off the right edge, clear of the steps', async () => {
    const html = await render()
    const wash = tagWith(html, 'kcc-wash')

    expect(wash).toContain('--c:var(--color-peach)')
    expect(wash).toContain('--x:90%')
    expect(wash).toContain('--y:10%')
    expect(wash).toContain('--w:40%')
    expect(wash).toContain('--h:50%')
    expect((html.match(/kcc-wash/g) ?? []).length).toBe(1)
  })

  it('keeps the section heading for assistive tech while the label carries the printed name', async () => {
    const html = await render()

    expect(html).toMatch(/<h2 class="sr-only">\s*Instructions\s*<\/h2>/)
    expect((html.match(/kcc-label/g) ?? []).length).toBe(1)
  })

  it('counts the steps in the kick, in Sono', async () => {
    const kick = (await render()).match(/<p class="kcc-kick">.*?<\/p>/s)?.[0] ?? ''

    expect(kick).toContain('<span class="kcc-num">3</span>')
    expect(kick).toContain('steps')
  })

  it("sets the method as the kit's numbered steps, zero-padded", async () => {
    const html = await render()

    expect(html).toMatch(/<ol class="kcc-steps[^"]*">/)
    expect(html).toContain('<span class="kcc-n">01</span>')
    expect(html).toContain('<span class="kcc-n">02</span>')
    expect(html).toContain('<span class="kcc-n">03</span>')
    expect(html).toContain('<p class="kcc-body">Brown the butter until it smells of hazelnuts.</p>')
  })

  it('prints the step number the recipe gives, not the row position', async () => {
    const html = await render({ instructions: [{ step: 7, text: 'Serve.' }] })

    expect(html).toContain('<span class="kcc-n">07</span>')
  })

  it('carries no Softbound hook, weight or paper radius', async () => {
    const html = await render()

    expect(html).not.toContain('sk-')
    expect(html).not.toContain('font-bold')
    expect(html).not.toMatch(/rounded-(?:lg|xl|2xl|3xl)/)
  })
})
