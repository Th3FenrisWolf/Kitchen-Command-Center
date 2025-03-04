import qs, { type IParseOptions } from 'qs'

export const parseQueryString = <ReturnType>(options?: IParseOptions): ReturnType => {
  const query = location.search.slice(1)
  return qs.parse(query, options) as unknown as ReturnType
}
