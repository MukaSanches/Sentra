# WhatsApp — SENTRA

## Objetivo

O SENTRA utiliza exclusivamente a **Meta WhatsApp Business Platform / Cloud API oficial**.

Não são utilizados WhatsApp Web, scraping, Selenium, Playwright, Baileys, whatsapp-web.js, emulação de navegador ou engenharia reversa.

A integração foi desenhada para que o computador da portaria conheça somente a API do SENTRA. Credenciais Meta permanecem no backend.

## Superfície oficial utilizada

O M2 cobre a superfície da Cloud API necessária à operação de portaria:

- WhatsApp Business Account (WABA);
- Phone Number ID;
- inscrição da aplicação na WABA;
- Webhooks;
- mensagens de texto;
- templates;
- botões de resposta;
- listas interativas;
- WhatsApp Flows;
- imagens;
- áudio;
- vídeo;
- documentos;
- upload e download de mídia;
- status sent/delivered/read/failed;
- confirmação de leitura;
- contatos/localização/reaction/interactive recebidos;
- SignalR para atualização da central Windows.

APIs de Commerce, Payments e catálogo de produtos não fazem parte do domínio de uma portaria e não são dependências do SENTRA.

## Configuração obrigatória no servidor

Variáveis:

```
META_GRAPH_VERSION=
META_APP_ID=
META_WABA_ID=
META_PHONE_NUMBER_ID=
META_ACCESS_TOKEN=
META_VERIFY_TOKEN=
META_APP_SECRET=
```

Nenhum desses valores pode ser incluído no executável Windows ou versionado no Git.

### META_GRAPH_VERSION

É configurável de propósito. O Graph API é versionado e o SENTRA não congela uma versão dentro do código.

A versão escolhida deve estar ativa e suportada pela Meta no ambiente de produção.

### META_ACCESS_TOKEN

Produção deve utilizar credencial server-side apropriada para operação contínua.

As permissões necessárias dependem das operações habilitadas, especialmente:

- `whatsapp_business_messaging`;
- `whatsapp_business_management`.

Tokens temporários são adequados apenas para testes iniciais.

### META_VERIFY_TOKEN

Valor secreto definido pelo proprietário do SENTRA e informado à Meta na configuração do webhook.

Não é o Access Token.

### META_APP_SECRET

Segredo da aplicação Meta.

É usado para verificar a assinatura `X-Hub-Signature-256` dos POSTs de webhook.

## Configuração na Meta

Fluxo operacional:

1. criar/selecionar o Business Portfolio;
2. criar/selecionar a aplicação Meta;
3. adicionar WhatsApp;
4. criar/selecionar a WABA;
5. adicionar e registrar o número;
6. concluir os requisitos exigidos pela Meta para o número;
7. obter WABA ID;
8. obter Phone Number ID;
9. provisionar a credencial server-side;
10. configurar as variáveis no backend SENTRA;
11. publicar o backend em HTTPS;
12. configurar a Callback URL;
13. informar o mesmo Verify Token configurado no servidor;
14. executar a verificação pelo SENTRA.

## URL de webhook

```
GET  /api/v1/integrations/whatsapp/webhook
POST /api/v1/integrations/whatsapp/webhook
```

Exemplo:

```
https://SEU-SERVIDOR/api/v1/integrations/whatsapp/webhook
```

A tela **WhatsApp** do cliente Windows calcula e exibe a URL a partir da URL do backend.

## Validação real

O botão **Validar integração** não verifica somente se existem strings de configuração.

O backend:

1. consulta o Phone Number ID;
2. consulta os números da WABA;
3. confirma que o Phone Number ID pertence à WABA configurada;
4. solicita/valida a inscrição da aplicação em `subscribed_apps`;
5. consulta templates;
6. conta templates aprovados;
7. consulta Flows;
8. conta Flows publicados;
9. registra a integração no banco;
10. grava auditoria;
11. somente então muda o estado operacional para Connected.

Se Phone Number ID e WABA não pertencerem um ao outro, o SENTRA rejeita a configuração.

Sem credenciais reais, o estado continua **AGUARDANDO CONFIGURAÇÃO**.

## Webhook GET

O GET implementa o challenge da Meta e compara o Verify Token.

## Webhook POST

O POST:

1. limita o payload;
2. valida `X-Hub-Signature-256` via HMAC-SHA256;
3. rejeita assinatura inválida;
4. valida JSON;
5. calcula hash SHA-256 do payload;
6. grava o evento bruto na inbox;
7. responde rapidamente à Meta;
8. processa o evento fora do request.

O corpo bruto não é escrito nos logs.

## Inbox durável

Eventos são persistidos antes do processamento operacional.

O worker PostgreSQL utiliza locking concorrente para que duas instâncias não processem o mesmo evento ao mesmo tempo.

Falhas transitórias recebem retry com backoff.

Após o limite definido, o evento passa para DeadLetter em vez de desaparecer.

## Idempotência

Camadas:

- hash único do webhook;
- `wamid` único;
- status único por mensagem/status/timestamp;
- `ClientRequestId` único para envio iniciado pelo SENTRA.

Antes de chamar a Meta, o SENTRA grava a intenção de envio.

Se houver timeout depois que a requisição saiu, a mensagem muda para **Uncertain**. O sistema não repete cegamente a chamada, evitando mensagem duplicada.

## Moradores

O `wa_id` é normalizado e comparado ao telefone cadastrado.

