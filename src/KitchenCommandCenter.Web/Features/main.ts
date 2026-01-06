import '~/Styles/Main.css'

import { createApp, h } from 'vue'
import App from '~/App.vue'
import { registerGlobalComponents } from '~/vue-components/register-global-components'

const serverContent = document.getElementById('server-content')
if (serverContent) serverContent.remove()

const RootComponent = {
  components: { App },
  setup() {
    return () => h(App, { serverContent: serverContent?.innerHTML.trim() ?? '' })
  },
}

const app = createApp(RootComponent)
registerGlobalComponents(app)
app.mount(document.body)
