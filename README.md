# FCG Payments API

## Visão Geral

A **FCG Payments API** é o microsserviço responsável pelo processamento dos pagamentos da plataforma **FIAP Cloud Games**.

Sua principal responsabilidade é consumir eventos de compra publicados pela **CatalogAPI**, registrar o pagamento no banco de dados e publicar um novo evento informando o resultado da operação para os demais microsserviços.

Diferentemente de uma API tradicional, a PaymentsAPI não inicia o fluxo de compra por meio de requisições HTTP. O processamento ocorre de forma assíncrona através do **RabbitMQ**, tornando a comunicação entre os microsserviços desacoplada, resiliente e escalável.

Além do processamento dos pagamentos, a API disponibiliza um endpoint para consulta dos pagamentos registrados e um endpoint de *Health Check* utilizado para monitoramento da aplicação.

---

# Arquitetura

A PaymentsAPI foi desenvolvida seguindo os princípios de **Domain-Driven Design (DDD)**, **Clean Architecture** e **CQRS (Command Query Responsibility Segregation)**, separando claramente responsabilidades de leitura, escrita e infraestrutura.

As principais tecnologias utilizadas são:

- .NET 10
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- MediatR
- CQRS
- RabbitMQ
- MassTransit
- Docker
- Kubernetes
- Swagger / OpenAPI
- ILogger

O processamento dos pagamentos é orientado a eventos, utilizando o RabbitMQ como broker de mensagens e o MassTransit como framework de mensageria.

---

# Estrutura da Solução

O projeto está organizado em camadas, mantendo a separação entre regras de negócio, casos de uso, infraestrutura e contratos compartilhados.

```text
FCG-Payments-Api
│
├── src
│   ├── FCG.Payments.Api
│   │   ├── Controllers
│   │   ├── Middlewares
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   └── Dockerfile
│   │
│   ├── FCG.Payments.Application
│   │   ├── Commands
│   │   ├── Consumers
│   │   ├── DTOs
│   │   └── Queries
│   │
│   ├── FCG.Payments.Domain
│   │   ├── Entities
│   │   ├── Enums
│   │   └── Repositories
│   │
│   ├── FCG.Payments.Infrastructure
│   │   ├── Data
│   │   └── Repositories
│   │
│   └── FCG.Payments.Contracts
│       └── Events
│
└── tests
```

### Camadas

**FCG.Payments.Api**

Responsável pela inicialização da aplicação, configuração do pipeline HTTP, Swagger, Middlewares, Controllers, Health Check e configuração do RabbitMQ.

**FCG.Payments.Application**

Contém os casos de uso da aplicação implementados através de **Commands**, **Queries**, **Consumers** e **Handlers** utilizando MediatR.

**FCG.Payments.Domain**

Implementa as regras de negócio relacionadas aos pagamentos, contendo a entidade `Payment`, enums e contratos de repositório.

**FCG.Payments.Infrastructure**

Responsável pela persistência dos dados utilizando Entity Framework Core e SQL Server.

**FCG.Payments.Contracts**

Contém os contratos compartilhados entre microsserviços, como o evento `PaymentProcessedEvent`.

---

# Tecnologias Utilizadas

| Tecnologia | Finalidade |
|------------|------------|
| .NET 10 | Plataforma principal |
| ASP.NET Core Web API | API REST |
| Entity Framework Core | Persistência de dados |
| SQL Server | Banco de dados |
| CQRS | Separação entre comandos e consultas |
| MediatR | Implementação do CQRS |
| RabbitMQ | Broker de mensagens |
| MassTransit | Comunicação entre microsserviços |
| Swagger | Documentação da API |
| Docker | Containerização |
| Kubernetes | Orquestração de containers |
| ILogger | Registro de logs |

---

# Fluxo de Processamento

O fluxo de pagamento é totalmente baseado em eventos.

```text
Cliente

    │

    ▼

CatalogAPI
    │
    │ Publica OrderPlacedEvent
    ▼

RabbitMQ
    │
    ▼

PaymentsAPI
    │
    ├── Consome OrderPlacedEvent
    ├── Processa o pagamento
    ├── Persiste o pagamento
    └── Publica PaymentProcessedEvent
            │
            ▼

RabbitMQ
     │
     ├───────────────► CatalogAPI
     │                 Conclui a compra
     │
     └───────────────► NotificationsAPI
                       Simula o envio da confirmação
```

## Resumo do Fluxo

