using System.Runtime.ConstrainedExecution;

for (int i = 0; i < 50; ++i)
{
  var obj = new ClassWithFinalizer();
  Console.WriteLine(obj.GetType().Name);
  obj = null;
  GC.Collect(2);
  GC.WaitForFullGCComplete();
  GC.WaitForPendingFinalizers(); 
}

class ClassWithFinalizer : CriticalFinalizerObject
{
  ~ClassWithFinalizer()
  {
    Console.WriteLine("Hello world");
  }
}