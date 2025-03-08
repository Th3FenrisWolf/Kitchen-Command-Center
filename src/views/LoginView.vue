<script setup lang="ts">
import { ref } from 'vue'
import { signIn, signUp } from '~/utilities/auth'

const signInEmail = ref('')
const signInPassword = ref('')
const signUpEmail = ref('')
const signUpPassword = ref('')
</script>

<template>
  <div>
    <h1>Sign In / Sign Up</h1>
    <form
      @submit="
        (e) => {
          e.preventDefault()
          signIn(signInEmail, signInPassword)
        }
      "
    >
      <h2>Sign In</h2>
      <input type="email" v-model="signInEmail" placeholder="Email" />
      <input type="password" v-model="signInPassword" placeholder="Password" />
      <button type="submit">Sign In</button>
    </form>

    <form
      @submit="
        (e) => {
          e.preventDefault()
          signUp(signUpEmail, signUpPassword)
        }
      "
    >
      <h2>Sign Up</h2>
      <input type="email" v-model="signUpEmail" placeholder="Email" />
      <input type="password" v-model="signUpPassword" placeholder="Password" />
      <button type="submit">Sign Up</button>
    </form>
  </div>
</template>
