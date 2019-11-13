using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace LockTest
{
    class Program
    {
        //1． 互斥量与临界区的作用非常相似，但互斥量是可以命名的，也就是说它可以跨越进程使用。所以创建互斥量需要的资源更多，
        //            所以如果只为了在进程内部是用的话使用临界区会带来速度上的优势并能够减少资源占用量。因为互斥量是跨进程
        //            的互斥量一旦被创建，就可以通过名字打开它。
        //2． 互斥量（Mutex），信号灯（Semaphore），事件（Event）都可以被跨越进程使用来进行同步数据操作，而其他的对象与数
        //            据同步操作无关，但对于进程和线程来讲，如果进程和线程在运行状态则为无信号状态，在退出后为有信号状态。
        //            所以可以使用WaitForSingleObject来等待进程和线程退出。
        //3． 通过互斥量可以指定资源被独占的方式使用，但如果有下面一种情况通过互斥量就无法处理，比如现在一位用户购买了一份
        //            三个并发访问许可的数据库系统，可以根据用户购买的访问许可数量来决定有多少个线程/进程能同时进行数据库
        //            操作，这时候如果利用互斥量就没有办法完成这个要求，信号灯对象可以说是一种资源计数器。


        // https://blog.csdn.net/ajioy/article/details/7620780
        // https://blog.csdn.net/prettyboy4/article/details/6846718
        // https://www.cnblogs.com/yifengjianbai/p/5468449.html
        // https://blog.csdn.net/xwdpepsi/article/details/6346890
        // https://www.cnblogs.com/suntp/p/8258488.html
        // https://www.cnblogs.com/3xiaolonglong/p/9650908.html

        private static int runTimes;
        private static int loopTimes;

        private static int atomicInt;
        private static void AtomicIntAdd()
        {
            for (var i = 0; i < runTimes; i++)
            {
                Interlocked.Increment(ref atomicInt);
            }
        }

        //资源状态锁，0--未被占有， 1--已被占有  
        private static int isLock;
        private static int ceInt;
        private static void CEIntAdd()
        {
            //long tmp = 0;
            for (var i = 0; i < runTimes; i++)
            {
                //while (Interlocked.CompareExchange(ref isLock, 1, 0) == 1)
                //{
                //    tmp++;
                //    tmp--;
                //}
                while (Interlocked.CompareExchange(ref isLock, 1, 0) == 1) { Thread.Sleep(1); }
                //while (Interlocked.CompareExchange(ref isLock, 1, 0) == 1) { }

                ceInt++;
                Interlocked.Exchange(ref isLock, 0);
            }
        }

        private static object obj = new object();
        private static int lockInt;
        private static void LockIntAdd()
        {
            for (var i = 0; i < runTimes; i++)
            {
                lock (obj)
                {
                    lockInt++;
                }
            }
        }

        private static Mutex mutex = new Mutex();
        private static int mutexInt;
        private static void MutexIntAdd()
        {
            for (var i = 0; i < runTimes; i++)
            {
                mutex.WaitOne();
                mutexInt++;
                mutex.ReleaseMutex();
            }
        }

        private static ReaderWriterLockSlim LockSlim = new ReaderWriterLockSlim();
        private static int lockSlimInt;
        private static void LockSlimIntAdd()
        {
            for (var i = 0; i < runTimes; i++)
            {
                LockSlim.EnterWriteLock();
                lockSlimInt++;
                LockSlim.ExitWriteLock();
            }
        }

        private static Semaphore sema = new Semaphore(1, 1);
        private static int semaphoreInt;
        private static void SemaphoreIntAdd()
        {
            for (var i = 0; i < runTimes; i++)
            {
                sema.WaitOne();
                semaphoreInt++;
                sema.Release();
            }
        }

        public static AutoResetEvent autoResetEvent = new AutoResetEvent(true);
        private static int autoResetEventInt;
        private static void AutoResetEventIntAdd()
        {
            for (var i = 0; i < runTimes; i++)
            {
                if (autoResetEvent.WaitOne())
                {
                    autoResetEventInt++;
                    autoResetEvent.Set();
                }
            }
        }

        private static int noLockInt;
        private static void NoLockIntAdd()
        {
            for (var i = 0; i < runTimes; i++)
            {
                noLockInt++;
            }
        }


        static void Main(string[] args)
        {
            while (true)
            {
                //Console.WriteLine();
                //Console.Write("单线程测试，请输入执行次数:");
                //runTimes = Console.ReadLine().ToInt(1);

                //Init();
                //RunS();

                Console.WriteLine("多线程测试");
                Console.Write("请输入线程个数:");
                loopTimes = Convert.ToInt32(Console.ReadLine());

                Console.Write("请输入执行次数:");
                runTimes = Convert.ToInt32(Console.ReadLine());

                Init();
                RunM();
            }

            Console.ReadKey();
        }

        private static void Init()
        {
            //runTimes = 0;
            //loopTimes = 0;

            atomicInt = 0;
            isLock = 0;
            ceInt = 0;
            lockInt = 0;
            mutexInt = 0;
            lockSlimInt = 0;
            semaphoreInt = 0;
            autoResetEventInt = 0;
            noLockInt = 0;
        }

        private static void RunM()
        {
            var stopwatch = new Stopwatch();
            var taskList = new Task[loopTimes];

            // 多线程
            Console.WriteLine();
            Console.WriteLine($"              线程数:{loopTimes}");
            Console.WriteLine($"            执行次数:{runTimes}");
            Console.WriteLine($"        校验值应等于:{runTimes * loopTimes}");

            // AtomicIntAdd
            stopwatch.Restart();
            for (var i = 0; i < loopTimes; i++)
            {
                taskList[i] = Task.Factory.StartNew(() => { AtomicIntAdd(); });
            }
            Task.WaitAll(taskList);
            Console.WriteLine($"{GetFormat("AtomicIntAdd")}, 总耗时:{stopwatch.ElapsedMilliseconds}毫秒, 校验值:{atomicInt}");

            // CEIntAdd
            taskList = new Task[loopTimes];
            stopwatch.Restart();

            for (var i = 0; i < loopTimes; i++)
            {
                taskList[i] = Task.Factory.StartNew(() => { CEIntAdd(); });
            }
            Task.WaitAll(taskList);
            Console.WriteLine($"{GetFormat("CEIntAdd")}, 总耗时:{stopwatch.ElapsedMilliseconds}毫秒, 校验值:{ceInt}");

            // LockIntAdd
            taskList = new Task[loopTimes];
            stopwatch.Restart();

            for (var i = 0; i < loopTimes; i++)
            {
                taskList[i] = Task.Factory.StartNew(() => { LockIntAdd(); });
            }
            Task.WaitAll(taskList);
            Console.WriteLine($"{GetFormat("LockIntAdd")}, 总耗时:{stopwatch.ElapsedMilliseconds}毫秒, 校验值:{lockInt}");

            // MutexIntAdd
            taskList = new Task[loopTimes];
            stopwatch.Restart();

            for (var i = 0; i < loopTimes; i++)
            {
                taskList[i] = Task.Factory.StartNew(() => { MutexIntAdd(); });
            }
            Task.WaitAll(taskList);
            Console.WriteLine($"{GetFormat("MutexIntAdd")}, 总耗时:{stopwatch.ElapsedMilliseconds}毫秒, 校验值:{mutexInt}");

            // LockSlimIntAdd
            taskList = new Task[loopTimes];
            stopwatch.Restart();

            for (var i = 0; i < loopTimes; i++)
            {
                taskList[i] = Task.Factory.StartNew(() => { LockSlimIntAdd(); });
            }
            Task.WaitAll(taskList);
            Console.WriteLine($"{GetFormat("LockSlimIntAdd")}, 总耗时:{stopwatch.ElapsedMilliseconds}毫秒, 校验值:{lockSlimInt}");

            // SemaphoreIntAdd
            taskList = new Task[loopTimes];
            stopwatch.Restart();

            for (var i = 0; i < loopTimes; i++)
            {
                taskList[i] = Task.Factory.StartNew(() => { SemaphoreIntAdd(); });
            }
            Task.WaitAll(taskList);
            Console.WriteLine($"{GetFormat("SemaphoreIntAdd")}, 总耗时:{stopwatch.ElapsedMilliseconds}毫秒, 校验值:{semaphoreInt}");


            // AutoResetEventIntAdd
            taskList = new Task[loopTimes];
            stopwatch.Restart();

            for (var i = 0; i < loopTimes; i++)
            {
                taskList[i] = Task.Factory.StartNew(() => { AutoResetEventIntAdd(); });
            }
            Task.WaitAll(taskList);
            Console.WriteLine($"{GetFormat("AutoResetEventIntAdd")}, 总耗时:{stopwatch.ElapsedMilliseconds}毫秒, 校验值:{autoResetEventInt}");

            // NoLockIntAdd
            taskList = new Task[loopTimes];
            stopwatch.Restart();

            for (var i = 0; i < loopTimes; i++)
            {
                taskList[i] = Task.Factory.StartNew(() => { NoLockIntAdd(); });
            }
            Task.WaitAll(taskList);
            Console.WriteLine($"{GetFormat("NoLockIntAdd")}, 总耗时:{stopwatch.ElapsedMilliseconds}毫秒, 校验值:{noLockInt}");
            Console.WriteLine();
        }

        private static void RunS()
        {
            var stopwatch = new Stopwatch();

            // 单线程
            Console.WriteLine();
            Console.WriteLine($"            执行次数:{runTimes}");
            Console.WriteLine($"        校验值应等于:{runTimes}");

            // AtomicIntAdd
            stopwatch.Start();
            AtomicIntAdd();
            Console.WriteLine($"{GetFormat("AtomicIntAdd")}, 总耗时:{stopwatch.ElapsedMilliseconds}毫秒, 校验值:{atomicInt}");

            // CEIntAdd
            stopwatch.Restart();
            CEIntAdd();
            Console.WriteLine($"{GetFormat("CEIntAdd")}, 总耗时:{stopwatch.ElapsedMilliseconds}毫秒, 校验值:{ceInt}");

            // LockIntAdd
            stopwatch.Restart();
            LockIntAdd();
            Console.WriteLine($"{GetFormat("LockIntAdd")}, 总耗时:{stopwatch.ElapsedMilliseconds}毫秒, 校验值:{lockInt}");

            // MutexIntAdd
            stopwatch.Restart();
            MutexIntAdd();
            Console.WriteLine($"{GetFormat("MutexIntAdd")}, 总耗时:{stopwatch.ElapsedMilliseconds}毫秒, 校验值:{mutexInt}");

            // LockSlimIntAdd
            stopwatch.Restart();
            LockSlimIntAdd();
            Console.WriteLine($"{GetFormat("LockSlimIntAdd")}, 总耗时:{stopwatch.ElapsedMilliseconds}毫秒, 校验值:{lockSlimInt}");

            // SemaphoreIntAdd
            stopwatch.Restart();
            SemaphoreIntAdd();
            Console.WriteLine($"{GetFormat("SemaphoreIntAdd")}, 总耗时:{stopwatch.ElapsedMilliseconds}毫秒, 校验值:{semaphoreInt}");

            // AutoResetEventIntAdd
            stopwatch.Restart();
            AutoResetEventIntAdd();
            Console.WriteLine($"{GetFormat("AutoResetEventIntAdd")}, 总耗时:{stopwatch.ElapsedMilliseconds}毫秒, 校验值:{autoResetEventInt}");

            // NoLockIntAdd
            stopwatch.Restart();
            NoLockIntAdd();
            Console.WriteLine($"{GetFormat("NoLockIntAdd")}, 总耗时:{stopwatch.ElapsedMilliseconds}毫秒, 校验值:{noLockInt}");
        }

        private static string GetFormat(string str)
        {
            return str.PadLeft(20, ' ');
        }

    }

    /// <summary>  
    /// 一个类似于自旋锁的类，也类似于对共享资源的访问机制  
    /// 如果资源已被占有，则等待一段时间再尝试访问，如此循环，直到能够获得资源的使用权为止  
    /// </summary>  
    public class SpinLock
    {
        //资源状态锁，0--未被占有， 1--已被占有  
        private int isLock = 0;

        //public SpinLock()
        //{ }

        /// <summary>  
        /// 访问  
        /// </summary>  
        public void Lock(Action action)
        {
            //while (true)
            //{
            //    //如果已被占有，则继续等待  
            //    while (Interlocked.CompareExchange(ref isLock, 1, 0) == 1) { }

            //    action();
            //    isLock = 0;
            //    break;
            //}

            //如果已被占有，则继续等待  
            while (Interlocked.CompareExchange(ref isLock, 1, 0) == 1) { Thread.Sleep(1); }

            action();
            isLock = 0;
        }

        ///// <summary>  
        ///// 退出  
        ///// </summary>  
        //public void Exit()
        //{
        //    //重置资源锁  
        //    Interlocked.Exchange(ref theLock, 0);
        //}
    }


    public class SpinLock2
    {
        //资源状态锁，0--未被占有， 1--已被占有  
        private int theLock = 0;
        //等待时间  
        private int spinWait;

        public SpinLock2(int spinWait)
        {
            this.spinWait = spinWait;
        }

        /// <summary>  
        /// 访问  
        /// </summary>  
        public void Enter()
        {
            //如果已被占有，则继续等待  
            while (Interlocked.CompareExchange(ref theLock, 1, 0) == 1)
            {
                Thread.Sleep(spinWait);
            }
        }

        /// <summary>  
        /// 退出  
        /// </summary>  
        public void Exit()
        {
            //重置资源锁  
            Interlocked.Exchange(ref theLock, 0);
        }
    }

    /// <summary>  
    /// 自旋锁的管理类   
    /// </summary>  
    public class SpinLockManager : IDisposable
    {
        // 保证多次调用Dispose方式不会抛出异常
        private bool disposed;
        private SpinLock spinLock;

        public SpinLockManager(SpinLock spinLock)
        {
            disposed = false;
            this.spinLock = spinLock;
        }

        public void Lock(Action action)
        {
            //this.spinLock.Lock(action);
        }

        // 可以被客户直接调用 
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);

            //// 必须以Dispose(true)方式调用,以true告诉Dispose(bool disposing)函数是被客户直接调用的 
            //Dispose(true);     // MUST be true
            //GC.SuppressFinalize(this); // 告诉垃圾回收器从Finalization队列中清除自己,从而阻止垃圾回收器调用Finalize方法.         
        }

        protected virtual void Dispose(bool disposing)
        {
            // If you need thread safety, use a lock around these operations, as well as in your methods that use the resource.
            // Check to see if Dispose has already been called

            if (disposed) return;

            if (disposing) // 这个方法是被客户直接调用的,那么托管的,和非托管的资源都可以释放
            {
                // 在这里释放托管资源
                //spinLock.Exit();
            }

            // 在这里释放非托管资源 
            disposed = true; // Indicate that the instance has been disposed
        }

        // 析构函数自动生成 Finalize 方法和对基类的 Finalize 方法的调用.默认情况下,一个类是没有析构函数的,也就是说,对象被垃圾回收时不会被调用Finalize方法 
        ~SpinLockManager()
        {
            //Console.WriteLine("SpinLockManager被释放");
            //spinLock.Exit();
            //// 为了保持代码的可读性性和可维护性,千万不要在这里写释放非托管资源的代码 
            //// 必须以Dispose(false)方式调用,以false告诉Dispose(bool disposing)函数是从垃圾回收器在调用Finalize时调用的 
            Dispose(false);    // MUST be false
        }

    }

}
