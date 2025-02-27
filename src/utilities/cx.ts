export const cx = (...classNames: unknown[]) =>
  classNames
    .filter((className) => typeof className === 'string')
    .join(' ')
    .trim()

export default cx
