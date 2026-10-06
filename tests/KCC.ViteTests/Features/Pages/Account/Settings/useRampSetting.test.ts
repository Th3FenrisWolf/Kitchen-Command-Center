import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { useRampSetting } from '~/Pages/Account/Settings/useRampSetting'
import { FakeRoot } from '../../../../support/root'

let root: FakeRoot

beforeEach(() => {
  root = new FakeRoot()
  vi.stubGlobal('document', { documentElement: root })
  vi.stubGlobal('requestAnimationFrame', () => 0)
  vi.stubGlobal('window', { localStorage: { getItem: () => null }, matchMedia: () => ({ matches: false }) })
})

afterEach(() => {
  vi.unstubAllGlobals()
})

const answer = (ok: boolean, body: unknown) => ({
  ok,
  status: ok ? 200 : 400,
  text: () => Promise.resolve(JSON.stringify(body)),
})

const flushPromises = () => new Promise((resolve) => setTimeout(resolve))

function serve(...answers: ReturnType<typeof answer>[]) {
  const fetch = vi.fn()
  answers.forEach((a) => fetch.mockResolvedValueOnce(a))
  vi.stubGlobal('fetch', fetch)
  return fetch
}

function holdRequests() {
  const sent: unknown[] = []
  const unanswered: ((ok: boolean) => void)[] = []
  vi.stubGlobal(
    'fetch',
    vi.fn(
      (_url: string, init: { body: string }) =>
        new Promise((resolve) => {
          sent.push(JSON.parse(init.body))
          unanswered.push((ok) => resolve(answer(ok, ok ? { success: true } : {})))
        }),
    ),
  )
  const answerOldest = (ok: boolean) => unanswered.shift()?.(ok)
  return { sent, succeed: () => answerOldest(true), fail: () => answerOldest(false) }
}

describe('useRampSetting', () => {
  it('switches the page at once and saves the choice', async () => {
    const fetch = serve(answer(true, { success: true }))
    const { setting, choose } = useRampSetting('Device')

    const saving = choose('Dark')
    expect(root.getAttribute('data-theme')).toBe('dark')
    expect(setting.value).toBe('Dark')
    await saving

    expect(fetch).toHaveBeenCalledWith(
      '/api/profile/ramp',
      expect.objectContaining({ method: 'POST', body: JSON.stringify({ ramp: 'Dark' }) }),
    )
    expect(setting.value).toBe('Dark')
  })

  it('gives Device whatever the device prefers', async () => {
    serve(answer(true, { success: true }))
    vi.stubGlobal('window', { localStorage: { getItem: () => null }, matchMedia: () => ({ matches: true }) })
    const { choose } = useRampSetting('Light')

    await choose('Device')

    expect(root.getAttribute('data-theme')).toBe('dark')
  })

  it('puts the last saved choice back, on the page too, when the save fails', async () => {
    serve(answer(false, {}))
    const { setting, error, choose } = useRampSetting('Light')

    await choose('Dark')

    expect(setting.value).toBe('Light')
    expect(root.getAttribute('data-theme')).toBe('light')
    expect(error.value).toBe('The request could not be completed.')
  })

  it('puts back the choice that last saved, not the one the page loaded with', async () => {
    serve(answer(true, { success: true }), answer(false, {}))
    const { setting, error, choose } = useRampSetting('Device')

    await choose('Dark')
    await choose('Light')

    expect(setting.value).toBe('Dark')
    expect(root.getAttribute('data-theme')).toBe('dark')
    expect(error.value).toBe('The request could not be completed.')
  })

  it('keeps a newer choice when an older save fails', async () => {
    const { sent, succeed, fail } = holdRequests()
    const { setting, error, choose } = useRampSetting('Device')

    const first = choose('Dark')
    const second = choose('Light')
    await flushPromises()
    expect(sent).toEqual([{ ramp: 'Dark' }])

    fail()
    await flushPromises()
    expect(sent).toEqual([{ ramp: 'Dark' }, { ramp: 'Light' }])

    succeed()
    await Promise.all([first, second])

    expect(setting.value).toBe('Light')
    expect(root.getAttribute('data-theme')).toBe('light')
    expect(error.value).toBeNull()
  })

  it('puts the last saved choice back when overlapping saves both fail', async () => {
    vi.stubGlobal('window', { localStorage: { getItem: () => null }, matchMedia: () => ({ matches: true }) })
    const { fail } = holdRequests()
    const { setting, error, choose } = useRampSetting('Device')

    const first = choose('Dark')
    const second = choose('Light')
    await flushPromises()
    fail()
    await flushPromises()
    fail()
    await Promise.all([first, second])

    expect(setting.value).toBe('Device')
    expect(root.getAttribute('data-theme')).toBe('dark')
    expect(error.value).toBe('The request could not be completed.')
  })

  it('sends one save at a time, and only the latest choice made while one is in flight', async () => {
    const { sent, succeed } = holdRequests()
    const { setting, error, choose } = useRampSetting('Device')

    const settled = [choose('Light'), choose('Dark'), choose('Device')]
    await flushPromises()
    expect(sent).toEqual([{ ramp: 'Light' }])

    succeed()
    await flushPromises()
    expect(sent).toEqual([{ ramp: 'Light' }, { ramp: 'Device' }])

    succeed()
    await Promise.all(settled)

    expect(sent).toEqual([{ ramp: 'Light' }, { ramp: 'Device' }])
    expect(setting.value).toBe('Device')
    expect(error.value).toBeNull()
  })

  it('shows no error when a choice is taken back before its save fails', async () => {
    const { sent, fail } = holdRequests()
    const { setting, error, choose } = useRampSetting('Device')

    const settled = [choose('Dark'), choose('Device')]
    await flushPromises()
    expect(sent).toEqual([{ ramp: 'Dark' }])

    fail()
    await Promise.all(settled)

    expect(sent).toEqual([{ ramp: 'Dark' }])
    expect(setting.value).toBe('Device')
    expect(error.value).toBeNull()
  })

  it('clears the error as soon as another choice is made', async () => {
    serve(answer(false, {}), answer(true, { success: true }))
    const { error, choose } = useRampSetting('Device')
    await choose('Dark')
    expect(error.value).toBe('The request could not be completed.')

    const saving = choose('Light')
    expect(error.value).toBeNull()
    await saving
  })

  it('does nothing when the choice has not changed', async () => {
    const fetch = serve()
    const { choose } = useRampSetting('Dark')

    await choose('Dark')

    expect(fetch).not.toHaveBeenCalled()
    expect(root.getAttribute('data-theme')).toBeNull()
  })
})
