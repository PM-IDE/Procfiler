// See https://aka.ms/new-console-template for more information

for (int i = 0; i < 10_000; ++i)
{
  ThreadPool.QueueUserWorkItem(Console.WriteLine, i, false);
}

Thread.Sleep(2000);