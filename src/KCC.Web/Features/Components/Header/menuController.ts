import type { InjectionKey, Ref } from 'vue'

export interface MenuController {
  openId: Ref<string | null>
  setOpen: (id: string | null) => void
}

export const MENU_CONTROLLER_KEY: InjectionKey<MenuController> = Symbol('MenuController')
