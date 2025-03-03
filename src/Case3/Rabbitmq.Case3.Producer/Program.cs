﻿using System.Collections.Immutable;
using System.Drawing;
using Rabbitmq.Common.Data.Trades;
using Rabbitmq.Common.Display;
using RabbitMQ.Client;

namespace Rabbitmq.Case4.Producer
{
    internal sealed class Program
    {
        private static async Task Main()
        {
            Console.WriteLine("EXAMPLE 4 : ROUTING : PRODUCER");

            var connectionFactory = new ConnectionFactory
            {
                HostName = "192.168.88.20",
                UserName = "guest",
                Password = "guest"
            };

            using var connection = connectionFactory.CreateConnection();

            using var channel = connection.CreateModel();

            const string ExchangeName = "example4_trades_exchange";

            channel.ExchangeDeclare(
                exchange: ExchangeName,
                type: ExchangeType.Direct,
                durable: false,
                autoDelete: false,
                arguments: ImmutableDictionary<string, object>.Empty);

            var QueueNames = TradeData
                .Regions
                .Select(region =>
                {
                    var normalizedRegion = region.ToLower().Trim().Replace(" ", string.Empty);
                    var queueName = $"example4_trades_{normalizedRegion}_queue";
                    return new KeyValuePair<string, string>(region, queueName);
                })
                .ToImmutableDictionary();

            foreach (var region in TradeData.Regions)
            {
                var queue = channel.QueueDeclare(
                    queue: QueueNames[region],
                    durable: false,
                    exclusive: false,
                    autoDelete: false,
                    arguments: ImmutableDictionary<string, object>.Empty);

                channel.QueueBind(
                    queue: queue.QueueName,
                    exchange: ExchangeName,
                    routingKey: region,
                    arguments: ImmutableDictionary<string, object>.Empty);
            }

            while (true)
            {
                var trade = TradeData.GetFakeTrade();

                string routingKey = trade.Region;

                channel.BasicPublish(
                    exchange: ExchangeName,
                    routingKey: routingKey,
                    body: trade.ToBytes()
                );

                DisplayInfo<Trade>
                    .For(trade)
                    .SetExchange(ExchangeName)
                    .SetRoutingKey(routingKey)
                    .SetVirtualHost(connectionFactory.VirtualHost)
                    .Display(Color.Cyan);

                await Task.Delay(millisecondsDelay: 3000);
            }
        }
    }
}