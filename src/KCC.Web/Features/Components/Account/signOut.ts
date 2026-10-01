// The header's nav is content: its Logout entry is a link to this path, which the header swaps for the form.
export const SIGN_OUT_PATH = '/account/logout'

export const isSignOutUrl = (url?: string) => url?.replace(/\/+$/, '').toLowerCase() === SIGN_OUT_PATH
