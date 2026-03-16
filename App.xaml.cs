using System.Windows;
namespace GleemLet;
public partial class App : Application 
{ 
    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += (s, ex) =>
        {
            System.IO.File.WriteAllText("crash.log", ex.Exception.ToString());
            System.Windows.MessageBox.Show(ex.Exception.Message, "Hata");
            ex.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
        {
            System.IO.File.WriteAllText("crash.log", ex.ExceptionObject.ToString());
        };

        base.OnStartup(e);
    }
}