1. O usuário realiza a compra na **CatalogAPI**.
2. A CatalogAPI publica um **OrderPlacedEvent**.
3. A PaymentsAPI consome esse evento através do RabbitMQ.
4. O pagamento é processado e persistido no banco de dados.
5. Um **PaymentProcessedEvent** é publicado.
6. A CatalogAPI recebe o evento para concluir a compra.
7. A NotificationsAPI recebe o evento para simular a confirmação da compra.

Dessa forma, o microsserviço permanece desacoplado dos demais componentes da solução, comunicando-se exclusivamente por eventos durante o processamento da compra.

# CQRS

A PaymentsAPI implementa o padrão **CQRS (Command Query Responsibility Segregation)** para separar operações de escrita das operações de leitura.

As alterações de estado da aplicação são executadas por **Commands**, enquanto as consultas utilizam **Queries**, tornando o código mais organizado e aderente à Clean Architecture.

### Commands

Os comandos representam operações que modificam o estado da aplicação.

Atualmente, o principal comando é:

```text
ProcessPaymentCommand
        │
        ▼
ProcessPaymentCommandHandler
```

O `ProcessPaymentCommand` é responsável por iniciar o processamento de um pagamento contendo:

- OrderId
- UserId
- GameId
- Price

O handler executa toda a regra de negócio relacionada ao pagamento:

- valida se o pedido já foi processado;
- cria a entidade `Payment`;
- aprova o pagamento;
- persiste os dados;
- publica o evento `PaymentProcessedEvent`.

---

### Queries

As consultas não alteram dados.

Atualmente existe:

```text
GetPaymentsQuery
        │
        ▼
GetPaymentsQueryHandler
```

Essa consulta recupera todos os pagamentos registrados no banco de dados e os converte para `PaymentDto`, utilizado pela API para resposta ao cliente.

---

### Por que não existe um PaymentService?

Assim como nos demais microsserviços da solução, a PaymentsAPI utiliza o MediatR para implementar os casos de uso.

Dessa forma, cada Handler atua como um serviço de aplicação específico, eliminando a necessidade de um serviço genérico (`PaymentService`) que centralizaria diversas responsabilidades.

Essa abordagem mantém cada caso de uso isolado, facilitando manutenção, testes e evolução do projeto.

---

# Domínio

O domínio da PaymentsAPI é representado pela entidade `Payment`.

Ela contém todas as informações necessárias para representar um pagamento processado.

## Payment

| Propriedade | Descrição |
|-------------|-----------|
| Id | Identificador do pagamento |
| OrderId | Identificador do pedido |
| UserId | Usuário responsável pela compra |
| GameId | Jogo adquirido |
| Price | Valor pago |
| Status | Situação do pagamento |
| ProcessedAt | Data do processamento |

A entidade também implementa as operações:

```text
Approve()
Reject()
```

No fluxo atual do projeto, todos os pagamentos são aprovados automaticamente, porém a estrutura permite futuras integrações com gateways reais que poderão aprovar ou rejeitar uma transação.

Além disso, a entidade realiza validações básicas para impedir:

- OrderId vazio;
- UserId vazio;
- GameId vazio;
- valores menores ou iguais a zero.

---

# Eventos

Toda a comunicação da PaymentsAPI com os demais microsserviços ocorre através de eventos.

## Evento Consumido

### OrderPlacedEvent

Publicado pela CatalogAPI quando um usuário realiza uma compra.

```text
CatalogAPI
        │
        ▼
OrderPlacedEvent
        │
        ▼
PaymentsAPI
```

O evento contém:

- OrderId
- UserId
- GameId
- Price
- PlacedAt

Ao recebê-lo, o `OrderPlacedEventConsumer` encaminha o processamento para o `ProcessPaymentCommandHandler`.

---

## Evento Publicado

### PaymentProcessedEvent

Após concluir o processamento do pagamento, a PaymentsAPI publica o evento:

```text
PaymentProcessedEvent
```

Esse evento informa:

- OrderId
- UserId
- GameId
- Price
- Status
- ProcessedAt

Ele é consumido por:

- **CatalogAPI**, que conclui a compra e adiciona o jogo à biblioteca do usuário;
- **NotificationsAPI**, que simula o envio da confirmação da compra.

---

# RabbitMQ e MassTransit

A comunicação assíncrona entre os microsserviços é realizada utilizando **RabbitMQ** em conjunto com **MassTransit**.

O consumidor registrado na aplicação é:

```text
OrderPlacedEventConsumer
```

