# AGENTS.md — Diretrizes para Agentes de Engenharia .NET

> Escopo: repositórios .NET / C#. Este documento estabelece regras para agentes de IA e colaboradores que analisam, implementam, refatoram ou revisam código.
> Idioma das explicações: português (Brasil), salvo convenção existente. Nomes de símbolos, APIs e commits devem seguir a convenção do projeto.

## 1. Prioridades e regras inegociáveis

Ordem de prioridade em caso de conflito:
1. Segurança, privacidade, integridade de dados e correção funcional.
2. Requisitos explícitos, contratos públicos e compatibilidade exigida pelo projeto.
3. Clareza, testabilidade, manutenção e observabilidade.
4. Simplicidade proporcional à complexidade real do problema.
5. Preferências estilísticas e otimizações não demonstradas.

- **Não criar abstrações, camadas, padrões ou dependências sem uma necessidade demonstrável.**
- **Não confundir simplicidade com atalhos.** Uma solução curta que fragiliza segurança, consistência, tratamento de erros ou manutenção é uma solução inadequada.
- Adotar a solução **mais simples que preserve os invariantes e atenda aos requisitos não funcionais**.
- Respeitar a arquitetura e as convenções existentes; não reestruturar o projeto inteiro para implementar uma feature localizada.
- Não inventar requisitos, contratos, resultados de testes ou garantias de segurança. Declarar incertezas e limitações.
- Antes de ações destrutivas, mudanças irreversíveis, alterações de esquema ou de contratos públicos, explicar o impacto e solicitar autorização quando necessário.
- Não introduzir pacotes, infraestrutura, serviços externos ou padrões opcionais sem justificativa técnica.
- As seções **OBRIGATÓRIAS** permanecem ativas. As seções **OPCIONAIS** de arquitetura/práticas podem ser excluídas pelo responsável pelo projeto.

## 2. Fluxo obrigatório por feature

1. **Entender:** identificar comportamento esperado, contexto, limites, dependências, invariantes, riscos e critérios de aceitação.
2. **Inspecionar:** ler o código, testes e padrões existentes antes de propor mudanças; localizar pontos de integração.
3. **Planejar proporcionalmente:** para mudança trivial, plano mínimo; para mudança relevante, explicitar responsabilidades, dados, fluxos, falhas e estratégia de testes.
4. **Implementar em incrementos pequenos:** manter o diff focado, sem refatoração não relacionada.
5. **Verificar:** compilar, executar testes aplicáveis, analisadores e verificações disponíveis. Não alegar execução se não ocorreu.
6. **Revisar qualidade e segurança:** aplicar integralmente o checklist da seção 8 **antes de concluir cada feature**.
7. **Entregar:** resumir alterações, decisões, testes executados, riscos residuais e pendências. Evidenciar bloqueios.

**Critério de conclusão (Definition of Done):** comportamento validado, regressões relevantes cobertas, sem achados críticos/altos de segurança sem correção ou aceite explícito de risco por responsável autorizado, documentação/contratos atualizados quando necessário e revisão final registrada.

## 3. Diretrizes .NET / C#

- Preferir versões **suportadas** do .NET e pacotes com manutenção ativa; seguir a versão-alvo do repositório e não migrar silenciosamente.
- Habilitar e respeitar `Nullable`, analisadores Roslyn e regras de `.editorconfig` quando suportados pelo projeto. Corrigir warnings relevantes; não suprimir sem justificativa.
- Usar recursos idiomáticos do C# sem sacrificar legibilidade; escolher `record`, `class`, `struct` e interfaces de acordo com semântica, custo e necessidade reais.
- Preferir imutabilidade para dados e estados que não precisam mudar. Não expor coleções mutáveis indiscriminadamente.
- Usar injeção de dependência para dependências de fronteira e infraestrutura, sem criar interfaces inúteis para toda classe.
- Implementar operações assíncronas de I/O com `async`/`await` de ponta a ponta; evitar `.Result`, `.Wait()` e `async void` fora de event handlers apropriados.
- Propagar `CancellationToken` onde faça sentido e respeitar cancelamento, timeouts e finalização graciosa.
- Gerenciar corretamente `IDisposable`/`IAsyncDisposable`, streams, conexões e recursos nativos; não manter recursos vivos além do necessário.
- Usar `ILogger<T>` e logs estruturados, com contexto/correlation ID quando relevante; nunca registrar segredos ou dados sensíveis sem necessidade e proteção.
- Usar `IOptions<T>` e validação de configuração quando adequados. Falhar cedo para configurações inválidas críticas.
- Usar APIs nativas da plataforma (`HttpClientFactory`, middleware, autenticação/autorização ASP.NET Core, `System.Text.Json`, etc.) antes de criar alternativas próprias.
- Aplicar tratamento de falhas apropriado nas fronteiras: exceções para situações excepcionais; resultados tipados quando representarem falhas esperadas do domínio. Não engolir exceções.
- Evitar estados globais mutáveis, service locator, reflection desnecessária e otimização prematura.
- Considerar alocações, concorrência e contenção apenas quando relevantes; medir desempenho antes de introduzir soluções sofisticadas.

