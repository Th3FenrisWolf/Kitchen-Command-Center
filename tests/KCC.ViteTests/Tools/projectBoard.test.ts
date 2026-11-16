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

function bugSnapshot(slug: string, ref: string, { fix = false, title = 'Something breaks' } = {}) {
  return {
    slug,
    ref,
    files: ['assessment.md', ...(fix ? ['fix.md'] : [])],
    assessment: `# Bug Assessment: ${title}\n`,
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
    const open = derive({ specs: [specSnapshot('010-a', '010-a')], pulls: pr('OPEN') })
    const merged = derive({ specs: [specSnapshot('010-a', 'main')], pulls: pr('MERGED') })
    const closed = derive({ specs: [specSnapshot('010-a', 'main')], pulls: pr('CLOSED') })
    expect(open.get('010-a')?.fields.Status).toBe('In review')
    expect(open.get('010-a')?.body).toContain('[#7](https://pr/7)')
    expect(merged.get('010-a')?.fields.Status).toBe('Done')
    expect(closed.get('010-a')?.fields.Status).toBe('Specified')
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
    fields: { ...card.fields, ...fields },
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
