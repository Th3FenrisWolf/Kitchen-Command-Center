import { describe, expect, it } from 'vitest'
import { enclosingTag } from './recipeWizard'

describe('enclosingTag', () => {
  const markup = '<ul class="list"><li><span id="inner">x</span><br><input id="field"></li></ul>'

  it('returns the element that holds the needle, or the one further out', () => {
    expect(enclosingTag(markup, 'id="inner"')).toBe('<li>')
    expect(enclosingTag(markup, 'id="inner"', 1)).toBe('<ul class="list">')
    expect(enclosingTag(markup, 'id="field"')).toBe('<li>')
  })

  it('throws when the needle is not in the markup', () => {
    expect(() => enclosingTag(markup, 'id="missing"')).toThrow('id="missing" is not in the markup')
    expect(() => enclosingTag(`\n${markup}`, 'id="missing"')).toThrow('is not in the markup')
  })

  it('throws when a closing tag closes no open tag', () => {
    expect(() => enclosingTag('<div></span><b id="here">x</b></div>', 'id="here"')).toThrow('closes no open tag')
  })

  it('throws when outward reaches past the open elements', () => {
    expect(() => enclosingTag(markup, 'id="inner"', 5)).toThrow('out from')
  })
})
