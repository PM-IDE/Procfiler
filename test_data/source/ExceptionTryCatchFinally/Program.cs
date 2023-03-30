

try
{
  try
  {
    ThrowException();
  }
  catch (IndexOutOfRangeException ex)
  {
    Console.WriteLine(ex.Message);
  }
  catch (ArgumentOutOfRangeException ex)
  {
    var p = new Program();
  }
  finally
  {
    var x = new string('c', 1);
  }
}
catch
{
  
}
void ThrowException()
{
  var y = 0;
  var x = 1 / y;
}