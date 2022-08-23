# Byndyusoft.Messaging.RabbitMq

Библиотека для работы с RabbitMq. Главные сделанные выборы:

- Получение и публикация сообщений происходит через собственные объекты RabbitMqMessage и ReceivedRabbitMqMessage
- Контент передаётся через `HttpContent`
- Очередь ошибок есть всегда, в случае исключения сохраняется текст исключения
- Встроенная поддержка трассировки
- Встроенная поддержка метрик через активити сурц
- Очереди нужно явно декларировать, для поддержки миграторов

# IRabbtiMqСlient — рабочая лошадка библиотеки

IRabbtiMqСlient вдохновлён HttpClient'ом.

## Работа с сообщениями

- Опустошать очередь
- Получать количество сообщений в очереди
- Получать сообщения из очереди по одному
- Завершать обработку полученного сообщения одним из 4-х возможных исходов
  - Ack
  - NackWithRequeue
  - NackWithoutRequeue
  - Error
- Подписываться на сообщения очереди и возвращать один из 4-х возможных результатов
  - Ack
  - NackWithRequeue
  - NackWithoutRequeue
  - Error
- Публиковать сообщения в очередь

## Работа с топологией

- Создание очереди
- Проверка существования очереди
- Удаление очереди
- Создание обменника
- Проверка существования обменника
- Удаление обменника
- Связывание очереди и обменника

# Клиенты

Библиотекой можно пользоваться "как есть", но лучше использовать отдельные клиенты для каждого сервиса с которым происходит интеграция.

Клиент — инкапсулирует в себе:

- DTO
- название обменников и routing
- логику сериализации/десериализации DTO

## Сценарии, которые должны закрываться с помощью клиентов (пока не всё готово)

(+) Я как создатель сервиса, который владеет контрактом очереди, должен предоставлять клиента для отправки ему сообщений, и подписки на события от него

(-) Я как пользователь клиента могу изменить исходящее сообщение

(+) Я как пользователь клиента могу задавать очередь, через которую получаю события, и её настройки

(+) Я как пользователь клиента могу задать свою стратегию обработки полученных сообщений

(+) Я как создатель клиента, должен понимать, что должно быть в клиенте, а чего не должно быть (название обменников, ключей, дто, формат)

(+) Я как пользователь библиотеки, могу написать клиента для тех случаев, когда его нет по какой либо причине.

(-) Я как пользователь библиотеки, при подписке настраиваю все необходимые для обработки сообщения очереди. Библиотека автоматически создаст всё настроенное при появлении коннекта и пересоздаст при его восстановлении после обрыва

(-) Я как автор клиента имею готовую инфраструктуру для реализации RPC

# Simple usage

## Send message

Create queue in hosted service:

```csharp
public class QueueHostedService : IHostedService
{
    private readonly IRabbitMqClient _rabbitMqClient;

    public QueueHostedService(
        IRabbitMqClient rabbitMqClient)
    {
        _rabbitMqClient = rabbitMqClient;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _rabbitMqClient.CreateQueueAsync("app.sample_dto", o => o.AsAutoDelete(true), cancellationToken: cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
```

Send message:

```csharp
public async Task SendMessageAsync(int id)
{
    var sampleDto = new SampleDto
                        {
                            Id = id,
                            Name = "name"
                        };
    await _rabbitMqClient.PublishAsJsonAsync(exchangeName: null, 
                                             routingKey: "app.sample_dto", 
                                             model: sampleDto);
}
```

## Handle message

Register handler in hosted service:

```csharp
public class QueueHostedService : BackgroundService
{
    private readonly IRabbitMqClient _rabbitMqClient;
    private readonly SampleDtoHandler _sampleDtoHandler;

    public QueueHostedService(
        IRabbitMqClient rabbitMqClient,
        SampleDtoHandler sampleDtoHandler)
    {
        _rabbitMqClient = rabbitMqClient;
        _sampleDtoHandler = sampleDtoHandler;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _rabbitMqClient.SubscribeAsJson<SampleDto>("app.sample_dto", OnMessage)
                             .WithDeclareErrorQueue(o => o.AsAutoDelete(true))
                             .WithConstantTimeoutRetryStrategy(TimeSpan.FromSeconds(5), 5, o => o.AsAutoDelete(true))
                             .StartAsync(stoppingToken);
    }

    private async Task<ConsumeResult> OnMessage(SampleDto? dto, CancellationToken cancellationToken)
    {
        if (dto == null)
            throw new InvalidOperationException("content is null");

        await _sampleDtoHandler.HandleAsync(dto);
        return ConsumeResult.Ack;
    }
}
```

