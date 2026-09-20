# Segurança — SENTRA

## Baseline

- Credenciais de integrações ficam no backend.
- O cliente Windows não recebe chaves administrativas.
- Configuração confidencial não é versionada.
- Produção deve usar HTTPS.
- O backend aplica rate limiting e correlation ID.
- A autenticação é preparada para JWT emitido por autoridade configurada.
- A auditoria registra ações relevantes sem depender do modelo de inteligência.

## Entrada externa

Webhooks, mensagens e anexos devem ser tratados como dados não confiáveis e validados antes de qualquer operação.

## Inteligência

O futuro motor de inteligência poderá sugerir ações, mas o servidor validará schema, permissões, regras de negócio e nível de risco antes de executar qualquer operação.

Ações classificadas como críticas exigirão fluxo explícito de autorização e não serão disparadas diretamente por interpretação de linguagem.

## Logs

Não registrar valores confidenciais ou dados pessoais sem necessidade operacional. Logs devem privilegiar IDs de correlação e metadados técnicos.
