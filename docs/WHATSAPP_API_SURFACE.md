# WhatsApp Cloud API — mapa da superfície oficial

Este documento registra a revisão da superfície oficial da **Meta WhatsApp Business Platform / Cloud API** usada pelo SENTRA.

Fontes primárias:

- Meta for Developers: https://developers.facebook.com/docs/whatsapp/
- coleção oficial Meta no Postman: https://www.postman.com/meta/whatsapp-business-platform/
- exemplos oficiais Meta: https://github.com/fbsamples/whatsapp-api-examples

A regra do projeto é: implementar a superfície útil à operação de portaria e não misturar Commerce, Payments ou recursos de marketing sem necessidade operacional.

## Classificação

| Área oficial | Uso no SENTRA | Estado M2 |
|---|---|---|
| Cloud API / Graph versioning | Base de todas as chamadas | IMPLEMENTADO |
| WABA lookup | validação de conta | IMPLEMENTADO |
| WABA `subscribed_apps` | entrega de webhooks | IMPLEMENTADO |
| Phone Number ID / phone numbers | validação de número | IMPLEMENTADO |
| Registration | etapa de provisionamento Meta | DOCUMENTADO / CONFIGURAÇÃO EXTERNA |
| Webhook GET challenge | ativação do callback | IMPLEMENTADO |
| Webhook POST signature | autenticidade do evento | IMPLEMENTADO |
| Webhook messages | entrada de mensagens | IMPLEMENTADO |
| Webhook statuses | sent/delivered/read/failed | IMPLEMENTADO |
| Text messages | conversas da portaria | IMPLEMENTADO |
| Templates | mensagens aprovadas | IMPLEMENTADO |
| Interactive reply buttons | autorização rápida | IMPLEMENTADO |
| Interactive lists | menus operacionais | IMPLEMENTADO |
| WhatsApp Flows | formulários estruturados | IMPLEMENTADO |
| Media upload | envio imagem/áudio/vídeo/documento | IMPLEMENTADO |
| Media metadata/download | recebimento de anexos | IMPLEMENTADO |
| Read receipts | confirmação de leitura | IMPLEMENTADO |
| Typing indicators | UX de atendimento | IMPLEMENTADO NO ADAPTER |
| Reactions recebidas | preservação de contexto | IMPLEMENTADO |
| Contacts recebidos | preservação de contexto | IMPLEMENTADO |
| Location recebida | preservação de contexto | IMPLEMENTADO |
| Business Profile | diagnóstico/configuração administrativa | LEITURA PLANEJADA / NÃO BLOQUEANTE |
| Block users | segurança/abuso | NÃO NECESSÁRIO NO NÚCLEO M2 |
| QR codes da conta WhatsApp | aquisição de conversa | FORA DO NÚCLEO DE PORTARIA |
| Analytics | telemetria comercial WhatsApp | FORA DO NÚCLEO M2 |
| Billing | cobrança Meta | FORA DO NÚCLEO M2 |
| Commerce Settings | comércio | FORA DO ESCOPO |
| Catalog / product messages | comércio | FORA DO ESCOPO |
| Payments | pagamentos WhatsApp por mercado | FORA DO ESCOPO |
| Business Portfolio APIs | administração Meta | CONFIGURAÇÃO EXTERNA |
| Embedded Signup | onboarding de múltiplos clientes | FUTURO SE SENTRA VIRAR SaaS |
| On-Premises migration | legado | NÃO APLICÁVEL |
| Resumable Upload | assets administrativos grandes | NÃO NECESSÁRIO NO NÚCLEO M2 |

## Mensagens

A operação diária usa o endpoint versionado:

```
POST /{PHONE_NUMBER_ID}/messages
```

O adapter SENTRA suporta:

- texto;
- template;
- botões;
- lista;
- Flow;
- imagem;
- áudio;
- vídeo;
- documento;
- read receipt;
- read receipt + typing indicator.

A Meta devolve o identificador `wamid`, persistido pelo SENTRA para correlação com webhooks de status.

## Webhooks

O SENTRA valida:

1. challenge GET com Verify Token;
2. `X-Hub-Signature-256` do POST usando App Secret;
3. tamanho máximo do payload;
4. JSON válido;
5. idempotência antes do processamento;
6. persistência durável antes do ACK operacional.

O processamento é assíncrono e suporta retry somente em trabalho interno idempotente.

## Templates

O backend consulta:

```
GET /{WABA_ID}/message_templates
```

Antes de enviar, o SENTRA exige combinação nome/idioma com status `APPROVED`.

## Flows

O backend consulta os Flows da WABA e só permite envio de Flow com status `PUBLISHED`.

O `flow_token` é gerado no backend. O Windows não armazena token Meta nem segredo de Flow.

## Mídia

Upload:

```
POST /{PHONE_NUMBER_ID}/media
```

Consulta de mídia recebida:

```
GET /{MEDIA_ID}?phone_number_id={PHONE_NUMBER_ID}
```

Download usa a URL temporária retornada pela Meta com Bearer token apenas no backend.

## Read receipt e typing

A coleção oficial atual usa:

```
POST /{PHONE_NUMBER_ID}/messages
```

Read:

```json
{
  "messaging_product": "whatsapp",
  "status": "read",
  "message_id": "<WAMID>"
}
```

Read + typing:

```json
{
  "messaging_product": "whatsapp",
  "status": "read",
  "message_id": "<WAMID>",
  "typing_indicator": {
    "type": "text"
  }
}
```

## Recursos administrativos deliberadamente fora do núcleo

Commerce, catálogo, Payments, Billing e analytics comerciais são APIs reais da plataforma, mas não fazem parte do objetivo de uma central de portaria. Adicioná-las ao domínio operacional aumentaria superfície de segurança sem resolver um problema do SENTRA.

Block users e Business Profile podem entrar em uma camada administrativa futura sem alterar contratos de conversação.

## O que deve faltar ao final do M2

Depois de CI verde, a única lacuna externa admitida é configuração real da Meta. O App ID pode ser mantido como referência administrativa, mas não é requisito das chamadas operacionais abaixo:

- `META_GRAPH_VERSION`;
- `META_WABA_ID`;
- `META_PHONE_NUMBER_ID`;
- `META_ACCESS_TOKEN`;
- `META_VERIFY_TOKEN`;
- `META_APP_SECRET`;
- backend HTTPS público;
- cadastro da Callback URL no painel Meta;
- número/WABA reais e requisitos de conta Meta concluídos.

Sem esses dados, o estado correto é **CONFIGURADO MAS AGUARDANDO CREDENCIAL**.
