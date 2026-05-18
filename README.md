# SartorialWatcher

SartorialWatcher is a .NET application that tracks clothing prices across major Polish menswear stores and sends automated Telegram notifications when selected products drop below a target price.

I've written SartorialWatcher out of the need of having up-to-date information about shirt prices in shops like
Wólczanka or Bytom. I wanted to be notified in a convenient way which is for example Telegram.

# What it does?

The flow is divided into two pipelines.

### Scraping pipeline

1. Scheduler triggers a scraping job
2. Scraper loads the shop's site and extracts all products
3. Products are saved to database

### Reporting pipeline

1. Scheduler triggers a reporting job
2. Application filters products in a database by a condition (scrape date, shirt size, material or price)
3. The reporting layer formats a message ready for delivery
4. Report is sent via Telegram, for instance

# How it's built?

### Code is divided into layers according to the **Dependency Inversion Principle**:

- Domain layer: contains core business entities and abstractions for repositories and services.
- Scraping layer: implementations of the `IScraper` interfaces intended to load products from websites.
- Infrastructure layer: implementations of the domain contracts, mostly using AWS services like DynamoDB.
- I/O layer: runs the program as a console app or a Web Api.

### Solution is divided into projects:

- _SartorialWatcher.Core_ — code shared between I/O devices (library).
- _SartorialWatcher.Api_ — ASP.NET Core Web API for local testing or AWS Lambda hosting. Contains the
  `Program.cs` (executable).
- _SartorialWatcher.Console_ — implementation of the console app for local testing. Contains the `Program.cs` with
  scenario involving the scraping and showing the report locally (executable).

### DynamoDB schema

### What is planned to add?
- More stores (about 10).
- Historical price reports on-demand in a Telegram chat.
- Dividing the solution into projects corresponding to the architecture layers to increase the modularity.
- Smarten the alert rules and allow setting custom, not hardcoded conditions.
- Docker deployment.
- Change the architecture so one site scraping job corresponding on Lambda invocation (increasing the modularity).
- OpenTelemetry metrics.

# How to run it?

### Set up the environment variables
1. Create a Telegram bot and maybe create a DynamoDB table on AWS.
1. Set the user-secrets as following, replacing "X" with real values.
```shell
dotnet user-secrets set Telegram:Chat:Id="X" Telegram:Bot:Token="X" Scheduler:ApiKey="X" Aws:Dynamo:TableName="X" --project SartorialWatcher.Core
```
2. If you host your application at AWS, set `SECRET_NAME` variable in your hosting environment, for example in `serverless.template` file
##### Explaining the parameters
- `Scheduler:ApiKey` variable is an API key sent by the client to secure the application

### Run the Web API

```shell
dotnet restore
dotnet run --project SartorialWatcher.Api
```

### Run the console app

```shell
dotnet restore
dotnet run --project SartorialWatcher.Console
```

For developing locally, it's handy to run SartorialWatcher as classic .NET app.
The command invokes the scraping and reporting flows one-after-one.

# What technologies are inside?

- ASP.NET Core and C# at the backend level
- AWS Lambda and Amazon API Gateway for hosting the application
- Amazon DynamoDB for a cheap and fast storage
- Amazon EventBridge for scheduling the tasks
- CloudWatch for logging (the application uses Serilog)
- Resilient HTTP communication implemented with Polly retry policies
- Concurrent scraping is coordinated with SemaphoreSlim to avoid overloading target stores and reduce memory overhead

# What problems did I solve?
As of May 2026, There is no public scraper which compares prices from main polish menswear shops (Wólczanka, Vistula, Bytom, Giacomo Conti, Lavard) and sending them in a comfortable way to user.
The app is designed to notify about shirts when price drops below 100PLN which creates an opportunity to acquire high quality clothes very cheap and without spending time tracking the stores manually.

# Screenshots
![img.png](img.png)