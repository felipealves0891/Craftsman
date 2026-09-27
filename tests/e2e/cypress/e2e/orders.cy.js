describe('Orders', () => {
  beforeEach(() => {
    cy.resetE2E()
  })

  it('lists seeded orders and opens order details', () => {
    cy.seedOrdersScenario('listing-details').then(({ externalOrderId }) => {
      cy.loginAs('Operador')
      cy.visit('/Orders')

      cy.getByCy('orders-list').should('be.visible')
      cy.getByCy('order-row').contains(externalOrderId).should('be.visible')
      cy.getByCy('order-create').should('have.attr', 'href', '/ManualOrders/Create')
      cy.getByCy('order-row').contains(externalOrderId).parents('[data-cy="order-row"]').within(() => {
        cy.getByCy('order-details-link').click()
      })

      cy.location('pathname').should('contain', '/Orders/Details/')
      cy.getByCy('order-details').should('contain', externalOrderId)
      cy.getByCy('order-items-tab').should('be.visible')
      cy.getByCy('order-link-items').should('be.visible')
      cy.getByCy('order-production-tab').should('be.visible').click()
      cy.contains('h2', 'Producao').should('be.visible')
      cy.getByCy('order-shipments-tab').should('be.visible').click()
      cy.contains('h2', 'Envios').should('be.visible')
      cy.getByCy('order-edit').should('be.visible')
    })
  })

  it('imports deterministic orders and shows the import outcome', () => {
    cy.seedOrdersScenario('import-support')
    cy.loginAs('Operador')
    cy.visit('/Orders')

    cy.getByCy('orders-import').click()

    cy.location('pathname').should('eq', '/Orders/Import')
    cy.contains('Importados').should('be.visible')
    cy.contains('SIM-1001').should('be.visible')
    cy.contains('Falhas na importacao').should('be.visible')
    cy.contains('SIM-INVALID').should('be.visible')
  })

  it('sends a plannable order to production', () => {
    cy.seedOrdersScenario('plannable-order').then(({ orderId }) => {
      cy.loginAs('Operador')
      cy.visit(`/Orders/Details/${orderId}`)

      cy.getByCy('order-send-to-production').click()

      cy.location('pathname').should('eq', `/Orders/Details/${orderId}`)
      cy.getByCy('flash-success').should('contain', 'Pedido enviado para producao.')
      cy.getByCy('order-production-tab').click()
      cy.contains('td', 'Planejada').should('be.visible')
    })
  })

  it('shows the existing failure message when production cannot be planned', () => {
    cy.seedOrdersScenario('unplannable-order').then(({ orderId }) => {
      cy.loginAs('Operador')
      cy.visit(`/Orders/Details/${orderId}`)

      cy.getByCy('order-send-to-production').click()

      cy.location('pathname').should('eq', `/Orders/Details/${orderId}`)
      cy.getByCy('flash-error').should('contain', 'Pedido nao pode ser enviado para producao.')
    })
  })

  it('deletes an eligible order and removes it from the listing', () => {
    cy.seedOrdersScenario('deletable-order').then(({ orderId, externalOrderId }) => {
      cy.loginAs('Operador')
      cy.visit(`/Orders/Details/${orderId}`)

      cy.getByCy('order-delete').click()

      cy.location('pathname').should('eq', '/Orders')
      cy.getByCy('orders-list').should('not.contain', externalOrderId)
    })
  })

  it('deletes an order with operational references using the complete deletion flow', () => {
    cy.seedOrdersScenario('referenced-delete').then(({ orderId, externalOrderId }) => {
      cy.loginAs('Operador')
      cy.visit(`/Orders/Details/${orderId}`)

      cy.getByCy('order-delete').click()

      cy.location('pathname').should('eq', '/Orders')
      cy.getByCy('orders-list').should('not.contain', externalOrderId)
    })
  })

  it('blocks Consulta from order write operations', () => {
    cy.seedOrdersScenario('plannable-order').then(({ orderId }) => {
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

      cy.request({
        method: 'POST',
        url: `/Orders/SendToProduction/${orderId}`,
        followRedirect: false,
        failOnStatusCode: false
      }).then(response => {
        expect(response.status).to.eq(302)
        expect(response.redirectedToUrl).to.include('/Account/AccessDenied')
      })

      cy.request({
        method: 'POST',
        url: `/Orders/Delete/${orderId}`,
        followRedirect: false,
        failOnStatusCode: false
      }).then(response => {
        expect(response.status).to.eq(302)
        expect(response.redirectedToUrl).to.include('/Account/AccessDenied')
      })
    })
  })
})
