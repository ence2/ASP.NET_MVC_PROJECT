using System.Data;
using ServiceStack.OrmLite;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace WebServer
{
    public enum DB_TYPE
    {
        None,
    }

    public class ContextPool
    {
        //로그용 타입 구분
        DB_TYPE type;

        //로그용 샤드 구분
        int shardID;

        //SemaphoreSlim semaphore;

        //사용 전 DB Pool
        System.Collections.Concurrent.ConcurrentQueue<OrmLiteConnectionFactory> DBpool = new System.Collections.Concurrent.ConcurrentQueue<OrmLiteConnectionFactory>();

        //사용 중 DB Pool
        ConcurrentDictionary<int, OrmLiteConnectionFactory> usedDBpool = new ConcurrentDictionary<int, OrmLiteConnectionFactory>();

        public void Init(string connStr, DB_TYPE dbType, int shard = 0)
        {
            //for loging
            type = dbType;
            shardID = shard;

            //Config상 접근 허용치만큼 세마포 구성
            //semaphore = new SemaphoreSlim(ServerConfig.Instance.Data.DBaccessPermitSize, ServerConfig.Instance.Data.DBaccessPermitSize);

            for (int i = 0; i < ServerConfig.Instance.Data.DBaccessPermitSize; ++i)
            {
                var factory = new OrmLiteConnectionFactory(connStr, MySqlDialect.Provider);
                factory.AutoDisposeConnection = false;
                factory.OnDispose += Disposed;
                factory.Open();

                DBpool.Enqueue(factory);
            }
        }

        public int CurrentThreadCount()
        {
            int currentWorker, currentIOC;

            ThreadPool.GetAvailableThreads(out currentWorker, out currentIOC);

            return currentWorker;
        }

        public IDbConnection GetDB()
        {
            //semaphore.Wait();

            OrmLiteConnectionFactory factory;
            Stopwatch watch = new Stopwatch();
            watch.Start();

            var cnt = CurrentThreadCount();
            if (cnt < (int)(ServerConfig.Instance.Data.ThreadPoolSize * (float)0.8f))
            {
                DataContext.SetBusy(true);
                DataContext.SetBusyCheck(DateTime.Now);
            }

            while (!DBpool.TryDequeue(out factory))
            {
                Thread.Sleep(500);

                if (watch.ElapsedMilliseconds > 1000)
                {
                    //Log.Warning("DBPool long waiting...: {0}, trace : {1}", type, Environment.StackTrace.ToString());
                }
            }

            IDbConnection con = factory.CreateDbConnection();

            long counter = 0;
            //Dispose 콜백의 parameter는 IdbConnection.
            while (!usedDBpool.TryAdd(con.GetHashCode(), factory))
            {
                counter++;

                if (counter > 1000000)
                    throw new Exception("Infinity Loop");
            }

            //Connection 유지
            try
            {
                con.ExecuteSql("SELECT 1;");
            }
            catch (Exception e)
            {
                //Log.Info("!Database reconnect: {0}({1}) {2}", type, shardID, e.Message);
                con.Open();
            }

            if (con.State == ConnectionState.Closed || con.State == ConnectionState.Broken)
            {
                //Log.Info("Database reconnect: {0}({1}) {2}", type, shardID);
                con.Open();
            }

            return con;
        }

        public void Disposed(OrmLiteConnection con)
        {
            if (DataContext.GetBusy())
            {
                if (CurrentThreadCount() >= (int)(ServerConfig.Instance.Data.ThreadPoolSize * (float)0.8f))
                {
                    if (DataContext.GetBusyCheck().AddMinutes(1) < DateTime.Now)
                        DataContext.SetBusy(false);
                }
            }

            OrmLiteConnectionFactory c;
            usedDBpool.TryGetValue(con.GetHashCode(), out c);
            DBpool.Enqueue(c);

            long counter = 0;
            OrmLiteConnectionFactory removed;
            while (!usedDBpool.TryRemove(con.GetHashCode(), out removed))
            {
                counter++;

                if (counter > 1000000)
                    throw new Exception("Infinity Loop");
            }

            //semaphore.Release();
        }

    }

    public class DataContext
    {
        static ReaderWriterLockSlim cs = new ReaderWriterLockSlim();
        static bool isBusy = false;
        static ReaderWriterLockSlim csTime = new ReaderWriterLockSlim();
        static DateTime busyCheck;

        static ContextPool userDB = new ContextPool();
        //static Dictionary<byte, ContextPool> gameDB = new Dictionary<byte, ContextPool>();
        //static ContextPool opDB = new ContextPool();
        //static ContextPool logDB = new ContextPool();

        //Config상 DB 접근허용 수 만큼 각 DB의 커넥션풀을 생성하는 초기작업.
        public static void InitPool()
        {
            userDB.Init(ServerConfig.Instance.Data.DBConnectionString, DB_TYPE.None);
        }

        public static IDbConnection OpenUserDB()
        {
            return userDB.GetDB();
        }

        public static void SetBusy(bool _isBusy)
        {
            try
            {
                cs.EnterWriteLock();
                isBusy = _isBusy;
            }
            finally
            {
                cs.ExitWriteLock();
            }
        }

        public static bool GetBusy()
        {
            try
            {
                cs.EnterReadLock();
                return isBusy;
            }
            finally
            {
                cs.ExitReadLock();
            }
        }

        public static void SetBusyCheck(DateTime time)
        {
            try
            {
                csTime.EnterWriteLock();
                busyCheck = time;
            }
            finally
            {
                csTime.ExitWriteLock();
            }
        }

        public static DateTime GetBusyCheck()
        {
            try
            {
                csTime.EnterReadLock();
                return busyCheck;
            }
            finally
            {
                csTime.ExitReadLock();
            }
        }
    }
}
