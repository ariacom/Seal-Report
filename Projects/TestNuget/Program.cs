using Seal.Model;
using System.Diagnostics;
using System.Text;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

//Repository.RepositoryConfigurationPath = @"C:\ProgramData\Seal Report Repository"; //to specify another Repository Path
Repository repository = Repository.Create();
Report report = Report.LoadFromFile(@"C:\ProgramData\Seal Report Repository\Reports\Search - Orders.srex", repository);
ReportExecution execution = new ReportExecution() { Report = report };
execution.Execute();
while (report.IsExecuting)
{
    Thread.Sleep(100);
}
string resultPath = execution.GenerateHTMLResult();
var p = new Process();
p.StartInfo = new ProcessStartInfo(resultPath) { UseShellExecute = true }; //resultPath contains the file path of the HTML report result
p.Start();
