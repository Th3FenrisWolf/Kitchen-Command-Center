import type { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api'
import { umbHttpClient } from '@umbraco-cms/backoffice/http-client'
import { tryExecute } from '@umbraco-cms/backoffice/resources'

const base = '/umbraco/management/api/v1/kcc/contributions'

// Without a security scheme the backoffice client sends no bearer token.
const security = [{ scheme: 'bearer', type: 'http' }] as const

export interface WaitingMember {
  key: string
  name: string
  userName: string
  email: string
  registered: string
}

export interface WaitingDraft {
  key: string
  kind: 'recipe' | 'variant'
  name: string
  recipeName: string | null
  authorName: string | null
  created: string
}

export interface Waiting {
  members: WaitingMember[]
  drafts: WaitingDraft[]
}

export interface Entry {
  id: number
  variantKey: string
  variantName: string | null
  recipeName: string | null
  memberName: string | null
  rating: number | null
  text: string | null
  created: string
}

export interface EntryPage {
  total: number
  page: number
  pageSize: number
  items: Entry[]
}

export type EntryKind = 'reviews' | 'notes'

export interface EntryEdit {
  rating?: number
  text: string
}

export async function getWaiting(host: UmbControllerHost): Promise<Waiting | undefined> {
  const { data } = await tryExecute(host, umbHttpClient.get<{ 200: Waiting }>({ url: `${base}/waiting`, security }))
  return data
}

export async function approveMember(host: UmbControllerHost, key: string): Promise<boolean> {
  const { error } = await tryExecute(host, umbHttpClient.post({ url: `${base}/members/${key}/approval`, security }))
  return !error
}

export async function getEntries(host: UmbControllerHost, kind: EntryKind, page: number, pageSize: number): Promise<EntryPage | undefined> {
  const { data } = await tryExecute(
    host,
    umbHttpClient.get<{ 200: EntryPage }>({ url: `${base}/${kind}`, query: { page, pageSize }, security }),
  )
  return data
}

export async function editEntry(host: UmbControllerHost, kind: EntryKind, id: number, edit: EntryEdit): Promise<boolean> {
  const { error } = await tryExecute(host, umbHttpClient.put({ url: `${base}/${kind}/${id}`, body: edit, security }))
  return !error
}

export async function deleteEntry(host: UmbControllerHost, kind: EntryKind, id: number): Promise<boolean> {
  const { error } = await tryExecute(host, umbHttpClient.delete({ url: `${base}/${kind}/${id}`, security }))
  return !error
}
