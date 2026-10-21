import { nextTick, reactive } from 'vue'
import { describe, expect, it } from 'vitest'
import { useRaisedRules } from '~/Components/Forms/useRaisedRules'

describe('useRaisedRules', () => {
  it('starts at the minimum', () => {
    const props = reactive({ rules: 3 })

    expect(useRaisedRules(() => props.rules).value).toBe(3)
  })

  it('grows when the minimum is raised after mount', async () => {
    const props = reactive({ rules: 3 })
    const rules = useRaisedRules(() => props.rules)

    props.rules = 5
    await nextTick()

    expect(rules.value).toBe(5)
  })

  it('keeps a dragged height when the minimum is raised below it', async () => {
    const props = reactive({ rules: 3 })
    const rules = useRaisedRules(() => props.rules)
    rules.value = 8

    props.rules = 5
    await nextTick()

    expect(rules.value).toBe(8)
  })

  it('never shrinks when the minimum is lowered', async () => {
    const props = reactive({ rules: 5 })
    const rules = useRaisedRules(() => props.rules)

    props.rules = 3
    await nextTick()

    expect(rules.value).toBe(5)
  })
})
