using System;
using System.Threading;
using System.IO;

namespace smo
{
    public class procEventArgs : EventArgs
    {
        public int id { get; set; }
    }
    struct PoolRecord
    {
        public Thread thread;
        public bool in_use;
        public int wait;
        public int work;
    }
    class Server
    {
        public int requestCount = 0;
        public int processedCount = 0;
        public int rejectedCount = 0;
        public int poolcount;
        object threadLock;
        public PoolRecord[] pool;

        public Server(int k, PoolRecord[] pool)
        {

            this.pool = pool;
            this.poolcount = k;
            threadLock = new object();
            for (int i = 0; i < poolcount; i++)
                pool[i].in_use = false;
        }

        void Answer(object e)
        {
            Thread.Sleep(10);
            Console.WriteLine("Заявка с номером {0} выполнена", e);
            for (int i = 0; i < poolcount; i++)
            {
                if (pool[i].thread == Thread.CurrentThread)
                {
                    pool[i].in_use = false;
                    pool[i].thread = null;
                    break;
                }
            }
        }
        public void proc(object sender, procEventArgs e)
        {
            lock (threadLock)
            {
                Console.WriteLine("Заявка с номером {0}", e.id);
                requestCount++;
                for (int i = 0; i < poolcount; i++)
                {
                    if (!pool[i].in_use)
                        pool[i].wait++;
                }
                for (int i = 0; i < poolcount; i++)
                {
                    if (!pool[i].in_use)
                    {
                        pool[i].work++;
                        pool[i].in_use = true;
                        pool[i].thread = new Thread(new ParameterizedThreadStart(Answer));
                        pool[i].thread.Start(e.id);
                        processedCount++;
                        return;
                    }
                }
                rejectedCount++;
            }
        }
    }

    class Client
    {
        public event EventHandler<procEventArgs> request;
        Server server;
        public Client(Server server)
        {
            this.server = server;
            this.request += server.proc;
        }
        protected virtual void OnProc(procEventArgs e)
        {
            request?.Invoke(this, e);
        }
        public void Work(int index = 0)
        {
            procEventArgs e = new procEventArgs();
            index++;
            e.id = index;
            this.OnProc(e);
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            long func(long n)
            {
                if (n == 0)
                    return 1;
                else
                    return n * func(n - 1);
            }
            int ThreadCount = 8;
            int RequestCount = 64;
            PoolRecord[] pool = new PoolRecord[ThreadCount];
            Server server = new Server(ThreadCount, pool);
            Client client = new Client(server);
            for (int i = 0; i < RequestCount; i++)
                client.Work();
            Thread.Sleep(2000);
            Console.WriteLine("=============================================================================");
            Console.WriteLine($"Всего : {server.requestCount}. Выполнено: {server.processedCount}. Отклонено: {server.rejectedCount}");
            for (int i = 0; i < ThreadCount; ++i)
            {
                Console.WriteLine($"Потоком {i + 1} выполнено {server.pool[i].work} заявок. Время простоя {server.pool[i].wait} ");
            }

            double p = server.requestCount / server.poolcount;
            double temp = 0;
            for (int i = 0; i < server.poolcount; i++)
                temp += Math.Pow(p, i) / func(i);
            double p0 = 1 / temp;
            double pn = Math.Pow(p, server.poolcount) * p0 / func(server.poolcount);
            Console.WriteLine("=============================================================================");
            Console.WriteLine($"Приведенная интенсивность потока заявок: {p}");
            Console.WriteLine($"Вероятность простоя системы: {p0}");
            Console.WriteLine($"Вероятность отказа системы: {pn}");
            Console.WriteLine($"Относительная пропускная способность: {1 - pn}");
            Console.WriteLine($"Абсолютная пропускная способность: {(server.requestCount * (1 - pn))}");
            Console.WriteLine($"Среднее число занятых каналов: {((server.requestCount * (1 - pn)) / server.poolcount)}");
            Console.ReadKey();
        }
    }


}
