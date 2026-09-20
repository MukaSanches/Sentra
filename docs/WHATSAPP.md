# WhatsApp — SENTRA

## Integração suportada

O SENTRA utiliza exclusivamente a **Meta WhatsApp Business Platform / Cloud API oficial**.

Não são utilizados WhatsApp Web, scraping, Selenium, Playwright, Baileys, whatsapp-web.js ou engenharia reversa.

## Configuração obrigatória

No servidor:

```
META_GRAPH_VERSION=
META_APP_ID=
META_WABA_ID=
META_PHONE_NUMBER_ID=
META_ACCESS_TOKEN=
META_VERIFY_TOKEN=
META_APP_SECRET=
```

Nenhum desses valores deve ser incluído no cliente Windows ou versionado no Git.

## Validação real

A integração somente muda para `Connected` depois que o backend:

1. consulta o Phone Number ID na Graph API;
2. recebe resposta válida;
3. solicita a inscrição da WABA em `subscribed_apps`;
4. persiste a verificação;
5. registra auditoria.

Preencher campos de configuração não é suficiente para exibir "Conectado".

Sem credenciais válidas, o estado é **AGUARDANDO CONFIGURAÇÃO**.

## Webhook

Endpoint:

```
GET  /api/v1/integrations/whatsapp/webhook
POST /api/v1/integrations/whatsapp/webhook
```

O GET executa a verificação de challenge da Meta.

O POST:

1. limita o payload a 1 MiB;
2. valida `X-Hub-Signature-256` usando HMAC-SHA256 e `META_APP_SECRET`;
3. valida a estrutura JSON;
4. calcula SHA-256 do payload;
5. persiste o evento bruto em inbox durável;
6. responde rapidamente;
7. deixa o processamento operacional para background.

## Idempotência

Há proteção em múltiplas camadas:

- hash único do payload do webhook;
- `external_message_id` único para `wamid`;
- eventos de status únicos por mensagem/status/timestamp;
- `ClientRequestId` único para mensagens iniciadas pelo operador.

Um retry do mesmo webhook não cria uma segunda mensagem.

## Background processing

O worker usa PostgreSQL com `FOR UPDATE SKIP LOCKED`.

Eventos travados por queda de processo podem ser reclamados após a janela de stale processing.

Falhas transitórias entram em retry com backoff.

Após o limite de tentativas, o evento muda para `DeadLetter` e não é descartado.

O conteúdo bruto do webhook não é escrito nos logs.

## Identificação de moradores

O `wa_id` recebido é normalizado para E.164 e comparado com `resident_phones.e164`.

Quando há correspondência válida, a conversa é ligada ao morador e à unidade ativa/principal conhecida.

Quando não há correspondência, a conversa permanece como participante externo; o SENTRA não inventa nome, unidade ou relação.

## Status de mensagem

Estados conhecidos:

- Pending
- Received
- Accepted
- Sent
- Delivered
- Read
- Failed

Status desconhecidos são preservados como eventos, mas não alteram o estado operacional da mensagem.

O state machine impede regressões, por exemplo `Delivered -> Sent`.

## Envio

O endpoint de envio exige `ClientRequestId`.

O SENTRA persiste a intenção antes de chamar a Meta.

Se a resposta da Meta for incerta por timeout/falha de rede, a operação não é repetida automaticamente. Isso evita envio duplicado de mensagem.

## Mídia

O parser e o domínio suportam imagem, áudio, vídeo e documento, preservando o Media ID e metadados recebidos.

A retenção permanente do arquivo original depende de um Object Storage real.

Enquanto um provider de storage não estiver configurado e implementado, anexos ficam explicitamente em **AwaitingConfiguration**. O SENTRA não finge que armazenou o arquivo.

## SignalR

Atualizações operacionais são enviadas somente ao grupo do condomínio:

`condominium:{id}`

O hub exige autenticação e não transmite eventos globais entre condomínios.

## Estado do M2

A implementação local/CI pode ser validada sem credenciais Meta.

A integração externa só pode ser classificada como **FUNCIONANDO** depois de um teste real com credenciais e número oficiais do proprietário.