[![(License)](https://img.shields.io/github/license/Byndyusoft/Byndyusoft.Messaging.RabbitMq.Abstractions.svg)](LICENSE.txt)

| Package name | Nuget | Downloads |
| ------- | ------------ | --------- |
| [**Byndyusoft.Messaging.RabbitMq.Abstractions**](https://www.nuget.org/packages/Byndyusoft.Messaging.RabbitMq.Abstractions/) | [![Nuget](https://img.shields.io/nuget/v/Byndyusoft.Messaging.RabbitMq.Abstractions.svg)](https://www.nuget.org/packages/Byndyusoft.Messaging.RabbitMq.Abstractions/) | [![Downloads](https://img.shields.io/nuget/dt/Byndyusoft.Messaging.RabbitMq.Abstractions.svg)](https://www.nuget.org/packages/Byndyusoft.Messaging.RabbitMq.Abstractions/) |
| [**Byndyusoft.Messaging.RabbitMq.Core**](https://www.nuget.org/packages/Byndyusoft.Messaging.RabbitMq.Core/) | [![Nuget](https://img.shields.io/nuget/v/Byndyusoft.Messaging.RabbitMq.Core.svg)](https://www.nuget.org/packages/Byndyusoft.Messaging.RabbitMq.Core/) | [![Downloads](https://img.shields.io/nuget/dt/Byndyusoft.Messaging.RabbitMq.Core.svg)](https://www.nuget.org/packages/Byndyusoft.Messaging.RabbitMq.Core/) |
| [**Byndyusoft.Messaging.RabbitMq**](https://www.nuget.org/packages/Byndyusoft.Messaging.RabbitMq/) | [![Nuget](https://img.shields.io/nuget/v/Byndyusoft.Messaging.RabbitMq.svg)](https://www.nuget.org/packages/Byndyusoft.Messaging.RabbitMq/) | [![Downloads](https://img.shields.io/nuget/dt/Byndyusoft.Messaging.RabbitMq.svg)](https://www.nuget.org/packages/Byndyusoft.Messaging.RabbitMq/) |
| [**Byndyusoft.Messaging.RabbitMq.OpenTelemetry**](https://www.nuget.org/packages/Byndyusoft.Messaging.RabbitMq.OpenTelemetry/) | [![Nuget](https://img.shields.io/nuget/v/Byndyusoft.Messaging.RabbitMq.OpenTelemetry.svg)](https://www.nuget.org/packages/Byndyusoft.Messaging.RabbitMq.OpenTelemetry/) | [![Downloads](https://img.shields.io/nuget/dt/Byndyusoft.Messaging.RabbitMq.OpenTelemetry.svg)](https://www.nuget.org/packages/Byndyusoft.Messaging.RabbitMq.OpenTelemetry/) |

# Byndyusoft.Messaging.RabbitMq.Abstractions
Abstractions for Byndyusoft.Messaging.RabbitMq.

## Installing

```shell
dotnet add package Byndyusoft.Messaging.RabbitMq.Abstractions
```

# Byndyusoft.Messaging.RabbitMq.Core
Core implementation for Byndyusoft.Messaging.RabbitMq.

## Installing

```shell
dotnet add package Byndyusoft.Messaging.RabbitMq.Core
```

# Byndyusoft.Messaging.RabbitMq
EasyNetQ implementation for Byndyusoft.Messaging.RabbitMq.

## Installing

```shell
dotnet add package Byndyusoft.Messaging.RabbitMq
```

# Byndyusoft.Messaging.RabbitMq.OpenTelemetry
OpenTelemetry support for Byndyusoft.Messaging.RabbitMq.

## Installing

```shell
dotnet add package Byndyusoft.Messaging.RabbitMq.OpenTelemetry
```


# Contributing

To contribute, you will need to setup your local environment, see [prerequisites](#prerequisites). For the contribution and workflow guide, see [package development lifecycle](#package-development-lifecycle).

A detailed overview on how to contribute can be found in the [contributing guide](CONTRIBUTING.md).

## Prerequisites

Make sure you have installed all of the following prerequisites on your development machine:

- Git - [Download & Install Git](https://git-scm.com/downloads). OSX and Linux machines typically have this already installed.
- .NET Core (version 6.0 or higher) - [Download & Install .NET Core](https://dotnet.microsoft.com/download/dotnet/6.0).
- RabbitMq [Download & Install](https://www.rabbitmq.com/).

## General folders layout

### src
- source code

### tests

- unit-tests

### example

- example console application

## Package development lifecycle

- Implement package logic in `src`
- Add or addapt unit-tests (prefer before and simultaneously with coding) in `tests`
- Add or change the documentation as needed
- Open pull request in the correct branch. Target the project's `master` branch

# Maintainers

[github.maintain@byndyusoft.com](mailto:github.maintain@byndyusoft.com)

