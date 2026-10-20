using KCC.E2ETests.Config;
using Microsoft.Playwright;

namespace KCC.E2ETests.Features.Chrome;

public class RuleTests : BasePageTests
{
    private const string Search = "/recipes/";

    private const string Recipe = "/recipes/spicy-ramen-flight/";

    private const string Variant = "/recipes/spicy-ramen-flight/chili-oil-shoyu";

    private const string CreateRecipe = "/recipes/create-recipe";

    private const string Home = "/";

    private const string Login = "/account/login";

    private const string Account = "/account";

    private const string Settings = "/account/settings";

    private const string NotFound = "/this-page-does-not-exist";

    // A method step with a timer, so the readout is on the sheet, and servings, so the scaler is.
    private const string CookVariant = "/recipes/cold-brew-concentrate/12-hour-steep";

    private const string CookMode = "cook-mode";

    private const string SearchList = "search-list";

    private const string Main = "main";

    // Not read as text block by block: a field. Its input or select is read on its own, through its baseline, and
    // so are the marks beside it, the stepper's unit and the pad's key hint.
    private const string Unmeasured = ".kcc-field";

    // Each text block's first line is read through a zero-size inline-block placed before its first text: the
    // marker's top is that line's baseline. A text node that is a flex or grid container's own child is wrapped
    // first, or the marker would become an item of its own. Every transform and rotate inside a slip is lifted
    // for the read, so the slips' tilt does not skew it; translate, which the shim and the controls use, stays.
    // Text folded away inside an overflow of no height, such as a closed drawer, is not read. With lasts, each
    // block's last line is read too, through a marker split in after its last visible character: a marker after
    // trailing white space could break onto a line of its own.
    private const string ReadBaselines = """
        ({ scope, labels, skip, lasts }) => {
          const lift = document.createElement('style')
          lift.textContent = '.kcc-slip, .kcc-slip * { transform: none !important; rotate: none !important; transition: none !important; }'
          document.head.append(lift)
          try {
            const length = (sheet, value) => {
              const probe = document.createElement('div')
              probe.style.cssText = `position: absolute; visibility: hidden; height: ${value}`
              sheet.append(probe)
              const height = probe.getBoundingClientRect().height
              probe.remove()
              return height
            }
            const describe = (element) =>
              `${element.tagName.toLowerCase()}${[...element.classList].map((name) => `.${name}`).join('')} "${element.textContent.replace(/\s+/g, ' ').trim().slice(0, 40)}"`
            const flowsThrough = (element) => ['inline', 'contents'].includes(getComputedStyle(element).display)
            const blockOf = (text) => {
              let element = text.parentElement
              while (element && flowsThrough(element)) element = element.parentElement
              return element
            }
            const ruled = (sheet) =>
              sheet.checkVisibility() && !sheet.classList.contains('kcc-sheet--blank') && getComputedStyle(sheet).backgroundImage.includes('gradient')
            const clippedAway = (element) => {
              for (; element && !element.classList.contains('kcc-sheet'); element = element.parentElement)
                if (getComputedStyle(element).overflow !== 'visible' && element.getBoundingClientRect().height === 0) return true
              return false
            }
            const texts = (root) => {
              const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT)
              const found = []
              while (walker.nextNode()) {
                const text = walker.currentNode
                const parent = text.parentElement
                if (!text.data.trim() || !parent) continue
                if (parent.closest('script, style, template, textarea, select, option, svg, .sr-only')) continue
                if (!parent.checkVisibility({ visibilityProperty: true }) || clippedAway(parent)) continue
                found.push(text)
              }
              return found
            }
            const baselineOf = (text) => {
              const marker = document.createElement('span')
              marker.style.cssText = 'display: inline-block; width: 0; height: 0; margin: 0; padding: 0; border: 0; vertical-align: baseline'
              const parent = text.parentElement
              let wrap = null
              if (/flex|grid/.test(getComputedStyle(parent).display)) {
                wrap = document.createElement('span')
                text.before(wrap)
                wrap.append(marker, text)
              } else {
                text.before(marker)
              }
              const top = marker.getBoundingClientRect().top
              marker.remove()
              if (wrap) {
                wrap.before(text)
                wrap.remove()
              }
              return top
            }
            const lastBaselineOf = (text) => {
              const end = text.data.trimEnd().length
              const tail = text.splitText(end)
              const marker = document.createElement('span')
              marker.style.cssText = 'display: inline-block; width: 0; height: 0; margin: 0; padding: 0; border: 0; vertical-align: baseline'
              const parent = text.parentElement
              let wrap = null
              if (/flex|grid/.test(getComputedStyle(parent).display)) {
                wrap = document.createElement('span')
                text.before(wrap)
                wrap.append(text, marker)
              } else {
                text.after(marker)
              }
              const top = marker.getBoundingClientRect().top
              marker.remove()
              if (wrap) {
                wrap.before(text)
                wrap.remove()
              }
              text.appendData(tail.data)
              tail.remove()
              return top
            }
            // An input's baseline is its text's, so a marker set beside it on one line, in a wrapper with no strut
            // of its own, tops out at that baseline. The wrapper takes the input's place in the field's layout. The
            // marker sits outside the input, so the input's own translate, which the shim uses, is added back.
            const inputBaselineOf = (input) => {
              const [, shift = '0'] = getComputedStyle(input).translate.split(' ')
              const wrap = document.createElement('span')
              wrap.style.cssText = 'display: block; line-height: 0; font-size: 0; margin: 0; padding: 0; border: 0'
              const marker = document.createElement('span')
              marker.style.cssText = 'display: inline-block; width: 0; height: 0; margin: 0; padding: 0; border: 0; vertical-align: baseline'
              input.before(wrap)
              wrap.append(marker, input)
              const top = marker.getBoundingClientRect().top
              wrap.before(input)
              wrap.remove()
              return top + (parseFloat(shift) || 0)
            }
            const offRuleAt = (sheet, baseline) => {
              const style = getComputedStyle(sheet)
              const box = sheet.getBoundingClientRect()
              const contentTop = box.top + parseFloat(style.borderTopWidth) + parseFloat(style.paddingTop)
              const bl = length(sheet, 'var(--bl)')
              const drop = length(sheet, 'var(--rule-drop)')
              const offset = baseline - contentTop - drop
              const rest = ((offset % bl) + bl) % bl
              return rest <= bl / 2 ? rest : rest - bl
            }
            const offRule = (sheet, text) => offRuleAt(sheet, baselineOf(text))
            const lastTextOf = (block) => texts(block).filter((text) => blockOf(text) === block).at(-1)

            const faults = []
            let measured = 0
            let fields = 0
            const measure = (block, text) => {
              const sheet = block.closest('.kcc-sheet')
              measured++
              const off = offRule(sheet, text)
              if (Math.abs(off) > 0.75) {
                const within = []
                for (let element = block.parentElement; element && element !== sheet; element = element.parentElement)
                  within.push([element.tagName.toLowerCase(), ...element.classList].join('.'))
                faults.push(`${describe(block)}: baseline ${off.toFixed(2)}px off the rule, in ${within.join(' < ') || 'the sheet'}`)
              }
              const last = lasts && lastTextOf(block)
              if (last) {
                measured++
                const lastOff = offRuleAt(sheet, lastBaselineOf(last))
                if (Math.abs(lastOff) > 0.75)
                  faults.push(`${describe(block)}: last line's baseline ${lastOff.toFixed(2)}px off the rule`)
              }
            }
            const controls = []
            const measureInput = (sheet, input) => {
              measured++
              fields++
              controls.push(input)
              const off = offRuleAt(sheet, inputBaselineOf(input))
              if (Math.abs(off) > 0.75)
                faults.push(`${describe(input)} [${input.value || input.placeholder || input.selectedOptions?.[0]?.text}]: baseline ${off.toFixed(2)}px off the rule, in ${[...input.closest('.kcc-field').classList].join('.')}`)
            }
            // The lowest few pixels of a field's hit area still reach its control, so the whole 36px takes a click.
            const reachesItsControl = (control) => {
              control.scrollIntoView({ block: 'center' })
              const field = control.closest('.kcc-field').getBoundingClientRect()
              const box = control.getBoundingClientRect()
              const hit = document.elementFromPoint(box.left + box.width / 2, field.bottom - 3)
              if (!hit || !(hit === control || control.contains(hit)))
                faults.push(`${describe(control)}: the foot of its field's hit area reaches ${hit ? describe(hit) : 'nothing'}, not the control`)
            }
            const writable = (input) =>
              !['checkbox', 'radio', 'range', 'file', 'hidden'].includes(input.type) &&
              input.checkVisibility({ visibilityProperty: true }) &&
              !input.closest('.sr-only') &&
              !clippedAway(input)
            const blocksIn = (root, keep) => {
              const blocks = new Map()
              for (const text of texts(root)) {
                const block = blockOf(text)
                if (block && keep(block, text) && !blocks.has(block)) blocks.set(block, text)
              }
              return blocks
            }

            if (labels) {
              for (const { selector, text: wanted } of labels) {
                const label = [...document.querySelectorAll(`${scope} ${selector}`)].find(
                  (element) => element.checkVisibility() && (!wanted || element.textContent.trim().toLowerCase() === wanted),
                )
                if (!label) {
                  faults.push(`no visible ${selector}${wanted ? ` "${wanted}"` : ''} in ${scope}`)
                  continue
                }
                const sheet = label.closest('.kcc-sheet')
                if (!sheet || !ruled(sheet)) {
                  faults.push(`${describe(label)} is not on a ruled sheet`)
                  continue
                }
                const row = label.closest('.kcc-row') ?? label.parentElement
                for (const [block, text] of blocksIn(label, () => true)) measure(block, text)
                for (const control of row.querySelectorAll('.kcc-btn, .kcc-seg')) {
                  for (const [block, text] of blocksIn(control, () => true)) measure(block, text)
                }
              }
            } else {
              for (const sheet of document.querySelectorAll(`${scope} .kcc-sheet`)) {
                if (!ruled(sheet)) continue
                const own = blocksIn(sheet, (block) => block.closest('.kcc-sheet') === sheet && !block.closest(skip))
                for (const [block, text] of own) measure(block, text)
                for (const input of sheet.querySelectorAll('.kcc-field input, .kcc-field select')) {
                  if (input.closest('.kcc-sheet') === sheet && writable(input)) measureInput(sheet, input)
                }
                for (const mark of sheet.querySelectorAll('.kcc-field :is(.kcc-unit, .pad-key)')) {
                  if (mark.closest('.kcc-sheet') !== sheet || !mark.checkVisibility({ visibilityProperty: true })) continue
                  for (const [block, text] of blocksIn(mark, () => true)) measure(block, text)
                }
              }
              for (const control of controls) reachesItsControl(control)
            }

            return { measured, fields, faults }
          } finally {
            lift.remove()
          }
        }
        """;

