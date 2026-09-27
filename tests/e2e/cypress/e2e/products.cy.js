const fillProductForm = (name, status, duration, hourlyRate) => {
  cy.getByCy('product-name').clear().type(name)
  cy.getByCy('product-status').select(status)
  cy.getByCy('product-production-duration').clear().type(`${duration}`)
  cy.getByCy('product-hourly-rate').clear().type(hourlyRate)
}

const productRow = productName => {
  return cy.getByCy('product-row').contains(productName).parents('[data-cy="product-row"]')
}

describe('Products', () => {
  beforeEach(() => {
    cy.resetE2E()
  })

  it('lists seeded products with catalog fields', () => {
    cy.seedProductsScenario('listing').then(({ productName }) => {
      cy.loginAs('Operador')
      cy.visit('/Products')

      cy.getByCy('products-list').should('be.visible')
      productRow(productName).within(() => {
        cy.contains(productName).should('be.visible')
        cy.contains('Ativo').should('be.visible')
        cy.contains('2 hora(s)').should('be.visible')
        cy.contains('25,00').should('be.visible')
        cy.contains('1 item(ns)').should('be.visible')
        cy.getByCy('product-edit').should('be.visible')
        cy.getByCy('product-bom').should('be.visible')
      })
    })
  })

  it('creates a product through the browser form', () => {
    const productName = 'Produto E2E Cypress'

    cy.loginAs('Operador')
    cy.visit('/Products')
    cy.getByCy('product-create').click()

    cy.location('pathname').should('eq', '/Products/Create')
    fillProductForm(productName, 'Active', 4, '37,50')
    cy.getByCy('product-save').click()

    cy.location('pathname').should('eq', '/Products')
    productRow(productName).within(() => {
      cy.contains('Ativo').should('be.visible')
      cy.contains('4 hora(s)').should('be.visible')
      cy.contains('37,50').should('be.visible')
      cy.contains('0 item(ns)').should('be.visible')
    })
  })

  it('edits an existing product', () => {
    cy.seedProductsScenario('editable-product').then(({ productName }) => {
      const updatedName = 'Produto E2E Editado'

      cy.loginAs('Operador')
      cy.visit('/Products')

      productRow(productName).within(() => {
        cy.getByCy('product-edit').click()
      })

      cy.location('pathname').should('contain', '/Products/Edit/')
      fillProductForm(updatedName, 'Inactive', 6, '42,75')
      cy.getByCy('product-save').click()

      cy.location('pathname').should('eq', '/Products')
      productRow(updatedName).within(() => {
        cy.contains('Inativo').should('be.visible')
        cy.contains('6 hora(s)').should('be.visible')
        cy.contains('42,75').should('be.visible')
      })
      cy.getByCy('products-list').should('not.contain', productName)
    })
  })

  it('rejects invalid product form data', () => {
    cy.seedProductsScenario('validation-support')
    cy.loginAs('Operador')
    cy.visit('/Products/Create')

    cy.getByCy('product-name').clear().type('Produto E2E Invalido')
    cy.getByCy('product-production-duration').clear().type('0')
    cy.getByCy('product-save').click()

    cy.location('pathname').should('eq', '/Products/Create')
    cy.getByCy('product-production-duration').then($field => {
      expect($field[0].checkValidity()).to.eq(false)
      expect($field[0].validationMessage).to.not.eq('')
    })
  })

  it('saves bill of materials rows including a dynamically added row', () => {
    cy.seedProductsScenario('bom-support').then(({ productName, rawMaterials }) => {
      cy.loginAs('Operador')
      cy.visit('/Products')

      productRow(productName).within(() => {
        cy.getByCy('product-bom').click()
      })

      cy.location('pathname').should('contain', '/Products/BillOfMaterials/')
      cy.getByCy('bom-material').first().select(rawMaterials[0].id)
      cy.getByCy('bom-quantity').first().clear().type('2')

      cy.getByCy('bom-add-row').last().click()
      cy.getByCy('bom-material').last().select(rawMaterials[1].id)
      cy.getByCy('bom-quantity').last().clear().type('3')
      cy.getByCy('bom-save').click()

      cy.location('pathname').should('eq', '/Products')
      productRow(productName).within(() => {
        cy.contains('2 item(ns)').should('be.visible')
      })
    })
  })

  it('rejects decimal bill of materials quantities', () => {
    cy.seedProductsScenario('bom-support').then(({ productName, rawMaterials }) => {
      cy.loginAs('Operador')
      cy.visit('/Products')

      productRow(productName).within(() => {
        cy.getByCy('product-bom').click()
      })

      cy.getByCy('bom-material').first().select(rawMaterials[0].id)
      cy.getByCy('bom-quantity').first().clear().type('1.5')
      cy.getByCy('bom-save').click()

      cy.location('pathname').should('contain', '/Products/BillOfMaterials/')
      cy.getByCy('bom-quantity').first().then($field => {
        expect($field[0].checkValidity()).to.eq(false)
        expect($field[0].validationMessage).to.not.eq('')
      })
    })
  })

  it('creates a product mapping', () => {
    cy.seedProductsScenario('mapping-support').then(({ productId, productName }) => {
      cy.loginAs('Operador')
      cy.visit('/Products/Mappings')

      cy.getByCy('mapping-source').clear().type('Shopee')
      cy.getByCy('mapping-external-item').clear().type('SKU-E2E')
      cy.getByCy('mapping-product').select(productId)
      cy.getByCy('mapping-save').click()

      cy.location('pathname').should('eq', '/Products/Mappings')
      cy.contains('td', 'Shopee').should('be.visible')
      cy.contains('td', 'SKU-E2E').should('be.visible')
      cy.contains('td', productName).should('be.visible')
    })
  })

  it('blocks Consulta from product catalog write operations', () => {
    cy.seedProductsScenario('readonly-support').then(({ productId, rawMaterials }) => {
      cy.loginAs('Consulta')

      cy.request({
        method: 'POST',
        url: '/Products/Edit',
        body: {
          name: 'E2E Consulta Create',
          status: 'Active',
          productionDurationHours: 1,
          hourlyRate: 10
        },
        followRedirect: false,
        failOnStatusCode: false
      }).then(response => {
        expect(response.status).to.eq(302)
        expect(response.redirectedToUrl).to.include('/Account/AccessDenied')
      })

      cy.request({
        method: 'POST',
        url: '/Products/Edit',
        body: {
          id: productId,
          name: 'E2E Consulta Edit',
          status: 'Inactive',
          productionDurationHours: 2,
          hourlyRate: 20
        },
        followRedirect: false,
        failOnStatusCode: false
      }).then(response => {
        expect(response.status).to.eq(302)
        expect(response.redirectedToUrl).to.include('/Account/AccessDenied')
      })

      cy.request({
        method: 'POST',
        url: '/Products/BillOfMaterials',
        body: {
          productId,
          items: [
            {
              rawMaterialId: rawMaterials[0].id,
              quantityPerUnit: 1
            }
          ]
        },
        followRedirect: false,
        failOnStatusCode: false
      }).then(response => {
        expect(response.status).to.eq(302)
        expect(response.redirectedToUrl).to.include('/Account/AccessDenied')
      })

      cy.request({
        method: 'POST',
        url: '/Products/Mappings',
        body: {
          source: 'Shopee',
          externalItemId: 'SKU-CONSULTA',
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
