# Especificação de Testes E2E com Cypress para ASP.NET MVC

## 1. Objetivo

Implementar uma suíte de testes End-to-End utilizando Cypress para validar os principais fluxos funcionais de uma aplicação ASP.NET MVC.

Os testes devem executar contra a aplicação real em ambiente controlado, validando a integração entre:

- Interface web;
- ASP.NET MVC;
- Autenticação e autorização;
- APIs internas;
- Banco de dados;
- Serviços dependentes relevantes ao fluxo.

O objetivo não é testar todas as combinações possíveis pela interface, mas garantir que os fluxos críticos do sistema funcionem corretamente de ponta a ponta.

---

## 2. Princípios

A suíte E2E deve seguir os seguintes princípios:

- Testes independentes entre si;
- Execução determinística;
- Dados de teste controlados;
- Mínima dependência de estado anterior;
- Seletores de UI estáveis;
- Testes focados em comportamento de negócio;
- Evitar validações que pertencem a testes unitários ou de integração;
- Possibilidade de execução local e em CI;
- Diagnóstico simples quando um teste falhar.

---

## 3. Arquitetura

Estrutura esperada:

```text
Repository
│
├── src/
│   └── MyApplication/
│       ├── Controllers/
│       ├── Models/
│       ├── Views/
│       └── ...
│
├── tests/
│   └── e2e/
│       ├── cypress/
│       │   ├── e2e/
│       │   ├── fixtures/
│       │   ├── support/
│       │   │   ├── commands.js
│       │   │   └── e2e.js
│       │   └── helpers/
│       │
│       ├── cypress.config.js
│       └── package.json
│
└── ...
```

Os testes Cypress devem permanecer separados da aplicação ASP.NET MVC.

---

# 4. Ambiente E2E

Deve existir um ambiente específico para execução de testes E2E.

Exemplo:

```text
ASPNETCORE_ENVIRONMENT=E2E
```

A configuração E2E deve possuir:

- banco de dados isolado;
- configurações específicas;
- credenciais exclusivas para testes;
- integrações externas desabilitadas ou substituídas;
- mecanismos para criar e limpar dados.

Exemplo:

```text
appsettings.json
appsettings.Development.json
appsettings.E2E.json
```

O ambiente E2E não deve utilizar banco de produção.

---

# 5. Inicialização da aplicação

Antes da execução dos testes:

1. preparar banco;
2. executar migrations;
3. iniciar aplicação ASP.NET MVC;
4. aguardar endpoint de healthcheck;
5. executar Cypress.

Fluxo:

```text
Database
   ↓
Migrations
   ↓
ASP.NET MVC
   ↓
Healthcheck OK
   ↓
Cypress
```

A aplicação deverá disponibilizar um endpoint como:

```http
GET /health
```

Resposta esperada:

```text
HTTP 200
```

O Cypress somente deverá iniciar depois que a aplicação estiver disponível.

---

# 6. Banco de dados

Os testes devem possuir controle explícito sobre os dados utilizados.

Não utilizar dados previamente existentes no banco.

Cada teste deverá:

```text
Arrange
↓
Execute
↓
Assert
```

Os dados necessários deverão ser criados durante o Arrange.

Exemplo:

```javascript
beforeEach(() => {
    cy.task('db:reset')
    cy.task('db:seedUser', {
        email: 'user@test.local'
    })
})
```

---

# 7. Reset de estado

O banco deve ser restaurado para um estado conhecido antes de cada teste ou conjunto de testes.

Estratégias possíveis:

- recriação completa;
- truncamento das tabelas;
- restore de snapshot;
- Respawn;
- transaction rollback;
- container efêmero.

A implementação escolhida deve priorizar velocidade e isolamento.

Contrato esperado:

```javascript
cy.task('db:reset')
```

---

# 8. Seed de dados

Seeds devem ser explícitos.

Evitar um banco global com centenas de registros usados implicitamente pelos testes.

Preferir:

```javascript
cy.task('seed:createUser', {
    id: '...',
    name: 'Felipe',
    email: 'felipe@test.local'
})
```

ao invés de:

```text
"Use o usuário número 15 que já existe no banco"
```

Os testes devem declarar suas dependências.

---