    private const string ReadTitleAndAction = """
        () => {
          const lift = document.createElement('style')
          lift.textContent = '.kcc-slip, .kcc-slip * { transform: none !important; rotate: none !important; transition: none !important; }'
          document.head.append(lift)
          try {
            const textsOf = (root) => {
              const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT)
              const found = []
              while (walker.nextNode()) if (walker.currentNode.data.trim()) found.push(walker.currentNode)
              return found
            }
            const markAt = (place) => {
              const marker = document.createElement('span')
              marker.style.cssText = 'display: inline-block; width: 0; height: 0; vertical-align: baseline'
              place(marker)
              const top = marker.getBoundingClientRect().top
              marker.remove()
              return top
            }
            const title = document.querySelector('main h1.kcc-display')
            const action = title.parentElement.querySelector('.kcc-btn')
            const titleTexts = textsOf(title)
            const probe = document.createElement('div')
            probe.style.cssText = 'position: absolute; visibility: hidden; height: var(--bl)'
            title.append(probe)
            const bl = probe.getBoundingClientRect().height
            probe.remove()
            return {
              titleFirst: markAt((marker) => titleTexts[0].before(marker)),
              titleLast: markAt((marker) => titleTexts.at(-1).after(marker)),
              action: markAt((marker) => textsOf(action)[0].before(marker)),
              wrapped: action.getBoundingClientRect().top >= title.getBoundingClientRect().bottom - 0.5,
              bl,
            }
          } finally {
            lift.remove()
          }
        }
        """;

