<!-- spectro-managed: true -->
# Especificação: Suprimir alertas de estoque de matérias-primas inativas

## Problema e objetivo

- Problema: Uma matéria-prima inativada continua gerando notificações de estoque baixo ou crítico, embora ela não deva mais demandar reposição.
- Objetivo: Remover matérias-primas inativas de todos os alertas de falta de estoque e impedir novos alertas enquanto permanecerem inativas, preservando os dados de saldo e o histórico para consulta e auditoria.

## Escopo

### Incluído

- Excluir matérias-primas inativas de todos os alertas de estoque baixo e crítico e mantê-los desativados enquanto o status permanecer inativo.
- Refletir a inativação nos pontos da aplicação alimentados pelos alertas de estoque, incluindo o sino global, os alertas críticos da tela de movimentos e os alertas de estoque relacionados a pedidos.
- Manter inalterados os alertas aplicáveis às matérias-primas ativas.

### Excluído

- Excluir a matéria-prima inativa, seu saldo ou suas movimentações do cadastro, das consultas ou do histórico.
- Alterar os limites configurados de estoque mínimo e crítico.
- Alterar regras de movimentação, consumo, planejamento de produção ou disponibilidade de estoque.
- Alterar o comportamento visual das telas de cadastro e saldos fora das notificações de estoque.

## Requisitos

### Requisitos funcionais

- `FR-001`: Uma matéria-prima com status inativo não deve compor nem gerar alertas de estoque baixo ou crítico em nenhum dos pontos de alerta da aplicação, independentemente de seu saldo e dos limites de estoque configurados.
- `FR-002`: Após a inativação ser concluída com sucesso, todos os alertas da matéria-prima devem ser removidos das consultas e apresentações subsequentes, sem contabilizá-la na quantidade de notificações, e devem permanecer desativados enquanto seu status for inativo.
- `FR-003`: Matérias-primas ativas devem continuar gerando alertas de estoque conforme as regras existentes de saldo mínimo e crítico.

### Requisitos não funcionais

- Nenhum requisito não funcional adicional foi identificado para esta mudança.

### Restrições

- `CON-001`: A mudança não deve remover nem alterar movimentações, saldos, limites ou outros dados históricos da matéria-prima inativada.
- `CON-002`: A especificação define o comportamento observável e não determina em qual camada ou componente o filtro por status deve ser aplicado.

## Critérios de aceite

- `AC-001` (`FR-001`): Dada uma matéria-prima inativa com saldo igual ou inferior ao limite mínimo ou crítico, quando qualquer ponto da aplicação consultar alertas de estoque, então ela não aparece em nenhum alerta retornado.
- `AC-002` (`FR-002`): Dada uma matéria-prima ativa que aparece em um ou mais alertas de estoque, quando sua inativação for salva com sucesso e os alertas forem consultados novamente, então todos os seus alertas deixam de aparecer e ela deixa de ser contabilizada no indicador de notificações.
- `AC-003` (`FR-003`): Dada uma matéria-prima ativa com saldo que atende a um limite de alerta configurado, quando os alertas forem consultados, então ela continua aparecendo com a classificação de aviso ou crítico prevista pela regra existente.
- `AC-004` (`FR-001`, `CON-001`): Dada uma matéria-prima inativa suprimida dos alertas, quando seu saldo ou histórico de movimentações for consultado, então os dados existentes permanecem disponíveis e inalterados.
- `AC-005` (`FR-002`): Dada uma matéria-prima inativa cujo saldo permanece abaixo de um limite configurado, quando os alertas forem consultados novamente enquanto ela continuar inativa, então nenhum novo alerta é gerado para ela.

## Riscos, dependências e perguntas abertas

- Dependência: O status da matéria-prima deve estar persistido como inativo antes da nova consulta de alertas.
- Dependência: Os pontos atuais de notificação de estoque compartilham o serviço de alertas existente.
- Risco: Resultados de alertas mantidos em cache podem conservar temporariamente uma notificação anterior à inativação e violar o `FR-002`.
- Decisão confirmada: A supressão abrange todos os alertas de estoque baixo ou crítico da aplicação, e não somente o sino global.
