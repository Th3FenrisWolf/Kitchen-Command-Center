<script setup lang="ts">
  import { antiforgeryToken } from '~/Utilities/Api'
  import { SIGN_OUT_PATH } from '~/Components/Account/signOut'

  export interface SignOutFormProps {
    action?: string
  }

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
