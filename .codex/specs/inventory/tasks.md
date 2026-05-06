# Inventory Specs Tasks

## Tarefas

### I-009 - Alerta de estoque baixo
- [x] Gerar especificacao da tarefa I-009.

Critérios de aceitacao:
- A especificacao descreve regras de aviso e critico para materia-prima.
- A especificacao cobre cadastro/edicao, envio para producao, entrada de estoque e sino de notificacoes.
- A especificacao define plano de testes e checklist de conclusao.

### I-010 - Ajustes em movimentos de estoque
- [x] Gerar especificacao da tarefa I-010.
- [x] Ocultar o campo de referencia no lancamento manual.
- [x] Exibir e exigir motivo apenas para saida (`Outbound`).
- [x] Ajustar alertas de erro e sucesso do fluxo manual.
- [x] Liberar ajuste (`Adjustment`) quando o movimento for manual.
- [x] Criar ou atualizar testes automatizados.

Criterios de aceitacao:
- Usuario nao visualiza nem informa referencia no formulario manual de movimentos de estoque.
- Motivo aparece e e obrigatorio apenas quando o tipo selecionado for saida.
- Entrada e ajuste manual nao exigem motivo.
- Alertas de erro e sucesso sao claros, consistentes e sem detalhes tecnicos.
- Ajuste manual fica disponivel e respeita as regras de saldo existentes.
