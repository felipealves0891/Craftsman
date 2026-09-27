Cypress.Commands.add('getByCy', (selector, ...args) => {
  return cy.get(`[data-cy="${selector}"]`, ...args)
})

Cypress.Commands.add('resetE2E', () => {
  cy.clearCookies()
  cy.request('POST', '/__e2e/reset')
  cy.clearCookies()
})

Cypress.Commands.add('loginAs', role => {
  cy.request('POST', '/__e2e/login', { role })
})

Cypress.Commands.add('loginThroughUi', (email, password, rememberMe = false) => {
  cy.visit('/Account/Login')
  cy.getByCy('login-email').clear().type(email)
  cy.getByCy('login-password').clear().type(password, { log: false })

  if (rememberMe) {
    cy.getByCy('login-remember-me').check({ force: true })
  }

  cy.getByCy('login-submit').click()
})
