using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace sleep
{
	static class Program
	{
		/// <summary>
		/// アプリケーションのメイン エントリ ポイントです。
		/// </summary>
		[STAThread]
		static void Main()
		{
			//フォーム(Form1)のインスタンスを作成
			Form1 f1 = new Form1();
			//メッセージループを開始する
			Application.Run();
		}
	}
}
