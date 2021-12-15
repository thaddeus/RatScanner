﻿using Microsoft.Win32;
using SingleInstanceCore;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using System.Windows;

namespace RatScanner
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application, ISingleInstance
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			SetupExceptionHandling();

			var winLogonKey = Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\WOW6432Node\\Microsoft\\EdgeUpdate\\Clients\\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}", "pv", null);
			if (winLogonKey == null)
			{
				using var client = new WebClient();
				client.DownloadFile("https://go.microsoft.com/fwlink/p/?LinkId=2124703", "MicrosoftEdgeWebview2Setup.exe");

				ProcessStartInfo startInfo = new ProcessStartInfo();
				startInfo.CreateNoWindow = false;
				startInfo.UseShellExecute = false;
				startInfo.FileName = "MicrosoftEdgeWebview2Setup.exe";
				startInfo.WindowStyle = ProcessWindowStyle.Hidden;
				startInfo.Arguments = "/silent /install";

				try
				{
					// Start the process with the info we specified.
					// Call WaitForExit and then the using statement will close.
					Process exeProcess = Process.Start(startInfo);
					string output = exeProcess.StandardOutput.ReadToEnd();
					Logger.LogInfo(output);
					exeProcess.WaitForExit();
				}
				catch (Exception ex)
				{
					Logger.LogError("Could not install Webview2", ex);
				}

				try { System.IO.File.Delete("MicrosoftEdgeWebview2Setup.exe"); }
				catch { }
			}

			var guid = "{a057bb64-c126-4ef4-a4ed-3037c2e7bc89}";
			var isFirstInstance = this.InitializeAsFirstInstance(guid);
			if (!isFirstInstance)
			{
				SingleInstance.Cleanup();
				Current.Shutdown(2);
			}
		}

		public void OnInstanceInvoked(string[] args)
		{
			Application.Current.Dispatcher.Invoke(() =>
			{
				MainWindow.Activate();
				MainWindow.WindowState = WindowState.Normal;

				// Invert the topmost state twice to bring the window on
				// top if it wasnt previously or do nothing
				MainWindow.Topmost = !MainWindow.Topmost;
				MainWindow.Topmost = !MainWindow.Topmost;
			});
		}

		private void SetupExceptionHandling()
		{
			AppDomain.CurrentDomain.UnhandledException += (s, e) =>
			{
				LogUnhandledException((Exception)e.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException");
			};

			DispatcherUnhandledException += (s, e) =>
			{
				LogUnhandledException(e.Exception, "Application.Current.DispatcherUnhandledException");
				e.Handled = true;
			};

			TaskScheduler.UnobservedTaskException += (s, e) =>
			{
				LogUnhandledException(e.Exception, "TaskScheduler.UnobservedTaskException");
				e.SetObserved();
			};
		}

		private void LogUnhandledException(Exception exception, string source)
		{
			var message = $"Unhandled exception ({source})";
			try
			{
				var assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName();
				message = $"Unhandled exception in {assemblyName.Name} {RatConfig.Version}";
			}
			catch (Exception ex)
			{
				Logger.LogError("Exception in LogUnhandledException", ex);
			}
			finally
			{
				Logger.LogError(message, exception);
			}
		}
	}
}
