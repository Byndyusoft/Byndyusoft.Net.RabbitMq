using System;
using System.Collections.Generic;
using Xunit;

namespace Byndyusoft.Net.RabbitMq.Tests
{
    public class RabbitMqConnectionStringBuilderTests
    {
        public static IEnumerable<object[]> Data =>
            new List<object[]>
            {
                new object[]
                {
                    "host=myServer:1234;virtualHost=myVirtualHost;username=mike;password=topsecret;requestedHeartbeat=10;prefetchcount=20;timeout=30",
                    new RabbitMqConnectionStringBuilder
                    {
                        Hosts = new List<RabbitMqHost> {new RabbitMqHost("myServer", 1234)},
                        VirtualHost = "myVirtualHost",
                        UserName = "mike",
                        Password = "topsecret",
                        RequestedHeartbeat = TimeSpan.FromSeconds(10),
                        PrefetchCount = 20,
                        ConnectionTimeout = TimeSpan.FromSeconds(30)
                    }
                },
                new object[]
                {
                    "host=host1:1234,host2:5678",
                    new RabbitMqConnectionStringBuilder
                    {
                        Hosts = new List<RabbitMqHost>
                            {new RabbitMqHost("host1", 1234), new RabbitMqHost("host2", 5678)}
                    }
                }
            };

        [Fact]
        public void Constructor_Default()
        {
            // act
            var builder = new RabbitMqConnectionStringBuilder();

            // assert
            Assert.Equal("guest", builder.UserName);
            Assert.Equal("guest", builder.Password);
            Assert.Empty(builder.Hosts);
            Assert.Equal("/", builder.VirtualHost);
            Assert.Equal(TimeSpan.FromSeconds(10), builder.ConnectionTimeout);
            Assert.Equal(TimeSpan.FromSeconds(10), builder.RequestedHeartbeat);
            Assert.Equal(50, builder.PrefetchCount);
        }

        [Theory]
        [MemberData(nameof(Data))]
        public void Constructor(string connectionString, RabbitMqConnectionStringBuilder expected)
        {
            // act
            var builder = new RabbitMqConnectionStringBuilder(connectionString);

            // assert
            Assert.Equal(expected.UserName, builder.UserName);
            Assert.Equal(expected.Password, builder.Password);
            Assert.Equal(expected.Hosts, builder.Hosts);
            Assert.Equal(expected.VirtualHost, builder.VirtualHost);
            Assert.Equal(expected.PrefetchCount, builder.PrefetchCount);
            Assert.Equal(expected.ConnectionTimeout, builder.ConnectionTimeout);
            Assert.Equal(expected.RequestedHeartbeat, builder.RequestedHeartbeat);
        }

        [Theory]
        [MemberData(nameof(Data))]
        public void ConnectionString_Setter(string connectionString, RabbitMqConnectionStringBuilder expected)
        {
            // act
            var builder = new RabbitMqConnectionStringBuilder {ConnectionString = connectionString};

            // assert
            Assert.Equal(expected.UserName, builder.UserName);
            Assert.Equal(expected.Password, builder.Password);
            Assert.Equal(expected.Hosts, builder.Hosts);
            Assert.Equal(expected.VirtualHost, builder.VirtualHost);
            Assert.Equal(expected.ConnectionTimeout, builder.ConnectionTimeout);
            Assert.Equal(expected.RequestedHeartbeat, builder.RequestedHeartbeat);
        }
    }
}