## 4. Qualidade, programação funcional e código limpo

### Funções e responsabilidades
- Preferir funções pequenas **e coesas**, com nome que expresse intenção; não impor limites arbitrários de linhas.
- Cada função deve possuir responsabilidade clara e nível de abstração consistente.
- Isolar lógica de negócio pura quando possível: entrada explícita, saída previsível, poucos efeitos colaterais.
- Concentrar efeitos colaterais (I/O, relógio, rede, persistência) nas fronteiras e torná-los explícitos/testáveis.
- Preferir composição e transformação de dados a fluxos imperativos difíceis de compreender **quando isso melhora a clareza**.
- Usar guard clauses para reduzir aninhamento; não substituir código simples por pipelines artificiais.
- Evitar argumentos booleanos ambíguos, parâmetros excessivos e estados parcialmente válidos; considerar tipos expressivos quando houver ganho real.
- Manter invariantes perto dos dados que protegem; validar entradas nas fronteiras e preservar integridade internamente.

### Clean Code / Object Calisthenics (heurísticas, não dogmas)
- Usar nomenclatura de domínio explícita; evitar nomes genéricos, abreviações obscuras e comentários que apenas repetem o código.
- Reduzir aninhamento e complexidade ciclomática; dividir condições densas em conceitos com nome quando ajudar.
- Evitar cadeias extensas de acesso a objetos (Lei de Deméter) e acoplamento desnecessário entre módulos.
- Preferir encapsular comportamentos com seus dados quando existir uma responsabilidade de domínio concreta; evitar modelos apenas com getters/setters e lógica espalhada.
- Encapsular coleções que tenham regras/invariantes próprios; não criar wrappers sem semântica.
- Evitar tipos primitivos soltos para conceitos de domínio relevantes (por exemplo, `Money`, `EmailAddress`), mas não criar value objects para tudo.
- Minimizar estado mutável e exposição indevida de propriedades; manter objetos em estados válidos.
- Usar Object Calisthenics como ferramenta de revisão e ensino, **não** como obrigação literal (por exemplo, proibir `else`, permitir somente um nível de indentação ou exigir um único campo em todas as classes).
- Praticar DRY sem antecipar generalizações: duplicação pequena pode ser preferível a uma abstração errada.
- Aplicar YAGNI e KISS sem eliminar validações, tratamento de falhas, testes ou controles de segurança necessários.

### Regra de proporcionalidade
> A complexidade da implementação deve acompanhar a complexidade essencial do problema — nem menos (gambiarras e fragilidade), nem mais (overengineering).

Em revisões, questionar: **que requisito, invariante, risco ou evidência justifica cada camada, padrão, abstração e dependência?** Se não houver resposta concreta, simplificar.

## 5. Twelve-Factor App

Aplicar os 12 fatores a serviços e aplicações implantáveis. Para bibliotecas, ferramentas locais e outros tipos de projeto, marcar fatores não aplicáveis com justificativa; não introduzir infraestrutura apenas para cumprir uma lista.

| Fator | Regra prática para .NET |
| --- | --- |
| **I. Codebase** | Uma base versionada como fonte de verdade, suportando diferentes ambientes/deploys por configuração; mudanças rastreáveis. |
| **II. Dependencies** | Declarar dependências em `.csproj`/gestão central de pacotes; builds reproduzíveis e restore controlado; evitar dependências implícitas na máquina. |
| **III. Config** | Separar configuração de código; usar providers do .NET e variáveis de ambiente; segredos somente via meios seguros, nunca em Git. |
| **IV. Backing Services** | Tratar banco, filas, cache e serviços externos como recursos configuráveis/substituíveis por ambiente, com limites e tratamento de indisponibilidade. |
| **V. Build, Release, Run** | Separar compilação/publicação de parametrização por ambiente e execução; evitar recompilar para mudar configuração. |
| **VI. Processes** | Preferir processos stateless; manter estado durável fora da instância e considerar sessões/distribuição quando necessário. |
| **VII. Port Binding** | Em serviços HTTP, expor a aplicação por endpoint/porta configuráveis, usando Kestrel/hosting padrão. |
| **VIII. Concurrency** | Escalar com instâncias/processos e limites explícitos de concorrência; evitar paralelismo ilimitado e pressupostos sobre instância única. |
| **IX. Disposability** | Inicialização previsível, health checks quando pertinentes, shutdown gracioso, cancelamento, drenagem/ack seguro de mensagens. |
| **X. Dev/Prod Parity** | Minimizar diferenças entre desenvolvimento, testes, homologação e produção (configuração, dependências, runtime e infraestrutura). |
| **XI. Logs** | Emitir logs estruturados para stdout/stderr ou provider integrado; agregação, retenção e transporte são responsabilidade da plataforma. |
| **XII. Admin Processes** | Executar migrações e tarefas administrativas como processos pontuais, rastreáveis, seguros e preferencialmente idempotentes. |

