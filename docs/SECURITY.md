# Segurança — SENTRA

## Baseline

- Credenciais de integrações ficam exclusivamente no backend.
- O cliente Windows não recebe chaves administrativas.
- Configuração confidencial não é versionada.
- Produção deve usar HTTPS.
- O backend aplica rate limiting e correlation ID.
- Autenticação e autorização são separadas por RBAC.
- A auditoria não depende do motor de inteligência.

## Webhooks externos

Webhooks são dados não confiáveis.

O webhook Meta:

- limita tamanho do payload;
- valida HMAC-SHA256 em `X-Hub-Signature-256`;
- rejeita payload inválido;
- usa hash para deduplicação;
- persiste antes do processamento;
- não registra conteúdo bruto em logs;
- usa processamento background com dead-letter.

A assinatura é calculada sobre os bytes exatos recebidos.

## Isolamento entre condomínios

Operações HTTP derivam o condomínio da claim autenticada.

SignalR adiciona conexões somente ao grupo do respectivo `condominium_id`.

Uma conversa de outro condomínio não é retornada por ID sem validação de tenant.

## Mensagens externas

Mensagens do WhatsApp são conteúdo não confiável.

M2 apenas normaliza e persiste comunicação. No M3, qualquer interpretação passará por schema, allowlist, Policy Engine e confirmação humana conforme risco.

## Retry

Retries são aplicados a processamento idempotente de webhook.

Envio de mensagem externa com resultado incerto **não recebe retry automático**, porque a Meta pode ter aceitado a mensagem antes de a conexão falhar.

## Segredos

Nunca registrar:

- META_ACCESS_TOKEN;
- META_APP_SECRET;
- META_VERIFY_TOKEN;
- AUTH_SIGNING_KEY;
- senhas;
- payload sensível sem necessidade operacional.

## Logs

Priorizar IDs técnicos, código de erro sanitizado e correlation ID.

Payload de mensagem, tokens e chaves não devem aparecer em logs operacionais.
