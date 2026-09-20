# Validação SENTRA 1.0

Última atualização: 20/09/2026.

| Funcionalidade | Estado | Teste executado? | Evidência | Limitação |
|---|---|---|---|---|
| Restore da solução | FUNCIONANDO | Sim | GitHub Actions | — |
| Build Release Windows | FUNCIONANDO | Sim | GitHub Actions | — |
| Testes automatizados | FUNCIONANDO | Sim | Domain + Infrastructure + WhatsApp | cobertura continuará crescendo |
| Migrations M0/M1 | FUNCIONANDO | Sim | EF snapshot + PostgreSQL limpo | — |
| M1 condomínio/blocos/unidades | FUNCIONANDO | Sim | build/test + migration | UI completa ainda será ampliada |
| M1 moradores/telefone E.164 | FUNCIONANDO | Sim | testes de domínio | importação CSV entra em marco posterior |
| M1 autenticação local/RBAC | FUNCIONANDO | Sim | testes + PostgreSQL | requer configuração externa dos segredos |
| SignalR por condomínio | IMPLEMENTADO | build/test | OperationsHub | teste desktop end-to-end pendente |
| WhatsApp adapter oficial | IMPLEMENTADO | Sim | testes parser/HMAC + build | credenciais Meta reais não fornecidas |
| WhatsApp webhook durável | EM VALIDAÇÃO | CI M2 | inbox/idempotência/worker | validação final PostgreSQL do M2 em andamento |
| WhatsApp externo real | CONFIGURADO MAS AGUARDANDO CREDENCIAL | Não | endpoint real de verificação preparado | requer credenciais Meta e número oficial |
| Mídia WhatsApp | PARCIAL | parser/modelo | Media ID e metadados preservados | Object Storage real ainda pendente |
| Inteligência | NÃO IMPLEMENTADO | Não | — | M3 |
| Visitantes/autorizações/QR | NÃO IMPLEMENTADO | Não | — | M4 |
| Encomendas/ocorrências/turnos | NÃO IMPLEMENTADO | Não | — | M5 |
| Offline SQLite/outbox | NÃO IMPLEMENTADO | Não | — | M6 |
| Instalador | NÃO IMPLEMENTADO | Não | — | M8 |
| SENTRA 1.0.0 | NÃO CONCLUÍDA | — | — | critérios finais ainda não atendidos |

Uma integração externa nunca é elevada para **FUNCIONANDO** sem teste real com o serviço correspondente.