    [Test]
    [NotInParallel(MemberSession.Serial)]
    [Arguments(1280, Search)]
    [Arguments(1280, Settings)]
    [Arguments(1280, "add-variant")]
    [Arguments(375, Search)]
    [Arguments(375, Settings)]
    [Arguments(375, "add-variant")]
    public async Task AHeadingsAction_SitsOnTheTitlesBaselineRule_OrOneRuleUnderIt(int width, string page)
    {
        if (page == "add-variant")
        {
            _ = await OpenWizardAsync(width, page);
        }
        else
        {
            await Page.SetViewportSizeAsync(width, 900);
            if (page == Settings)
            {
                await MemberSession.SignInAsync(Page);
            }

            _ = await OpenAsync(width, page);
        }

        await Page.EvaluateAsync("() => document.fonts.ready");
        var read = await Page.EvaluateAsync<TitleAndAction>(ReadTitleAndAction);

        if (width >= 1280)
        {
            _ = await Assert.That(read.Wrapped).IsFalse();
            _ = await Assert.That(Math.Abs(read.Action - read.TitleFirst)).IsLessThanOrEqualTo(0.75);
        }
        else
        {
            // Wrapped, the action's text keeps one empty rule between it and the title's last line.
            _ = await Assert.That(read.Wrapped).IsTrue();
            _ = await Assert.That(Math.Abs(read.Action - read.TitleLast - (2 * read.Bl))).IsLessThanOrEqualTo(0.75);
        }
    }

    private const string ReadPadBar = """
        () => {
          const lift = document.createElement('style')
          lift.textContent = '.kcc-slip, .kcc-slip * { transform: none !important; rotate: none !important; transition: none !important; }'
          document.head.append(lift)
          try {
            const bar = [...document.querySelectorAll('.pad-bar')].find((element) => element.checkVisibility())
            if (!bar) return ['no visible pad bar']
            const outer = bar.getBoundingClientRect()
            const paper = bar.closest('.kcc-sheet').getBoundingClientRect()
            const faults = []
            let controls = 0
            for (const control of bar.querySelectorAll('.kcc-btn, .kcc-field, .pad-menu, .pad-glyph, .pad-logo')) {
              if (!control.checkVisibility()) continue
              controls++
              const box = control.getBoundingClientRect()
              const name = `${[...control.classList].join('.')} "${control.textContent.replace(/\s+/g, ' ').trim().slice(0, 30)}"`
              if (box.left < outer.left - 0.5 || box.right > outer.right + 0.5)
                faults.push(`${name}: ${box.left.toFixed(1)}-${box.right.toFixed(1)} outside the bar's ${outer.left.toFixed(1)}-${outer.right.toFixed(1)}`)
              if (box.top < paper.top - 0.5 || box.bottom > paper.bottom + 0.5)
                faults.push(`${name}: ${box.top.toFixed(1)}-${box.bottom.toFixed(1)} off the sheet's ${paper.top.toFixed(1)}-${paper.bottom.toFixed(1)}`)
            }
            if (controls === 0) faults.push('no visible control in the pad bar')
            if (bar.scrollWidth > bar.clientWidth) faults.push(`the bar scrolls ${bar.scrollWidth - bar.clientWidth}px sideways`)
            for (const input of bar.querySelectorAll('.kcc-field input')) {
              if (input.checkVisibility() && input.scrollWidth > input.clientWidth) faults.push(`the search field cuts its placeholder "${input.placeholder}"`)
            }
            return faults
          } finally {
            lift.remove()
          }
        }
        """;

    [Test]
    [NotInParallel(MemberSession.Serial)]
    [Arguments(1024, false)]
    [Arguments(1024, true)]
    [Arguments(375, false)]
    [Arguments(375, true)]
    public async Task ThePadsControls_FitItsBar(int width, bool member)
    {
        await Page.SetViewportSizeAsync(width, 900);
        if (member)
        {
            await MemberSession.SignInAsync(Page);
        }

        _ = await OpenAsync(width, Home);
        await Page.EvaluateAsync("() => document.fonts.ready");
        var faults = await Page.EvaluateAsync<string[]>(ReadPadBar);

        _ = await Assert.That(string.Join(Environment.NewLine, faults)).IsEmpty();
        if (member && width >= 1024)
        {
            await Expect(Page.Locator("#pad-bar-desk a.kcc-btn span")).ToBeVisibleAsync();
        }
    }

