# Validação SENTRA 1.0

Última atualização: 20/09/2026.

## Evidência M2

- PR: #8 — M2 WhatsApp Cloud API oficial.
- CI #187 / SHA `9d4eca2c3ac64f6095a765886aa7414453601031`:
  - restore: sucesso;
  - build Release: sucesso;
  - testes: sucesso;
  - TRX: gerado;
  - snapshot EF: sucesso;
  - migrations M0 + M1 + M2 aplicadas em PostgreSQL limpo: sucesso.
- CI de PR #188 no mesmo SHA: sucesso.
- Alterações posteriores são de alinhamento documental/configuração e permanecem sujeitas ao mesmo gate de CI antes do merge.

| Funcionalidade | Estado | Teste executado? | Evidência | Limitação |
|---|---|---|---|---|
| Restore da solução | FUNCIONANDO | Sim | GitHub Actions | — |
| Build Release Windows | FUNCIONANDO | Sim | GitHub Actions | — |
| Testes automatizados | FUNCIONANDO | Sim | Domain + Infrastructure + Desktop + WhatsApp | cobertura continuará crescendo |
| Migrations M0/M1/M2 | FUNCIONANDO | Sim | EF snapshot + PostgreSQL limpo | — |
| M1 condomínio/blocos/unidades | FUNCIONANDO | Sim | build/test + migrations | — |
| M1 moradores/telefone E.164 | FUNCIONANDO | Sim | testes de domínio | importação CSV entra em marco posterior |
| M1 autenticação/RBAC | FUNCIONANDO | Sim | testes + PostgreSQL | segredos reais dependem do ambiente |
| SignalR por condomínio | IMPLEMENTADO E TESTADO EM BUILD | Sim | OperationsHub + cliente oficial | teste com dois desktops reais entra na validação de implantação |
| Adapter Meta Cloud API | IMPLEMENTADO | Sim | contrato HTTP: WABA, Phone Number, templates, Flows, mídia, mensagens, read/typing | conexão externa depende de credencial Meta |
| Configuração segura WhatsApp | IMPLEMENTADO | Sim | diagnóstico lista somente nomes/erros, nunca valores de segredo | valores reais não fornecidos |
| Verificação WABA/Phone Number | IMPLEMENTADO | Sim com HTTP fake | adapter + endpoint /verify | chamada real aguarda Meta |
| Inscrição WABA subscribed_apps | IMPLEMENTADO | Sim com HTTP fake | adapter oficial | chamada real aguarda Meta |
| Webhook GET challenge | IMPLEMENTADO | Sim | verifier | challenge real aguarda cadastro da Callback URL |
| Webhook HMAC SHA-256 | IMPLEMENTADO | Sim | MetaWebhookVerifierTests | assinatura real aguarda evento Meta |
| Webhook durável/idempotente | FUNCIONANDO INTERNAMENTE | Sim | inbox + hash + worker + PostgreSQL | evento externo real aguarda Meta |
| Parser WhatsApp inbound | FUNCIONANDO INTERNAMENTE | Sim | texto/imagem/áudio/vídeo/documento/localização/contatos/interativo/reação | payload externo real aguarda Meta |
| Status sent/delivered/read/failed | IMPLEMENTADO | Sim | parser + state machine | status externo real aguarda Meta |
| Texto outbound | IMPLEMENTADO | Sim com HTTP fake | /messages + wamid | envio real aguarda Meta |
| Templates outbound | IMPLEMENTADO | Sim com HTTP fake | template aprovado por nome/idioma | template real aprovado aguarda Meta |
| Botões/listas interativas | IMPLEMENTADO | Sim com HTTP fake | payloads oficiais | envio real aguarda Meta |
| WhatsApp Flows | IMPLEMENTADO | Sim com HTTP fake | lista PUBLISHED + flow_token server-side | Flow real publicado aguarda Meta |
| Read receipt | IMPLEMENTADO | Sim | POST /messages + status=read | chamada real aguarda Meta |
| Typing indicator | IMPLEMENTADO NO ADAPTER | Sim | POST /messages + typing_indicator | uso real aguarda Meta |
| Upload/download de mídia | IMPLEMENTADO OPERACIONALMENTE | Sim com HTTP fake | Media ID + proxy backend autenticado | cópia histórica independente da Meta é opcional e requer object storage |
| Central Windows de conversas | IMPLEMENTADA | Sim em build/test | WPF + API + SignalR client | operação real aguarda conexão Meta |
| WhatsApp externo real | CONFIGURADO MAS AGUARDANDO CREDENCIAL | Não | integração preparada | requer WABA/número/token/App Secret/Verify Token e backend HTTPS |
| Inteligência | NÃO IMPLEMENTADO | Não | — | M3 |
| Visitantes/autorizações/QR | NÃO IMPLEMENTADO | Não | — | M4 |
| Encomendas/ocorrências/turnos | NÃO IMPLEMENTADO | Não | — | M5 |
| Offline SQLite/outbox | NÃO IMPLEMENTADO | Não | — | M6 |
| Instalador | NÃO IMPLEMENTADO | Não | — | M8 |
| SENTRA 1.0.0 | NÃO CONCLUÍDA | — | — | critérios finais ainda não atendidos |

## Regra de estado

A integração WhatsApp **não** recebe estado FUNCIONANDO EXTERNAMENTE até que o SENTRA execute com credenciais reais:

1. validação do Phone Number ID;
2. confirmação do vínculo com a WABA;
3. inscrição de webhook;
4. mensagem real recebida;
5. mensagem real enviada;
6. status real recebido por webhook;
7. template aprovado real testado;
8. mídia real testada.

Até lá, o estado correto é **CONFIGURADO MAS AGUARDANDO CREDENCIAL**.
