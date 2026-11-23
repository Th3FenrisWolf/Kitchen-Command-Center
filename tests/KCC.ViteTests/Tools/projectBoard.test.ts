import { readdirSync, readFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { describe, expect, it, vi } from 'vitest'
// @ts-expect-error plain JavaScript with no declarations
import * as board from '../../../tools/project-board/derive.mjs'

type Card = {
  key: string
  aliases: string[]
  parentKey?: string
  held?: boolean
  title: string
  body: string
  fields: Record<string, string>
}

const root = fileURLToPath(new URL('../../../', import.meta.url))

function specSnapshot(
  dir: string,
  ref: string,
  { title = 'Thing', plan = false, tasks = null as string | null, extra = '' } = {},
) {
  const files = ['spec.md', ...(plan ? ['plan.md'] : []), ...(tasks === null ? [] : ['tasks.md'])]
  const spec = `# Feature Specification: ${title}\n\n**Feature Branch**: \`${dir}\`\n\n${extra}`
  return { dir, ref, files, spec, tasks }
}

function bugSnapshot(slug: string, ref: string, { fix = false, title = 'Something breaks', extra = '' } = {}) {
  return {
    slug,
    ref,
    files: ['assessment.md', ...(fix ? ['fix.md'] : [])],
    assessment: `# Bug Assessment: ${title}\n${extra}`,
  }
}

function derive(input: { specs?: unknown[]; bugs?: unknown[]; pulls?: unknown[] }): Map<string, Card> {
  const cards: Card[] = board.deriveCards({ specs: [], bugs: [], pulls: [], ...input })
  return new Map(cards.map((card) => [card.key, card]))
}

const phases006 = `| Phase | Spec | Covers |
|---|---|---|
| 1 | 006 (this spec) | Baseline and paint cost |
| 2 | later | Script weight |
| 3 | later | Server rendering |`

describe('phases table', () => {
  it('reads each row, with later as no spec', () => {
    expect(board.parsePhases(`intro\n\n${phases006}\n\nafter`)).toEqual([
      { phase: 1, spec: '006', covers: 'Baseline and paint cost' },
      { phase: 2, spec: null, covers: 'Script weight' },
      { phase: 3, spec: null, covers: 'Server rendering' },
    ])
  })

  it('skips a row with missing cells or no phase number', () => {
    const table = '| Phase | Spec | Covers |\n|---|---|---|\n| 1 | 006 | One |\n| 2 |\n| ? | later | Unknown |'
    expect(board.parsePhases(table)).toEqual([{ phase: 1, spec: '006', covers: 'One' }])
  })

  it('returns nothing for a spec with no phases table', () => {
    expect(board.parsePhases('| Row | Check |\n|---|---|\n| D1 | build |')).toEqual([])
  })
})

describe('idea line', () => {
  const url = 'https://github.com/Th3FenrisWolf/Kitchen-Command-Center/issues'

  it('reads the number from the spec, assessment and bare forms', () => {
    expect(board.parseIdeaLine(`**Input**: x\n**Idea**: [#148](${url}/148)\n`)).toBe(148)
    expect(board.parseIdeaLine(`- **Source**: y\n- **Idea**: [#150](${url}/150)\n`)).toBe(150)
    expect(board.parseIdeaLine('**Idea**: #148')).toBe(148)
  })

  it('ignores anything after the number', () => {
    expect(board.parseIdeaLine('**Idea**: [#148](u) and more 999')).toBe(148)
  })

  it('takes the first of two lines', () => {
    expect(board.parseIdeaLine('**Idea**: #1\n**Idea**: #2')).toBe(1)
  })

  it('returns null with no line, and for the words inside running text', () => {
    expect(board.parseIdeaLine('# Title\n\nNothing here')).toBeNull()
    expect(board.parseIdeaLine('See the **Idea**: #148 for more')).toBeNull()
  })

  it('reads only the header above the first section', () => {
    const spec = '# Feature Specification: X\n\n**Input**: y\n\n## Contract\n\n```markdown\n**Idea**: [#7](u)\n```\n'
    expect(board.parseIdeaLine(spec)).toBeNull()
    expect(board.parseIdeaLine(`# T\n\n**Idea**: [#148](u)\n**Input**: y\n\n## Contract\n\n**Idea**: #7\n`)).toBe(148)
    expect(board.parseIdeaLine('# T\n\n- **Source**: y\n- **Idea**: [#150](u)\n\n## Report\n\n> **Idea**: #7\n')).toBe(150)
  })
})

describe('idea on a card', () => {
  const line = '**Idea**: [#148](https://example/148)\n'

  it('comes from the spec snapshot the card is built from', () => {
    const unmerged = derive({
      specs: [specSnapshot('010-a', 'main', { extra: '**Idea**: #1\n' }), specSnapshot('010-a', '010-a', { extra: line })],
    })
    expect(unmerged.get('010-a')?.idea).toBe(148)
    const merged = derive({
      specs: [specSnapshot('010-a', 'main', { extra: '**Idea**: #1\n' }), specSnapshot('010-a', '010-a', { extra: line })],
      pulls: [{ number: 7, url: 'u', head: '010-a', state: 'MERGED' }],
    })
    expect(merged.get('010-a')?.idea).toBe(1)
  })

  it('comes from the assessment the bug card is built from', () => {
    const cards = derive({ bugs: [bugSnapshot('fixing', 'main', { extra: `\n- **Idea**: [#150](u)\n` })] })
    expect(cards.get('bug/fixing')?.idea).toBe(150)
  })

  it('is absent when no line names an idea', () => {
    const cards = derive({ specs: [specSnapshot('010-a', 'main')], bugs: [bugSnapshot('x', 'main')] })
    expect(cards.get('010-a')).not.toHaveProperty('idea')
    expect(cards.get('bug/x')).not.toHaveProperty('idea')
  })
})

describe('spec stage', () => {
  it('follows the files on the spec branch', () => {
    const cards = derive({
      specs: [
        specSnapshot('010-a', '010-a'),
        specSnapshot('011-b', '011-b', { plan: true }),
        specSnapshot('012-c', '012-c', { plan: true, tasks: '- [ ] T001 one\n- [ ] T002 two' }),
        specSnapshot('013-d', '013-d', { plan: true, tasks: '- [X] T001 one\n- [ ] T002 two' }),
      ],
    })
    expect(cards.get('010-a')?.fields.Status).toBe('Specified')
    expect(cards.get('011-b')?.fields.Status).toBe('Specified')
    expect(cards.get('012-c')?.fields.Status).toBe('Specified')
    expect(cards.get('013-d')?.fields.Status).toBe('Building')
    expect(cards.get('013-d')?.body).toContain('Tasks 1/2')
    expect(cards.get('011-b')?.body).toContain('blob/011-b/specs/011-b/plan.md')
    expect(cards.get('010-a')?.body).not.toContain('[Plan]')
  })

  it('prefers the spec branch to main while the spec is not merged', () => {
    const cards = derive({
      specs: [specSnapshot('010-a', 'main'), specSnapshot('010-a', '010-a', { plan: true, tasks: '- [X] T001' })],
    })
    expect(cards.get('010-a')?.fields.Status).toBe('Building')
  })

  it('is In review while its pull request is open, and Done once it merges', () => {
    const pr = (state: string) => [{ number: 7, url: 'https://pr/7', head: '010-a', state }]
    const built = { plan: true, tasks: '- [X] T001\n- [X] T002' }
    const open = derive({ specs: [specSnapshot('010-a', '010-a', built)], pulls: pr('OPEN') })
    const merged = derive({ specs: [specSnapshot('010-a', 'main', built)], pulls: pr('MERGED') })
    const closed = derive({ specs: [specSnapshot('010-a', 'main', built)], pulls: pr('CLOSED') })
    expect(open.get('010-a')?.fields.Status).toBe('In review')
    expect(open.get('010-a')?.body).toContain('[#7](https://pr/7)')
    expect(merged.get('010-a')?.fields.Status).toBe('Done')
    expect(closed.get('010-a')?.fields.Status).toBe('Building')
  })

  it('stays Specified while a pull request carries only the spec', () => {
    const pr = (state: string) => [{ number: 7, url: 'https://pr/7', head: '010-a', state }]
    const unticked = { plan: true, tasks: '- [ ] T001\n- [ ] T002' }
    const open = derive({ specs: [specSnapshot('010-a', '010-a')], pulls: pr('OPEN') })
    const merged = derive({
      specs: [specSnapshot('010-a', 'main'), specSnapshot('010-a', '010-a')],
      pulls: pr('MERGED'),
    })
    const planned = derive({ specs: [specSnapshot('010-a', 'main', unticked)], pulls: pr('MERGED') })
    expect(open.get('010-a')?.fields.Status).toBe('Specified')
    expect(open.get('010-a')?.body).toContain('[#7](https://pr/7)')
    expect(merged.get('010-a')?.fields.Status).toBe('Specified')
    expect(planned.get('010-a')?.fields.Status).toBe('Specified')
  })

  it('follows the build on the same branch once the spec pull request has merged', () => {
    const specPull = { number: 7, url: 'https://pr/7', head: '010-a', state: 'MERGED' }
    const buildPull = { number: 9, url: 'https://pr/9', head: '010-a', state: 'OPEN' }
    const specs = [
      specSnapshot('010-a', 'main'),
      specSnapshot('010-a', '010-a', { plan: true, tasks: '- [X] T001\n- [ ] T002' }),
    ]
    const building = derive({ specs, pulls: [specPull] })
    const inReview = derive({ specs, pulls: [specPull, buildPull] })
    const reversed = derive({ specs, pulls: [buildPull, specPull] })
    expect(building.get('010-a')?.fields.Status).toBe('Building')
    expect(building.get('010-a')?.body).toContain('Tasks 1/2')
    expect(inReview.get('010-a')?.fields.Status).toBe('In review')
    expect(inReview.get('010-a')?.body).toContain('[#9](https://pr/9)')
    expect(reversed.get('010-a')?.body).toContain('[#9](https://pr/9)')
  })

  it('is Done once the build merges after its spec, and links the build', () => {
    const specs = [
      specSnapshot('010-a', 'main', { plan: true, tasks: '- [X] T001\n- [X] T002' }),
      specSnapshot('010-a', '010-a', { plan: true, tasks: '- [X] T001\n- [X] T002' }),
    ]
    const specPull = { number: 7, url: 'https://pr/7', head: '010-a', state: 'MERGED' }
    const buildPull = { number: 9, url: 'https://pr/9', head: '010-a', state: 'MERGED' }
    for (const pulls of [
      [specPull, buildPull],
      [buildPull, specPull],
    ]) {
      const card = derive({ specs, pulls }).get('010-a')
      expect(card?.fields.Status).toBe('Done')
      expect(card?.body).toContain('[#9](https://pr/9)')
    }
  })

  it('stays Done when tasks were left open on a merged spec', () => {
    const cards = derive({
      specs: [specSnapshot('010-a', 'main', { plan: true, tasks: '- [X] T001\n- [ ] T002' })],
      pulls: [{ number: 7, url: 'u', head: '010-a', state: 'MERGED' }],
    })
    expect(cards.get('010-a')?.fields.Status).toBe('Done')
  })

  it('titles the card from the spec heading', () => {
    const cards = derive({ specs: [specSnapshot('010-a', 'main', { title: 'Paper Rule Scheme' })] })
    expect(cards.get('010-a')).toMatchObject({
      title: '010 · Paper Rule Scheme',
      fields: { Kind: 'Feature', Feature: '010 Paper Rule Scheme', Phase: '' },
    })
  })
})

describe('phased features', () => {
  const phase1 = specSnapshot('006-sweep', 'main', { title: 'Performance Sweep, Phase 1 - Baseline', extra: phases006 })

  it('gives each later phase an Idea card under its feature', () => {
    const cards = derive({ specs: [phase1] })
    expect(cards.get('006-sweep')?.fields).toMatchObject({ Feature: '006 Performance Sweep', Phase: '1 of 3' })
    expect(cards.get('006-sweep/phase-2')).toMatchObject({
      title: '006 · Phase 2: Script weight',
      fields: { Status: 'Idea', Kind: 'Phase', Feature: '006 Performance Sweep', Phase: '2 of 3' },
    })
    expect(cards.has('006-sweep/phase-3')).toBe(true)
  })

  it('turns the placeholder into the phase spec once a table row names it', () => {
    const named = phases006.replace('| 2 | later |', '| 2 | 009 |')
    const phase2 = specSnapshot('009-sweep-two', '009-sweep-two', { title: 'Performance Sweep, Phase 2', extra: named })
    const cards = derive({ specs: [phase1, phase2] })
    expect(cards.has('006-sweep/phase-2')).toBe(false)
    expect(cards.get('006-sweep')?.parentKey).toBeUndefined()
    expect(cards.get('009-sweep-two')).toMatchObject({
      aliases: ['006-sweep/phase-2'],
      parentKey: '006-sweep',
      fields: { Kind: 'Phase', Feature: '006 Performance Sweep', Phase: '2 of 3', Status: 'Specified' },
    })
  })

  it('keeps the placeholder while the named spec has not been pushed', () => {
    const named = phases006.replace('| 2 | later |', '| 2 | 009 |')
    const cards = derive({ specs: [specSnapshot('006-sweep', 'main', { title: 'Sweep, Phase 1', extra: named })] })
    expect(cards.get('006-sweep/phase-2')?.fields.Status).toBe('Idea')
  })
})

describe('a spec number two folders share', () => {
  const naming008 = phases006.replace('| 2 | later |', '| 2 | 008 |')
  const sweep = specSnapshot('006-sweep', 'main', { title: 'Performance Sweep, Phase 1 - Baseline', extra: naming008 })
  const capture = specSnapshot('008-capture', 'claude/cloud', { title: 'Phone Idea Capture' })
  const hydration = specSnapshot('008-hydration', '008-hydration', { title: 'Performance Sweep, Phase 2', extra: naming008 })
  const other = specSnapshot('010-other', 'main', { title: 'Other' })
  const clashing = [sweep, capture, hydration, other]
  const held = (cards: Map<string, Card>) => [...cards.values()].filter((card) => card.held).map((card) => card.key)
  const boardItem = (key: string) => ({
    id: `item-${key}`,
    type: 'Issue',
    contentId: `issue-${key}`,
    state: 'OPEN',
    parentId: null,
    title: 'stale',
    body: '',
    fields: { Key: key, Status: 'Specified', Kind: 'Feature', Feature: '', Phase: '' },
  })

  it('names the number with each folder and the branches that hold it', () => {
    expect(board.numberClashes([...clashing, { ...hydration, ref: 'main' }])).toEqual([
      {
        number: '008',
        folders: [
          { dir: '008-capture', refs: ['claude/cloud'] },
          { dir: '008-hydration', refs: ['008-hydration', 'main'] },
        ],
      },
    ])
  })

  it('is no clash when one folder is on several branches', () => {
    expect(board.numberClashes([sweep, { ...sweep, ref: '006-sweep' }, other])).toEqual([])
  })

  it('holds both folders and every card of the feature whose table names the number', () => {
    const cards = derive({ specs: clashing })
    expect(held(cards)).toEqual(['006-sweep', '008-capture', '008-hydration'])
    expect(cards.get('008-capture')?.parentKey).toBeUndefined()
    expect(cards.get('010-other')?.held).toBeUndefined()
  })

  it("holds the feature when only another branch's table names the shared number", () => {
    const naming009 = phases006.replace('| 2 | later |', '| 2 | 009 |')
    const onMain = specSnapshot('006-sweep', 'main', { title: 'Performance Sweep, Phase 1 - Baseline', extra: naming009 })
    const nine = specSnapshot('009-hydration', 'main', { title: 'Performance Sweep, Phase 2', extra: naming009 })
    const onStale = specSnapshot('006-sweep', 'stale', { title: 'Performance Sweep, Phase 1 - Baseline', extra: naming008 })
    const old = specSnapshot('008-old', 'stale', { title: 'Old' })
    const cards = derive({ specs: [onMain, nine, onStale, capture, old, other] })
    expect(held(cards)).toEqual(['006-sweep', '008-capture', '008-old', '009-hydration'])
    expect(cards.get('009-hydration')?.parentKey).toBeUndefined()
  })

  it("holds the whole feature when phase 1's number is the shared one", () => {
    const plain = specSnapshot('006-sweep', 'main', { title: 'Performance Sweep, Phase 1 - Baseline', extra: phases006 })
    const rival = specSnapshot('006-rival', 'stale', { title: 'Rival' })
    const two = specSnapshot('009-two', '009-two', {
      title: 'Performance Sweep, Phase 2',
      extra: phases006.replace('| 2 | later |', '| 2 | 009 |'),
    })
    expect(held(derive({ specs: [plain, rival, two, other] }))).toEqual(['006-rival', '006-sweep', '009-two'])
  })

  it('holds nothing while every number names one folder', () => {
    expect(held(derive({ specs: [sweep, hydration, other] }))).toEqual([])
  })

  it('plans no change for a held card and calls none of its items a stray', () => {
    const cards: Card[] = board.deriveCards({ specs: clashing, bugs: [], pulls: [] })
    const keys = ['006-sweep', '006-sweep/phase-2', '006-sweep/phase-3', '008-capture', '010-other', '011-gone']
    const items = keys.map(boardItem)
    expect(board.planChanges(cards, items).map((op: { card: Card }) => op.card.key)).toEqual(['010-other'])
    expect(board.strayKeys(cards, items)).toEqual(['011-gone'])
  })

  it('reports the clash and the held cards plainly', () => {
    const cards: Card[] = board.deriveCards({ specs: clashing, bugs: [], pulls: [] })
    expect(board.clashReport(board.numberClashes([...clashing, { ...hydration, ref: 'main' }]), cards)).toBe(
      'Spec number 008 names more than one folder: 008-capture on claude/cloud; 008-hydration on 008-hydration, main.\n' +
        'Renumber all but one of them. Until then the sync leaves these cards as they are: ' +
        '006-sweep, 008-capture, 008-hydration.',
    )
  })
})

describe('bugs', () => {
  it('is Specified while only assessed, Building once fixed on its branch, Done on main', () => {
    const cards = derive({
      bugs: [
        bugSnapshot('only-assessed', 'main'),
        bugSnapshot('fixing', 'fix/fixing', { fix: true }),
        bugSnapshot('landed', 'main', { fix: true }),
      ],
    })
    expect(cards.get('bug/only-assessed')?.fields.Status).toBe('Specified')
    expect(cards.get('bug/fixing')?.fields.Status).toBe('Building')
    expect(cards.get('bug/landed')?.fields.Status).toBe('Done')
    expect(cards.get('bug/landed')).toMatchObject({ title: 'Bug · Something breaks', fields: { Kind: 'Bug' } })
  })

  it('is Building when the assessment is on main and the fix only on its branch', () => {
    const cards = derive({
      bugs: [bugSnapshot('split', 'main'), bugSnapshot('split', 'fix/split', { fix: true })],
    })
    expect(cards.get('bug/split')?.fields.Status).toBe('Building')
    expect(cards.get('bug/split')?.body).toContain('blob/fix/split/.specify/bugs/split/fix.md')
  })

  it('follows a fix/<slug> pull request', () => {
    const cards = derive({
      bugs: [bugSnapshot('fixing', 'fix/fixing', { fix: true })],
      pulls: [{ number: 9, url: 'u', head: 'fix/fixing', state: 'OPEN' }],
    })
    expect(cards.get('bug/fixing')?.fields.Status).toBe('In review')
  })
})

describe('planned changes', () => {
  const card: Card = {
    key: '010-a',
    aliases: ['006-sweep/phase-2'],
    title: '010 · A',
    body: 'links',
    fields: { Key: '010-a', Status: 'Building', Kind: 'Feature', Feature: '010 A', Phase: '' },
  }
  const item = (fields: Record<string, string>, extra = {}) => ({
    id: 'item',
    type: 'Issue',
    contentId: 'issue',
    state: 'OPEN',
    parentId: null,
    title: card.title,
    body: board.syncedBody(card.body),
    fields: { ...card.fields, Iteration: 'Current', ...fields },
    ...extra,
  })
  const update = { op: 'update', itemId: 'item', type: 'Issue', contentId: 'issue', card }
  const at = (status: string, extra: Partial<Card> = {}): Card => ({
    ...card,
    ...extra,
    fields: { ...card.fields, Status: status },
  })

  it('creates an Idea card as a draft and any later card as an issue, closed once Done', () => {
    expect(board.planChanges([at('Idea')], [])).toEqual([{ op: 'create', card: at('Idea'), as: 'draft' }])
    expect(board.planChanges([card], [])).toEqual([{ op: 'create', card, as: 'issue' }])
    expect(board.planChanges([at('Done')], [])).toEqual([{ op: 'create', card: at('Done'), as: 'issue', state: 'CLOSED' }])
  })

  it('changes only what differs', () => {
    expect(board.planChanges([card], [item({ Status: 'Specified' })])).toEqual([
      { ...update, fields: { Status: 'Building' } },
    ])
    expect(board.planChanges([card], [item({})])).toEqual([])
  })

  it('turns a draft into an issue once its card leaves Idea, and closes it when Done', () => {
    const draft = (status: string) => item({ Status: status }, { type: 'DraftIssue', contentId: 'draft', state: undefined })
    const fromDraft = { ...update, type: 'DraftIssue', contentId: 'draft' }
    expect(board.planChanges([card], [draft('Building')])).toEqual([{ ...fromDraft, convert: true }])
    expect(board.planChanges([at('Done')], [draft('Done')])).toEqual([
      { ...fromDraft, card: at('Done'), convert: true, state: 'CLOSED' },
    ])
    expect(board.planChanges([at('Idea')], [draft('Idea')])).toEqual([])
  })

  it('closes an issue when Done and reopens it when it leaves Done', () => {
    expect(board.planChanges([at('Done')], [item({ Status: 'Done' })])).toEqual([
      { ...update, card: at('Done'), state: 'CLOSED' },
    ])
    expect(board.planChanges([card], [item({}, { state: 'CLOSED' })])).toEqual([{ ...update, state: 'OPEN' }])
  })

  it('leaves the state of an issue whose card is an Idea alone', () => {
    expect(board.planChanges([at('Idea')], [item({ Status: 'Idea' }, { state: 'CLOSED' })])).toEqual([])
  })

  it("makes a later phase's issue a sub-issue of phase 1's", () => {
    const phase = at('Building', { parentKey: '006-sweep' })
    const root = item({ Key: '006-sweep' }, { id: 'root-item', contentId: 'root-issue' })
    expect(board.planChanges([phase], [root, item({})])).toEqual([{ ...update, card: phase, parent: '006-sweep' }])
    expect(board.planChanges([phase], [root, item({}, { parentId: 'root-issue' })])).toEqual([])
    expect(board.planChanges([phase], [])).toEqual([{ op: 'create', card: phase, as: 'issue', parent: '006-sweep' }])
    expect(board.planChanges([{ ...phase, fields: { ...phase.fields, Status: 'Idea' } }], [])).toEqual([
      { op: 'create', card: { ...phase, fields: { ...phase.fields, Status: 'Idea' } }, as: 'draft' },
    ])
  })

  it('takes over the item of an alias and rekeys it', () => {
    const placeholder = item(
      { Key: '006-sweep/phase-2', Status: 'Idea' },
      { type: 'DraftIssue', contentId: 'draft', state: undefined },
    )
    expect(board.planChanges([card], [placeholder])).toEqual([
      {
        ...update,
        type: 'DraftIssue',
        contentId: 'draft',
        fields: { Key: '010-a', Status: 'Building' },
        convert: true,
      },
    ])
  })

  it('keeps the owner notes outside the synced block', () => {
    const noted = item({}, { body: `${board.syncedBody('old links')}\n\nMy notes` })
    expect(board.planChanges([card], [noted])).toEqual([{ ...update, body: `${board.syncedBody('links')}\n\nMy notes` }])
  })

  it('puts the synced block above an idea that gains a key', () => {
    const idea = item({}, { body: 'Raw idea' })
    expect(board.planChanges([card], [idea])[0]).toMatchObject({ body: `${board.syncedBody('links')}\n\nRaw idea` })
  })

  it('never touches a card without a key', () => {
    expect(board.planChanges([], [item({ Key: '' }, { title: 'An idea' })])).toEqual([])
  })
})

describe('archived items', () => {
  const card: Card = {
    key: '010-a',
    aliases: ['006-sweep/phase-2'],
    title: '010 · A',
    body: 'links',
    fields: { Key: '010-a', Status: 'Building', Kind: 'Feature', Feature: '010 A', Phase: '' },
  }
  const item = (id: string, key: string, extra = {}) => ({
    id,
    type: 'Issue',
    contentId: `issue-${id}`,
    state: 'OPEN',
    parentId: null,
    title: card.title,
    body: board.syncedBody(card.body),
    fields: { ...card.fields, Key: key },
    ...extra,
  })

  it('is matched by its Key or alias and neither created nor updated', () => {
    const stale = { title: 'old', body: 'old', fields: { ...card.fields, Status: 'Idea' }, archived: true }
    expect(board.planChanges([card], [item('a', '010-a', stale)])).toEqual([])
    expect(board.planChanges([card], [item('a', '006-sweep/phase-2', stale)])).toEqual([])
  })

  describe('sharing one Key', () => {
    const stale = { fields: { ...card.fields, Status: 'Idea' } }
    const planned = (items: unknown[]) => board.planChanges([card], items).map((op: { itemId?: string }) => op.itemId)

    it.each(['010-a', '006-sweep/phase-2'])('updates the unarchived item over an archived one first (%s)', (key) => {
      expect(planned([item('old', key, { ...stale, archived: true }), item('live', key, stale)])).toEqual(['live'])
    })

    it.each(['010-a', '006-sweep/phase-2'])('takes the first of two unarchived items (%s)', (key) => {
      expect(planned([item('one', key, stale), item('two', key, stale)])).toEqual(['one'])
    })

    it.each(['010-a', '006-sweep/phase-2'])('plans nothing when every item is archived (%s)', (key) => {
      expect(
        planned([item('one', key, { ...stale, archived: true }), item('two', key, { ...stale, archived: true })]),
      ).toEqual([])
    })
  })

  it('calls no archived leftover a stray', () => {
    const gone = item('gone', '011-gone', { archived: true })
    const live = item('live', '012-gone')
    expect(board.strayKeys([card], [gone, live])).toEqual(['012-gone'])
  })
})

describe('an empty read', () => {
  it('refuses a read that finds no spec and no bug', () => {
    expect(board.emptyReadError([], [])).toMatch(/no spec and no bug/)
  })

  it('accepts a read with a spec or a bug', () => {
    expect(board.emptyReadError([specSnapshot('010-a', 'main')], [])).toBeNull()
    expect(board.emptyReadError([], [bugSnapshot('slow', 'main')])).toBeNull()
  })
})

describe('board fields', () => {
  const card = { fields: { Key: '010-a', Status: 'Building', Kind: 'Feature' } }
  const fields = new Map([
    ['Key', { dataType: 'TEXT' }],
    ['Status', { dataType: 'SINGLE_SELECT', options: [{ name: 'Specified' }] }],
  ])

  it('offers exactly the five stages', () => {
    expect(board.STAGES).toEqual(['Idea', 'Specified', 'Building', 'In review', 'Done'])
  })

  it('names every missing field and option before anything is written', () => {
    expect(board.schemaProblems(fields, [card])).toEqual([
      'The Status field has no Building option',
      'The project has no Kind field',
      'The project has no Iteration field',
    ])
  })

  it('finds nothing when the board has every field and option', () => {
    const complete = new Map([
      ...fields,
      ['Status', { dataType: 'SINGLE_SELECT', options: [{ name: 'Building' }] }],
      ['Kind', { dataType: 'SINGLE_SELECT', options: [{ name: 'Feature' }] }],
      ['Iteration', { dataType: 'SINGLE_SELECT', options: [{ name: 'Current' }] }],
    ])
    expect(board.schemaProblems(complete, [card])).toEqual([])
  })
})

describe('a created issue', () => {
  const issue = (...projects: string[]) => ({
    id: 'I_1',
    projectItems: { nodes: projects.map((project, at) => ({ id: `PVTI_${at}`, project: { id: project } })) },
  })

  it('finds its item on this board', () => {
    expect(board.boardItemOf(issue('P_other', 'P_board'), 'P_board')).toBe('PVTI_1')
  })

  it('has no item when createIssue answers without one for this board, and that is no error', () => {
    expect(board.boardItemOf(issue(), 'P_board')).toBeNull()
    expect(board.boardItemOf(issue('P_other'), 'P_board')).toBeNull()
  })

  it('takes the item from the createIssue reply without reading the issue again', () => {
    const reread = vi.fn()
    const pause = vi.fn()
    expect(board.waitForBoardItem(issue('P_board'), 'P_board', reread, pause)).toBe('PVTI_0')
    expect(reread).not.toHaveBeenCalled()
    expect(pause).not.toHaveBeenCalled()
  })

  it('reads the issue again, pausing longer each time, until its item appears', () => {
    const replies = [issue(), issue('P_other', 'P_board')]
    const reread = vi.fn(() => replies.shift())
    const pause = vi.fn()
    expect(board.waitForBoardItem(issue(), 'P_board', reread, pause)).toBe('PVTI_1')
    expect(reread.mock.calls).toEqual([['I_1'], ['I_1']])
    expect(pause.mock.calls).toEqual([[1000], [2000]])
  })

  it('keeps reading when a read fails or finds no issue', () => {
    const replies = [
      () => {
        throw new Error('gh: Could not resolve to a node')
      },
      () => null,
      () => issue('P_board'),
    ]
    const reread = vi.fn(() => replies.shift()!())
    const pause = vi.fn()
    expect(board.waitForBoardItem(issue(), 'P_board', reread, pause)).toBe('PVTI_0')
    expect(pause.mock.calls.flat()).toEqual([1000, 2000, 4000])
  })

  it('gives up with no item, and no error, when every read fails', () => {
    const reread = vi.fn(() => {
      throw new Error('gh: rate limited')
    })
    const pause = vi.fn()
    expect(board.waitForBoardItem(issue(), 'P_board', reread, pause)).toBeNull()
    expect(reread).toHaveBeenCalledTimes(4)
  })

  it('gives up with no item after the last read', () => {
    const reread = vi.fn(() => issue('P_other'))
    const pause = vi.fn()
    expect(board.waitForBoardItem(issue(), 'P_board', reread, pause)).toBeNull()
    expect(pause.mock.calls.flat()).toEqual([1000, 2000, 4000, 8000])
    expect(reread).toHaveBeenCalledTimes(4)
  })
})

describe('owner ideas', () => {
  const issue = (id: string, number: number, extra = {}) => ({
    id,
    number,
    state: 'OPEN',
    synced: false,
    body: 'my idea',
    ...extra,
  })
  const boardItem = (contentId: string, fields: Record<string, string> = {}) => ({
    id: `item-${contentId}`,
    type: 'Issue',
    contentId,
    state: 'OPEN',
    parentId: null,
    title: 't',
    body: '',
    fields,
  })

  it('returns an issue that is off the board with a null item', () => {
    expect(board.ownerIdeas([issue('i1', 148)], [])).toEqual([{ issue: issue('i1', 148), item: null }])
  })

  it('returns an issue on the board with blank fields, with its item', () => {
    const item = boardItem('i1')
    expect(board.ownerIdeas([issue('i1', 148)], [item])).toEqual([{ issue: issue('i1', 148), item }])
  })

  it('leaves out a synced issue', () => {
    expect(board.ownerIdeas([issue('i1', 148, { synced: true })], [])).toEqual([])
  })

  it('leaves out an issue whose item carries a Key', () => {
    expect(board.ownerIdeas([issue('i1', 148)], [boardItem('i1', { Key: '008-x' })])).toEqual([])
  })

  it('picks the first unarchived item when several carry one issue', () => {
    const first = { ...boardItem('i1'), id: 'first' }
    const second = { ...boardItem('i1'), id: 'second' }
    const archived = { ...boardItem('i1'), id: 'archived', archived: true }
    expect(board.ownerIdeas([issue('i1', 148)], [archived, first, second])[0].item.id).toBe('first')
  })

  it('picks the first archived item when every item of an issue is archived', () => {
    const one = { ...boardItem('i1'), id: 'one', archived: true }
    const two = { ...boardItem('i1'), id: 'two', archived: true }
    expect(board.ownerIdeas([issue('i1', 148)], [one, two])[0].item.id).toBe('one')
  })

  it('never returns an item whose content is not among the owner issues', () => {
    expect(board.ownerIdeas([issue('i1', 148)], [boardItem('other')])).toEqual([{ issue: issue('i1', 148), item: null }])
    expect(board.ownerIdeas([], [boardItem('other')])).toEqual([])
  })
})

describe('planned idea changes', () => {
  const issue = (number: number, extra = {}) => ({
    id: `i${number}`,
    number,
    state: 'OPEN',
    synced: false,
    body: '',
    ...extra,
  })
  const item = (fields: Record<string, string>) => ({ id: 'item', contentId: 'i148', type: 'Issue', fields })
  const all = { Status: 'Idea', Kind: 'Idea', Iteration: 'Current' }

  it('adds an open idea that is off the board with all three fields', () => {
    expect(board.planIdeas([{ issue: issue(148), item: null }])).toEqual([
      { op: 'idea', issue: 148, contentId: 'i148', add: true, fields: all },
    ])
  })

  it('fills all three blank fields of an idea on the board, naming its item', () => {
    expect(board.planIdeas([{ issue: issue(148), item: item({}) }])).toEqual([
      { op: 'idea', issue: 148, contentId: 'i148', itemId: 'item', fields: all },
    ])
  })

  it('counts a field as blank when it is missing or empty', () => {
    const [op] = board.planIdeas([{ issue: issue(148), item: item({ Status: '', Kind: 'Idea' }) }])
    expect(op.fields).toEqual({ Status: 'Idea', Iteration: 'Current' })
  })

  it('writes only Status when the owner set Iteration and Kind', () => {
    const [op] = board.planIdeas([{ issue: issue(148), item: item({ Iteration: 'Future', Kind: 'Bug' }) }])
    expect(op.fields).toEqual({ Status: 'Idea' })
  })

  it('plans nothing for an idea whose three fields are set', () => {
    expect(
      board.planIdeas([{ issue: issue(148), item: item({ Status: 'Specified', Kind: 'Feature', Iteration: 'Current' }) }]),
    ).toEqual([])
  })

  it('plans nothing for a claimed idea', () => {
    expect(board.planIdeas([{ issue: issue(148), item: null }], new Set([148]))).toEqual([])
  })

  it('never carries a title, body, label or state', () => {
    const [op] = board.planIdeas([{ issue: issue(148, { body: 'secret', title: 'x' }), item: null }])
    expect(Object.keys(op).sort()).toEqual(['add', 'contentId', 'fields', 'issue', 'op'])
  })

  describe('closed and archived ideas', () => {
    const closed = issue(160, { state: 'CLOSED' })
    const open = issue(160)
    const archivedItem = (fields: Record<string, string>) => ({ ...item(fields), archived: true })

    it('archives a closed idea whose item is on the board, and nothing else', () => {
      expect(board.planIdeas([{ issue: closed, item: item({}) }])).toEqual([
        { op: 'idea', issue: 160, contentId: 'i160', itemId: 'item', archive: true },
      ])
    })

    it('archives a closed idea whatever its fields hold', () => {
      const [op] = board.planIdeas([{ issue: closed, item: item(all) }])
      expect(op.archive).toBe(true)
      expect(op.fields).toBeUndefined()
    })

    it('plans nothing for a closed idea that is off the board', () => {
      expect(board.planIdeas([{ issue: closed, item: null }])).toEqual([])
    })

    it('plans nothing for a closed idea whose item is archived', () => {
      expect(board.planIdeas([{ issue: closed, item: archivedItem({}) }])).toEqual([])
    })

    it('restores an open idea whose item is archived, filling only its blanks', () => {
      expect(board.planIdeas([{ issue: open, item: archivedItem({ Kind: 'Bug' }) }])).toEqual([
        {
          op: 'idea',
          issue: 160,
          contentId: 'i160',
          itemId: 'item',
          restore: true,
          fields: { Status: 'Idea', Iteration: 'Current' },
        },
      ])
    })

    it('restores alone when the archived item has all three fields', () => {
      expect(board.planIdeas([{ issue: open, item: archivedItem(all) }])).toEqual([
        { op: 'idea', issue: 160, contentId: 'i160', itemId: 'item', restore: true },
      ])
    })

    it('never changes the state of the issue', () => {
      const ops = board.planIdeas([
        { issue: closed, item: item({}) },
        { issue: open, item: archivedItem({}) },
      ])
      for (const op of ops) {
        expect(op).not.toHaveProperty('state')
      }
    })

    it('plans nothing for a claimed closed idea', () => {
      expect(board.planIdeas([{ issue: closed, item: item({}) }], new Set([160]))).toEqual([])
    })
  })
})

describe('claiming ideas', () => {
  const issue = (number: number, extra = {}) => ({
    id: `i${number}`,
    number,
    state: 'OPEN',
    synced: false,
    body: 'my idea',
    ...extra,
  })
  const card = (key: string, idea?: number, extra: Partial<Card> = {}): Card & { idea?: number } => ({
    key,
    aliases: [],
    title: `${key} title`,
    body: 'links',
    fields: { Key: key, Status: 'Specified', Kind: 'Feature', Feature: key, Phase: '' },
    ...(idea === undefined ? {} : { idea }),
    ...extra,
  })
  const ideaItem = (number: number, extra = {}) => ({
    id: `item-${number}`,
    type: 'Issue',
    contentId: `i${number}`,
    state: 'OPEN',
    parentId: null,
    title: 'my idea',
    body: 'my idea',
    fields: { Status: 'Idea', Kind: 'Idea', Iteration: 'Current' },
    ...extra,
  })
  const keyed = (key: string, number: number, extra = {}) => ({
    ...ideaItem(number),
    fields: { Key: key },
    ...extra,
  })

  describe('claimIdeas', () => {
    it('gives a card the idea item it names', () => {
      const item = ideaItem(148)
      const { claims, warnings } = board.claimIdeas([card('008-a', 148)], [issue(148)], [item])
      expect([...claims]).toEqual([['008-a', { issue: issue(148), item }]])
      expect(warnings).toEqual([])
    })

    it('gives an idea off the board the item an adopt writes against', () => {
      const { claims } = board.claimIdeas([card('008-a', 148)], [issue(148, { state: 'CLOSED' })], [])
      expect(claims.get('008-a').item).toEqual({
        type: 'Issue',
        contentId: 'i148',
        state: 'CLOSED',
        body: 'my idea',
        fields: {},
      })
    })

    it('claims nothing for a card that names no idea', () => {
      expect(board.claimIdeas([card('008-a')], [issue(148)], [ideaItem(148)])).toEqual({
        claims: new Map(),
        warnings: [],
      })
    })

    it.each([
      ['an issue that is not among the owner issues', [], []],
      ['a synced issue', [issue(148, { synced: true })], [ideaItem(148)]],
      ['an issue whose item carries a Key', [issue(148)], [keyed('007-z', 148)]],
    ])('claims nothing and warns for %s', (_name, issues, items) => {
      const { claims, warnings } = board.claimIdeas([card('008-a', 148)], issues, items)
      expect(claims.size).toBe(0)
      expect(warnings).toHaveLength(1)
      expect(warnings[0]).toContain('008-a')
      expect(warnings[0]).toContain('#148')
    })

    it('names the card that already holds the idea in the warning', () => {
      const { warnings } = board.claimIdeas([card('008-a', 148)], [issue(148)], [keyed('007-z', 148)])
      expect(warnings).toEqual(['008-a names #148, which 007-z already holds'])
    })

    it('lets the key that sorts first claim an idea two cards name, and warns for the other', () => {
      const { claims, warnings } = board.claimIdeas([card('009-b', 148), card('008-a', 148)], [issue(148)], [ideaItem(148)])
      expect([...claims.keys()]).toEqual(['008-a'])
      expect(warnings).toHaveLength(1)
      expect(warnings[0]).toContain('009-b')
      expect(warnings[0]).toContain('008-a')
      expect(warnings[0]).toContain('#148')
    })

    it('leaves a card that a Key or an alias matches without a claim', () => {
      const aliased = card('008-a', 148, { aliases: ['old/a'] })
      expect(board.claimIdeas([card('008-a', 148)], [issue(148)], [ideaItem(148), keyed('008-a', 200)]).claims.size).toBe(0)
      expect(board.claimIdeas([aliased], [issue(148)], [ideaItem(148), keyed('old/a', 200)]).claims.size).toBe(0)
    })

    it('never puts a title in a warning', () => {
      const { warnings } = board.claimIdeas([card('008-a', 148)], [], [])
      expect(warnings.join()).not.toContain('title')
    })
  })

  describe('a card matched by its Key or alias that names an idea', () => {
    const warned = (cardKey: string, item: unknown, issues: unknown[]) =>
      board.claimIdeas([card(cardKey, 148, { aliases: ['old/a'] })], issues, [item]).warnings

    it.each(['008-a', 'old/a'])('gives no warning when its item is the idea it names (%s)', (key) => {
      expect(warned('008-a', keyed(key, 148), [issue(148, { synced: true })])).toEqual([])
    })

    it.each(['008-a', 'old/a'])('warns once when its item is another issue (%s)', (key) => {
      const warnings = warned('008-a', keyed(key, 200), [issue(148, { synced: true }), issue(200)])
      expect(warnings).toHaveLength(1)
      expect(warnings[0]).toContain('008-a')
      expect(warnings[0]).toContain('#148')
    })

    it('warns when the number is missing from the owner-issue read', () => {
      expect(warned('008-a', keyed('008-a', 148), [])).toHaveLength(1)
    })

    it('uses the unarchived item over an archived duplicate listed first', () => {
      const archived = keyed('008-a', 300, { archived: true })
      const live = keyed('008-a', 148)
      expect(board.claimIdeas([card('008-a', 148)], [issue(148, { synced: true })], [archived, live]).warnings).toEqual([])
    })
  })

  describe('planChanges with a claim', () => {
    const claimed = (number: number, item: unknown, state = 'OPEN', status = 'Specified') => {
      const c = card('008-a', number, { fields: { ...card('008-a').fields, Status: status } })
      const claims = board.claimIdeas([c], [issue(number, { state })], item ? [item as object] : []).claims
      return { c, claims, ops: board.planChanges([c], item ? [item] : [], claims) }
    }

    it('updates the idea item with the Key, title, fields and a body that puts the synced block above the owner text', () => {
      const { ops, c } = claimed(148, ideaItem(148))
      expect(ops).toEqual([
        {
          op: 'update',
          itemId: 'item-148',
          type: 'Issue',
          contentId: 'i148',
          card: c,
          takes: 148,
          title: c.title,
          body: `${board.syncedBody('links')}\n\nmy idea`,
          fields: { Key: '008-a', Status: 'Specified', Kind: 'Feature', Feature: '008-a' },
        },
      ])
    })

    it('fills a blank Iteration on the idea item it takes, and keeps a set one', () => {
      const blank = { Status: 'Idea', Kind: 'Idea' }
      expect(claimed(148, ideaItem(148, { fields: blank })).ops[0].fields).toMatchObject({ Iteration: 'Current' })
      expect(claimed(148, ideaItem(148, { fields: { ...blank, Iteration: '' } })).ops[0].fields).toMatchObject({
        Iteration: 'Current',
      })
      expect(claimed(148, ideaItem(148, { fields: { ...blank, Iteration: 'Future' } })).ops[0].fields).not.toHaveProperty(
        'Iteration',
      )
    })

    it('fills a blank Iteration on an issue its Key matches, as after a failed claim, and keeps a set one', () => {
      const c = card('008-a', 148)
      const fields = { ...c.fields, Status: 'Specified' }
      const synced = { title: c.title, body: board.syncedBody('links') }
      const plan = (extra: object) => board.planChanges([c], [keyed('008-a', 148, { ...synced, ...extra })])
      expect(plan({ fields })).toMatchObject([{ op: 'update', fields: { Iteration: 'Current' } }])
      expect(plan({ fields: { ...fields, Iteration: 'Future' } })).toEqual([])
      expect(plan({ type: 'DraftIssue', contentId: null, fields })[0]?.fields?.Iteration).toBeUndefined()
    })

    it('unarchives an archived idea item', () => {
      const { ops } = claimed(148, ideaItem(148, { archived: true }))
      expect(ops).toHaveLength(1)
      expect(ops[0]).toMatchObject({ op: 'update', itemId: 'item-148', unarchive: true, takes: 148 })
      expect(ops[0].state).toBeUndefined()
    })

    it('reopens a closed idea whose card is not done, and leaves it closed for a done card', () => {
      expect(claimed(148, ideaItem(148, { state: 'CLOSED', archived: true }), 'CLOSED')).toMatchObject({
        ops: [{ unarchive: true, state: 'OPEN' }],
      })
      expect(claimed(148, ideaItem(148, { state: 'CLOSED' }), 'CLOSED', 'Done').ops[0].state).toBeUndefined()
    })

    it('plans no create for a claimed card', () => {
      for (const item of [ideaItem(148), ideaItem(148, { archived: true }), null]) {
        expect(claimed(148, item).ops.map((op: { op: string }) => op.op)).toEqual(['update'])
      }
    })

    it("updates the idea item with a bug card's Key, title, Kind Bug and stage, and plans no create", () => {
      const bug = card('bug/fix-it', 148, {
        title: 'Bug · Fix it',
        fields: { Key: 'bug/fix-it', Status: 'Building', Kind: 'Bug', Feature: '', Phase: '' },
      })
      const item = ideaItem(148)
      const { claims } = board.claimIdeas([bug], [issue(148)], [item])
      const ops = board.planChanges([bug], [item], claims)
      expect(ops.map((op: { op: string }) => op.op)).toEqual(['update'])
      expect(ops[0]).toMatchObject({
        itemId: 'item-148',
        takes: 148,
        title: 'Bug · Fix it',
        fields: { Key: 'bug/fix-it', Status: 'Building', Kind: 'Bug' },
      })
    })

    it('lets the spec take a Key-less idea a bug also names, and warns for the bug', () => {
      const spec = card('008-a', 148)
      const bug = card('bug/fix-it', 148, {
        fields: { Key: 'bug/fix-it', Status: 'Building', Kind: 'Bug', Feature: '', Phase: '' },
      })
      const item = ideaItem(148)
      const { claims, warnings } = board.claimIdeas([bug, spec], [issue(148)], [item])
      expect([...claims.keys()]).toEqual(['008-a'])
      expect(warnings).toHaveLength(1)
      expect(warnings[0]).toContain('bug/fix-it')
      expect(warnings[0]).toContain('008-a')
      expect(warnings[0]).toContain('#148')
    })

    it('matches the plan without claims', () => {
      const c = card('008-a', 148)
      expect(board.planChanges([c], [])).toEqual(board.planChanges([c], [], new Map()))
      expect(board.planChanges([c], [])[0].op).toBe('create')
    })

    it('adopts an idea off the board, with the add, Iteration Current and the synced block above the owner text', () => {
      const { ops, c } = claimed(148, null)
      expect(ops).toEqual([
        {
          op: 'update',
          itemId: undefined,
          type: 'Issue',
          contentId: 'i148',
          card: c,
          takes: 148,
          adopt: true,
          title: c.title,
          body: `${board.syncedBody('links')}\n\nmy idea`,
          fields: {
            Key: '008-a',
            Status: 'Specified',
            Kind: 'Feature',
            Feature: '008-a',
            Iteration: 'Current',
          },
        },
      ])
    })

    it('reopens a closed idea off the board unless the card is done', () => {
      expect(claimed(148, null, 'CLOSED').ops[0].state).toBe('OPEN')
      expect(claimed(148, null, 'CLOSED', 'Done').ops[0].state).toBeUndefined()
      expect(claimed(148, null, 'OPEN').ops[0].state).toBeUndefined()
    })

    it('gives an idea with no body the synced block alone', () => {
      const c = card('008-a', 148)
      const claims = board.claimIdeas([c], [issue(148, { body: '' })], []).claims
      expect(board.planChanges([c], [], claims)[0].body).toBe(board.syncedBody('links'))
    })

    it('leaves a claimed idea out of planIdeas and prints a takeover', () => {
      const ideas = board.ownerIdeas([issue(148)], [])
      expect(board.planIdeas(ideas, new Set([148]))).toEqual([])
      expect(board.describeOp({ op: 'update', card: card('008-a'), unarchive: true, state: 'OPEN', takes: 148 })).toBe(
        'update 008-a: reopen, takes over #148',
      )
    })
  })
})

describe('describing an operation', () => {
  it('prints an archive and a restore line', () => {
    expect(board.describeOp({ op: 'idea', issue: 160, archive: true })).toBe('idea #160: archive')
    expect(board.describeOp({ op: 'idea', issue: 160, restore: true })).toBe('idea #160: restore')
    expect(board.describeOp({ op: 'idea', issue: 160, restore: true, fields: { Status: 'Idea' } })).toBe(
      'idea #160: restore, Status=Idea',
    )
  })

  const card = { key: '010-a', fields: { Status: 'Building' } }

  it('prints a create line', () => {
    expect(board.describeOp({ op: 'create', card, as: 'issue' })).toBe('create 010-a as issue: Building')
    expect(board.describeOp({ op: 'create', card, as: 'issue', parent: '001-x' })).toBe(
      'create 010-a as issue: Building, under 001-x',
    )
  })

  it('prints an update line', () => {
    expect(
      board.describeOp({
        op: 'update',
        card,
        convert: true,
        title: 'a',
        body: 'b',
        fields: { Status: 'Done', Phase: '' },
        state: 'CLOSED',
        parent: '001-x',
      }),
    ).toBe('update 010-a: to an issue, title, body, Status=Done, Phase=(empty), close, under 001-x')
    expect(board.describeOp({ op: 'update', card, state: 'OPEN' })).toBe('update 010-a: reopen')
  })

  it('prints an add and a fill by number and fields only', () => {
    const fields = { Status: 'Idea', Kind: 'Idea', Iteration: 'Current' }
    expect(board.describeOp({ op: 'idea', issue: 148, contentId: 'i', add: true, fields })).toBe(
      'idea #148: add, Status=Idea, Kind=Idea, Iteration=Current',
    )
    expect(board.describeOp({ op: 'idea', issue: 148, contentId: 'i', itemId: 'x', fields })).toBe(
      'idea #148: fill, Status=Idea, Kind=Idea, Iteration=Current',
    )
    expect(board.describeOp({ op: 'idea', issue: 148, contentId: 'i', itemId: 'x', fields: { Status: 'Idea' } })).toBe(
      'idea #148: fill, Status=Idea',
    )
  })

  it('prints neither title nor body of an idea', () => {
    const line = board.describeOp({
      op: 'idea',
      issue: 148,
      contentId: 'i',
      add: true,
      title: '::error::hijack',
      body: '::error::hijack',
      fields: { Status: 'Idea' },
    })
    expect(line).not.toContain('hijack')
    expect(line).not.toContain('::')
  })
})

describe('building at pickup', () => {
  const label = { id: 'L1', name: 'building' }
  const issue = (labels?: { id: string; name: string }[], extra = {}) => ({ type: 'Issue', state: 'OPEN', labels, ...extra })

  it('names the label building', () => {
    expect(board.BUILDING_LABEL).toBe('building')
  })

  it('lifts a labeled Specified card to Building and removes no label', () => {
    expect(board.pickupStage('Specified', issue([label]))).toEqual({ stage: 'Building' })
  })

  it.each([
    ['no label', []],
    ['another label', [{ id: 'L2', name: 'bug' }]],
    ['a label named Building', [{ id: 'L3', name: 'Building' }]],
  ])('leaves a Specified card with %s at Specified', (_name, labels) => {
    expect(board.pickupStage('Specified', issue(labels))).toEqual({ stage: 'Specified' })
  })

  it.each(['Building', 'In review', 'Done'])('removes the label from a labeled %s card', (stage) => {
    expect(board.pickupStage(stage, issue([label]))).toEqual({ stage, unlabel: 'L1' })
  })

  it('keeps the label on a labeled Idea card, which is not past Specified', () => {
    expect(board.pickupStage('Idea', issue([label]))).toEqual({ stage: 'Idea' })
  })

  it.each(['Building', 'In review', 'Done'])('removes nothing from an unlabeled %s card', (stage) => {
    expect(board.pickupStage(stage, issue([]))).toEqual({ stage })
  })

  it('leaves a draft and an item with no labels alone', () => {
    expect(board.pickupStage('Specified', { type: 'DraftIssue', labels: [label] })).toEqual({ stage: 'Specified' })
    expect(board.pickupStage('Specified', issue(undefined))).toEqual({ stage: 'Specified' })
  })

  it('ignores the issue state', () => {
    expect(board.pickupStage('Specified', issue([label], { state: 'CLOSED' }))).toEqual({ stage: 'Building' })
    expect(board.pickupStage('Done', issue([label], { state: 'CLOSED' }))).toEqual({ stage: 'Done', unlabel: 'L1' })
  })

  describe('in planned changes', () => {
    const card: Card = {
      key: '010-a',
      aliases: [],
      title: '010 · A',
      body: 'links',
      fields: { Key: '010-a', Status: 'Specified', Kind: 'Feature', Feature: '010 A', Phase: '' },
    }
    const at = (status: string): Card => ({ ...card, fields: { ...card.fields, Status: status } })
    const item = (status: string, extra = {}) => ({
      id: 'item',
      type: 'Issue',
      contentId: 'issue',
      state: 'OPEN',
      parentId: null,
      title: card.title,
      body: board.syncedBody(card.body),
      fields: { ...card.fields, Status: status, Iteration: 'Current' },
      labels: [label],
      ...extra,
    })
    const update = { op: 'update', itemId: 'item', type: 'Issue', contentId: 'issue' }

    it('writes Building for a labeled Specified card', () => {
      expect(board.planChanges([card], [item('Specified')])).toEqual([{ ...update, card, fields: { Status: 'Building' } }])
    })

    it('plans nothing for a labeled Specified card already at Building', () => {
      expect(board.planChanges([card], [item('Building')])).toEqual([])
    })

    it.each(['Building', 'In review', 'Done'])('removes the label from a %s card, beside its other change', (status) => {
      const derived = at(status)
      const state = status === 'Done' ? { state: 'CLOSED' } : {}
      expect(board.planChanges([derived], [item('Specified')])).toEqual([
        { ...update, card: derived, fields: { Status: status }, ...state, unlabel: 'L1' },
      ])
    })

    it('removes the label of a card with no other change', () => {
      expect(board.planChanges([at('Building')], [item('Building')])).toEqual([
        { ...update, card: at('Building'), unlabel: 'L1' },
      ])
    })

    it('plans nothing for an archived labeled item or a held card', () => {
      expect(board.planChanges([card], [item('Specified', { archived: true })])).toEqual([])
      expect(board.planChanges([{ ...at('Done'), held: true }], [item('Specified')])).toEqual([])
    })

    it('reads the label of a claimed idea item the same way', () => {
      const claimed = item('Specified', { archived: false })
      const claims = new Map([['010-a', { issue: { number: 148 }, item: claimed }]])
      const [op] = board.planChanges([at('In review')], [], claims)
      expect(op.unlabel).toBe('L1')
      expect(op.takes).toBe(148)
    })

    it('plans no label change for an owner idea that carries the label', () => {
      const idea = { id: 'item', contentId: 'i148', type: 'Issue', fields: {}, labels: [label] }
      const ideaIssue = { id: 'i148', number: 148, state: 'OPEN', synced: false, body: '' }
      const [op] = board.planIdeas([{ issue: ideaIssue, item: idea }])
      expect(op).toEqual({
        op: 'idea',
        issue: 148,
        contentId: 'i148',
        itemId: 'item',
        fields: { Status: 'Idea', Kind: 'Idea', Iteration: 'Current' },
      })
    })
  })

  it('prints unlabel building at the end of an update line, never a title', () => {
    const card = { key: '010-a', fields: { Status: 'Building' } }
    expect(board.describeOp({ op: 'update', card, fields: { Status: 'Building' }, unlabel: 'L1' })).toBe(
      'update 010-a: Status=Building, unlabel building',
    )
    expect(board.describeOp({ op: 'update', card, title: '::error::x', unlabel: 'L1' })).toBe(
      'update 010-a: title, unlabel building',
    )
  })
})

describe('the project board workflow', () => {
  const workflow = readFileSync(`${root}.github/workflows/project-board.yml`, 'utf8')
  const filter = [
    'github.event.issue.user.login == github.repository_owner',
    "contains(github.event.issue.body, '<!-- sync -->')",
  ]

  it('runs on opened, reopened, closed, labeled and unlabeled issues', () => {
    expect(workflow).toMatch(/^ {2}issues:\n {4}types: \[opened, reopened, closed, labeled, unlabeled\]$/m)
  })

  const labelEvent = "(github.event.action == 'labeled' || github.event.action == 'unlabeled')"
  const otherEvent = "github.event.action != 'labeled' && github.event.action != 'unlabeled'"
  const labelTest = `github.event.label.name == '${board.BUILDING_LABEL}'`
  const issueFilter = `(${filter[0]} && ((${otherEvent} && !${filter[1]}) || (${labelEvent} && ${labelTest})))`

  it('filters the job on the owner, the sync marker and the building label, for issue events only', () => {
    const job = workflow.match(/^ {4}if: (.+)$/m)?.[1] ?? ''
    expect(job).toContain(`(github.event_name != 'issues' || ${issueFilter})`)
  })

  it('filters the concurrency group the same way, skipping a filtered issue event only', () => {
    const group = workflow.match(/^ {2}group: \$\{\{ (.+) \}\}$/m)?.[1] ?? ''
    expect(group).toBe(
      `github.event_name == 'issues' && !${issueFilter} && format('project-board-skip-{0}', github.run_id) || 'project-board'`,
    )
  })

  it('never cancels a run in progress', () => {
    expect(workflow).toMatch(/^ {2}cancel-in-progress: false$/m)
  })

  it('keeps github.event out of every run line', () => {
    const runs = workflow.split('\n').filter((line) => /^\s*(- )?run:/.test(line))
    expect(runs.length).toBeGreaterThan(0)
    for (const line of runs) {
      expect(line).not.toContain('github.event')
    }
  })

  it('guards on the repository board.json names', () => {
    expect(workflow).toContain(`github.repository == '${board.config.owner}/${board.config.repo}'`)
  })
})

describe('idea.mjs rules', () => {
  const owner = board.config.owner

  it.each(['abc', '#148', '-1', '12a', ''])('rejects the argument %j', (arg) => {
    expect(board.ideaArgumentError(arg)).toBe(`${arg} is not an issue number`)
  })

  it('asks for a number when none is given', () => {
    expect(board.ideaArgumentError(undefined)).toBe('no issue number given')
  })

  it('accepts digits', () => {
    expect(board.ideaArgumentError('148')).toBeNull()
  })

  it('refuses an issue that does not exist', () => {
    expect(board.ideaRefusal(200, null)).toBe('#200 does not exist in this repository')
  })

  it('refuses another account before anything else', () => {
    expect(board.ideaRefusal(200, { author: 'someone', key: 'k', synced: true })).toBe('#200 was opened by another account')
  })

  it('refuses an issue with a Key before a synced body', () => {
    expect(board.ideaRefusal(149, { author: owner, key: '008-phone-idea-capture', synced: true })).toBe(
      '#149 already carries the Key 008-phone-idea-capture',
    )
  })

  it('refuses a card the sync made', () => {
    expect(board.ideaRefusal(7, { author: owner, key: null, synced: true })).toBe('#7 is a board card the sync made')
  })

  it("answers null for the owner's Key-less issue", () => {
    expect(board.ideaRefusal(148, { author: owner, key: null, synced: false })).toBeNull()
    expect(board.ideaRefusal(148, { author: owner, key: '', synced: false })).toBeNull()
  })

  it('refuses a viewer who is not the owner', () => {
    const message = board.ideaViewerError('twinright-bzs')
    expect(message).toContain('twinright-bzs')
    expect(board.ideaViewerError('someone-else')).toContain('someone-else')
  })

  it('accepts the owner as viewer', () => {
    expect(board.ideaViewerError(owner)).toBeNull()
  })

  it('reads no title or body in the gate query', () => {
    expect(board.IDEA_GATE_QUERY).toMatch(/\bauthor\b/)
    expect(board.IDEA_GATE_QUERY).toMatch(/\bprojectItems\(includeArchived: true[,)]/)
    expect(board.IDEA_GATE_QUERY).not.toMatch(/\btitle\b/)
    expect(board.IDEA_GATE_QUERY).not.toMatch(/\bbody\b/)
  })

  it('asks the gate query whether more board items follow', () => {
    expect(board.IDEA_GATE_QUERY).toMatch(/\bprojectItems\([^)]*\)\s*\{[\s\S]*\bpageInfo\s*\{\s*hasNextPage\s*\}/)
  })

  it('refuses an issue on more boards than the gate reads', () => {
    expect(board.ideaGateOverflow(148, { pageInfo: { hasNextPage: true }, nodes: [] })).toContain('#148')
    expect(board.ideaGateOverflow(148, { pageInfo: { hasNextPage: false }, nodes: [] })).toBeNull()
  })

  it('reads title, body and url in the text query', () => {
    for (const word of ['title', 'body', 'url']) {
      expect(board.IDEA_TEXT_QUERY).toMatch(new RegExp(`\\b${word}\\b`))
    }
  })
})

describe('pickup', () => {
  it('asks for a card key when none is given', () => {
    expect(board.pickupArgumentError(undefined)).toBe('no card key given')
  })

  it.each(['abc', '#152', 'bug/', 'fix/x', 'bug/X y', '009_script', ''])('refuses %j as a card key', (arg) => {
    expect(board.pickupArgumentError(arg)).toBe(`${arg} is not a card key`)
  })

  it.each(['009-script-hydration-cost', 'bug/imagesharp-3-2-0'])('accepts %s as a card key', (arg) => {
    expect(board.pickupArgumentError(arg)).toBeNull()
  })

  const card = (key: string, extra = {}) => ({
    type: 'Issue',
    state: 'OPEN',
    archived: false,
    number: 152,
    fields: { Key: key },
    ...extra,
  })

  it('answers with the number and repository of an open issue', () => {
    expect(board.pickupTarget('009-x', [card('009-x')])).toEqual({
      key: '009-x',
      number: 152,
      repository: 'Th3FenrisWolf/Kitchen-Command-Center',
    })
  })

  it('refuses a key no item carries', () => {
    expect(board.pickupTarget('011-example', [card('009-x')])).toEqual({
      key: '011-example',
      refused: 'no card on the board carries the Key 011-example',
    })
  })

  it('refuses a draft', () => {
    expect(board.pickupTarget('009-x', [card('009-x', { type: 'DraftIssue' })])).toEqual({
      key: '009-x',
      refused: "009-x's card is a draft, not an issue",
    })
  })

  it('refuses an item that is neither an issue nor a draft', () => {
    expect(board.pickupTarget('009-x', [card('009-x', { type: 'PullRequest' })])).toEqual({
      key: '009-x',
      refused: "009-x's card is not an issue",
    })
  })

  it('refuses an archived card', () => {
    expect(board.pickupTarget('009-x', [card('009-x', { archived: true })])).toEqual({
      key: '009-x',
      refused: "009-x's card is archived",
    })
  })

  it('refuses a closed issue', () => {
    expect(board.pickupTarget('009-x', [card('009-x', { state: 'CLOSED' })])).toEqual({
      key: '009-x',
      refused: "009-x's issue is closed",
    })
  })

  it('answers from the unarchived item when a key has both', () => {
    const items = [card('009-x', { archived: true, number: 1 }), card('009-x', { number: 2 })]
    expect(board.pickupTarget('009-x', items)).toMatchObject({ number: 2 })
    expect(board.pickupTarget('009-x', [...items].reverse())).toMatchObject({ number: 2 })
  })
})

describe.each([
  [
    'speckit-specify',
    [
      'Create or update the feature specification from a natural language description',
      'grilling the user on every open decision rather than guessing',
    ],
    '**Idea**: [#',
    'on its own line above the `**Input**:` line',
  ],
  [
    'speckit-bug-assess',
    ['Assess a bug report against the codebase', 'grilling the user on every open decision the report leaves'],
    '- **Idea**: [#',
    'as the bullet after `- **Source**:`',
  ],
])('the regenerated %s skill', (name, preText, ideaForm, placement) => {
  const skill = readFileSync(`${root}.claude/skills/${name}/SKILL.md`, 'utf8')
  const description = (skill.match(/^description: ([\s\S]*?)\n[a-z-]+:/m)?.[1] ?? '').replace(/\n\s+/g, ' ')

  it('comes from the kcc-ship preset', () => {
    expect(skill).toContain('source: preset:kcc-ship')
  })

  it('runs idea.mjs and writes the Idea line', () => {
    expect(skill).toContain('node tools/project-board/idea.mjs')
    expect(skill).toContain(ideaForm)
  })

  it('puts the Idea line ahead of any quoted issue text', () => {
    expect(skill.replace(/\s+/g, ' ')).toContain(placement)
  })

  it('keeps the description it had before the idea input', () => {
    for (const part of preText) expect(description).toContain(part)
    expect(description).toContain('#N')
  })

  it('puts the composed text in place of the typed arguments', () => {
    expect(skill).toMatch(/the composed (description|report) replaces `\$ARGUMENTS`/)
  })

  it('composes the folder name itself, never from the issue text', () => {
    const prose = skill.replace(/\s+/g, ' ')
    expect(prose).toContain('in lowercase letters, digits and hyphens only')
    expect(prose).toContain('never copy issue text into it')
  })

  it('keeps the grilling protocol', () => {
    expect(skill).toContain('## Question protocol')
  })
})

describe.each([
  ['speckit-card-implement', '## Close out through ship-branch', 'node tools/project-board/pickup.mjs'],
  ['speckit-bug-fix', '## Publish through ship-branch', 'node tools/project-board/pickup.mjs bug/'],
])('the regenerated %s skill at pickup', (name, laterHeading, call) => {
  const skill = readFileSync(`${root}.claude/skills/${name}/SKILL.md`, 'utf8')
  const prose = skill.replace(/\s+/g, ' ')

  it('comes from the kcc-ship preset', () => {
    expect(skill).toContain('source: preset:kcc-ship')
  })

  it('has a Building at pickup section before its ship-branch section', () => {
    const pickup = skill.indexOf('## Building at pickup')
    expect(pickup).toBeGreaterThan(-1)
    expect(pickup).toBeLessThan(skill.indexOf(laterHeading))
  })

  it('runs pickup.mjs and adds the building label', () => {
    expect(prose).toContain(call)
    expect(prose).toMatch(/gh issue edit .*--add-label building/)
  })
})

describe('the specs in this repository', () => {
  const dirs = readdirSync(`${root}specs`, { withFileTypes: true })
    .filter((entry) => entry.isDirectory())
    .map((entry) => entry.name)

  it.each(dirs)('%s links its Status line to its board card', (dir) => {
    const spec = readFileSync(`${root}specs/${dir}/spec.md`, 'utf8')
    expect(spec).toContain(`**Status**: ${board.statusLink(dir)}`)
  })

  it('has the template write the same link', () => {
    const template = readFileSync(`${root}.specify/templates/spec-template.md`, 'utf8')
    expect(template).toContain(`**Status**: ${board.statusLink('[###-feature-name]')}`)
  })

  it.each(dirs)('%s names itself in its phases table, if it has one', (dir) => {
    const number = dir.slice(0, 3)
    const rows: { spec: string | null }[] = board.parsePhases(readFileSync(`${root}specs/${dir}/spec.md`, 'utf8'))
    expect(rows.length === 0 || rows.some((row) => row.spec === number)).toBe(true)
  })

  it('gives both phased specs a phases table', () => {
    for (const dir of ['001-photo-recipe-import', '006-performance-sweep']) {
      expect(board.parsePhases(readFileSync(`${root}specs/${dir}/spec.md`, 'utf8')).length).toBeGreaterThan(1)
    }
  })
})
