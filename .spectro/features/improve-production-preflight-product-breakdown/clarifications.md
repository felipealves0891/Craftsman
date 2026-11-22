<!-- spectro-managed: true -->
# Clarificações: quebra por produto no preflight de produção

## Estado

`resolved`

## CL-001 — Matéria-prima compartilhada entre produtos

- Severidade: `Blocking`
- Pergunta: quando a mesma matéria-prima aparecer na ficha técnica de mais de um produto, como ela deve ser apresentada no modal?
- Opções:
  - `A — Repetir por produto`: exibir em cada grupo a necessidade específica daquele produto e a mesma disponibilidade global atual, identificando que o saldo é compartilhado. Mantém a quebra por produto mais direta, mas repete o saldo visualmente.
  - `B — Consolidar`: exibir a matéria-prima uma única vez em uma seção consolidada do pedido, com a necessidade total e a disponibilidade global. Evita repetição, mas reduz a relação direta entre material e produto.
  - `C — Ambos`: exibir a necessidade específica dentro de cada produto e uma síntese consolidada por matéria-prima ao final. Oferece as duas referências, mas aumenta a complexidade e o espaço do modal.
- Decisão: usar a opção `C — Ambos`. O modal deve apresentar uma lista expansível de produtos, com uma sublista de materiais dentro de cada produto, e também um resumo consolidado por matéria-prima para o pedido.
- Racional: a sublista preserva a referência solicitada entre produto e material; o resumo consolidado torna explícita a necessidade total e a disponibilidade global de matérias-primas compartilhadas, alinhada à validação efetiva do planejamento.
- Requisitos afetados: `FR-003`, `FR-006`, `NFR-001`, `NFR-002`, `CON-005`, `AC-002`, `AC-003`, `AC-006`, `AC-009`.
- Data da pergunta: 2026-11-22
- Data da decisão: 2026-11-22

## CL-002 — Identificação do produto

- Severidade: `Resolved-Minor`
- Pergunta: qual identificação deve ser priorizada no cabeçalho do grupo?
- Decisão: usar o nome cadastrado do produto como identificação principal; o identificador interno pode permanecer apenas como apoio técnico, se necessário para rastreabilidade, mas não é requisito visual.
- Racional: o agregado `Product` observado no código possui `Name` e `Id`, sem código comercial dedicado. O nome é a referência compreensível para a pessoa usuária.
- Requisitos afetados: `FR-001`, `FR-002`, `NFR-003`, `AC-001`.
- Data: 2026-11-22
