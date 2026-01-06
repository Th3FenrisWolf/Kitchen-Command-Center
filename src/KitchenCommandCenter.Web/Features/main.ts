import '~/styles/main.css'

import { createApp, h } from 'vue'
import App from '~/App.vue'
import { registerGlobalComponents } from '~/vue-components/register-global-components'

// Capture server-rendered content before mounting
const serverContentEl = document.getElementById('server-content')
const serverContent = serverContentEl ? serverContentEl.innerHTML.trim() : ''

// Remove the server content container to avoid duplication
serverContentEl?.remove()

const RootComponent = {
  components: { App },
  setup() {
    return () => h(App, { serverContent })
  },
}

const app = createApp(RootComponent)
registerGlobalComponents(app)
app.mount(document.body)