Esse consumidor permanece aguardando mensagens na fila:

```text
payments-order-placed-event
```

Sempre que um novo evento é recebido:

```text
OrderPlacedEvent
        │
        ▼
OrderPlacedEventConsumer
        │
        ▼
ProcessPaymentCommand
        │
        ▼
ProcessPaymentCommandHandler
```

Após o processamento, um novo evento é publicado automaticamente no broker.

Essa arquitetura desacoplada permite que novos microsserviços consumam os eventos futuramente sem necessidade de alterações na PaymentsAPI.

---

# Repositório

O acesso aos dados é realizado através do padrão **Repository**.

A camada de aplicação depende apenas da abstração:

```text
IPaymentRepository
```

A implementação concreta é fornecida pela camada de infraestrutura através de:

```text
PaymentRepository
```

As principais operações disponíveis são:

- Adicionar pagamento;
- Buscar pagamento por OrderId;
- Listar pagamentos.

Essa separação reduz o acoplamento entre as regras de negócio e a tecnologia de persistência.

---

# Persistência

A persistência dos pagamentos é realizada utilizando **Entity Framework Core** com **SQL Server**.

O contexto responsável pelo acesso ao banco é:

```text
PaymentsDbContext
```

Sempre que um novo pagamento é processado:

1. O Handler consulta se já existe um pagamento para o mesmo `OrderId`;
2. Caso exista, o registro é reutilizado;
3. Caso contrário, um novo pagamento é criado;
4. O Entity Framework grava os dados no SQL Server;
5. O evento `PaymentProcessedEvent` é publicado.

Essa verificação garante que um mesmo pedido não gere pagamentos duplicados, mesmo em cenários de reprocessamento de mensagens.

O banco de dados utilizado é exclusivo da PaymentsAPI, mantendo independência em relação aos demais microsserviços e respeitando o princípio de banco de dados por serviço.

# Endpoints

A PaymentsAPI possui endpoints HTTP auxiliares para consulta dos pagamentos processados e verificação da disponibilidade do serviço.

O processamento de um pagamento não é iniciado por endpoint HTTP. O fluxo principal ocorre por meio do consumo do `OrderPlacedEvent` no RabbitMQ.

## GET /api/payments

Retorna todos os pagamentos persistidos no banco de dados.

```http
GET /api/payments
```

### Exemplo com PowerShell

```powershell
curl http://localhost:8082/api/payments
```

### Resposta esperada

```json
[
  {
    "id": "4dc2a410-02bb-4dd4-aebc-956495087322",
    "orderId": "78022c0a-fb35-494c-b373-a3df22f7d7dd",
    "userId": "167d025c-17d6-47cc-b4f3-a82748c56f9d",
    "gameId": "fb98ecb7-c927-4449-9052-9757926c46e5",
    "price": 99.90,
    "status": "Approved",
    "processedAt": "2026-07-14T15:00:00Z"
  }
]
```

Os pagamentos são retornados em ordem decrescente pela data de processamento, apresentando os registros mais recentes primeiro.

O endpoint não possui autenticação JWT na implementação atual.

---

## GET /health

Verifica se a PaymentsAPI está em execução e respondendo às requisições HTTP.

```http
GET /health
```

### Exemplo com PowerShell

```powershell
curl http://localhost:8082/health
```

### Resposta esperada

```json
{
  "service": "PaymentsAPI",
  "status": "Healthy"
}
```

O health check atual valida a disponibilidade da aplicação, mas não realiza uma verificação profunda da conexão com o SQL Server ou RabbitMQ.

---

# Configuração

As configurações não sensíveis ficam no arquivo `appsettings.json`.

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning",
      "MassTransit": "Warning",
      "RabbitMQ.Client": "Warning"
    }
  },
  "AllowedHosts": "*",
  "RabbitMq": {
    "Host": "localhost"
  }
}
```

Credenciais, senhas e connection strings não devem ser armazenadas diretamente no arquivo versionado.

## Variáveis de ambiente

O ASP.NET Core utiliza dois caracteres de sublinhado para representar níveis de configuração.

| Variável                               | Finalidade                         |
| -------------------------------------- | ---------------------------------- |
| `ConnectionStrings__DefaultConnection` | Conexão com o banco SQL Server     |
| `RabbitMq__Host`                       | Host do RabbitMQ                   |
| `RabbitMq__Username`                   | Usuário do RabbitMQ                |
| `RabbitMq__Password`                   | Senha do RabbitMQ                  |
| `ASPNETCORE_ENVIRONMENT`               | Ambiente da aplicação              |
| `ASPNETCORE_HTTP_PORTS`                | Porta HTTP interna                 |
| `DOTNET_RUNNING_IN_CONTAINER`          | Identifica a execução em container |

### Exemplo local

```powershell
$env:ConnectionStrings__DefaultConnection = "Server=localhost,1433;Database=FCGPaymentsDb;User Id=sa;Password=<SQLSERVER_PASSWORD>;TrustServerCertificate=True;"

