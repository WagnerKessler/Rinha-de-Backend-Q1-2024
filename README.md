# Rinha-de-Backend-Q1-2024 -- API em .NET 8 com Native AOT.

---
**Escopo da solução:**
========================
Enquanto idealizava o projeto, pensei em colocar um Informix na base pois estava interessado em validar a forma como ele gerencia row level locking por padrão, diferente do MVCC do Postgres, e o efeito que isso teria junto de um código otimizado, assim como validar seu baixo tempo em OLTP. Ao mesmo tempo, estava contemplando um Envoy no load balancer também por aguentar um paralelismo melhor do que o NGINX, ao menos em benchmarks. Mas a rota do "free and open source" falou mais alto e dropei o Informix, eventualmente desisti do envoy ao ver que o nginx não seria gargalo e não teria diferença significativa de performance. Desta forma, mantive a escolha bem vanilla com o postgresql e o nginx que a maioria tem usado e conhece bem, porém focando na otimização dos recursos para tentar uma performance razoável e brincar entre os monstros da rinha. 

**Kestrel montado no postgres :)**
<br>
![ASP.NET and Postgres](https://th.bing.com/th/id/OIG2.Q21F.uNfeHTS7EhCTSMc)

**Referência para otimizações no banco...**
<br>
![XGH](https://atitudereflexiva.files.wordpress.com/2015/10/xgh-e1330433625262.jpg)


---
**Evolução do projeto:**
========================
- v0 No modelo MVC, usando o próprio Kestrel como web server, contratos funcionais para a rinha. Alto uso de memória, overhead de processamento, problema de concorrência entre as APIs.
- v0.5 Remoção completa do Entity Framework, ORMs por SQL queries, troca da conexão via EF com PostgreSQL pelo Npgsql, conversão pra Minimal API. Melhora na performance.
- v1 Resolução da concorrência, melhor forma em termos de performance foi select for update direto para a transação.
- v2 Trimming e redução de quaisquer pacotes desnecessários (prévia para Native AOT). Menor uso de memória e maior performance.
- v3 Adaptação remanescente com projeto rodando em Native AOT. Saída do Visual Studio no Windows com WSL pelo VSCode no Ubuntu diretamente para testar na plataforma da rinha. Consumo de memória adequado, processamento otimizado.
 
- v4 ~~XGH~~ Melhorias em código e testes de carga (objetivo: aguentar 3x a carga dos testes originais sem dar KO, sem arriar, por 10+ minutos):
  - Troca de TCP por Unix sockets (TCP mantido em fallback no NGINX para a API e na API para o banco).
  - Ao subir o volume para além de 2x dos testes padrão, Gatling começava a retornar premature close. Remoção de limites no docker não resolveu, consumo de CPU e memória baixo. Troca da rede do docker de bridge para host, adaptação do código para funcionar desta forma, 100% funcional sem limites de CPU/memória. Para cargas até 2x, p95 em 1ms, p99 em 2ms.
  - Ao subir o volume para 3x e colocar limites novamente, CPU se tornava o gargalo. Impossível - naquele momento - entregar com 1.5 core.
    - Ajustes de performance e redução de overhead no NGINX. Com 0.15 core, o processamento do NGINX nunca se torna o gargalo para 3x do volume original, porém CPU começava a engargalar entre API e banco, memória enchia para bufferizar até estourar o load balancer ou API e retornar KOs (DoS).
    - "Remoção" de WAL, uso de unlogged tables, synchronous commit off, fsync off (vai cavalo, durabilidade é para os fracos). 
    - Ajustes de memória entre os recursos, banco nunca chega aos 400MB, configuração do effective_cache_size e shared_buffers pensando neste limite.
    - Distribuição da memória remanescente entre API e NGINX de forma igualitária para aguentar picos de memória em eventuais gargalos por conta de possível instabilidade no processamento.
    - Quebra na tabela de transações, de 1 tabela com índice composto com cliente e data desc para um único índice simples decrescente.  
    - Validação da distribuição do 1.35 core remanescente entre API e Banco para achar um ponto de equilíbrio, sem sucesso.
    - Falhas nos ajustes perdurou até redução do connection pooling com banco de 100 conexões para 10. Redução incrível de overhead no processamento e uso de memória, ainda não ficou 100%, mas melhor entrega até o momento.
    - Aplicação performou melhor com mínimo de 5 conexões e máximo de 10 no pooling.
    - Validação de prepared statements em código vs Multiplex=on na connection string, melhora (sutil) com o multiplex=on ao invés do prepared statements para as queries utilizadas na rinha.
    - Validação de stored procedures no postgres para updates e inserts, sem melhoria aparente, maior dificuldade para balancear a CPU entre banco e API.
    - Neste novo cenário, versão publicada do código, limite de 1.35 core testado começando com 0.25 core por instância da API e 0.85 no banco, até 0.375 por instância de API e 0.6 no banco (cenários ainda maiores e menores de CPU na API já haviam sido testados no 3x com a duração original e falhado). Aplicação ficou 100% livre de KOs de forma consistente com 0.3375 core na API e 0.675 no banco e 0.35 core na API e 0.65 no banco.
    - Testes de carga com longa execução e 3x o volume, comparando ambos os cenários válidos, p95 ficou levemente mais baixo com 0.35 core na API.

Por fim, feita organizaçao da estrutura de pastas do projeto para submissão, produção do readme com os aprendizados "\u2764", publicação do galo-cinza e entrada do mesmo na rinha via PR. 

---
**Testes & Resultados:**
========================

**Volume original dos testes para a rinha:**
========================

*        debitos.inject(
          rampUsersPerSec(1).to(220).during(2.minutes),
          constantUsersPerSec(220).during(2.minutes)
        ),
        creditos.inject(
          rampUsersPerSec(1).to(110).during(2.minutes),
          constantUsersPerSec(110).during(2.minutes)
        ),
        extratos.inject(
          rampUsersPerSec(1).to(10).during(2.minutes),
          constantUsersPerSec(10).during(2.minutes)*

![Volume Original](https://raw.githubusercontent.com/WagnerKessler/Rinha-de-Backend-Q1-2024/minimal/Images/Original.png)          

**2x o volume:**
========================

*        debitos.inject(
          rampUsersPerSec(1).to(440).during(2.minutes),
          constantUsersPerSec(440).during(2.minutes)
        ),
        creditos.inject(
          rampUsersPerSec(1).to(220).during(2.minutes),
          constantUsersPerSec(220).during(2.minutes)
        ),
        extratos.inject(
          rampUsersPerSec(1).to(20).during(2.minutes),
          constantUsersPerSec(20).during(2.minutes)*

![20 minutos](https://raw.githubusercontent.com/WagnerKessler/Rinha-de-Backend-Q1-2024/minimal/Images/2x-volume.png)

**3x o volume:**
========================

*        debitos.inject(
          rampUsersPerSec(1).to(660).during(2.minutes),
          constantUsersPerSec(660).during(2.minutes)
        ),
        creditos.inject(
          rampUsersPerSec(1).to(330).during(2.minutes),
          constantUsersPerSec(330).during(2.minutes)
        ),
        extratos.inject(
          rampUsersPerSec(1).to(30).during(2.minutes),
          constantUsersPerSec(30).during(2.minutes)*

![20 minutos](https://raw.githubusercontent.com/WagnerKessler/Rinha-de-Backend-Q1-2024/minimal/Images/3x-volume.png)

**3x o volume, com o galo-cinza rinheiro sofrendo pancada por 20 minutos:**
========================

*        debitos.inject(
          rampUsersPerSec(1).to(660).during(5.minutes),
          constantUsersPerSec(660).during(15.minutes)
        ),
        creditos.inject(
          rampUsersPerSec(1).to(330).during(5.minutes),
          constantUsersPerSec(330).during(15.minutes)
        ),
        extratos.inject(
          rampUsersPerSec(1).to(30).during(5.minutes),
          constantUsersPerSec(30).during(15.minutes)*

![20 minutos](https://raw.githubusercontent.com/WagnerKessler/Rinha-de-Backend-Q1-2024/minimal/Images/20-mins.png)