# 9. Seletores HTML

Os testes não devem depender de classes CSS destinadas exclusivamente à apresentação.

Evitar:

```javascript
cy.get('.btn.btn-primary.mt-3')
```

Preferir atributos dedicados:

```html
<button data-cy="save-user">
    Salvar
</button>
```

Teste:

```javascript
cy.get('[data-cy="save-user"]').click()
```

Convenção:

```text
data-cy="<contexto>-<elemento>"
```

Exemplos:

```text
data-cy="login-email"
data-cy="login-password"
data-cy="login-submit"
data-cy="customer-name"
data-cy="customer-save"
```

---

# 10. Organização dos testes

Os testes devem ser organizados por funcionalidade.

Exemplo:

```text
cypress/e2e/

authentication/
    login.cy.js
    logout.cy.js

customers/
    create-customer.cy.js
    edit-customer.cy.js
    delete-customer.cy.js

orders/
    create-order.cy.js
    cancel-order.cy.js
```

Evitar arquivos genéricos:

```text
tests.cy.js
app.cy.js
system.cy.js
```

---

# 11. Estrutura de um teste

Utilizar uma estrutura próxima de:

```text
Given
When
Then
```

Exemplo:

```javascript
describe('Create customer', () => {

    beforeEach(() => {
        cy.task('db:reset')
        cy.login()
    })

    it('creates a customer', () => {

        cy.visit('/customers/create')

        cy.get('[data-cy="customer-name"]')
            .type('Felipe')

        cy.get('[data-cy="customer-email"]')
            .type('felipe@example.com')

        cy.get('[data-cy="customer-save"]')
            .click()

        cy.url()
            .should('include', '/customers')

        cy.contains('Felipe')
            .should('be.visible')
    })

})
```

---

# 12. Autenticação

Evitar realizar login pela UI em todos os testes.

A UI de login deverá possuir seus próprios testes.

Os demais testes devem utilizar autenticação programática.

Exemplo:

```javascript
Cypress.Commands.add('login', () => {

    cy.request({
        method: 'POST',
        url: '/account/login',
        form: true,
        body: {
            username: 'e2e-user',
            password: 'e2e-password'
        }
    })

})
```

Quando possível utilizar:

```javascript
cy.session()
```

Exemplo:

```javascript
Cypress.Commands.add('login', () => {

    cy.session('default-user', () => {

        cy.request({
            method: 'POST',
            url: '/account/login',
            body: {
                username: 'e2e-user',
                password: 'e2e-password'
            }
        })

    })

})
```

---

# 13. Commands

Comportamentos reutilizáveis podem ser implementados através de Cypress Commands.

Exemplo:

```javascript
Cypress.Commands.add('createCustomer', customer => {

    cy.visit('/customers/create')

    cy.get('[data-cy="customer-name"]')
        .type(customer.name)

    cy.get('[data-cy="customer-save"]')
        .click()

})
```

Entretanto, Commands não devem esconder excessivamente o comportamento do teste.

Evitar:

```javascript
cy.executeCompleteBusinessFlow()
```

Preferir commands pequenos e semanticamente claros.

---

# 14. Page Objects

Não utilizar Page Object tradicional como padrão obrigatório.

Preferir abstrações pequenas relacionadas à intenção.

Exemplo:

```javascript
export const LoginPage = {

    visit() {
        cy.visit('/account/login')
    },

    login(username, password) {

        cy.get('[data-cy="login-email"]')
            .type(username)

        cy.get('[data-cy="login-password"]')
            .type(password)

        cy.get('[data-cy="login-submit"]')
            .click()
    }
}
```

Criar abstrações somente quando houver reutilização real.

---

# 15. APIs internas para testes

Caso necessário, a aplicação poderá disponibilizar endpoints exclusivos para testes.

Exemplo:

```text
POST /e2e/reset
POST /e2e/users
POST /e2e/orders
```

Esses endpoints somente deverão estar disponíveis quando:

```text
ASPNETCORE_ENVIRONMENT=E2E
```

Exemplo:

```csharp
if (app.Environment.IsEnvironment("E2E"))
{
    app.MapE2ETestEndpoints();
}
```

Eles nunca devem estar disponíveis em produção.

---