$env:RabbitMq__Host = "localhost"
$env:RabbitMq__Username = "<RABBITMQ_USER>"
$env:RabbitMq__Password = "<RABBITMQ_PASSWORD>"
```

Substitua os placeholders pelos valores válidos do ambiente.

Nunca publique credenciais reais no GitHub.

---

# Execução Local

## Pré-requisitos

Para executar a PaymentsAPI localmente, é necessário possuir:

* .NET SDK 10;
* SQL Server disponível;
* RabbitMQ disponível;
* banco configurado;
* credenciais válidas;
* migrações aplicadas, quando necessário.

## Acessar o projeto

```powershell
cd D:\FIAP-FCG-MICROSERVICOS\FCG-Payments-Api
```

## Restaurar dependências

```powershell
dotnet restore
```

## Compilar a solução

```powershell
dotnet build
```

## Executar a aplicação

```powershell
dotnet run --project src/FCG.Payments.Api
```

Após a inicialização, o console deverá exibir mensagens semelhantes a:

```text
Configured endpoint payments-order-placed-event
Bus started
Application started
```

A porta utilizada na execução local é definida pelo arquivo `launchSettings.json`.

## Aplicar migrations

Quando necessário, execute:

```powershell
dotnet ef database update `
  --project src/FCG.Payments.Infrastructure `
  --startup-project src/FCG.Payments.Api
```

Para listar as migrations existentes:

```powershell
dotnet ef migrations list `
  --project src/FCG.Payments.Infrastructure `
  --startup-project src/FCG.Payments.Api
```

---

# Docker

A PaymentsAPI utiliza um Dockerfile com múltiplos estágios.

## Etapas do Dockerfile

```text
Restore
   |
   v
Build e Publish
   |
   v
Imagem final ASP.NET Runtime
```

A imagem final utiliza:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
```

A porta interna configurada é:

```dockerfile
ENV ASPNETCORE_HTTP_PORTS=8082
EXPOSE 8082
```

## Criar a imagem

Na raiz do repositório:

```powershell
docker build -t fcg-payments-api:1.0 .
```

## Executar isoladamente

```powershell
docker run --rm `
  --name fcg-payments-api `
  -p 8082:8082 `
  -e ConnectionStrings__DefaultConnection="<PAYMENTS_CONNECTION_STRING>" `
  -e RabbitMq__Host="<RABBITMQ_HOST>" `
  -e RabbitMq__Username="<RABBITMQ_USER>" `
  -e RabbitMq__Password="<RABBITMQ_PASSWORD>" `
  fcg-payments-api:1.0
