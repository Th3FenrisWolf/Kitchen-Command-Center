import type { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api'
import { umbHttpClient } from '@umbraco-cms/backoffice/http-client'
import { tryExecute } from '@umbraco-cms/backoffice/resources'

const base = '/umbraco/management/api/v1/kcc/recipe-editor'

// Without a security scheme the backoffice client sends no bearer token.
const security = [{ scheme: 'bearer', type: 'http' }] as const

export async function getRecipeUnits(host: UmbControllerHost): Promise<string[]> {
  const { data } = await tryExecute(host, umbHttpClient.get<{ 200: string[] }>({ url: `${base}/units`, security }))
  return data ?? []
}

export async function getRecipeIcons(host: UmbControllerHost): Promise<string[]> {
  const { data } = await tryExecute(host, umbHttpClient.get<{ 200: string[] }>({ url: `${base}/icons`, security }))
  return data ?? []
}

export async function suggestRecipeIcon(host: UmbControllerHost, name: string, description: string): Promise<string | undefined> {
  const { data } = await tryExecute(
    host,
    umbHttpClient.post<{ 200: { icon: string } }>({ url: `${base}/icon-suggestion`, body: { name, description }, security }),
  )
  return data?.icon
}
