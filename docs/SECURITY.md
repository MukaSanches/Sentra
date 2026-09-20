# Segurança — SENTRA

## Baseline

- Credenciais de integrações ficam no backend.
- O cliente Windows não recebe chaves administrativas.
- Configuração confidencial não é versionada.
- Produção deve usar HTTPS.
- O backend aplica rate limiting e correlation ID.
- A autenticação usa autoridade JWT configurável.
- A auditoria registra ações relevantes sem depender do motor de inteligência.

## Entrada externa

Webhooks, mensagens e anexos são dados não confiáveis e precisam ser validados antes de produzir efeitos no domínio.

## Inteligência

O motor de inteligência poderá preparar sugestões e ações estruturadas. O servidor validará schema, permissão, regra de negócio e nível de risco antes de executar qualquer operação.

Ações críticas exigirão autorização explícita.

## Logs

Não registrar valores confidenciais ou dados pessoais sem necessidade operacional. Priorizar IDs de correlação e metadados técnicos.
