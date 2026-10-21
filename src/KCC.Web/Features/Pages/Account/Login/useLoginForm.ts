import { ref } from 'vue'
import { post } from '~/Utilities/Api'

export type LoginField = 'UserName' | 'Password' | 'PasswordConfirmation'

export interface LoginFormOptions {
  returnUrl?: string
  register?: boolean
  defaultUserName?: string
  defaultPassword?: string
  defaultRememberMe?: boolean
}

const INVALID_CREDENTIALS = 'invalid-credentials'

export function useLoginForm(options: LoginFormOptions) {
  const isSignIn = ref(!options.register)
  const isSubmitting = ref(false)

  const userName = ref(options.defaultUserName ?? '')
  const email = ref('')
  const password = ref(options.defaultPassword ?? '')
  const passwordConfirmation = ref('')
  const rememberMe = ref(options.defaultRememberMe ?? false)
  const formError = ref<string | null>(null)
  const fieldsInError = ref<LoginField[]>([])

  const showError = (message: string | null, fields: LoginField[] = []) => {
    formError.value = message
    fieldsInError.value = fields
  }

  const switchMode = () => {
    isSignIn.value = !isSignIn.value
    userName.value = ''
    email.value = ''
    password.value = ''
    passwordConfirmation.value = ''
    showError(null)
  }

  const handleSubmit = async () => {
    showError(null)

    if (!isSignIn.value && password.value !== passwordConfirmation.value) {
      showError('Passwords do not match.', ['Password', 'PasswordConfirmation'])
      return
    }

    isSubmitting.value = true
    const endpoint = isSignIn.value ? '/api/account/login' : '/api/account/register'
    const body = isSignIn.value
      ? { userName: userName.value, password: password.value, rememberMe: rememberMe.value, returnUrl: options.returnUrl }
      : { userName: userName.value, email: email.value, password: password.value }

    const result = await post<{ redirectUrl?: string }>(endpoint, body)
    isSubmitting.value = false

    if (!result.success) {
      showError(result.errorMessage, result.code === INVALID_CREDENTIALS ? ['UserName', 'Password'] : [])
      return
    }

    window.location.href = result.data.redirectUrl ?? '/'
  }

  return {
    isSignIn,
    isSubmitting,
    userName,
    email,
    password,
    passwordConfirmation,
    rememberMe,
    formError,
    fieldsInError,
    switchMode,
    handleSubmit,
  }
}
