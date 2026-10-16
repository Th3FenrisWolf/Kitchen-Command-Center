# Contract: the pile's markup and classes

The interface this feature exposes is the kit itself: the markup a Vue or Razor author writes, the `KccSheet` props,
and the classes the kit.md Classes table lists. `kitClasses.test.ts` holds the table and the CSS to each other.

## A piled sheet (Razor, or a hand-written Vue slip)

```html
<div class="kcc-slip kcc-tear-3">
  <div class="kcc-torn">
    <div class="kcc-sheet">…</div>
  </div>
  <span class="kcc-label">…</span>
  <span class="kcc-blank" aria-hidden="true"><span></span></span>
</div>
```

- The blank is the **last** child of the slip and appears once.
- It takes no tear class. Its tear and base direction come from the slip's tear class; a flip on the slip
  (`kcc-slip--flip`) or a region factor on a container (`--blank-region`) turns it the other way (see "Direction").
- The slip is `height: fit-content` (or `100%` with `kcc-slip--fill`), so the blank is always as tall as its sheet,
  even in a grid that stretches its items.
- It carries no `kcc-torn`, `kcc-sheet` or `kcc-slip` class.
- A one-element slip (`kcc-slip kcc-torn` on one element) cannot hold a blank. The slip and the torn wrapper are two
  elements.

## A sheet that lifts

```html
<article class="kcc-slip kcc-slip--fill kcc-slip--lift kcc-recipe kcc-tear-2">
  <a class="kcc-torn block" href="…"><div class="kcc-sheet">…</div></a>
  <div class="kcc-tilewrap">
    <div class="kcc-torn"><div class="kcc-tile kcc-tear-tile-1" aria-hidden="true">…</div></div>
    <span class="kcc-tape" aria-hidden="true"></span>
  </div>
  <span class="kcc-blank" aria-hidden="true"><span></span></span>
</article>
```

`kcc-slip--lift` replaces the `transition-transform hover:-translate-y-1 focus-within:-translate-y-1` utilities.
Every child but the blank lifts 4px. The tile and its tape lift with the sheet. The lift's transition lists
`translate` and also `outline-color 0.2s, outline-offset 0.2s`, so a link that is the slip's child keeps the focus
ring's fade.

## Direction

A page's blanks alternate in reading order: the visible sheets in `main`, in document order. The generated
`--blank-dir` alternates inside any list that takes `listTearFor(index)`. Across a page:

- `kcc-slip--flip` on a slip (`KccSheet` `flip`) turns that blank the other way. Set it on a fixed sheet that would
  match its neighbour, and compute it for a sheet that follows a run of varying length.
- `[--blank-region:-1]` on a container, usually behind a breakpoint variant (`max-lg:`), turns every blank inside it
  the other way, for a run whose neighbour a breakpoint hides.

Research R12 lists every page's factors.

## An applied tile inside a sheet

```html
<div class="kcc-sheet flex items-center gap-4" style="--pad: 16px">
  <div class="kcc-tilewrap kcc-tilewrap--flow">
    <div class="kcc-torn"><div class="kcc-tile kcc-tear-tile-2" aria-hidden="true">…</div></div>
    <span class="kcc-tape" aria-hidden="true"></span>
  </div>
  …
</div>
```

`kcc-tilewrap--flow` keeps the tile in the row's flow (`position: relative`, no offset, no turn) and gives it its strip.
The recipe list row uses it. It is the one tilewrap allowed inside a sheet, inside the clip and the filter; every
other tilewrap, label and tape stays a sibling of `.kcc-torn`.

## A wide sheet

```html
<div class="kcc-slip kcc-slip--wide kcc-recipe kcc-tear-hero">…<span class="kcc-blank" aria-hidden="true"><span></span></span></div>
```

`kcc-slip--wide` turns the blank 0.5° and scales it 1.005. It goes on every hero-torn sheet, on the header pad's page,
and on every sheet wider than 700px at a 1280px viewport (the list is in research R5). A new surface that wide takes
it too: `wide` on `KccSheet`, or the class on a hand-written slip.

## Tape

`kcc-tape` is styled only as a direct child of `kcc-tilewrap`. Anywhere else it draws nothing, and `pile.test.ts`
fails the build.

## `KccSheet.vue`

| Prop | Change |
|---|---|
| `tape` | Removed |
| `piled` | New, `boolean`, default `true`. `false` renders no blank. Only `PadCard.vue` passes `false` |
| `tear` | Unchanged. `'hero'` also adds `kcc-slip--wide` |
| `wide` | New, `boolean`, default `false`. Adds `kcc-slip--wide`. Set on every `KccSheet` surface wider than 700px |
| `flip` | New, `boolean`, default `false`. Adds `kcc-slip--flip` |

Every other prop, slot and attribute passes through as today. The blank renders after the label slot.

## Classes table entries (kit.md)

| Class | What | Notes |
|---|---|---|
| `kcc-blank` › `span` | the blank sheet under a sheet: torn paper with the fibre and fall, no ruling | last child of a slip; `aria-hidden`; tear and base direction from the slip's tear class; turns `--blank-turn` × direction × `--blank-flip` × `--blank-region` and scales `--blank-scale`; the span is `display: block`; stays still when the slip lifts |
| `kcc-slip--wide` | the wide pair: the blank turns 0.5° and scales 1.005 | hero-torn sheets, the pad's page, any sheet wider than 700px |
| `kcc-slip--lift` | a slip you can open lifts its top sheet 4px on hover or focus within; the blank stays | no lift under reduced motion; keeps the focus ring's fade |
| `kcc-slip--flip` | turns the slip's blank the other way | set where a page's reading order needs it |
| `kcc-tilewrap--flow` | a tilewrap in the flow of a sheet's content | a list row's tile and its strip |
| `kcc-tape` | one strip over an applied tile | only inside `kcc-tilewrap`; a sheet is never taped |

## The page

`#app` takes `overflow-x: clip`, so a blank's overhang past a tall sheet's side never scrolls the page.

## The header pad's page

```html
<div class="pad-page kcc-slip kcc-slip--fill kcc-slip--wide kcc-tear-hero-2" aria-hidden="true">
  <div class="kcc-torn"><div class="kcc-sheet"></div></div>
</div>
```

The page is the pad's blank, so it holds no `.kcc-blank`. It keeps `kcc-slip--fill`: `.pad-page` sets `inset: 0` and
no height, and without `--fill` the slip's `height: fit-content` would shrink it to its padding.
