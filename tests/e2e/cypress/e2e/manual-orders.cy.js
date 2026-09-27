const futureDate = () => {
  const date = new Date()
  date.setDate(date.getDate() + 10)
  return date.toISOString().slice(0, 10)
}

const removeTrailingBlankItem = () => {
  cy.getByCy('manual-order-item-product').last().then($select => {
    if ($select.val() === '') {
      cy.getByCy('manual-order-remove-item').last().click()
    }
  })
}

const fillItem = (index, productId, description, quantity, unitPrice) => {
  cy.getByCy('manual-order-item-product').eq(index).select(productId)
  cy.getByCy('manual-order-item-description').eq(index).clear().type(description)
  cy.getByCy('manual-order-item-quantity').eq(index).clear().type(`${quantity}`)
  cy.getByCy('manual-order-item-unit-price').eq(index).clear().type(unitPrice)
}

describe('Manual orders', () => {
  beforeEach(() => {
    cy.resetE2E()
  })

  it('creates an order source and a manual order with two items', () => {
    cy.seedManualOrdersScenario('catalog').then(({ productId, source }) => {
      const createdSource = 'E2E Fonte Cypress'
      const reference = 'E2E-MANUAL-CREATE-001'

      cy.loginAs('Operador')
      cy.visit('/ManualOrders/Create')

      cy.getByCy('order-source-create-name').type(createdSource)
      cy.getByCy('order-source-create-submit').click()

      cy.location('pathname').should('eq', '/ManualOrders/Create')
      cy.getByCy('manual-order-source').should('contain', createdSource)

      cy.getByCy('manual-order-source').select(createdSource)
      cy.getByCy('manual-order-reference').clear().type(reference)
      cy.getByCy('manual-order-shipping-date').clear().type(futureDate())

      fillItem(0, productId, 'E2E item principal', 2, '15,50')
      fillItem(1, productId, 'E2E item adicional', 1, '20,00')
      removeTrailingBlankItem()

      cy.getByCy('manual-order-save').click()

      cy.location('pathname').should('contain', '/Orders/Details/')
      cy.getByCy('order-details').should('contain', reference)
      cy.getByCy('order-details').should('contain', createdSource)
      cy.contains('td', 'E2E item principal').should('be.visible')
      cy.contains('td', 'E2E item adicional').should('be.visible')
      cy.contains('td', '2').should('be.visible')
      cy.contains('td', '1').should('be.visible')
      cy.getByCy('order-edit').should('be.visible')
      cy.getByCy('order-link-items').should('be.visible')
    })
  })

  it('updates dynamic item and order totals', () => {
    cy.seedManualOrdersScenario('catalog').then(({ productId }) => {
      cy.loginAs('Operador')
      cy.visit('/ManualOrders/Create')

      fillItem(0, productId, 'E2E item com total', 2, '15,50')

      cy.getByCy('manual-order-item-total').first().should('contain', '31,00')
      cy.getByCy('manual-order-total').should('contain', '31,00')
    })
  })

  it('adds and removes item rows before saving remaining valid rows', () => {
    cy.seedManualOrdersScenario('catalog').then(({ productId, source }) => {
      const reference = 'E2E-MANUAL-ROWS-001'

      cy.loginAs('Operador')
      cy.visit('/ManualOrders/Create')

      cy.getByCy('manual-order-source').select(source)
      cy.getByCy('manual-order-reference').clear().type(reference)
      cy.getByCy('manual-order-shipping-date').clear().type(futureDate())

      cy.getByCy('manual-order-add-item').click()
      fillItem(0, productId, 'E2E item removido', 1, '10,00')
      fillItem(1, productId, 'E2E item mantido', 3, '12,00')
      removeTrailingBlankItem()
      cy.getByCy('manual-order-remove-item').first().click()

      cy.getByCy('manual-order-save').click()

      cy.location('pathname').should('contain', '/Orders/Details/')
      cy.getByCy('order-details').should('contain', reference)
      cy.contains('td', 'E2E item mantido').should('be.visible')
      cy.contains('td', 'E2E item removido').should('not.exist')
      cy.contains('td', '3').should('be.visible')
    })
  })

  it('edits an existing manual order', () => {
    cy.seedManualOrdersScenario('editable-order').then(({ orderId, productId }) => {
      const updatedReference = 'E2E-MANUAL-EDITED-001'

      cy.loginAs('Operador')
      cy.visit(`/Orders/Details/${orderId}`)
      cy.getByCy('order-edit').click()

      cy.location('pathname').should('eq', `/ManualOrders/Edit/${orderId}`)
      cy.getByCy('manual-order-reference').clear().type(updatedReference)
      cy.getByCy('manual-order-item-product').first().select(productId)
      cy.getByCy('manual-order-item-quantity').first().clear().type('4')
      removeTrailingBlankItem()
      cy.getByCy('manual-order-save').click()

      cy.location('pathname').should('eq', `/Orders/Details/${orderId}`)
      cy.getByCy('order-details').should('contain', updatedReference)
      cy.contains('td', '4').should('be.visible')
    })
  })

  it('links an unlinked item to an internal product', () => {
    cy.seedManualOrdersScenario('unlinked-order').then(({ orderId, productId }) => {
      cy.loginAs('Operador')
      cy.visit(`/ManualOrders/LinkItems/${orderId}`)

      cy.getByCy('manual-order-link-product').first().select(productId)
      cy.getByCy('manual-order-link-submit').first().click()

      cy.location('pathname').should('eq', `/ManualOrders/LinkItems/${orderId}`)
      cy.getByCy('manual-order-link-row').first().should('contain', productId)

      cy.visit(`/Orders/Details/${orderId}`)
      cy.getByCy('order-items-tab').should('be.visible')
      cy.contains('td', productId).should('be.visible')
    })
  })

  it('rejects invalid manual order data in the browser form', () => {
    cy.seedManualOrdersScenario('validation-support').then(({ source }) => {
      cy.loginAs('Operador')
      cy.visit('/ManualOrders/Create')

      cy.getByCy('manual-order-source').select(source)
      cy.getByCy('manual-order-reference').clear().type('E2E-MANUAL-INVALID-001')
      cy.getByCy('manual-order-shipping-date').clear().type(futureDate())
      cy.getByCy('manual-order-save').click()

      cy.location('pathname').should('eq', '/ManualOrders/Create')
      cy.getByCy('manual-order-item-product').first().then($field => {
        expect($field[0].checkValidity()).to.eq(false)
        expect($field[0].validationMessage).to.not.eq('')
      })
      cy.getByCy('order-details').should('not.exist')
    })
  })

  it('blocks Consulta from manual order write operations', () => {
    cy.seedManualOrdersScenario('readonly-support').then(({ orderId, orderItemId, productId, source }) => {
      cy.loginAs('Consulta')

      cy.request({
        method: 'POST',
        url: '/ManualOrders/CreateSource',
        body: { name: 'E2E Consulta Source' },
        followRedirect: false,
        failOnStatusCode: false
      }).then(response => {
        expect(response.status).to.eq(302)
        expect(response.redirectedToUrl).to.include('/Account/AccessDenied')
      })

      cy.request({
        method: 'POST',
        url: '/ManualOrders/Create',
        body: {
          source,
          reference: 'E2E-CONSULTA-CREATE',
          shippingDate: futureDate()
        },
        followRedirect: false,
        failOnStatusCode: false
      }).then(response => {
        expect(response.status).to.eq(302)
        expect(response.redirectedToUrl).to.include('/Account/AccessDenied')
      })

      cy.request({
        method: 'POST',
        url: `/ManualOrders/Edit/${orderId}`,
        body: {
          source,
          reference: 'E2E-CONSULTA-EDIT',
          shippingDate: futureDate()
        },
        followRedirect: false,
        failOnStatusCode: false
      }).then(response => {
        expect(response.status).to.eq(302)
        expect(response.redirectedToUrl).to.include('/Account/AccessDenied')
      })

      cy.request({
        method: 'POST',
        url: '/ManualOrders/LinkItem',
        body: {
          orderId,
          itemId: orderItemId,
          productId
        },
        followRedirect: false,
        failOnStatusCode: false
      }).then(response => {
        expect(response.status).to.eq(302)
        expect(response.redirectedToUrl).to.include('/Account/AccessDenied')
      })
    })
  })
})
