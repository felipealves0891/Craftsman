# Especificacao SH-006 - Selecionar pedido em modal ao criar envio

## Summary

Ajustar o fluxo de criacao de envio para permitir que o usuario selecione o pedido em um modal com detalhes do pedido, em vez de precisar informar manualmente o identificador do pedido.

## Objetivo

- Facilitar a selecao do pedido ao criar um envio.
- Exibir detalhes suficientes do pedido no modal para evitar selecao incorreta.
- Manter o envio vinculado ao pedido interno existente.

## Fora de escopo

- Alterar regras de status de envio.
- Alterar regras de rastreamento ou integracoes com transportadoras.
- Criar ou editar pedidos dentro do fluxo de envio.
- Alterar o ciclo de vida do pedido.

## Regras de negocio

- O envio deve continuar vinculado a um pedido interno existente.
- O usuario deve selecionar apenas pedidos disponiveis para envio conforme as regras ja existentes na aplicacao.
- O modal deve exibir dados do pedido suficientes para identificacao operacional, como identificador, origem, status, data de envio e itens.
- A selecao do pedido nao deve alterar os dados do pedido.
- Validacoes de criacao de envio devem continuar passando pelo servico de aplicacao.

## Aplicacao e UI

Atualizar a tela `Shipments/Create.cshtml`:

- Substituir o campo livre de `OrderId` por um fluxo de selecao em modal.
- Abrir modal para pesquisar/listar pedidos disponiveis.
- Exibir detalhes do pedido no modal antes da selecao.
- Preencher o pedido selecionado no formulario de envio.
- Manter o usuario capaz de informar o codigo de rastreio no formulario.

Atualizar servicos/modelos quando necessario:

- Disponibilizar dados de pedidos para o modal sem expor entidades de persistencia.
- Reusar consultas/modelos de pedido existentes quando possivel.
- Manter a criacao do envio em `ShippingAppService`/servico de dominio.

## Criterios de aceitacao

- Usuario consegue abrir um modal na tela de criacao de envio para selecionar um pedido.
- O modal lista pedidos disponiveis para envio.
- O modal exibe detalhes do pedido antes da selecao, incluindo itens.
- Ao selecionar um pedido, o formulario mostra o pedido escolhido e envia seu `OrderId`.
- O usuario nao precisa digitar manualmente o `OrderId`.
- A criacao do envio continua validando pedido existente e codigo de rastreio pelo fluxo atual de aplicacao/dominio.
- A interface nao acessa `DbContext` diretamente.

## Test Plan

Criar ou atualizar testes para:

- Razor/view:
  - tela de criacao de envio possui acionador do modal de pedidos;
  - campo de `OrderId` nao fica exposto como entrada manual principal;
  - modal exibe dados e itens do pedido.
- Aplicacao:
  - consulta de pedidos para selecao retorna apenas dados necessarios ao modal;
  - criacao de envio usa o `OrderId` selecionado.
- Integracao, se houver alteracao de consulta/persistencia:
  - pedidos disponiveis para selecao podem ser consultados com seus itens.

Rodar ao final:

```powershell
$env:DOTNET_CLI_HOME='D:\Source\Repos\Dotnet\Craftsman\.dotnet-home\.dotnet'
dotnet build Craftsman.slnx -v:minimal /p:UseSharedCompilation=false /nr:false
dotnet test Craftsman.slnx --no-build -v:minimal
```

## Checklist de conclusao

- [ ] Modal de selecao de pedido criado na tela de envio.
- [ ] Detalhes do pedido exibidos no modal.
- [ ] Pedido selecionado preenchendo o formulario de envio.
- [ ] Campo livre de `OrderId` removido do fluxo principal.
- [ ] Testes automatizados criados ou atualizados.
- [ ] Build executado.
- [ ] Testes executados.
- [ ] SH-006 marcada como concluida em `.codex/tasks/shipping/tasks.md` apos implementacao.
