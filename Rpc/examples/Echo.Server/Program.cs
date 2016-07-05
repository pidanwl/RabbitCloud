﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rabbit.Rpc;
using Rabbit.Rpc.Address;
using Rabbit.Rpc.Routing;
using Rabbit.Rpc.Runtime.Server;
using Rabbit.Transport.DotNetty;
using System;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace Echo.Server
{
    internal class Program
    {
        static Program()
        {
            //因为没有引用Echo.Common中的任何类型
            //所以强制加载Echo.Common程序集以保证Echo.Common在AppDomain中被加载。
            Assembly.Load("Echo.Common");
        }

        private static void Main()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection
                .AddLogging()
                .AddRpcCore()
                .AddServiceRuntime()
                .UseSharedFileRouteManager("d:\\routes.txt")
                //zookeeper服务路由管理者。
                //                .UseZooKeeperRouteManager(new ZooKeeperServiceRouteManager.ZookeeperConfigInfo("172.18.20.132:2181"))
                .UseDotNettyTransport();

            var serviceProvider = serviceCollection.BuildServiceProvider();

            serviceProvider.GetRequiredService<ILoggerFactory>()
                .AddConsole((c, l) => (int)l >= 3);

            //自动生成服务路由（这边的文件与Echo.Client为强制约束）
            {
                var serviceEntryManager = serviceProvider.GetRequiredService<IServiceEntryManager>();
                var addressDescriptors = serviceEntryManager.GetEntries().Select(i => new ServiceRoute
                {
                    Address = new[] { new IpAddressModel { Ip = "127.0.0.1", Port = 9981 } },
                    ServiceDescriptor = i.Descriptor
                });

                var serviceRouteManager = serviceProvider.GetRequiredService<IServiceRouteManager>();
                serviceRouteManager.AddRoutesAsync(addressDescriptors).Wait();
            }

            var serviceHost = serviceProvider.GetRequiredService<IServiceHost>();

            Task.Factory.StartNew(async () =>
            {
                //启动主机
                await serviceHost.StartAsync(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9981));
                Console.WriteLine($"服务端启动成功，{DateTime.Now}。");
            });
            Console.ReadLine();
        }
    }
}