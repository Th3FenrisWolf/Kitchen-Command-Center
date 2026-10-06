// Just enough of an element for code that walks focus: the tests run in Node, with no DOM.
export class FakeElement {
  static focused: FakeElement | null = null

  readonly children: FakeElement[]

  constructor(
    readonly name: string,
    children: FakeElement[] = [],
    readonly options: { boxed?: boolean; keepsKeys?: boolean } = {},
  ) {
    this.children = children
  }

  querySelectorAll() {
    return this.children.flatMap((child) => [child, ...child.querySelectorAll()])
  }

  getClientRects() {
    return this.options.boxed === false ? [] : [{}]
  }

  contains(node: unknown) {
    return node === this || this.querySelectorAll().includes(node as FakeElement)
  }

  // A field, or a modal dialog, which keeps the keys typed into it.
  closest() {
    return this.options.keepsKeys ? this : null
  }

  matches() {
    return false
  }

  focus() {
    FakeElement.focused = this
  }
}

export const keydown = (key: string, target: unknown, modifiers: Partial<KeyboardEvent> = {}) => {
  let prevented = false
  const event = {
    key,
    target,
    shiftKey: false,
    ctrlKey: false,
    metaKey: false,
    altKey: false,
    ...modifiers,
    preventDefault: () => {
      prevented = true
    },
  }
  return { event: event as unknown as KeyboardEvent, prevented: () => prevented }
}
