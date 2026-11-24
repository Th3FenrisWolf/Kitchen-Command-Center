import { defineAsyncComponent } from 'vue'

// Only a member's page renders the note and review forms, so a reader who is not signed in never fetches the field.
// On a member's page Vue hydrates the server's field once its chunk arrives.
export const TextAreaField = defineAsyncComponent(() => import('~/Components/Forms/TextAreaField.vue'))

// Until the field's chunk arrives the model has not seen what the reader typed into the server's textarea.
export function typedText(holder: Element | null | undefined, modelText: string): string {
  const field = holder?.querySelector('textarea')
  return field && field.value !== field.defaultValue ? field.value : modelText
}
