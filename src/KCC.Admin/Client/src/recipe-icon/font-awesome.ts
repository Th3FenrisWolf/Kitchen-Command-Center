import { unsafeCSS } from '@umbraco-cms/backoffice/external/lit'
import fontAwesome from '@fortawesome/fontawesome-pro/css/fontawesome.min.css?raw'
import duotone from '@fortawesome/fontawesome-pro/css/duotone.min.css?raw'
import duotoneFont from '@fortawesome/fontawesome-pro/webfonts/fa-duotone-900.woff2?url'

// Browsers ignore a font face declared inside a shadow root, so the icon font is added to the document instead and
// only the class rules go into each element's styles.
const withoutFontFaces = (css: string) => css.replace(/@font-face\s*{[^}]*}/g, '')

export const fontAwesomeStyles = unsafeCSS(withoutFontFaces(fontAwesome) + withoutFontFaces(duotone))

let registered = false

export function registerFontAwesome() {
  if (registered) {
    return
  }

  registered = true
  document.fonts.add(
    new FontFace('Font Awesome 7 Duotone', `url(${duotoneFont}) format('woff2')`, { style: 'normal', weight: '900', display: 'block' }),
  )
}