```

A execução isolada exige que o container consiga acessar o SQL Server e o RabbitMQ.

## Executar com Docker Compose

A forma recomendada para executar todo o ambiente é utilizar o repositório de orquestração.

```powershell
cd D:\FIAP-FCG-MICROSERVICOS\FCG-Orchestration-Api
docker compose config
docker compose build
docker compose up -d
docker compose ps
```

A PaymentsAPI ficará disponível em:

```text
http://localhost:8082
```

## Acompanhar os logs

```powershell
docker compose logs -f payments-api
```

Para acompanhar o fluxo completo de compra:

```powershell
docker compose logs -f --tail=0 catalog-api payments-api notifications-api
```

## Parar o ambiente

```powershell
docker compose down
```

Esse comando remove containers e redes criadas pelo Compose, mas mantém os volumes.

Para remover também os dados persistidos:

```powershell
docker compose down -v
```

O parâmetro `-v` remove os volumes do SQL Server e RabbitMQ. Deve ser utilizado somente quando houver necessidade de reinicializar completamente os dados.

---

# Kubernetes

Os manifestos Kubernetes são centralizados no repositório:

```text
FCG-Orchestration-Api
```

Docker Compose e Kubernetes são formas alternativas de execução.

Não é necessário executar os dois ambientes ao mesmo tempo.

## Aplicar os manifestos

```powershell
cd D:\FIAP-FCG-MICROSERVICOS\FCG-Orchestration-Api
kubectl apply -f k8s/
```

## Verificar os pods

```powershell
kubectl get pods
```

Para acompanhar alterações em tempo real:

```powershell
kubectl get pods -w
```

## Verificar o deployment

```powershell
kubectl get deployment payments-api
```

## Verificar os services

```powershell
kubectl get services
```

## Consultar os logs

```powershell
kubectl logs deployment/payments-api
```

Para acompanhar continuamente:

```powershell
kubectl logs -f deployment/payments-api
```

## Reiniciar a PaymentsAPI

```powershell
kubectl rollout restart deployment payments-api
```

## Acompanhar o reinício

```powershell
kubectl rollout status deployment/payments-api
```

## Remover o ambiente

```powershell
kubectl delete -f k8s/
```

## ConfigMap

As configurações não sensíveis podem ser armazenadas no ConfigMap, como:

```text
ASPNETCORE_ENVIRONMENT
RabbitMq__Host
```

## Secret

Informações sensíveis devem ficar no Kubernetes Secret, como:

```text
RabbitMq__Username
RabbitMq__Password
PaymentsConnectionString
MSSQL_SA_PASSWORD
```

Não armazene senhas reais diretamente no README ou nos manifestos versionados.

---

# Swagger

A PaymentsAPI possui Swagger configurado para documentação do endpoint HTTP.

```csharp
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(
        "v1",
        new OpenApiInfo
        {
            Title = "FCG Payments API",
            Version = "v1",
            Description =
                "Microsserviço responsável pelo processamento de pagamentos da FIAP Cloud Games."
        });
});
```

O Swagger é habilitado somente em ambiente de desenvolvimento:

```csharp
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
```

Quando a aplicação estiver em `Development`, acesse:

```text
http://localhost:<PORTA>/swagger
```

Caso a aplicação esteja sendo executada na porta 8082 em modo de desenvolvimento:

```text
http://localhost:8082/swagger
```

No Dockerfile, o ambiente padrão está configurado como:

```dockerfile
ENV ASPNETCORE_ENVIRONMENT=Production
```

Por esse motivo, o Swagger não será exibido no container enquanto o ambiente permanecer como `Production`.

Nesse cenário, utilize:

```powershell
curl http://localhost:8082/health
curl http://localhost:8082/api/payments
```

para validar a aplicação.


# Logs Esperados

A PaymentsAPI registra logs durante todas as etapas do processamento de um pagamento utilizando a interface `ILogger`.

Esses registros facilitam o acompanhamento do fluxo de eventos, a identificação de falhas e a validação da integração entre os microsserviços.

---

## Recebimento do Evento

Quando um novo pedido é recebido através do RabbitMQ, o consumidor registra a chegada da mensagem.

```text
OrderPlacedEvent recebido.
OrderId: ...
UserId: ...
GameId: ...
Price: ...
```

Esse log confirma que:

* a CatalogAPI publicou o evento corretamente;
* o RabbitMQ entregou a mensagem;
* o Consumer iniciou o processamento.

---

## Início do Processamento

Após encaminhar o evento para o MediatR, o Handler inicia o processamento.

```text
Pagamento iniciado.
OrderId: ...
UserId: ...
GameId: ...
Price: ...
```

Nesse momento a aplicação:

* verifica pagamentos existentes;
* cria a entidade `Payment`;
* aplica as regras de negócio.

---

## Pagamento Aprovado

Quando o processamento é concluído com sucesso:

```text
Pagamento aprovado.
PaymentId: ...
OrderId: ...
```

Esse log indica que o pagamento foi persistido no banco de dados.

---

## Evento Publicado

Após salvar o pagamento, a aplicação publica um novo evento.

```text
Evento PaymentProcessedEvent publicado.
OrderId: ...
Status: Approved
```

Esse evento será consumido pela:

* CatalogAPI;
* NotificationsAPI.

---

## Pedido Já Processado

Caso o mesmo `OrderId` seja recebido novamente, o Handler evita a criação de um novo pagamento.

```text
Pagamento já processado para o pedido ...
O registro existente será retornado.
```

Essa verificação evita registros duplicados em cenários de reprocessamento de mensagens.

---

## Consultando os Logs

### Docker Compose

```powershell
docker compose logs -f payments-api
```

Para acompanhar todo o fluxo da compra:

```powershell
docker compose logs -f --tail=0 catalog-api payments-api notifications-api
```

### Kubernetes

```powershell
kubectl logs deployment/payments-api
```

Para acompanhar continuamente:

```powershell
kubectl logs -f deployment/payments-api
```

---

# Troubleshooting

## RabbitMQ indisponível

Sintomas:

* Consumer não inicia;
* mensagens permanecem na fila;
* exceção `Broker Unreachable`.

Verifique:

```powershell
docker compose ps
docker compose logs rabbitmq
```

Confirme:

* RabbitMq__Host;
* RabbitMq__Username;
* RabbitMq__Password;
* disponibilidade da porta **5672**.

---

## SQL Server indisponível

Sintomas:

* falha ao salvar pagamentos;
* exceções do Entity Framework Core;
* conexão recusada.

Verifique:

```powershell
docker compose logs sqlserver
```

Confirme:

* connection string;
* senha do SQL Server;
* banco configurado corretamente;
* porta **1433**.

---

## Evento não consumido

Se o pagamento não for processado:

* confirme se a CatalogAPI publicou o `OrderPlacedEvent`;
* verifique a fila `payments-order-placed-event`;
* valide se o Consumer está conectado;
* consulte os logs da PaymentsAPI.

Também é importante confirmar que o contrato do evento é compatível entre CatalogAPI e PaymentsAPI.

---

## Pagamento não aparece na consulta

Caso o endpoint:

```text
GET /api/payments
```

retorne vazio:

* confirme que o evento foi recebido;
* valide o log **Pagamento aprovado**;
* verifique se o banco utilizado é o esperado;
* confira se as migrations foram aplicadas.

---

## Swagger indisponível

No Docker, o ambiente padrão é:

```dockerfile
ASPNETCORE_ENVIRONMENT=Production
```

Como o Swagger é habilitado apenas em desenvolvimento, utilize:

```powershell
curl http://localhost:8082/health
```

para validar a aplicação.

---

## Porta em uso

Caso a porta **8082** esteja ocupada:

```powershell
netstat -ano | findstr :8082
```

Encerrar o processo:

```powershell
taskkill /PID <PID> /F
```

---

## Banco antigo em volume Docker

Para remover apenas os containers:

```powershell
docker compose down
```

Para remover também os volumes persistidos:

```powershell
docker compose down -v
```

Utilize `-v` apenas quando desejar recriar completamente o ambiente.

---

# Checklist de Validação

Antes de considerar a PaymentsAPI pronta para utilização, confirme os seguintes itens:

## Infraestrutura

* [ ] SQL Server em execução.
* [ ] RabbitMQ em execução.
* [ ] Banco de dados criado.
* [ ] Migrations aplicadas.

## Aplicação

* [ ] Projeto compila sem erros.
* [ ] PaymentsAPI inicia corretamente.
* [ ] Health Check retorna **Healthy**.
* [ ] Swagger disponível em ambiente Development.

## Mensageria

* [ ] Fila `payments-order-placed-event` criada.
* [ ] Consumer conectado ao RabbitMQ.
* [ ] OrderPlacedEvent recebido.
* [ ] PaymentProcessedEvent publicado.

## Persistência

* [ ] Pagamento salvo no banco.
* [ ] GET `/api/payments` retorna os registros.
* [ ] Pagamentos duplicados não são criados para o mesmo `OrderId`.

## Integração

* [ ] CatalogAPI conclui a compra.
* [ ] NotificationsAPI registra a confirmação da compra.

---

# Conclusão

A **FCG Payments API** é responsável pelo processamento dos pagamentos da plataforma **FIAP Cloud Games**, atuando como um microsserviço orientado a eventos e totalmente desacoplado dos demais componentes da solução.

Sua arquitetura baseada em **DDD**, **CQRS**, **MediatR**, **RabbitMQ**, **MassTransit** e **Entity Framework Core** permite que o processamento dos pagamentos ocorra de forma organizada, escalável e de fácil manutenção.

No fluxo implementado para o Tech Challenge, a PaymentsAPI:

* consome o `OrderPlacedEvent`;
* processa e registra o pagamento;
* evita duplicidade por `OrderId`;
* publica o `PaymentProcessedEvent`;
* integra-se à CatalogAPI e NotificationsAPI através do RabbitMQ.

Essa abordagem garante baixo acoplamento entre os microsserviços e facilita futuras evoluções, como integração com gateways de pagamento reais, novos status de processamento e mecanismos mais avançados de confiabilidade, mantendo a arquitetura preparada para crescimento sem impactar os demais serviços da plataforma.