    // A clip-path also clips hit testing, so a point on a focus ring that the tear cuts away hits no part of the
    // sheet. The ring is traced as the rounded outline it draws, read from the transparent outline every control
    // carries before it takes focus.
    private const string ReadPadRings = """
        () => {
          const lift = document.createElement('style')
          lift.textContent = '.kcc-slip, .kcc-slip * { transform: none !important; rotate: none !important; transition: none !important; }'
          document.head.append(lift)
          try {
            const bar = [...document.querySelectorAll('.pad-bar')].find((element) => element.checkVisibility())
            if (!bar) return ['no visible pad bar']
            const sheet = bar.closest('.kcc-sheet')
            const paper = sheet.getBoundingClientRect()
            const edges = `${paper.left.toFixed(1)},${paper.top.toFixed(1)} to ${paper.right.toFixed(1)}`
            const faults = []
            for (const control of bar.querySelectorAll('a, button')) {
              if (!control.checkVisibility()) continue
              const style = getComputedStyle(control)
              const ring = parseFloat(style.outlineOffset) + parseFloat(style.outlineWidth)
              const box = control.getBoundingClientRect()
              const left = box.left - ring
              const right = box.right + ring
              const top = box.top - ring
              const height = box.height + 2 * ring
              const radius = Math.min(parseFloat(style.borderTopLeftRadius) + ring, height / 2, (right - left) / 2)
              for (let x = left + 0.5; x <= right - 0.5; x += 1) {
                const reach = x < left + radius ? left + radius - x : x > right - radius ? x - (right - radius) : 0
                const y = top + radius - Math.sqrt(Math.max(radius * radius - reach * reach, 0)) + 0.5
                const hit = document.elementFromPoint(x, y)
                if (!hit || !sheet.contains(hit)) {
                  faults.push(`${control.tagName.toLowerCase()}.${[...control.classList].join('.')} "${control.textContent.replace(/\s+/g, ' ').trim().slice(0, 30)}": its ring at ${x.toFixed(1)},${y.toFixed(1)} is torn off the sheet at ${edges}`)
                  break
                }
              }
            }
            return faults
          } finally {
            lift.remove()
          }
        }
        """;

    [Test]
    [NotInParallel(MemberSession.Serial)]
    [Arguments(1024, false)]
    [Arguments(1024, true)]
    [Arguments(1280, true)]
    [Arguments(375, false)]
    [Arguments(375, true)]
    public async Task ThePadsFocusRings_StayOnItsTornSheet(int width, bool member)
    {
        await Page.SetViewportSizeAsync(width, 900);
        if (member)
        {
            await MemberSession.SignInAsync(Page);
        }

        _ = await OpenAsync(width, Home);
        await Page.EvaluateAsync("() => document.fonts.ready");
        var faults = await Page.EvaluateAsync<string[]>(ReadPadRings);

        _ = await Assert.That(string.Join(Environment.NewLine, faults)).IsEmpty();
    }

    [Test]
    [NotInParallel(MemberSession.Serial)]
    [Arguments(1024, false, "top")]
    [Arguments(1024, true, "top")]
    [Arguments(1024, false, "recipes")]
    [Arguments(1024, true, "recipes")]
    [Arguments(1024, true, "kitchen")]
    [Arguments(1024, false, "search")]
    [Arguments(1024, true, "search")]
    [Arguments(375, false, "top")]
    [Arguments(375, true, "top")]
    [Arguments(375, false, "find")]
    [Arguments(375, true, "find")]
    [Arguments(375, false, "menu")]
    [Arguments(375, true, "menu")]
    public async Task EveryPadTextBlock_SitsOnTheRule(int width, bool member, string sheet)
    {
        await Page.SetViewportSizeAsync(width, 900);
        if (member)
        {
            await MemberSession.SignInAsync(Page);
        }

        _ = await OpenAsync(width, Home);
        if (sheet == "top")
        {
            await ExpectOnTheRuleAsync(".pad-top", null);
            return;
        }

        var card = Page.Locator($"#pad-card-{sheet}");
        if (sheet == "search")
        {
            await Page.Locator("#pad-search-input").ClickAsync();
        }
        else
        {
            await Page.Locator($"#pad-menu-{sheet}").ClickAsync();
        }

        await Expect(card).ToHaveClassAsync(new System.Text.RegularExpressions.Regex(@"\bis-open\b"));
        await ExpectOnTheRuleAsync($"#pad-card-{sheet}", null);
    }

    [Test]
    [Arguments(375, Search)]
    [Arguments(1280, Search)]
    [Arguments(375, SearchList)]
    [Arguments(1280, SearchList)]
    [Arguments(375, Recipe)]
    [Arguments(1280, Recipe)]
    [Arguments(375, Variant)]
    [Arguments(1280, Variant)]
    [Arguments(375, Home)]
    [Arguments(1280, Home)]
    [Arguments(375, Login)]
    [Arguments(1280, Login)]
    [Arguments(375, CookMode)]
    [Arguments(1280, CookMode)]
    public async Task EveryTextBlock_SitsOnTheRule(int width, string page)
    {
        var scope = await OpenAsync(width, page);

        await ExpectOnTheRuleAsync(scope, null);
    }

