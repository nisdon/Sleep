using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace sleep
{
	public partial class Form1 : Form
	{
		/// <summary>
		/// ウェイト時間（６０秒）
		/// </summary>
		private const int TIMER_INTERVAL = 60000;
		private HotKey hotKey;
		private Point MousePoint = Point.Empty;
		private int OffTimer;
		/// <summary>
		/// 抑止プロセスのリスト
		/// </summary>
		static readonly string[] ProcessList = new string[]
        {
			"TVTest",
			"epgdatacap_bon",
			"TMPGEncVMW5",
			"TMPGEncVMW5Batch",
			"TMPGEncMPEGSmartRenderer4",
			"TMPGEncMSR4Batch",
			"HandBrake"
    	};
		/// <summary>
		/// フォームコンストラクタ
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public Form1()
		{
			InitializeComponent();
			hotKey = new HotKey(MOD_KEY.CONTROL | MOD_KEY.SHIFT, Keys.F12);
            hotKey.HotKeyPush += new EventHandler(hotKey_HotKeyPush);
			notifyIcon1.ContextMenuStrip = contextMenuStrip1;
			ToolStripMenuItem_timer.Checked = false;
			timer1.Interval = TIMER_INTERVAL;
			MousePoint = Cursor.Position;
			OffTimer = 0;
		}
		/// <summary>
		/// タイマーイベント
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void timer1_Tick(object sender, EventArgs e)
		{
			// 抑止プロセスのチェック
			foreach (string proc in ProcessList)
			{
				if (System.Diagnostics.Process.GetProcessesByName(proc).Length > 0)
				{
					// プロセス有り
					OffTimer = 0;
					return;
				}
			}

			//マウスポインタの位置を取得
			if (MousePoint != System.Windows.Forms.Cursor.Position)
			{
				// 操作中
				MousePoint = System.Windows.Forms.Cursor.Position;
				OffTimer = 0;
				return;
			}

			//タイマーカウント開始（１０分）
			OffTimer++;
			if (10 <= OffTimer)
			{
				// タイマーをリセットしてスタンバイ
				timer1.Interval = TIMER_INTERVAL;
				OffTimer = 0;
				StandBy();
			}
		}
		/// <summary>
		/// ホットキー入力通知
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void hotKey_HotKeyPush(object sender, EventArgs e)
		{
			// タイマーをリセットしてスタンバイ
			timer1.Interval = TIMER_INTERVAL;
			OffTimer = 0;
			StandBy();
		}
		/// <summary>
		/// 終了メニュー選択
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void 終了ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			hotKey.Dispose();
			Application.Exit();
		}
		/// <summary>
		/// タイマー始動メニュー
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ToolStripMenuItem_timer_Click(object sender, EventArgs e)
		{
			if (ToolStripMenuItem_timer.Checked)
			{
				// タイマー中止
				ToolStripMenuItem_timer.Checked = false;
				timer1.Stop();
			}
			else
			{
				// タイマー開始
				OffTimer = 0;
				ToolStripMenuItem_timer.Checked = true;
				timer1.Interval = TIMER_INTERVAL;
				timer1.Start();
			}
		}
		/// <summary>
		/// スリープメソッド
		/// </summary>
#if true
		private void StandBy()
		{
            Console.Beep();    
            System.Threading.Thread.Sleep(1000);
            Application.SetSuspendState(PowerState.Suspend, false, false);
		}
#else
		[System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
		private static extern IntPtr GetCurrentProcess();

		[System.Runtime.InteropServices.DllImport("advapi32.dll", SetLastError = true)]
		private static extern bool OpenProcessToken(IntPtr ProcessHandle,
													uint DesiredAccess,
													out IntPtr TokenHandle);

		[System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool CloseHandle(IntPtr hObject);

		[System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
		private static extern int SetSystemPowerState(bool fSuspend, bool fForce);

		[System.Runtime.InteropServices.DllImport("advapi32.dll", SetLastError = true,
												  CharSet = System.Runtime.InteropServices.CharSet.Auto)]
		private static extern bool LookupPrivilegeValue(string lpSystemName,
														string lpName,
														out long lpLuid);

		[System.Runtime.InteropServices.StructLayout(
													 System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 1)]
		private struct TOKEN_PRIVILEGES
		{
			public int PrivilegeCount;
			public long Luid;
			public int Attributes;
		}

		[System.Runtime.InteropServices.DllImport("advapi32.dll", SetLastError = true)]
		private static extern bool AdjustTokenPrivileges(IntPtr TokenHandle,
														 bool DisableAllPrivileges,
														 ref TOKEN_PRIVILEGES NewState,
														 int BufferLength,
														 IntPtr PreviousState,
														 IntPtr ReturnLength);

		//シャットダウンするためのセキュリティ特権を有効にする
		private void StandBy()
		{
			const uint TOKEN_ADJUST_PRIVILEGES = 0x20;
			const uint TOKEN_QUERY = 0x8;
			const int SE_PRIVILEGE_ENABLED = 0x2;
			const string SE_SHUTDOWN_NAME = "SeShutdownPrivilege";

			if (Environment.OSVersion.Platform != PlatformID.Win32NT)
				return;

			Console.Beep();
            System.Threading.Thread.Sleep(1000);

            //トークンを取得する
            IntPtr tokenHandle;
			if(OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out tokenHandle)){
				//LUIDを取得する
				TOKEN_PRIVILEGES tp = new TOKEN_PRIVILEGES();
				tp.Attributes = SE_PRIVILEGE_ENABLED;
				tp.PrivilegeCount = 1;
				LookupPrivilegeValue(null, SE_SHUTDOWN_NAME, out tp.Luid);
				//特権を有効にする
				AdjustTokenPrivileges(tokenHandle, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero);
				//閉じる
				CloseHandle(tokenHandle);
			}
			SetSystemPowerState(true, false);
		}
#endif
	}
}