**Observação:** Twelve-Factor não substitui autorização, criptografia, backups, observabilidade, testes ou controles de segurança.

## 6. Testes e verificação

- Criar/atualizar testes para alterações de comportamento; incluir casos de sucesso, erros, limites, invariantes e regressões relevantes.
- Priorizar testes unitários rápidos para lógica isolada; usar testes de integração para fronteiras reais (banco, rede, autenticação, mensageria) e end-to-end apenas quando justificáveis.
- Evitar mocks de detalhes internos e testes acoplados à implementação. Verificar comportamentos e contratos.
- Não reduzir cobertura removendo testes para fazer o pipeline passar.
- Validar também concorrência, idempotência, autorização e tratamento de falhas quando o risco exigir.
- Comandos usuais (adaptar à solution e CI):

```bash
dotnet restore
dotnet build --no-restore
dotnet test --no-restore
dotnet format --verify-no-changes
dotnet list package --vulnerable --include-transitive
```

A disponibilidade e sintaxe dos comandos podem variar por versão/SDK; usar o equivalente suportado no repositório. Não afirmar que uma ferramenta realizou uma verificação não executada. Uma verificação sem achados não prova ausência de vulnerabilidades.

## 7. Práticas e arquiteturas

**Como configurar:** mantenha somente os blocos opcionais desejados e **apague inteiramente** os demais blocos, inclusive seus marcadores. É permitido combinar vários. A ausência de um bloco significa **não impor o respectivo padrão**. TDD é uma prática de desenvolvimento; SOLID é um conjunto de princípios de design; os demais são abordagens de modelagem/arquitetura — não são alternativas mutuamente exclusivas.

### TDD
- Trabalhar em ciclos **Red → Green → Refactor** para comportamento novo ou alterado: teste falhando, implementação mínima correta, refatoração com testes verdes.
- Começar pelos exemplos e casos de borda de maior valor; evitar testes sem significado ou implementação condicionada exclusivamente ao teste.
- Não usar TDD como justificativa para omitir testes de integração e segurança.

### DDD
- Construir linguagem ubíqua a partir do domínio e manter nomes consistentes entre requisitos, código e testes.
- Definir bounded contexts somente quando houver fronteiras de linguagem/regras; respeitar os limites nas integrações.
- Modelar entidades, value objects, agregados, serviços de domínio e repositórios apenas conforme necessidade real.
- Proteger invariantes e definir limites transacionais coerentes; não colocar regras de negócio em controllers ou infraestrutura.
- Evitar tratar DDD como obrigação de criar uma classe para cada conceito ou um repositório genérico sobre todo ORM.

### Clean Architecture
- Dependências de código apontam para dentro: o núcleo de negócio não depende de frameworks, UI, banco ou transporte.
- Organizar responsabilidades em domínio, aplicação/casos de uso, adaptadores e infraestrutura **conforme a escala do sistema**; não impor projetos separados desnecessários.
- Manter contratos nas fronteiras necessárias; DTOs de transporte não devem contaminar regras de domínio.
- Wiring/DI ocorre na composição da aplicação. Testar o núcleo sem exigir infraestrutura externa.

### SOLID — OPCIONAL
- **S — Single Responsibility:** cada módulo tem razão de mudança coesa e discernível.
- **O — Open/Closed:** permitir extensão onde existirem pontos de variação reais; evitar hierarquias especulativas.
- **L — Liskov Substitution:** subtipos preservam contratos, invariantes e expectativas comportamentais.
- **I — Interface Segregation:** interfaces pequenas e orientadas aos consumidores; evitar interfaces artificiais.
- **D — Dependency Inversion:** dependências de alto nível não ficam acopladas a detalhes externos; introduzir abstrações onde reduzam acoplamento real.
- SOLID não exige interfaces para toda classe, herança indiscriminada nem padrões complexos para operações triviais.

## 8. Revisão obrigatória de segurança por feature

**Executar antes de declarar qualquer feature concluída**, inclusive alterações pequenas com impacto em entradas, persistência, autenticação, rede, configuração, logs ou dependências.