    [Test]
    [NotInParallel(MemberSession.Serial)]
    [Arguments(375, Account)]
    [Arguments(1280, Account)]
    [Arguments(375, Settings)]
    [Arguments(1280, Settings)]
    public async Task EveryMemberPageTextBlock_SitsOnTheRule(int width, string page)
    {
        await Page.SetViewportSizeAsync(width, 900);
        await MemberSession.SignInAsync(Page);
        var scope = await OpenAsync(width, page);

        await ExpectOnTheRuleAsync(scope, null);
    }

    [Test]
    [NotInParallel(MemberSession.Serial)]
    [Arguments(375)]
    [Arguments(1280)]
    public async Task EveryTextBlock_SitsOnTheRule_WithAReviewBeingWritten(int width)
    {
        await Page.SetViewportSizeAsync(width, 900);
        await MemberSession.SignInAsync(Page);
        _ = await OpenAsync(width, Variant);
        await Page.Locator("[data-value='4']").First.ClickAsync();
        await Expect(Page.GetByTestId("delete-review")).ToBeVisibleAsync();

        await ExpectOnTheRuleAsync(Main, null);
    }

    [Test]
    [NotInParallel(MemberSession.Serial)]
    [Arguments(375, "add-variant")]
    [Arguments(1280, "add-variant")]
    [Arguments(375, "start")]
    [Arguments(375, "basics")]
    [Arguments(375, "variant")]
    [Arguments(375, "ingredients")]
    [Arguments(375, "instructions")]
    [Arguments(375, "review")]
    [Arguments(375, "photos")]
    [Arguments(375, "match")]
    [Arguments(375, "match-review")]
    [Arguments(375, "leave")]
    [Arguments(375, "read-again")]
    [Arguments(1280, "start")]
    [Arguments(1280, "basics")]
    [Arguments(1280, "variant")]
    [Arguments(1280, "ingredients")]
    [Arguments(1280, "instructions")]
    [Arguments(1280, "review")]
    [Arguments(1280, "photos")]
    [Arguments(1280, "match")]
    [Arguments(1280, "match-review")]
    [Arguments(1280, "leave")]
    [Arguments(1280, "read-again")]
    public async Task EveryWizardTextBlock_SitsOnTheRule(int width, string step)
    {
        var scope = await OpenWizardAsync(width, step);

        await ExpectOnTheRuleAsync(scope, null);
    }

    [Test]
    [Arguments(375, Search)]
    [Arguments(1280, Search)]
    [Arguments(375, Recipe)]
    [Arguments(1280, Recipe)]
    [Arguments(375, Variant)]
    [Arguments(1280, Variant)]
    public async Task EveryControlRowLabel_SitsOnTheRuleWithItsControlsText(int width, string path)
    {
        _ = await OpenAsync(width, path);

        await ExpectOnTheRuleAsync(Main, path switch
        {
            Search => [new(".kcc-kick", "sort")],
            Recipe => [new(".kcc-title", "add variant")],
            _ => [new(".kcc-lbl", "makes")],
        });
    }

    [Test]
    [NotInParallel(MemberSession.Serial)]
    [Arguments(375, "ingredients")]
    [Arguments(375, "photos")]
    [Arguments(1280, "ingredients")]
    [Arguments(1280, "photos")]
    public async Task EveryWizardControlRowLabel_SitsOnTheRuleWithItsControlsText(int width, string step)
    {
        var scope = await OpenWizardAsync(width, step);

        await ExpectOnTheRuleAsync(scope, step == "photos" ? [new(".kcc-kick[aria-live]", null)] : [new(".kcc-kick", "to taste")]);
    }

    [Test]
    [NotInParallel(MemberSession.Serial)]
    [Arguments(375, Search)]
    [Arguments(1280, Search)]
    [Arguments(375, Variant)]
    [Arguments(1280, Variant)]
    [Arguments(375, Login)]
    [Arguments(1280, Login)]
    [Arguments(375, "wizard:basics")]
    [Arguments(1280, "wizard:basics")]
    [Arguments(375, "wizard:ingredients")]
    [Arguments(1280, "wizard:ingredients")]
    [Arguments(375, "wizard:match")]
    [Arguments(1280, "wizard:match")]
    public async Task EveryFieldsText_SitsOnTheRule(int width, string page)
    {
        var scope = page.StartsWith("wizard:", StringComparison.Ordinal)
            ? await OpenWizardAsync(width, page["wizard:".Length..])
            : await OpenAsync(width, page);

        var read = await ExpectOnTheRuleAsync(scope, null);

        _ = await Assert.That(read.Fields).IsGreaterThan(0);
    }

    [Test]
    [NotInParallel(MemberSession.Serial)]
    public async Task AFieldsX_IsDrawnButNeverRead()
    {
        _ = await OpenWizardAsync(1280, "basics");
        var name = Page.GetByTestId("wizard-recipe-name");
        var x = await name.EvaluateAsync<string>("(input) => getComputedStyle(input.closest('.kcc-field'), '::before').content");

        _ = await Assert.That(x).StartsWith("\"X\"");
        await Expect(name).ToHaveAccessibleNameAsync(new System.Text.RegularExpressions.Regex("^[^X]+$"));
    }

    [Test]
    [Arguments(375)]
    [Arguments(1280)]
    public async Task ARazorSheet_SitsOnTheRule_ToItsLastLine(int width)
    {
        await Page.SetViewportSizeAsync(width, 900);
        _ = await Page.GotoAsync(NotFound);
        await Piles.WaitForHydrationAsync(Page);

        await ExpectOnTheRuleAsync(Main, null, everyLine: true);
    }

