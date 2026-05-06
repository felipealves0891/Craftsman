# Especificacao I-010 - Ajustes em movimentos de estoque

## Summary

Ajustar o fluxo manual de movimentos de estoque para simplificar o formulario, condicionar o motivo a saidas, melhorar mensagens de feedback e permitir lancamentos manuais de ajuste.

## Objetivo

- Ocultar o campo de referencia no lancamento manual de movimentos.
- Solicitar motivo apenas quando o movimento for saida (`Outbound`).
- Padronizar alertas de erro e sucesso do fluxo manual.
- Permitir movimento manual do tipo ajuste (`Adjustment`).

## Fora de escopo

- Remover referencia das movimentacoes internas geradas por outros fluxos.
- Alterar o historico/auditoria de movimentos ja registrados.
- Alterar regras de consumo automatico de producao.
- Criar edicao ou exclusao de movimentos existentes.

## Regras de negocio

- Movimentos manuais nao devem exigir referencia informada pelo usuario.
- O sistema pode preencher referencia interna quando necessario para manter rastreabilidade tecnica, desde que o campo nao seja exposto no formulario manual.
- Motivo deve ser obrigatorio para saida manual.
- Motivo nao deve ser obrigatorio para entrada manual.
- Motivo nao deve ser obrigatorio para ajuste manual.
- Ajustes manuais devem continuar respeitando as validacoes de saldo e quantidade existentes.
- Erros de validacao devem ser exibidos como mensagens claras para o usuario, sem stack trace ou detalhe tecnico.
- Sucesso deve confirmar que o movimento foi registrado.

## Aplicacao e UI

Atualizar a tela `Inventory/Movements.cshtml`:

- Remover ou ocultar o campo de referencia do formulario manual.
- Exibir o campo motivo apenas quando o tipo selecionado for saida.
- Manter o campo motivo disponivel novamente se o usuario trocar o tipo para saida.
- Disponibilizar ajuste como opcao de tipo manual.
- Exibir alertas de erro e sucesso no padrao visual usado pela aplicacao.

Atualizar `InventoryAppService` e modelos relacionados:

- Nao exigir referencia para lancamento manual.
- Validar motivo apenas para saida manual.
- Aceitar ajuste manual quando a entrada for valida.
- Retornar mensagens de erro e sucesso adequadas ao controller/view.

## Criterios de aceitacao

- Usuario nao visualiza nem preenche referencia no formulario manual de movimentos de estoque.
- Movimentos gerados pelo fluxo manual continuam auditaveis sem referencia informada pela tela.
- O campo motivo aparece e e obrigatorio apenas para saidas manuais.
- Entradas manuais nao exigem motivo.
- Ajustes manuais nao exigem motivo.
- Mensagens de erro informam o problema validado sem expor detalhe tecnico.
- Mensagem de sucesso confirma que o movimento foi registrado.
- O tipo ajuste fica disponivel para lancamento manual.
- Ajustes manuais respeitam as regras de saldo ja existentes no dominio e na aplicacao.

## Test Plan

Criar ou atualizar testes para:

- Razor/view:
  - formulario manual nao contem campo de referencia visivel;
  - motivo e condicionado ao tipo saida;
  - tipo ajuste aparece entre as opcoes manuais.
- Aplicacao:
  - saida manual sem motivo retorna erro de validacao;
  - entrada manual sem motivo e aceita quando valida;
  - ajuste manual sem motivo e aceito quando valido;
  - alerta de erro usa mensagem clara;
  - alerta de sucesso confirma registro do movimento.
- Integracao, se houver alteracao de persistencia:
  - movimento manual criado sem referencia informada pela tela pode ser salvo e consultado.

Rodar ao final:

```powershell
$env:DOTNET_CLI_HOME='D:\Source\Repos\Dotnet\Craftsman\.dotnet-home\.dotnet'
dotnet build Craftsman.slnx -v:minimal /p:UseSharedCompilation=false /nr:false
dotnet test Craftsman.slnx --no-build -v:minimal
```

## Checklist de conclusao

- [ ] Formulario manual sem campo de referencia.
- [ ] Motivo condicionado a saida manual.
- [ ] Alertas de erro e sucesso ajustados.
- [ ] Ajuste manual liberado.
- [ ] Testes automatizados criados ou atualizados.
- [ ] Build executado.
- [ ] Testes executados.
- [ ] I-010 marcada como concluida em `.codex/tasks/inventory/tasks.md` apos implementacao.
