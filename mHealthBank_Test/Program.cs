using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SAGE.IoC.Autofac;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Topshelf;
using Topshelf.Runtime;
using Topshelf.StartParameters;

namespace mHealthBank
{
    public class Program
    {
        public const string ServiceName = "mHealthBank-Services";

        private static SAGE.Core.Interface.ILogger Log;

        public static void Main(string[] args)
        {
            mHealthBankLogFactory.InitLog("nlog.config");
            Log = mHealthBankLogFactory.GetLogger<Program>();

            //CreateHostBuilder(args).Build().Run();

            //AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            var newArgs = CreateArgs(ref args);

            CreateServices(args, newArgs);
        }

        static IEnumerable<string> CreateArgs(ref string[] args)
        {
            var condition = args
                .Where(c => !string.IsNullOrEmpty(c) && (c.StartsWith("::") || c.StartsWith(":::")));

            //var newArgs = condition
            //    .Select(c => new KeyValuePair<string, string>(c.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries)[0],
            //     c.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries)[1]))
            //    .ToArray();

            //var newArgs = condition
            //    .Select(c => c.Substring(2))
            //    .Select(c => c.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries))
            //    .Where(c => c.Length == 2)
            //    .SelectMany(c => new string[] { "--" + c[0], c[1] })
            //    .ToArray();
            var newArgs = condition
                    .Select(c => c.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
                    .Where(c => c.Length == 2)
                    .Select(c => c[0])
                    .ToArray();

            var lst = new HashSet<string>(args);

            args = lst.Where(c => !condition.Contains(c)).ToArray();

            lst.Clear();

            return newArgs;
        }

        static void CreateServices(string[] args, IEnumerable<string> newArgs)
        {
            HostFactory.Run(opt =>
            {
                opt.EnableStartParameters();

                ICollection<KeyValuePair<string, string>> argsCol = new HashSet<KeyValuePair<string, string>>();

                //opt.UseNLog
                opt.UseEagleScoreTopShelfLog();

                opt.RunAsLocalSystem();

                opt.StartAutomaticallyDelayed();

                opt.SetDescription(Program.GetAsmDescription());
                opt.SetDisplayName(Program.GetAsmTitle());
                opt.SetServiceName(ServiceName);

                opt.Service<mHealthBankService>(svc =>
                {
                    svc.ConstructUsing(hs => new mHealthBankService(hs, argsCol));
                    svc.WhenStarted((svc, hc) => svc.Start(hc));
                    svc.WhenStopped((svc, hc) => svc.Stop(hc));
                });

                opt.WithStartParameter("setenv", "env", val => argsCol.Add(new KeyValuePair<string, string>("--environment", val)));

                //opt.Service(sv => new EagleScoreService(sv, argsCol));

                opt.EnableServiceRecovery(r =>
                {
                    ////you can have up to three of these
                    //r.RestartComputer(5, "message");
                    //r.RestartService(0);
                    ////the last one will act for all subsequent failures
                    //r.RunProgram(7, "ping google.com");

                    ////should this be true for crashed or non-zero exits
                    //r.OnCrashOnly();

                    ////number of days until the error count resets
                    //r.SetResetPeriod(1);

                    r.RestartService(1);

                    r.RestartService(2);

                    r.RestartService(3);

                    r.SetResetPeriod(0);

                    r.OnCrashOnly();
                });
            });
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs ex)
        {
            string jsonData = null;

            if (ex.ExceptionObject != null)
            {
                Newtonsoft.Json.JsonConvert.SerializeObject(ex.ExceptionObject);
            }

            StringBuilder sb = new StringBuilder();

            sb.AppendLine("CurrentDomain_UnhandledException");
            sb.AppendLine(string.Concat("Object : ", jsonData));
            sb.AppendLine(string.Concat("Terminate : ", ex.IsTerminating));

            Log.Fatal(sb.ToString());

            sb.Clear();
        }

        static void CurrentDomain_FirstChanceException(object sender, FirstChanceExceptionEventArgs ex)
        {
            StringBuilder sb = new StringBuilder();


            sb.AppendLine("----------------------------");
            sb.AppendLine(ex.Exception.Message);
            sb.AppendLine("----------------------------");
            sb.AppendLine(ex.Exception.StackTrace);

            Log.Error(sb.ToString());

            sb.Clear();
        }

        static string GetAsmDescription()
        {
            string ret = null;

            var execAssembly = Assembly.GetExecutingAssembly();

            //Type of attribute that is desired
            Type type = typeof(AssemblyDescriptionAttribute);

            //Is there an attribute of this type already defined?
            if (Attribute.IsDefined(execAssembly, type))
            {
                //if there is, get attribute of desired type
                AssemblyDescriptionAttribute a = (AssemblyDescriptionAttribute)AssemblyDescriptionAttribute.GetCustomAttribute(execAssembly, type);

                //Print description
                ret = a.Description;
            }

            return ret;
        }

        static string GetAsmTitle()
        {
            string ret = null;

            var execAssembly = Assembly.GetExecutingAssembly();

            //Type of attribute that is desired
            Type type = typeof(AssemblyTitleAttribute);

            //Is there an attribute of this type already defined?
            if (Attribute.IsDefined(execAssembly, type))
            {
                //if there is, get attribute of desired type
                AssemblyTitleAttribute a = (AssemblyTitleAttribute)Attribute.GetCustomAttribute(execAssembly, type);

                //Print description
                ret = a.Title;
            }

            return ret;
        }
    }