Se houver associação inequívoca, a conversa é ligada ao morador/unidade.

Se não houver:

- não inventar morador;
- não inventar unidade;
- não inventar relação familiar;
- manter a conversa como participante externo.

## Mensagens recebidas

Tipos processados:

- text;
- image;
- audio;
- video;
- document;
- location;
- contacts;
- interactive;
- reaction;
- tipos desconhecidos de forma segura.

A mensagem original e seus identificadores permanecem rastreáveis.

## Mensagens enviadas

A Central de Conversas do Windows suporta backend real para:

- texto;
- template aprovado;
- botões;
- lista;
- Flow publicado;
- imagem;
- áudio;
- vídeo;
- documento.

Cada envio passa por:

```
Windows
  -> API SENTRA
  -> autenticação/RBAC
  -> idempotência
  -> persistência Pending
  -> Meta Cloud API
  -> wamid
  -> Accepted
  -> webhook de status
  -> Sent / Delivered / Read ou Failed
```

## Templates

O Windows lista os templates retornados pela WABA.

Envio de template exige nome + idioma correspondentes a um template aprovado.

O backend rejeita template que não esteja com status APPROVED.

Templates são o mecanismo adequado quando as regras da Meta exigem mensagem pré-aprovada.

## WhatsApp Flows

O Windows lista somente Flows disponíveis pela WABA e destaca os publicados.

O backend verifica que o Flow está PUBLISHED antes de enviar.

O `flow_token` é gerado no backend com aleatoriedade criptográfica. Ele não é digitado nem armazenado no cliente Windows.

Respostas interativas recebidas permanecem no pipeline normal de webhook e são preservadas para interpretação operacional posterior.

## Mídia

### Recebida

O webhook preserva Media ID e metadados.

Quando o porteiro abre/salva o anexo:

1. Windows solicita o anexo ao backend;
2. backend confirma tenant/conversa/mensagem;
3. backend consulta a Meta usando o Media ID;
4. obtém uma URL temporária;
5. baixa com Bearer token;
6. transmite os bytes ao Windows.

A URL temporária e o token Meta nunca são entregues ao desktop.

### Enviada

O Windows envia multipart ao backend.

O backend:

1. valida autenticação e conversa;
2. envia o arquivo para `/{Phone-Number-ID}/media`;
3. recebe o Media ID;
4. usa o Media ID no envio da mensagem;
5. persiste o estado operacional e `wamid`.

## Retenção

O proxy de mídia torna o uso operacional possível sem expor credenciais.

Retenção permanente independente da Meta exige Object Storage. Enquanto storage permanente não estiver configurado, o SENTRA não afirma que existe cópia permanente.

## Confirmação de leitura

O SENTRA usa `POST /{Phone-Number-ID}/messages` com `status=read`, conforme a coleção oficial atual da Meta.

Somente mensagens recebidas com External Message ID podem receber read receipt.

O SENTRA não tenta marcar mensagens enviadas pelo próprio sistema como lidas.

A falha do read receipt não impede o porteiro de continuar usando a conversa.

## Typing indicator

A Cloud API atual suporta `typing_indicator` no mesmo `POST /{Phone-Number-ID}/messages` usado pelo read receipt. O adapter possui operação específica para `status=read` + `typing_indicator.type=text`.

É um aprimoramento de UX e não altera a verdade operacional da conversa.

## Status

Estados operacionais:

- Pending;
- Received;
- Accepted;
- Sent;
- Delivered;
- Read;
- Failed;
- Uncertain.

A state machine impede regressão de estados.

Exemplo:

`Delivered -> Sent` é ignorado.

## SignalR

O hub é autenticado.

Cada cliente entra somente no grupo:

```
condominium:{id}
```

Alterações de conversa são enviadas apenas ao condomínio correspondente.

O Windows usa o cliente SignalR oficial e reconexão automática.

## Segurança do Windows

O aplicativo Windows persiste somente:

- URL do backend;
- Condominium ID;
- nome de usuário.

Não persiste:

- senha;
- JWT;
- Meta Access Token;
- App Secret;
- Verify Token.

O JWT permanece somente em memória durante a sessão.

## Recursos administrativos da Cloud API

A coleção oficial também oferece recursos como:

- Business Profile;
- bloqueio/desbloqueio de usuários;
- QR Codes;
- analytics;
- billing;
- commerce;
- payments.

Eles não são necessários para o núcleo de portaria do M2.

Se futuramente forem adicionados, deverão entrar por adapters/serviços explícitos, e não por chamadas Graph espalhadas pela aplicação.

## Definition of Done do M2

### Validável sem credenciais externas

- solution compila;
- testes passam;
- HMAC é testado;
- parser é testado;
- adapter Graph é testado por HTTP fake;
- migrations aplicam em PostgreSQL limpo;
- WPF compila;
- idempotência é testada;
- nenhum segredo está versionado.

### Requer Meta real

Somente estes itens dependem do proprietário configurar a conta:

- validar WABA e Phone Number reais;
- concluir webhook challenge no painel Meta;
- receber mensagem de um WhatsApp real;
- enviar mensagem real;
- observar status real via webhook;
- testar template aprovado real;
- testar Flow publicado real;
- testar mídia real.

Antes disso, a integração externa permanece **CONFIGURADO MAS AGUARDANDO CREDENCIAL**, nunca FUNCIONANDO.
