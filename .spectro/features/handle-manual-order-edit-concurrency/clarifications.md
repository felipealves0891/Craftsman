<!-- spectro-managed: true -->
# Clarificações: Tratar concorrência ao editar pedido manual

| ID | Dúvida | Classificação | Impacto | Decisão | Responsável | Data |
|---|---|---|---|---|---|---|
| `Q-001` | Qual comportamento deve ocorrer após o conflito de concorrência? | Blocking | Define a resposta do POST e a forma de recuperação do usuário. | Manter o formulário com os dados enviados e exibir mensagem segura orientando o usuário a recarregar os dados e tentar novamente. | Usuário | 2026-11-07 |
| `Q-002` | Qual deve ser a abrangência dos logs das manipulações do EF? | Major | Define o custo e o alcance da instrumentação. | Instrumentar somente o fluxo de edição manual do pedido, incluindo as operações relacionadas ao pedido, itens, cliente, produção e acerto financeiro da tentativa. | Usuário | 2026-11-07 |
| `Q-003` | Quais dados devem ser registrados para investigação? | Major | Define o nível de diagnóstico e o risco de exposição de dados. | Registrar metadados mínimos do fluxo percorrido pela requisição: correlação, pedido, etapas, operações, estados, resultado, duração e tipo da exceção; não registrar corpo completo, contatos do cliente ou valores antes/depois. | Usuário | 2026-11-07 |

## Resultado

- Bloqueios restantes: 0
- Pronto para planejamento: sim