    [Test]
    [Arguments(375)]
    [Arguments(1280)]
    public async Task AListWhoseRowsRunOn_SitsOnTheRule_ToItsLastLine(int width)
    {
        _ = await OpenAsync(width, Variant);
        var name = Page.Locator("main .kcc-check li label:not(.kcc-box)").First;
        await name.EvaluateAsync("(label) => { label.textContent = 'Ramen noodles, fresh from the chiller cabinet if the shop has them, or dried and boiled a minute short of the time on the packet, then rinsed' }");
        var row = await name.EvaluateAsync<double>("(label) => label.closest('li').getBoundingClientRect().height");

        _ = await Assert.That(row).IsGreaterThan(36);
        await ExpectOnTheRuleAsync("main .kcc-slip:has(.kcc-check)", null, everyLine: true);
    }

    // A range over an element's content spans the font's pinned ascent to its descent, so its centre is the line's.
    private const string ReadCentres = """
        (selectors) => {
          const lift = document.createElement('style')
          lift.textContent = '.kcc-slip, .kcc-slip * { transform: none !important; rotate: none !important; transition: none !important; }'
          document.head.append(lift)
          try {
            return selectors.map((selector) => {
              const element = [...document.querySelectorAll(selector)].find((candidate) => candidate.checkVisibility())
              if (!element) return null
              const box = element.getBoundingClientRect()
              const marker = document.createElement('span')
              marker.style.cssText = 'display: inline-block; width: 0; height: 0; vertical-align: baseline'
              let baseline
              if (element.matches('select')) {
                const wrap = document.createElement('span')
                wrap.style.cssText = 'display: block; line-height: 0; font-size: 0'
                element.before(wrap)
                wrap.append(marker, element)
                baseline = marker.getBoundingClientRect().top
                wrap.before(element)
                wrap.remove()
              } else {
                const walker = document.createTreeWalker(element, NodeFilter.SHOW_TEXT)
                while (walker.nextNode() && !walker.currentNode.data.trim());
                const text = walker.currentNode
                if (text && text !== element && text.data.trim()) {
                  text.before(marker)
                  baseline = marker.getBoundingClientRect().top
                  marker.remove()
                }
              }
              const range = document.createRange()
              range.selectNodeContents(element)
              const line = range.getBoundingClientRect()
              return { top: box.top, bottom: box.bottom, centre: box.top + box.height / 2, lineCentre: line.top + line.height / 2, baseline: baseline ?? null }
            })
          } finally {
            lift.remove()
          }
        }
        """;

    [Test]
    [NotInParallel(MemberSession.Serial)]
    [Arguments(768)]
    [Arguments(1280)]
    public async Task ThePickButton_SharesItsLineWithTheRecipeItPicks(int width)
    {
        _ = await OpenWizardAsync(width, "match");
        await Page.EvaluateAsync("() => document.fonts.ready");
        var read = await Page.EvaluateAsync<Centres?[]>(ReadCentres, new[] { "[data-testid='match-step'] select", "[data-testid='match-pick']" });

        _ = await Assert.That(read[0]).IsNotNull();
        _ = await Assert.That(read[1]).IsNotNull();
        _ = await Assert.That(read[1]!.Top).IsLessThan(read[0]!.Bottom);
        _ = await Assert.That(Math.Abs(read[1]!.Baseline!.Value - read[0]!.Baseline!.Value)).IsLessThanOrEqualTo(0.75);
    }

    [Test]
    [Arguments(375)]
    [Arguments(1280)]
    public async Task TheServingsCount_LinesUpWithItsButtons(int width)
    {
        _ = await OpenAsync(width, CookMode);
        await Page.EvaluateAsync("() => document.fonts.ready");
        var read = await Page.EvaluateAsync<Centres?[]>(
            ReadCentres,
            new[] { "[data-test='cook-servings-dec']", "[data-test='cook-servings-dec'] + .kcc-num", "[data-test='cook-servings-inc']" });

        _ = await Assert.That(read.All(centre => centre is not null)).IsTrue();
        _ = await Assert.That(Math.Abs(read[0]!.Centre - read[1]!.LineCentre)).IsLessThanOrEqualTo(0.75);
        _ = await Assert.That(Math.Abs(read[2]!.Centre - read[1]!.LineCentre)).IsLessThanOrEqualTo(0.75);
    }

    private const string ReadCloseOffLine = """
        () => {
          const lift = document.createElement('style')
          lift.textContent = '.kcc-slip, .kcc-slip * { transform: none !important; rotate: none !important; transition: none !important; }'
          document.head.append(lift)
          try {
            const close = document.querySelector("[data-test='cook-close']")
            const sheet = close.closest('.kcc-sheet')
            const length = (value) => {
              const probe = document.createElement('div')
              probe.style.cssText = `position: absolute; visibility: hidden; height: ${value}`
              sheet.append(probe)
              const height = probe.getBoundingClientRect().height
              probe.remove()
              return height
            }
            const style = getComputedStyle(sheet)
            const contentTop = sheet.getBoundingClientRect().top + parseFloat(style.borderTopWidth) + parseFloat(style.paddingTop)
            const bl = length('var(--bl)')
            const capsCentre = length('calc(var(--rule-drop) - var(--k-sono) * var(--text-caps))')
            const box = close.getBoundingClientRect()
            const offset = box.top + box.height / 2 - contentTop - capsCentre
            const rest = ((offset % bl) + bl) % bl
            return rest <= bl / 2 ? rest : rest - bl
          } finally {
            lift.remove()
          }
        }
        """;

