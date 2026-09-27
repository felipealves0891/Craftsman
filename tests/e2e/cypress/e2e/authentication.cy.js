describe('Authentication and authorization', () => {
  const password = Cypress.env('e2ePassword')
  const adminEmail = Cypress.env('adminEmail')

  beforeEach(() => {
    cy.resetE2E()
  })

  it('logs in as admin through the UI and reaches the dashboard', () => {
    cy.loginThroughUi(adminEmail, password)

    cy.location('pathname').should('eq', '/')
    cy.contains('h1', 'Painel Craftsman').should('be.visible')
  })

  it('keeps invalid credentials on the login page with the existing error', () => {
    cy.visit('/Account/Login')

    cy.getByCy('login-email').type('unknown@craftsman.local')
    cy.getByCy('login-password').type('Wrong_password_123!', { log: false })
    cy.getByCy('login-submit').click()

    cy.location('pathname').should('eq', '/Account/Login')
    cy.contains('Login invalido.').should('be.visible')
    cy.contains('Painel Craftsman').should('not.exist')
  })

  it('redirects an anonymous user from a protected route to login', () => {
    cy.visit('/Orders')

    cy.location('pathname').should('eq', '/Account/Login')
    cy.location('search').should('contain', 'ReturnUrl=%2FOrders')
    cy.getByCy('login-email').should('be.visible')
  })

  it('returns an admin to the originally requested local route after login', () => {
    cy.visit('/Users')

    cy.location('pathname').should('eq', '/Account/Login')
    cy.getByCy('login-email').type(adminEmail)
    cy.getByCy('login-password').type(password, { log: false })
    cy.getByCy('login-submit').click()

    cy.location('pathname').should('eq', '/Users')
    cy.contains('h1', 'Usuarios').should('be.visible')
  })

  it('logs out and requires authentication for protected routes again', () => {
    cy.loginAs('Admin')
    cy.visit('/')

    cy.get('#userDropdown').click()
    cy.getByCy('logout-submit').click()

    cy.location('pathname').should('eq', '/Account/Login')

    cy.visit('/Orders')
    cy.location('pathname').should('eq', '/Account/Login')
  })

  it('blocks Consulta from user administration', () => {
    cy.loginAs('Consulta')
    cy.visit('/Users')

    cy.location('pathname').should('eq', '/Account/AccessDenied')
    cy.getByCy('access-denied-message').should('be.visible')
  })

  it('blocks Consulta from a representative write operation', () => {
    cy.loginAs('Consulta')

    cy.request({
      method: 'POST',
      url: '/Orders/Import',
      followRedirect: false,
      failOnStatusCode: false
    }).then(response => {
      expect(response.status).to.eq(302)
      expect(response.redirectedToUrl).to.include('/Account/AccessDenied')
    })
  })

  it('allows Operador to authenticate but blocks user administration', () => {
    cy.loginAs('Operador')
    cy.visit('/')

    cy.contains('h1', 'Painel Craftsman').should('be.visible')

    cy.visit('/Users')
    cy.location('pathname').should('eq', '/Account/AccessDenied')
    cy.getByCy('access-denied-message').should('be.visible')
  })
})
