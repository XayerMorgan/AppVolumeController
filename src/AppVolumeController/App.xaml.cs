using System.Windows;
using AppVolumeController.Models;
using AppVolumeController.Services;
namespace AppVolumeController;
public partial class App : Application
{
 private Mutex? runningMutex;
 protected override void OnStartup(StartupEventArgs e)
 {
  base.OnStartup(e);
  if(e.Args.Contains("--logic-test")) { Environment.Exit(UiTest.RunLogic()); return; }
  if(e.Args.Contains("--self-test")) { Environment.Exit(SelfTest.Run()); return; }
  if(e.Args.Contains("--ui-test"))
  {
   ShutdownMode=ShutdownMode.OnExplicitShutdown;
   ThemeService.Initialize(new());
   Dispatcher.BeginInvoke(new Action(()=> { int result=UiTest.Run(); Shutdown(result); }));
   return;
  }
  // Setup and Uninstall check this mutex before replacing a running copy.
  runningMutex = new Mutex(false, "AppVolumeController.Running");
  var preferences=Preferences.Load();
  if(e.Args.Contains("--smoke-test")) preferences.RememberVolumes=false;
  ThemeService.Initialize(preferences.Appearance);
  DispatcherUnhandledException += (_, args) => { MessageBox.Show(args.Exception.Message, "App Volume Controller"); args.Handled = true; };
  var window = new MainWindow(preferences);window.Show();
  if(e.Args.Contains("--smoke-test"))
  {
   var timer=new System.Windows.Threading.DispatcherTimer {Interval=TimeSpan.FromSeconds(5)};
   timer.Tick+=(_,_)=> {
    timer.Stop();
    var bitmap=new System.Windows.Media.Imaging.RenderTargetBitmap((int)window.ActualWidth,(int)window.ActualHeight,96,96,System.Windows.Media.PixelFormats.Pbgra32);
    bitmap.Render(window);
    var encoder=new System.Windows.Media.Imaging.PngBitmapEncoder();encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
    using(var stream=System.IO.File.Create(System.IO.Path.Combine(AppContext.BaseDirectory,"ui-smoke.png")))encoder.Save(stream);
    window.Close();
   };timer.Start();
  }
 }
 protected override void OnExit(ExitEventArgs e) { ThemeService.Shutdown(); runningMutex?.Dispose(); base.OnExit(e); }
}