    class mHealthBankService : ServiceControl
    {
        private readonly SAGE.Core.Interface.ILogger Log;

        readonly ICollection<KeyValuePair<string, string>> argsCol;
        readonly HostSettings settings;
        readonly CancellationTokenSource cts;

        IHost host;

        public mHealthBankService(HostSettings settings, ICollection<KeyValuePair<string, string>> argsCol)
        {
            Log = mHealthBankLogFactory.GetLogger<mHealthBankService>();

            this.settings = settings;
            this.argsCol = argsCol;
            cts = new CancellationTokenSource();
        }

        public bool Start(HostControl hostControl)
        {
            hostControl.RequestAdditionalTime(TimeSpan.FromSeconds(60));

            //var args = string.Join(' ', lstArgs.ToArray());

            var args = argsCol.SelectMany(c => new[] { c.Key, c.Value }).ToArray();


            host = BuildHost(args).Build();
            host.Start();

            try
            {
                BuildAwaiter();
            }
            catch {; }

            return true;
        }

        public bool Stop(HostControl hostControl)
        {
            cts.Cancel();

            argsCol.Clear();

            mHealthBankLogFactory.ShutDown();

            host.StopAsync().GetAwaiter().GetResult();

            host.Dispose();

            return true;
        }

        private IHostBuilder BuildHost(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("hostsettings.json", optional: true)
                .AddEnvironmentVariables("ES_")
                .AddCommandLine(args)
                .Build();

            var configTimeout = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true)
                .Build();

            var timeLength = configTimeout.GetValue<int>("requestTimeout", 120);
            var time = TimeSpan.FromSeconds(timeLength);

            return Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
                .UseServiceProviderFactory(new IoCServiceProviderFactory())
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseConfiguration(config);
                    webBuilder.ConfigureKestrel(opt =>
                    {
                        opt.DisableStringReuse = true;
                        opt.Limits.KeepAliveTimeout = time;
                    });
                })
                .ConfigureLogging(logger =>
                {
                    logger.ClearProviders();
                    logger.SetMinimumLevel(LogLevel.Trace);
                })
                .ConfigureServices((context, services) =>
                {
                    services.Configure<KestrelServerOptions>(context.Configuration.GetSection("Kestrel"));
                })
                .UseEagleScoreLog();
        }

        void BuildAwaiter()
        {
            DateTime now = DateTime.Now;
            DateTime timeClearMemory;

            TimeSpan diffTime;

            timeClearMemory = now.Date + TimeSpan.Parse("23:00:00");

            //Task.
            if (now > timeClearMemory)
                timeClearMemory = timeClearMemory.AddDays(1);

            diffTime = timeClearMemory.Subtract(now);

            Task.Delay(diffTime, cts.Token).ContinueWith(c => CleanMemory(), cts.Token);
        }

        void Restart(object obj)
        {
            string args = string.Format("/C net stop \"{0}\" & net start \"{0}\"", settings.ServiceName);

            try
            {
                using (System.Diagnostics.Process proc = new System.Diagnostics.Process())
                {
                    System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo("cmd.exe", args);
                    psi.CreateNoWindow = true;
                    psi.LoadUserProfile = false;
                    psi.UseShellExecute = false;
                    psi.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                    proc.StartInfo = psi;

                    proc.Start();
                }
            }
            catch
            {
                Environment.Exit(1);
            }
        }

        void CleanMemory()
        {
            try
            {
                Log.Info("Clear memory");

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                Log.Info("Clear memory Done");
            }
            catch (Exception ex)
            {
                Log.Error($"CleanUpMemoryUsage", ex);
            }
            finally
            {
                try
                {
                    BuildAwaiter();
                }
                catch {; }

                Log.Info("Clear memory Completed");
            }
        }
    }
}
