<!-- spectro-managed: true -->
# Constituição do projeto Spectro

## Princípios arquiteturais

- A aplicação mantém as responsabilidades separadas em App (`src/App`), Domains (`src/Domains`) e Infra (`src/Infra`). O domínio não depende do Entity Framework nem de detalhes de persistência.
- Os contextos de vendas, integração, catálogo, estoque, produção, envio e financeiro preservam suas regras de negócio próprias; fluxos entre etapas usam os contratos e eventos internos existentes.
- Pedidos são normalizados para um modelo interno independente da origem. Origem e identificador externo servem para rastreabilidade e deduplicação, não para impor formatos externos ao domínio.
- Produção opera sobre produtos internos e fichas técnicas. Planejamento valida vínculo, estado do produto, ficha técnica e disponibilidade antes de criar tarefas e consumir estoque.
- Estoque é derivado de movimentações auditáveis. Alterações de saldo devem gerar movimentos rastreáveis e não podem permitir saldo negativo.
- O acerto financeiro ocorre após a entrega confirmada, usa receita do pedido e custos rastreáveis, e não pode ser duplicado para o mesmo pedido.
- Persistência usa PostgreSQL e Entity Framework Core na infraestrutura; novas implementações preservam essa fronteira entre domínio e persistência.

## Padrões de código

- A solução usa .NET 8 e C#. Mantenha a organização existente nos projetos `src/App`, `src/Domains` e `src/Infra`, incluindo as separações atuais entre domínio, entidades/configurações de persistência e repositórios.
- Interfaces Razor, pedidos importados e pedidos manuais devem convergir para os mesmos modelos e regras internas, sem duplicar regras por origem.
- Use nomes e conceitos coerentes com os domínios e estados já existentes. Transições de status e invariantes pertencem ao domínio, não devem ser contornadas pela interface ou pela infraestrutura.
- Registre custos, referências e eventos de forma que operações de estoque, produção, envio e financeiro possam ser rastreadas até sua origem.
- Preserve compatibilidade com a versão .NET e os padrões de organização já adotados. Não introduza convenções de estilo ou abstrações novas sem necessidade demonstrável.

## Tratamento de erros

- Rejeite operações que violem invariantes do domínio, incluindo saída que gere estoque negativo, transições de status inválidas, planejamento sem pré-requisitos e acerto antes da entrega ou duplicado.
- Um pedido importado inválido deve ser reportado como falha da importação sem impedir que o resumo identifique os demais resultados; pedidos já importados devem ser ignorados conforme a regra de deduplicação.
- Falha em vínculo, ficha técnica ou disponibilidade de estoque não deve descartar o pedido salvo nem criar tarefas ou consumir materiais parcialmente. A correção deve permitir nova tentativa conforme o fluxo existente.
- Mensagens e diagnósticos devem identificar a operação e a causa de forma útil, sem expor credenciais, segredos ou dados sensíveis.
- Publique eventos apenas quando a transição correspondente tiver sido confirmada, preservando consistência entre estado persistido e efeitos subsequentes.

## Convenções de testes

- Os testes ficam em `tests/Craftsman.Tests` e devem cobrir regras de domínio, integração e persistência conforme o comportamento alterado.
- Mudanças em invariantes, transições de status, deduplicação, consumo de estoque, entrega e cálculo financeiro devem incluir cenários para sucesso e rejeição dos casos inválidos relevantes.
- O comando documentado para executar a suíte é `dotnet test Craftsman.slnx`.
- Os gates configurados em `.spectro/config.yaml` são obrigatórios: build com avisos tratados como erros, testes, verificação de vulnerabilidades de pacotes e cobertura. Os limiares atuais de cobertura são 90% de linhas, 75% de branches, 90% de linhas novas e 85% de branches novos.

## Requisitos de segurança

- Não registre credenciais, tokens ou segredos de produção no repositório, em logs ou em mensagens de erro. Os valores de exemplo do README e do Docker Compose são apenas para desenvolvimento local; ambientes compartilhados ou de produção devem fornecer segredos por configuração segura do ambiente.
- Preserve rastreabilidade das operações por meio das movimentações de estoque e dos eventos de domínio persistidos. Não remova ou altere o histórico para simular ajustes; registre a correção como nova operação auditável.
- Novas integrações externas devem validar entradas e tratar falhas sem confiar em códigos, descrições ou payloads recebidos como dados internos já válidos.
- O README não define política de autenticação ou autorização. Mudanças que introduzam ou alterem esses controles devem explicitar o modelo adotado e seus efeitos antes de serem consideradas concluídas.

## Regras de dependências

- Prefira as bibliotecas e recursos já usados na solução (.NET 8, ASP.NET Core, Entity Framework Core e PostgreSQL) quando atenderem ao requisito.
- Uma nova dependência deve ter finalidade necessária, manutenção adequada e compatibilidade com a solução; evite adicionar pacotes para funcionalidades pequenas que possam ser resolvidas com os recursos existentes.
- Dependências de persistência, transporte ou provedores externos ficam na infraestrutura ou na camada de aplicação apropriada e não devem criar dependência do domínio em Entity Framework ou serviços externos.
- Atualizações e novas dependências devem passar pelo gate de vulnerabilidades configurado. Remova pacotes sem uso e mantenha arquivos de dependência e lock/configuração coerentes.

## Requisitos mínimos de qualidade

- Todos os gates requeridos em `.spectro/config.yaml` devem passar: build, testes, segurança de pacotes e cobertura.
- O build é executado com `--warnaserror`; avisos introduzidos pela mudança devem ser resolvidos.
- A cobertura deve atender aos limiares configurados: 90% de linhas, 75% de branches, 90% de linhas novas e 85% de branches novos.
- Alterações de esquema devem incluir migrations compatíveis com o fluxo de persistência. Alterações em fluxos de negócio devem manter deduplicação, rastreabilidade e consistência das operações relacionadas.
- Não declare um fluxo concluído se a implementação permitir estados ou registros financeiros/estoque que contradigam as regras descritas nesta constituição.

## Aprovação humana

- Mudanças de escopo exigem aprovação humana, conforme `.spectro/config.yaml`.
- Os estágios de plano e conclusão exigem aprovação humana, conforme a configuração do workflow.
- Decisões que alterem regras de negócio descritas nesta constituição — especialmente deduplicação de pedidos, consumo e custeio de estoque, transições de produção/envio ou critérios e fórmula de acerto financeiro — devem ser apresentadas para aprovação antes de substituir a regra vigente.
- Mudanças que afetem autenticação/autorização, exposição de dados sensíveis, integração com plataformas externas ou limiares de qualidade configurados também exigem aprovação humana explícita.