# 16. Integrações externas

Chamadas externas não devem tornar a suíte instável.

Integrações como:

```text
Stripe
Salesforce
SAP
Webhooks
APIs externas
SMTP
SMS
```

devem ser substituídas por:

- fake;
- stub;
- sandbox;
- WireMock;
- serviço fake executado localmente.

O comportamento da aplicação em relação à integração deve continuar sendo testado.

---

# 17. Interceptação HTTP

`cy.intercept()` deverá ser usado principalmente para:

- observar requests;
- aguardar operações;
- validar requests específicas.

Exemplo:

```javascript
cy.intercept('POST', '/customers')
    .as('createCustomer')

cy.get('[data-cy="customer-save"]')
    .click()

cy.wait('@createCustomer')
    .its('response.statusCode')
    .should('eq', 200)
```

Não substituir sistematicamente o backend real por intercepts, pois isso descaracterizaria o teste E2E.

---

# 18. Esperas

Não utilizar waits arbitrários.

Proibido:

```javascript
cy.wait(5000)
```

Preferir sincronização baseada em estado.

Exemplo:

```javascript
cy.wait('@createCustomer')
```

ou:

```javascript
cy.get('[data-cy="success-message"]')
    .should('be.visible')
```

---

# 19. Fluxos críticos

Devem possuir cobertura E2E prioritariamente:

### Autenticação

```text
Login
Logout
Acesso não autorizado
Expiração de sessão
```

### Operações principais

```text
Criar
Consultar
Editar
Excluir
```

quando essas operações representarem funcionalidades críticas.

### Workflows

Exemplo:

```text
Criar pedido
↓
Adicionar produtos
↓
Confirmar pedido
↓
Processar pagamento
↓
Visualizar status
```

Preferir validar workflows completos em vez de criar dezenas de testes pequenos via UI.

---

# 20. Cenários negativos

Devem existir testes para comportamentos relevantes de erro.

Exemplos:

```text
Credenciais inválidas
Formulário inválido
Permissão insuficiente
Recurso inexistente
Erro de integração
Conflito de dados
```

Não é necessário reproduzir via E2E todas as validações unitárias existentes.

---

# 21. Controle de permissões

Quando a aplicação possuir diferentes roles:

```text
Admin
Manager
User
Guest
```

deverão existir usuários específicos para cada papel.

Exemplo:

```javascript
cy.loginAs('admin')
cy.loginAs('manager')
cy.loginAs('user')
```

Os testes devem validar os fluxos relevantes de autorização.

---

# 22. Configuração Cypress

Exemplo:

```javascript
const { defineConfig } = require('cypress')

module.exports = defineConfig({

    e2e: {

        baseUrl: 'http://localhost:5000',

        video: true,

        screenshotOnRunFailure: true,

        setupNodeEvents(on, config) {

            on('task', {

                async 'db:reset'() {
                    // reset database
                    return null
                }

            })

        }

    }

})
```

---

# 23. Configuração por ambiente

Variáveis sensíveis ou específicas devem ser fornecidas externamente.

Exemplo:

```text
CYPRESS_BASE_URL=http://localhost:5000
CYPRESS_USERNAME=e2e-user
CYPRESS_PASSWORD=...
```

Nunca armazenar credenciais reais no repositório.

---

# 24. Execução local

Comando padrão:

```bash
npm run e2e
```

Internamente:

```bash
cypress run
```

Para desenvolvimento:

```bash
npm run e2e:open
```

Executando:

```bash
cypress open
```

---

# 25. Execução completa local

Idealmente existir um único comando:

```bash
npm run test:e2e
```

Responsável por:

```text
subir banco
↓
executar migrations
↓
subir ASP.NET
↓
aguardar healthcheck
↓
executar Cypress
↓
derrubar ambiente
```

---

# 26. Docker

Preferencialmente o ambiente deve poder ser executado via Docker Compose.

Exemplo:

```text
docker-compose.e2e.yml
```

Serviços:

```yaml
services:

  app:

  database:

  external-services:

  cypress:
```

Fluxo esperado:

```bash
docker compose -f docker-compose.e2e.yml up \
    --abort-on-container-exit \
    --exit-code-from cypress
```

---

# 27. CI

