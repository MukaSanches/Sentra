# Validação SENTRA 1.0

Este documento registra o estado real e **não declara a V1 pronta**.

| Funcionalidade | Estado | Teste | Evidência/limitação |
|---|---|---|---|
| Estrutura .NET | EM VALIDAÇÃO | CI | aguardando workflow |
| Domínio técnico inicial | EM VALIDAÇÃO | unit tests | webhook/idempotência básica |
| PostgreSQL/EF Core | PARCIAL | build + migration | conexão real ainda não configurada |
| Autenticação | PARCIAL | build | validação JWT preparada; emissão/login entra em M1 |
| Health endpoint | EM VALIDAÇÃO | build | `/health` |
| Desktop WPF | EM VALIDAÇÃO | build Windows | fundação visual somente |
| WhatsApp oficial | NÃO IMPLEMENTADO | — | M2 |
| Inteligência | NÃO IMPLEMENTADO | — | M3 |
| Instalador | NÃO IMPLEMENTADO | — | M8 |
| V1.0.0 | NÃO CONCLUÍDA | — | critérios finais ainda não atendidos |
