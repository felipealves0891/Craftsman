const { defineConfig } = require('cypress')

module.exports = defineConfig({
  e2e: {
    baseUrl: process.env.CYPRESS_BASE_URL || 'http://localhost:5177',
    specPattern: 'cypress/e2e/**/*.cy.js',
    supportFile: 'cypress/support/e2e.js',
    screenshotsFolder: 'cypress/artifacts/screenshots',
    videosFolder: 'cypress/artifacts/videos',
    video: process.env.CYPRESS_VIDEO === 'true',
    screenshotOnRunFailure: true,
    defaultCommandTimeout: 10000,
    requestTimeout: 10000,
    responseTimeout: 30000,
    env: {
      e2ePassword: process.env.CYPRESS_E2E_PASSWORD || 'E2e_user_123!',
      adminEmail: process.env.CYPRESS_E2E_ADMIN_EMAIL || 'e2e.admin@craftsman.local',
      operadorEmail: process.env.CYPRESS_E2E_OPERADOR_EMAIL || 'e2e.operador@craftsman.local',
      consultaEmail: process.env.CYPRESS_E2E_CONSULTA_EMAIL || 'e2e.consulta@craftsman.local'
    }
  }
})