### 8.1 Superfície de ataque e controles
- [ ] Identificar novas entradas, endpoints, permissões, integrações, dados sensíveis e limites de confiança.
- [ ] Validar entrada **no servidor** (formato, limites, tamanho, ranges, tipos e regras de domínio); escapar/encodar **na saída conforme contexto**.
- [ ] Verificar autenticação e **autorização por operação e por recurso** (inclusive multi-tenant e IDOR/BOLA); aplicar privilégio mínimo e negar por padrão.
- [ ] Verificar injeções SQL/NoSQL/OS command/template; usar consultas parametrizadas e APIs seguras. Nunca concatenar entrada não confiável em comandos/consultas.
- [ ] Avaliar XSS, CSRF (em autenticação baseada em cookies), SSRF, open redirect, upload malicioso, path traversal e desserialização insegura **quando aplicáveis**.
- [ ] Verificar HTTPS/TLS, CORS restritivo, cookies seguros, headers relevantes, rate limiting e proteção contra abuso quando pertinentes.
- [ ] Evitar exposição de stack traces, mensagens internas, informações de infraestrutura e dados pessoais em respostas, métricas e logs.
- [ ] Avaliar limites de recursos: timeouts, payloads, paralelismo, memória, retry/backoff e negação de serviço; evitar retries em operações não idempotentes sem estratégia.
- [ ] Revisar concorrência, transações, race conditions, integridade e idempotência de operações com efeitos colaterais.
- [ ] Usar primitivas criptográficas e bibliotecas consagradas; não criar algoritmos próprios. Senhas devem usar mecanismos específicos de password hashing, nunca hash genérico simples.

### 8.2 Segredos, dependências e supply chain
- [ ] Procurar chaves, tokens, senhas, connection strings, certificados privados e credenciais em código, testes, arquivos de configuração, exemplos, logs e diff.
- [ ] Usar secret manager/secret store conforme ambiente; `dotnet user-secrets` serve ao desenvolvimento local, **não** é cofre criptografado para produção.
- [ ] Se um segredo real foi exposto, **não apenas removê-lo do código**: reportar o incidente, revogar/rotacionar, investigar alcance e histórico conforme procedimento do projeto.
- [ ] Verificar pacotes diretos e transitivos, advisories, versões sem suporte e alterações inesperadas de dependências/lockfiles.
- [ ] Não executar scripts externos ou atualizações de pacote indiscriminadas como resposta automática a um alerta; avaliar proveniência, impacto e compatibilidade.

### 8.3 Qualidade da implementação com impacto em segurança
- [ ] Revisar código recém-escrito e caminhos de erro em busca de validações faltantes, `catch` silencioso, defaults inseguros, bypass de autenticação e falhas abertas (*fail-open*).
- [ ] Verificar ausência de endpoints de debug, bypasses temporários, credenciais de teste e configurações `Development` indevidas em produção.
- [ ] Garantir testes negativos de autorização/validação e cenários de abuso proporcionais ao risco.
- [ ] Revisar logs e telemetria contra vazamento de PII, tokens, cookies e payloads sensíveis.
- [ ] Registrar achados com severidade, evidência, correção e risco residual; **bloquear entrega** por achado crítico/alto não tratado, salvo aceite formal por responsável autorizado.

### 8.4 Evidências mínimas a reportar

No resumo final de **cada feature**, incluir:

```text
Revisão de segurança: concluída | parcial | não executada
Escopo revisado: <arquivos, fluxos e superfícies>
Verificações executadas: <testes, scanner de segredos, dependências, revisão manual>
Achados: <severidade, descrição e status; ou "nenhum identificado">
Riscos residuais / limitações: <descrição; nunca afirmar segurança absoluta>
```

Se não houver acesso a scanner, execução de testes ou ambiente, registrar a limitação e realizar a revisão estática possível. **Nunca simular resultados de ferramentas.**

## 9. Entrega e comportamento do agente

- Explicar *o que* mudou, *por que* a escolha é adequada e *como* foi verificada; ser conciso e preciso.
- Diferenciar fato observado, hipótese e recomendação.
- Apontar trade-offs arquiteturais relevantes e riscos não resolvidos, em vez de adicionar soluções invisíveis.
- Preferir alterações incrementais, reversíveis e passíveis de revisão.
- Não adicionar tecnologia apenas porque aparece nos blocos opcionais: um padrão selecionado deve guiar decisões **onde for aplicável**.
- Em caso de conflito entre duas práticas opcionais, preferir invariantes, clareza e requisitos explícitos; documentar a decisão.

## 10. Referências de orientação

- Twelve-Factor App: https://12factor.net/pt_br/
- OWASP .NET Security Cheat Sheet: https://cheatsheetseries.owasp.org/cheatsheets/DotNet_Security_Cheat_Sheet.html
- ASP.NET Core Security: https://learn.microsoft.com/aspnet/core/security/
- Microsoft — configuração e segredos: https://learn.microsoft.com/aspnet/core/fundamentals/configuration/

> Este arquivo estabelece expectativas de trabalho; não substitui threat modeling, pentest, revisão humana ou políticas de segurança da organização.
