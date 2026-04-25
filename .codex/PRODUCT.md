# ESPECIFICAÇÃO DO SISTEMA CRAFTSMAN

## 1. CONTEXTO

Você deve atuar como um **arquiteto de software sênior especializado em sistemas distribuídos e modelagem de domínio (DDD)**.

O objetivo é projetar e implementar um:
Sistema de Gestão de Pedidos, Produção, Estoque e Financeiro
Este sistema deve suportar múltiplas origens de pedidos, controle de produção baseado em ficha técnica e cálculo financeiro pós-entrega.

---

## 2. OBJETIVO DO SISTEMA

O sistema deve ser capaz de:

1. Receber pedidos de múltiplas fontes
2. Normalizar pedidos em um modelo interno único
3. Mapear itens para produtos internos
4. Gerar agenda de produção
5. Controlar matéria-prima e estoque
6. Acompanhar envio e entrega
7. Calcular valores financeiros e margem

---

## 3. PRINCÍPIOS ARQUITETURAIS

- Domain-Driven Design (DDD)
- Separação por Bounded Contexts
- Baixo acoplamento com integrações externas
- Modelo orientado a eventos (preferencial)
- Extensibilidade como requisito central

Evitar:
- If/Else por origem
- Lógica de negócio em integrações
- Acoplamento direto com APIs externas

---

## 4. BOUNDED CONTEXTS

1. Sales Context  
2. Product Catalog Context  
3. Inventory Context  
4. Production Context  
5. Shipping Context  
6. Finance Context  
7. Integration Context  

---

## 5. REQUISITOS FUNCIONAIS

Entrada de pedidos, normalização, produtos, produção, matéria-prima, estoque, envio, rastreamento e financeiro conforme definido.

---

## 6. MODELOS PRINCIPAIS

Order, Product, RawMaterial, ProductionTask, Shipment, FinancialSettlement, etc.

---

## 7. REGRAS DE NEGÓCIO

- Pedido deve ser independente da origem  
- Produção depende de material  
- Estoque deve ser auditável  
- Financeiro baseado em custo real  

---

## 8. INTERFACES

IOrderSource, IOrderNormalizer, IProductionPlanner, IShippingTracker, ISettlementCalculator

---

## 9. FLUXO PRINCIPAL

Importar → Normalizar → Produção → Envio → Entrega → Financeiro

---

## 10. FOCO

Clareza, extensibilidade e separação de responsabilidades

---

## 11. TECNOLOGIAS

### Linguagem e Arquitetura do Projeto

Usaremos C# com Razor para ter apenas uma aplicação, mas temos que tomar muito cuidado com a separação de responsabilidades.

Para acesso a banco de dados, usaremos Entity Framework com uma representação dos dados propria, não usaremos as entidades, então precisamos de um repositorio para mapear os DTOs para Entidade e Entidade para DTOs.

Estrutura de Pastas:
```
/App
  /Controllers
  /Models
  /Views
  /wwwroot
  /Services
/Domains
  /Domain
    /Repositories
    /Entities
    /ObjectValues
    /Services
    /Events
/Infra
  /Persistence
    /Configurations
    /Migrations
    /Repositories
    /Services
```

### Banco de Dados

Vamos utilizar o PostgreSQL como banco de dados

### Cache

Vamos utilizar cache em memoria e estrategias de desativação quando o dado mudar