    [Test]
    [Arguments(1280)]
    [Arguments(375)]
    public async Task TheCookModeCloseButton_CentresOnTheCapsOfItsLine(int width)
    {
        _ = await OpenAsync(width, CookMode);
        await Page.EvaluateAsync("() => document.fonts.ready");

        var off = await Page.EvaluateAsync<double>(ReadCloseOffLine);

        _ = await Assert.That(Math.Abs(off)).IsLessThanOrEqualTo(0.75);
    }

    private const string ReadMargin = """
        (within) => {
          const sheet = document.querySelector(within).closest('.kcc-sheet')
          const length = (value) => {
            const probe = document.createElement('div')
            probe.style.cssText = `position: absolute; visibility: hidden; width: ${value}`
            sheet.append(probe)
            const width = probe.getBoundingClientRect().width
            probe.remove()
            return width
          }
          const before = getComputedStyle(sheet, '::before')
          return {
            margin: sheet.classList.contains('kcc-sheet--margin'),
            line: before.content !== 'none' && before.display !== 'none' && before.backgroundImage.includes('gradient'),
            padding: parseFloat(getComputedStyle(sheet).paddingLeft),
            ruled: length('calc(var(--bl) * 3)'),
            plain: length('var(--pad, 24px)'),
          }
        }
        """;

    [Test]
    [Arguments(1280, Variant, "main .kcc-sheet:has(input[type='number'])", true)]
    [Arguments(375, Variant, "main .kcc-sheet:has(input[type='number'])", false)]
    [Arguments(1280, Search, "[data-testid='recipe-search-input']", false)]
    public async Task TheMarginRule_RulesReadingSheets_FromWideEnoughSheets(int width, string page, string within, bool drawn)
    {
        _ = await OpenAsync(width, page);
        var margin = await Page.EvaluateAsync<MarginReading>(ReadMargin, within);

        _ = await Assert.That(margin.Margin).IsEqualTo(page == Variant);
        _ = await Assert.That(margin.Line).IsEqualTo(drawn);
        _ = await Assert.That(Math.Abs(margin.Padding - (drawn ? margin.Ruled : margin.Plain))).IsLessThanOrEqualTo(0.5);
    }

    [Test]
    [NotInParallel(MemberSession.Serial)]
    [Arguments(Home)]
    [Arguments(Search)]
    [Arguments(SearchList)]
    [Arguments(Recipe)]
    [Arguments(Variant)]
    [Arguments(CookMode)]
    [Arguments(Login)]
    [Arguments(Account)]
    [Arguments(Settings)]
    [Arguments("wizard:add-variant")]
    [Arguments("wizard:start")]
    [Arguments("wizard:basics")]
    [Arguments("wizard:variant")]
    [Arguments("wizard:ingredients")]
    [Arguments("wizard:instructions")]
    [Arguments("wizard:review")]
    [Arguments("wizard:photos")]
    [Arguments("wizard:match")]
    [Arguments("wizard:leave")]
    [Arguments("wizard:read-again")]
    public async Task ThePage_DoesNotScrollSideways_AtPhoneWidth(string page)
    {
        if (page.StartsWith("wizard:", StringComparison.Ordinal))
        {
            _ = await OpenWizardAsync(375, page["wizard:".Length..]);
        }
        else
        {
            if (page is Account or Settings)
            {
                await Page.SetViewportSizeAsync(375, 900);
                await MemberSession.SignInAsync(Page);
            }

            _ = await OpenAsync(375, page);
        }

        await Page.EvaluateAsync("() => document.fonts.ready");
        var width = await Page.EvaluateAsync<int>("() => document.documentElement.scrollWidth");

        _ = await Assert.That(width).IsEqualTo(375);
    }

    private async Task<Reading> ExpectOnTheRuleAsync(string scope, Label[]? labels, bool everyLine = false)
    {
        await Page.EvaluateAsync("() => document.fonts.ready");
        var read = await Page.EvaluateAsync<Reading>(
            ReadBaselines,
            new { scope, labels = labels?.Select(label => new { selector = label.Selector, text = label.Text }).ToArray(), skip = Unmeasured, lasts = everyLine });

        _ = await Assert.That(read.Measured).IsGreaterThan(0);
        _ = await Assert.That(string.Join(Environment.NewLine, read.Faults)).IsEmpty();
        return read;
    }

    private async Task<string> OpenAsync(int width, string page)
    {
        await Page.SetViewportSizeAsync(width, 900);
        if (page == CookMode)
        {
            // The overlay opens only from the wide toolbar, and stays open as the window narrows.
            await Page.SetViewportSizeAsync(1280, 900);
            _ = await Page.GotoAsync(CookVariant);
            await Piles.WaitForHydrationAsync(Page);
            await Page.Locator("[data-test='cook-mode-open-desktop']").ClickAsync();
            await Expect(Page.Locator("[role='dialog']")).ToBeVisibleAsync();
            await Page.SetViewportSizeAsync(width, 900);
            return "[role='dialog']";
        }

        _ = await Page.GotoAsync(page == SearchList ? Search : page);
        await Piles.WaitForHydrationAsync(Page);
        if (page == SearchList)
        {
            await Page.Locator("[data-testid='view-list']").ClickAsync();
            await Expect(Page.Locator("main .kcc-slip--wide [data-testid='recipe-card']").First).ToBeVisibleAsync();
        }

        return Main;
    }

