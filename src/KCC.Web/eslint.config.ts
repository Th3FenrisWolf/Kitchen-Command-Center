import pluginVue from 'eslint-plugin-vue'
import prettierConfig from 'eslint-plugin-prettier/recommended'
import { defineConfigWithVueTs, vueTsConfigs } from '@vue/eslint-config-typescript'

export default defineConfigWithVueTs(
  {
    name: 'app/files-to-lint',
    files: ['**/*.{ts,vue}'],
  },

  {
    name: 'app/files-to-ignore',
    ignores: ['**/dist/**', '**/wwwroot/**'],
  },

  prettierConfig,
  pluginVue.configs['flat/essential'],
  vueTsConfigs.recommended,

  {
    rules: {
      'no-console': 'warn',
      'no-debugger': 'warn',
      '@typescript-eslint/no-unused-vars': 'warn',
      'vue/multi-word-component-names': 'off',
      'vue/script-indent': ['error', 2, { baseIndent: 1 }],
    },
  },
)
