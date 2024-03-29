﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ConsoleApp1.TaskPratice
{
    class Program
    {

        private static readonly object obj_lock = new object();

        static void Main(string[] args)
        {
            try
            {
        
                
                TaskFactory taskFactory = new TaskFactory();
                List<Task<string>> pTasks = new List<Task<string>>();
                List<Task<string>> evenTasks = new List<Task<string>>();
                bool firstEventFinished = false;

                CancellationTokenSource tokenSource = new CancellationTokenSource();

                //获取人物线配置内容
                List<StoryModel> storyList = ConfigHelper.CurrentInstance.GetStoryConfig();

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                foreach (StoryModel storyModel in storyList)
                {
                    string name = storyModel.Name;
                    //父线程
                    Task<string> PTask = (taskFactory.StartNew((() =>
                      {
                          ManualResetEvent resetEvent = new ManualResetEvent(false);
                          foreach (string eventName in storyModel.EventList)
                          {
                              string ename = eventName;
                              if (!tokenSource.IsCancellationRequested)
                              {

                                  //添加到父线程，父线程要等待子线程执行完毕才会执行
                                  evenTasks.Add(taskFactory.StartNew(() =>
                                  {
                                      FinishEvent(name, ename, tokenSource);
                                      resetEvent.Set();
                                      resetEvent.Reset();
                                      return name;
                                  }, tokenSource.Token, TaskCreationOptions.AttachedToParent, TaskScheduler.Current));
                                  taskFactory.ContinueWhenAny(evenTasks.ToArray(),
                                      (task =>
                                      {
                                          if (!firstEventFinished)
                                          {
                                              firstEventFinished = true;
                                              string content = "天龙八部就此拉开序幕。。。。";
                                              Console.WriteLine(content);
                                              LogHelper.CurrentInstance.LogActionInfo(new LogModel(content, 0, 0));
                                          }

                                      }), tokenSource.Token);
                                  resetEvent.WaitOne();

                              }
                              else
                              {
                                  break;
                              }
                          }

                          return name;

                      }), tokenSource.Token));
                    pTasks.Add(PTask);
                }

                

                //监控线程
                Task.Run((() =>
                {
                    while (!tokenSource.IsCancellationRequested)
                    {
                        int timespan = new Random(Guid.NewGuid().GetHashCode()).Next(0,10000);
                        LogHelper.CurrentInstance.LogActionInfo(new LogModel($"Thread={Thread.CurrentThread.ManagedThreadId} 监控线程间隔时间{timespan}ms", 0, 0));
                        if (timespan == DateTime.Now.Year)
                        {
                            tokenSource.Cancel();
                            break;
                        }
                    }
                    string content = "天降雷霆灭世，天龙八部的故事就此结束....";
                    Console.WriteLine($"ThreadId={Thread.CurrentThread.ManagedThreadId} {content}");
                    LogHelper.CurrentInstance.LogActionInfo(new LogModel(content, 0, 0));
                }), tokenSource.Token);

                taskFactory.ContinueWhenAny(pTasks.ToArray(), (task =>
                {
                    string content = $"{task.Result}已经准备好了";
                    Console.WriteLine(content);
                    LogHelper.CurrentInstance.LogActionInfo(new LogModel(content, 0, 0));
                }), tokenSource.Token);
                var action = new Action<Task<string>[]>(task =>
                  {
                      string content = "中原群雄大战辽兵，忠义两难一死谢天";
                      Console.WriteLine(content);
                      LogHelper.CurrentInstance.LogActionInfo(new LogModel(content, 0, 0));
                  });
                action += (task =>
                {
                    stopwatch.Stop();
                    string content = $"天龙八部的故事耗时{stopwatch.ElapsedMilliseconds}ms";
                    Console.WriteLine(content);
                    LogHelper.CurrentInstance.LogActionInfo(new LogModel(content, 0, 0));
                });
                taskFactory.ContinueWhenAll(pTasks.ToArray(), action, tokenSource.Token);
                Console.ReadKey();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

            }

            Console.ReadKey();
        }

        /// <summary>
        /// 完成人物的事件
        /// </summary>
        /// <param name="name"></param>
        /// <param name="eventname"></param>
        public static List<string> FinishEvent(string name, string eventname, CancellationTokenSource token)
        {
            if (token.IsCancellationRequested)
            {
                throw new AggregateException();
            }
            int seed = GetRandomSeedByGuid();
            //Console.WriteLine($"ThreadId={Thread.CurrentThread.ManagedThreadId} guid={seed}");
            Random random = new Random(seed);
            Thread.Sleep(random.Next(500, 1000));
            //输出内容
            string content = $"ThreadId={Task.CurrentId} {name} 已完成事件——{eventname}";
            Console.WriteLine(content);
            //记录日志
            LogHelper.CurrentInstance.LogActionInfo(new LogModel(content, 0, 0));

            return new List<string>() { name, eventname };
        }

        public static int GetRandomSeedByGuid()
        {
            return Guid.NewGuid().GetHashCode();
        }
    }
}