    private async Task<string> OpenWizardAsync(int width, string step)
    {
        await Page.SetViewportSizeAsync(width, 900);
        if (step == "add-variant")
        {
            await MemberSession.SignInAsync(Page);
            _ = await Page.GotoAsync(PhotoImport.RecipePath);
            await Page.GetByRole(AriaRole.Link, new() { Name = "Add Variant", Exact = true }).First.ClickAsync();
            await Page.WaitForURLAsync(url => new Uri(url).Query.StartsWith("?recipe=", StringComparison.Ordinal));
            await Piles.WaitForHydrationAsync(Page);
            await Expect(Page.GetByTestId("wizard-start-typing")).ToBeVisibleAsync();
            return Main;
        }

        if (step == "leave")
        {
            await PhotoImport.OpenOnAddVariantAsync(Page);
            await PhotoImport.AddFromDeviceAsync(Page, PhotoImport.FirstPage);
            await Page.GetByRole(AriaRole.Link, new() { Name = "Cancel", Exact = true }).ClickAsync();
            await Expect(Page.GetByTestId("leave-dialog")).ToBeVisibleAsync();
            return "[data-testid='leave-dialog']";
        }

        if (step == "read-again")
        {
            await PhotoImport.OpenOnAddVariantAsync(Page);
            await PhotoImport.AddFromDeviceAsync(Page, PhotoImport.FirstPage);
            await Page.GetByTestId("read-photos").ClickAsync();
            await Expect(Page.GetByTestId("review-step")).ToBeVisibleAsync();
            await Page.GetByTestId("review-edit-ingredients").ClickAsync();
            await Page.GetByTestId("wizard-ingredient-name").First.FillAsync("Flour, sifted");
            await Page.GetByTestId("back-to-review").ClickAsync();
            await Page.GetByTestId("read-again").ClickAsync();
            await Expect(Page.GetByTestId("read-again-warning")).ToBeVisibleAsync();
            return Main;
        }

        if (step is "match" or "match-review")
        {
            await PhotoImport.OpenOnCreateRecipeAsync(Page);
            await PhotoImport.AddFromDeviceAsync(Page, PhotoImport.FirstPage, PhotoImport.SecondPage);
            await Page.GetByTestId("read-photos").ClickAsync();
            await Expect(Page.GetByTestId("match-step")).ToBeVisibleAsync();
            if (step == "match-review")
            {
                await Page.GetByTestId("match-accept").ClickAsync();
                await Expect(Page.GetByTestId("review-step")).ToBeVisibleAsync();
            }

            return Main;
        }

        await MemberSession.SignInAsync(Page);
        _ = await Page.GotoAsync(CreateRecipe);
        await Piles.WaitForHydrationAsync(Page);
        await Expect(Page.GetByTestId("wizard-start-typing")).ToBeVisibleAsync();

        if (step == "photos")
        {
            await Page.GetByTestId("wizard-start-photos").ClickAsync();
            await Expect(Page.GetByTestId("photo-input")).ToBeAttachedAsync();
            return Main;
        }

        if (step == "start")
        {
            return Main;
        }

        await Page.GetByTestId("wizard-start-typing").ClickAsync();
        if (step == "basics")
        {
            await Expect(Page.GetByTestId("wizard-recipe-name")).ToBeVisibleAsync();
            return Main;
        }

        await Page.GetByTestId("wizard-recipe-name").FillAsync("Rule Walk");
        await Page.GetByTestId("wizard-recipe-description").FillAsync("Typed by the rule walker.");
        await NextAsync();
        if (step == "variant")
        {
            await Expect(Page.GetByTestId("wizard-variant-name")).ToBeVisibleAsync();
            return Main;
        }

        await Page.GetByTestId("wizard-variant-name").FillAsync("First Rule");
        await NextAsync();
        await Page.GetByTestId("wizard-ingredient-name").First.FillAsync("Salt");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Eyeball" }).First.ClickAsync();
        await Expect(Page.Locator("main .kcc-kick", new() { HasTextString = "to taste" })).ToBeVisibleAsync();
        if (step == "ingredients")
        {
            return Main;
        }

        await NextAsync();
        await Page.GetByTestId("wizard-instruction-text").First.FillAsync("Stir for 2 minutes.");
        if (step == "instructions")
        {
            return Main;
        }

        await NextAsync();
        await Expect(Page.GetByTestId("review-step")).ToBeVisibleAsync();
        return Main;
    }

    private Task NextAsync() => Page.GetByRole(AriaRole.Button, new() { Name = "Next", Exact = true }).ClickAsync();

    private sealed record Label(string Selector, string? Text);

    private sealed class Centres
    {
        public double Top { get; init; }

        public double Bottom { get; init; }

        public double Centre { get; init; }

        public double LineCentre { get; init; }

        public double? Baseline { get; init; }
    }

    private sealed class TitleAndAction
    {
        public double TitleFirst { get; init; }

        public double TitleLast { get; init; }

        public double Action { get; init; }

        public bool Wrapped { get; init; }

        public double Bl { get; init; }
    }

    private sealed class MarginReading
    {
        public bool Margin { get; init; }

        public bool Line { get; init; }

        public double Padding { get; init; }

        public double Ruled { get; init; }

        public double Plain { get; init; }
    }

    private sealed class Reading
    {
        public int Measured { get; init; }

        public int Fields { get; init; }

        public string[] Faults { get; init; } = [];
    }
}