A pipeline deverá executar:

```text
Build
↓
Start dependencies
↓
Database migration
↓
Start application
↓
Healthcheck
↓
Cypress
↓
Collect artifacts
```

Artifacts em caso de falha:

```text
screenshots
videos
logs da aplicação
logs do Cypress
```

---

# 28. Paralelismo

Os testes devem ser projetados desde o início para permitir paralelização.

Isso implica:

- nenhum teste depender de outro;
- dados exclusivos;
- ausência de estado global mutável;
- usuários independentes quando necessário.

Nunca criar dependências como:

```text
Teste A cria usuário
Teste B edita usuário criado pelo teste A
Teste C exclui usuário criado pelo teste A
```

Cada teste deverá construir seu próprio cenário.

---

# 29. Flakiness

Um teste que falha intermitentemente deve ser tratado como defeito.

Não utilizar retry para mascarar instabilidade estrutural.

Investigar:

```text
race conditions
dados compartilhados
timeouts
requests não aguardadas
renderizações assíncronas
dependência externa
estado residual
```

Retry pode existir como proteção adicional da infraestrutura, mas não como solução do problema.

---

# 30. Performance da suíte

Meta recomendada:

```text
teste individual crítico:
< 10 segundos

suíte inicial:
< 5 minutos
```

Evitar utilizar UI para criar toda a preparação do teste.

Por exemplo, em vez de:

```text
login
abrir clientes
criar cliente
abrir produtos
criar produto
criar pedido
```

para testar pedido, realizar setup diretamente:

```text
Seed Customer
Seed Product
↓
UI:
Create Order
```

A interface deve executar somente a parte relevante ao comportamento que está sendo validado.

---

# 31. Pirâmide de testes

E2E deve representar apenas uma parte da estratégia.

Distribuição conceitual:

```text
                 E2E
               /     \
          Integration
         /             \
        Unit Tests
```

Quantidade aproximada:

```text
Unit tests          muitos
Integration tests   moderados
E2E tests           poucos e estratégicos
```

O Cypress deve validar principalmente jornadas de usuário.

---

# 32. Critérios de aceite

A implementação estará completa quando:

- Cypress estiver integrado ao repositório;
- existir ambiente `E2E`;
- banco puder ser resetado automaticamente;
- dados de teste puderem ser criados programaticamente;
- aplicação puder ser iniciada automaticamente;
- Cypress aguardar readiness da aplicação;
- autenticação programática estiver disponível;
- elementos críticos utilizarem `data-cy`;
- pelo menos um fluxo crítico estiver implementado;
- screenshots forem gerados em falhas;
- vídeos forem armazenados em CI quando necessário;
- a suíte puder ser executada por um único comando;
- testes funcionarem em ambiente local;
- testes funcionarem na pipeline;
- nenhum teste depender da execução anterior.

---

# 33. Primeiro fluxo de referência

Como primeira implementação, utilizar um fluxo simples e completo.

Exemplo:

```text
Login
↓
Listar clientes
↓
Criar cliente
↓
Visualizar cliente criado
```

Teste:

```javascript
describe('Customer lifecycle', () => {

    beforeEach(() => {

        cy.task('db:reset')

        cy.loginAs('admin')

    })

    it('creates a customer', () => {

        cy.visit('/customers')

        cy.get('[data-cy="customer-create"]')
            .click()

        cy.get('[data-cy="customer-name"]')
            .type('Felipe')

        cy.get('[data-cy="customer-email"]')
            .type('felipe@test.local')

        cy.get('[data-cy="customer-save"]')
            .click()

        cy.get('[data-cy="customer-details"]')
            .should('contain', 'Felipe')

    })

})
```

Este teste servirá como referência arquitetural para os demais testes E2E.

---

# 34. Regra arquitetural principal

O teste E2E deve controlar o estado **por baixo** e validar o comportamento **por cima**.

```text
Setup
    ↓
API / banco / fixture

Execução
    ↓
Browser / Cypress

Validação
    ↓
UI + estado relevante
```

Ou seja:

```text
Preparar rapidamente
Testar realisticamente
Validar objetivamente
```

Essa regra evita que a suíte se transforme em uma sequência lenta e frágil de automações de interface.