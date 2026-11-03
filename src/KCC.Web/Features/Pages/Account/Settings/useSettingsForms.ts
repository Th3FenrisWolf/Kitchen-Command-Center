import { ref } from 'vue'
import { post } from '~/Utilities/Api'

export type ProfileField = 'FirstName' | 'LastName'

export type PasswordField = 'CurrentPassword' | 'NewPassword' | 'ConfirmPassword'

export interface SettingsMessage {
  ok: boolean
  text: string
}

const NAME_REQUIRED = 'name-required'
const PASSWORD_REQUIRED = 'password-required'
const WRONG_PASSWORD = 'wrong-password'
const WEAK_PASSWORD = 'weak-password'

type SentPasswords = Record<Exclude<PasswordField, 'ConfirmPassword'>, string>

const blankOf = <F extends string>(sent: Record<F, string>): F[] =>
  (Object.keys(sent) as F[]).filter((field) => !sent[field].trim())

function passwordsAtFault(code: string | null, sent: SentPasswords): PasswordField[] {
  if (code === PASSWORD_REQUIRED) return blankOf(sent)
  if (code === WRONG_PASSWORD) return ['CurrentPassword']
  if (code === WEAK_PASSWORD) return ['NewPassword']
  return []
}

export function useProfileForm(initial: { firstName: string; lastName: string }, rs: (key: string) => string) {
  const firstName = ref(initial.firstName)
  const lastName = ref(initial.lastName)
  const submitting = ref(false)
  const message = ref<SettingsMessage | null>(null)
  const fieldsInError = ref<ProfileField[]>([])

  const save = async () => {
    message.value = null
    fieldsInError.value = []
    submitting.value = true

    const sent: Record<ProfileField, string> = { FirstName: firstName.value, LastName: lastName.value }
    const result = await post('/api/profile', { firstName: sent.FirstName, lastName: sent.LastName })
    submitting.value = false

    if (!result.success) {
      message.value = { ok: false, text: result.errorMessage }
      fieldsInError.value = result.code === NAME_REQUIRED ? blankOf(sent) : []
      return
    }

    message.value = { ok: true, text: rs('ProfileSaved') }
  }

  return { firstName, lastName, submitting, message, fieldsInError, save }
}

export function usePasswordForm(rs: (key: string) => string) {
  const currentPassword = ref('')
  const newPassword = ref('')
  const confirmPassword = ref('')
  const submitting = ref(false)
  const message = ref<SettingsMessage | null>(null)
  const fieldsInError = ref<PasswordField[]>([])

  const refuse = (text: string, fields: PasswordField[]) => {
    message.value = { ok: false, text }
    fieldsInError.value = fields
  }

  const change = async () => {
    message.value = null
    fieldsInError.value = []
    if (newPassword.value !== confirmPassword.value) {
      refuse(rs('PasswordsDoNotMatch'), ['NewPassword', 'ConfirmPassword'])
      return
    }
    submitting.value = true

    const sent: SentPasswords = { CurrentPassword: currentPassword.value, NewPassword: newPassword.value }
    const result = await post('/api/profile/password', {
      currentPassword: sent.CurrentPassword,
      newPassword: sent.NewPassword,
    })
    submitting.value = false

    if (!result.success) {
      refuse(result.errorMessage, passwordsAtFault(result.code, sent))
      return
    }

    message.value = { ok: true, text: rs('PasswordUpdated') }
    currentPassword.value = ''
    newPassword.value = ''
    confirmPassword.value = ''
  }

  return { currentPassword, newPassword, confirmPassword, submitting, message, fieldsInError, change }
}
