### Medium — Ações de backend sem fallback visual

Alguns POSTs operacionais não tratam exceção com TempData/mensagem de tela, por exemplo avanço de produção em src/App/Controllers/
ProductionController.cs:33 e atualização de status de envio em src/App/Controllers/ShipmentsController.cs:63.

Impacto: erro de regra de negócio pode virar tela de erro/500 em vez de feedback amigável.

Telas Com Cobertura Boa

Login, criação de usuário, produto e matéria-prima têm DataAnnotations, asp-validation-summary, asp-validation-for e _ValidationScriptsPartial.

Movimentos de estoque cobre erros de backend com asp-validation-summary="All" e mostra validações relevantes, embora parte das regras venha do
serviço.