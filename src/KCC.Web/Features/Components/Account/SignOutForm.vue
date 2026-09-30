<!-- #region SignOutForm Component Properties -->
<script lang="ts">
  /**
   * Signing out is a POST, so every "sign out" is this small form around its button.
   */
  export default {
    name: 'SignOutForm',
  }

  // The header's nav is content: its Logout entry is a link to this path, which the header swaps for the form.
  export const SIGN_OUT_PATH = '/account/logout'

  export const isSignOutUrl = (url?: string) => url?.replace(/\/+$/, '').toLowerCase() === SIGN_OUT_PATH

  export interface SignOutFormProps {
    action?: string
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  import { antiforgeryToken } from '~/Utilities/Api'
  const { action = SIGN_OUT_PATH } = defineProps<SignOutFormProps>()

  // The server render cannot know the token Layout.cshtml hands the client, so it is read as the form submits.
  const fillToken = (event: Event) => {
    const form = event.currentTarget as HTMLFormElement
    const field = form.elements.namedItem('__RequestVerificationToken') as HTMLInputElement
    field.value = antiforgeryToken()
  }
</script>

<template>
  <form method="post" :action="action" class="contents" @submit="fillToken">
    <input type="hidden" name="__RequestVerificationToken" value="" />
    <slot />
  </form>
</template>
